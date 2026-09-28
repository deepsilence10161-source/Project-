#!/usr/bin/env bash
# Neon Dash emulator E2E. Runs inside reactivecircus/android-emulator-runner
# as ONE real shell (never an inline multi-line `script:`).
#
# The game reports its own events to logcat as "ND_EVT <name> k=v ..."
# (Scripts/TestProbe.cs, debug builds only). This script drives the game with
# real touch input and waits for the matching event after every action.
# It never decides pass/fail itself: ci/e2e/judge.py does that from the
# collected evidence, so nothing here can turn a failure green.
set -uo pipefail
APP=com.neondash.game
ACT=com.godot.game.GodotAppLauncher
OUT=out; mkdir -p "$OUT"
APK=$(ls build/*.apk 2>/dev/null | head -1)
: > "$OUT/recoveries.txt"
: > "$OUT/steps.txt"
step() { echo "[$(date +%T)] $*" | tee -a "$OUT/steps.txt"; }

grep -h '^Pkg.Revision' "${ANDROID_HOME:-$ANDROID_SDK_ROOT}/emulator/source.properties" > "$OUT/emulator-version.txt" 2>/dev/null || echo unknown > "$OUT/emulator-version.txt"

# --- boot fully and settle (a half-booted launcher ANRs over the app) --------
for i in $(seq 1 60); do [ "$(adb shell getprop sys.boot_completed | tr -d '\r')" = "1" ] && break; sleep 2; done
sleep 15
adb shell settings put global hide_error_dialogs 1 || true
adb shell settings put secure immersive_mode_confirmations confirmed || true

step "install $APK"
adb install -r "$APK" > "$OUT/install.log" 2>&1
echo $? > "$OUT/install-rc.txt"
cat "$OUT/install.log"
adb logcat -c || true

# --- helpers -----------------------------------------------------------------
evts() { adb logcat -d 2>/dev/null | grep -o 'ND_EVT.*' | tr -d '\r'; }
count() { evts | grep -cE "$1"; }
# wait_for <regex> <seconds> [min-count]: 0 when seen, 1 on timeout
wait_for() {
  local re="$1" secs="$2" need="${3:-1}" t=0
  while [ "$t" -lt "$secs" ]; do
    [ "$(count "$re")" -ge "$need" ] && { step "  saw '$re' (x$need) after ${t}s"; return 0; }
    sleep 1; t=$((t+1))
  done
  step "  TIMEOUT waiting ${secs}s for '$re'"; return 1
}
dismiss_anr() {
  if adb shell dumpsys window 2>/dev/null | grep -qE "Application Not Responding"; then
    adb shell uiautomator dump /sdcard/ui.xml > /dev/null 2>&1
    XY=$(adb shell cat /sdcard/ui.xml 2>/dev/null | python3 ci/e2e/find_wait.py)
    if [ -n "$XY" ]; then adb shell input tap $XY; echo "anr-dismissed at $XY ($(date +%T))" >> "$OUT/recoveries.txt"; sleep 2
    else echo "anr-seen-no-button ($(date +%T))" >> "$OUT/recoveries.txt"; fi
  fi
}
ensure_front() {
  for k in 1 2 3; do
    dismiss_anr
    TOP=$(adb shell dumpsys activity activities 2>/dev/null | grep -m1 -E "topResumedActivity|ResumedActivity" | tr -d '\r')
    echo "$TOP" | grep -q "$APP" && return 0
    echo "relaunch#$k top=[$TOP] ($(date +%T))" >> "$OUT/recoveries.txt"
    adb shell am start -n "$APP/$ACT" > /dev/null 2>&1
    sleep 8
  done
  return 1
}
shot() { adb exec-out screencap -p > "$OUT/$1.png" 2>/dev/null; step "  screenshot $1 ($(stat -c%s "$OUT/$1.png" 2>/dev/null) bytes)"; }

# --- launch ------------------------------------------------------------------
step "launch"
adb shell am start -W -n "$APP/$ACT" > "$OUT/start.log" 2>&1 || true
cat "$OUT/start.log"
wait_for '^ND_EVT ready' 90 || true
sleep 3
ensure_front
read W H < <(adb shell wm size | grep -oE '[0-9]+x[0-9]+' | tail -1 | tr 'x' ' ')
echo "${W}x${H}" > "$OUT/screen.txt"; step "screen ${W}x${H}"
shot 01-title

adb shell screenrecord --time-limit 170 --bit-rate 2000000 --size 432x720 /sdcard/e2e.mp4 > "$OUT/screenrecord.log" 2>&1 &
REC=$!
sleep 1

CX=$((W/2)); Y=$((H*6/10))
step "tap to start"
adb shell input tap $CX $Y
wait_for 'state from=Menu to=Playing' 15 || true
sleep 1; shot 02-playing

step "swipe left"
adb shell input swipe $((W*6/10)) $Y $((W*2/10)) $Y 150
wait_for 'lane dir=Left' 6 || true
sleep 0.5; shot 03-after-left

step "swipe right x2"
adb shell input swipe $((W*4/10)) $Y $((W*8/10)) $Y 150; sleep 0.4
adb shell input swipe $((W*4/10)) $Y $((W*8/10)) $Y 150
wait_for 'lane dir=Right' 6 || true
sleep 0.5; shot 04-after-right

step "swipe up (jump)"
adb shell input swipe $CX $((H*7/10)) $CX $((H*4/10)) 120
wait_for '^ND_EVT jump' 6 || true
shot 05-jump

step "no input: wait for the player to hit an obstacle"
wait_for '^ND_EVT crash' 150 || true
sleep 1.5; ensure_front; shot 06-game-over

step "tap to retry"
PLAYS=$(count 'to=Playing')
adb shell input tap $CX $Y
wait_for 'state from=GameOver to=Playing' 15 || true
sleep 4; shot 07-retry-running

adb shell pkill -2 screenrecord > /dev/null 2>&1 || true
sleep 3; kill $REC 2>/dev/null; wait $REC 2>/dev/null
adb pull /sdcard/e2e.mp4 "$OUT/e2e.mp4" > /dev/null 2>&1 || true
step "video $(stat -c%s "$OUT/e2e.mp4" 2>/dev/null || echo none) bytes"

PID=$(adb shell pidof "$APP" 2>/dev/null | tr -d '\r\n')
[ -n "$PID" ] && echo alive > "$OUT/app-state.txt" || echo dead > "$OUT/app-state.txt"
adb logcat -d > "$OUT/logcat.txt" 2>/dev/null || true
grep -o 'ND_EVT.*' "$OUT/logcat.txt" | tr -d '\r' > "$OUT/events.txt" || true
step "events captured: $(wc -l < "$OUT/events.txt")"
exit 0
