#!/usr/bin/env bash
# Runs inside reactivecircus/android-emulator-runner (one real shell).
# Usage: emu-probe.sh <variant> <gpu-mode>
set -uo pipefail
VARIANT="$1"; GPU="$2"
APP=com.probe.render
OUT=out; mkdir -p "$OUT"
APK=$(ls apk/*.apk 2>/dev/null | head -1)
echo "variant=$VARIANT gpu=$GPU apk=$APK"

grep -h '^Pkg.Revision' "${ANDROID_HOME:-$ANDROID_SDK_ROOT}/emulator/source.properties" > "$OUT/emulator-version.txt" 2>/dev/null || echo unknown > "$OUT/emulator-version.txt"

# Wait until Android has really finished booting, then let it settle: a
# half-booted system is what makes Pixel Launcher ANR over the app.
for i in $(seq 1 60); do [ "$(adb shell getprop sys.boot_completed | tr -d '\r')" = "1" ] && break; sleep 2; done
sleep 15
# No crash/ANR dialogs from OTHER apps covering the screen. Our own app's
# death is still detected independently (pidof + logcat).
adb shell settings put global hide_error_dialogs 1 || true
cat "$OUT/emulator-version.txt"
{
  for p in ro.build.version.sdk ro.product.cpu.abilist ro.hardware.egl ro.hardware.vulkan \
           ro.opengles.version ro.kernel.qemu.gles ro.boot.qemu.gltransport ro.boot.hardware.egl \
           ro.boot.hardware.vulkan debug.hwui.renderer; do
    echo "$p=$(adb shell getprop $p | tr -d '\r')"
  done
  echo "--- SurfaceFlinger GLES ---"
  adb shell dumpsys SurfaceFlinger 2>/dev/null | grep -iE 'GLES|vulkan' | head -5
} > "$OUT/props.txt"
cat "$OUT/props.txt"

# Stop Android's one-time "Viewing full screen" cling from covering the game.
adb shell settings put secure immersive_mode_confirmations confirmed || true
adb shell settings get secure immersive_mode_confirmations > "$OUT/immersive-setting.txt" || true

adb install -r "$APK" > "$OUT/install.log" 2>&1
echo $? > "$OUT/install-rc.txt"
cat "$OUT/install.log"

adb logcat -c || true
: > "$OUT/recoveries.txt"

# --- recovery helpers (every use is logged in recoveries.txt, never hidden) ---
dismiss_anr() {
  if adb shell dumpsys window 2>/dev/null | grep -qE "Application Not Responding"; then
    adb shell uiautomator dump /sdcard/ui.xml > /dev/null 2>&1
    XY=$(adb shell cat /sdcard/ui.xml 2>/dev/null | python3 probe/ci/find_wait.py)
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
    adb shell am start -n "$APP/com.godot.game.GodotAppLauncher" > /dev/null 2>&1
    sleep 8
  done
  return 1
}

adb shell am start -W -n "$APP/com.godot.game.GodotAppLauncher" > "$OUT/start.log" 2>&1 || true
cat "$OUT/start.log"
sleep 4
ensure_front
read W H < <(adb shell wm size | grep -oE '[0-9]+x[0-9]+' | tail -1 | tr 'x' ' ')
echo "screen ${W}x${H}" | tee "$OUT/screen.txt"

# Video of the whole interaction (device-side recorder).
adb shell screenrecord --time-limit 30 --size 540x1200 /sdcard/probe.mp4 > "$OUT/screenrecord.log" 2>&1 &
REC=$!

shot() { adb exec-out screencap -p > "$OUT/shot-$1.png" 2>/dev/null; echo "shot $1: $(stat -c%s "$OUT/shot-$1.png" 2>/dev/null) bytes"; }
sleep 4;  ensure_front; shot 08s
sleep 6;  ensure_front; shot 14s
# Real input: tap then swipe, scaled to the actual screen size.
adb shell input tap $((W/2)) $((H*6/10)); sleep 2
adb shell input swipe $((W*3/10)) $((H*3/4)) $((W*7/10)) $((H*3/4)) 300; sleep 3
shot 19s-after-touch
sleep 5; ensure_front; shot 24s
wait $REC 2>/dev/null
adb pull /sdcard/probe.mp4 "$OUT/probe.mp4" > /dev/null 2>&1 || true
echo "video: $(stat -c%s "$OUT/probe.mp4" 2>/dev/null || echo none) bytes"

PID=$(adb shell pidof "$APP" 2>/dev/null | tr -d '\r\n')
[ -n "$PID" ] && echo alive > "$OUT/app-state.txt" || echo dead > "$OUT/app-state.txt"
adb logcat -d > "$OUT/logcat.txt" 2>/dev/null || true
grep -E 'PROBE_|OpenGL API|Vulkan API|godot' "$OUT/logcat.txt" | grep -vE 'shader_gles3' | head -40 || true
exit 0
