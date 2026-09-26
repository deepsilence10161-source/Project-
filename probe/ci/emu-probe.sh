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
adb shell am start -W -n "$APP/com.godot.game.GodotAppLauncher" > "$OUT/start.log" 2>&1 || true
cat "$OUT/start.log"

# Video of the whole interaction (device-side recorder).
adb shell screenrecord --time-limit 30 --size 540x1200 /sdcard/probe.mp4 > "$OUT/screenrecord.log" 2>&1 &
REC=$!

shot() { adb exec-out screencap -p > "$OUT/shot-$1.png" 2>/dev/null; echo "shot $1: $(stat -c%s "$OUT/shot-$1.png" 2>/dev/null) bytes"; }
sleep 8;  shot 08s
sleep 6;  shot 14s
# Real input: tap then swipe (screen is 1080x2400 on pixel_6).
adb shell input tap 540 1500; sleep 2
adb shell input swipe 300 1800 800 1800 300; sleep 3
shot 19s-after-touch
sleep 5; shot 24s
wait $REC 2>/dev/null
adb pull /sdcard/probe.mp4 "$OUT/probe.mp4" > /dev/null 2>&1 || true
echo "video: $(stat -c%s "$OUT/probe.mp4" 2>/dev/null || echo none) bytes"

PID=$(adb shell pidof "$APP" 2>/dev/null | tr -d '\r\n')
[ -n "$PID" ] && echo alive > "$OUT/app-state.txt" || echo dead > "$OUT/app-state.txt"
adb logcat -d > "$OUT/logcat.txt" 2>/dev/null || true
grep -E 'PROBE_|OpenGL API|Vulkan API|godot' "$OUT/logcat.txt" | grep -vE 'shader_gles3' | head -40 || true
exit 0
