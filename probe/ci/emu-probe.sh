#!/usr/bin/env bash
# Runs inside reactivecircus/android-emulator-runner (one real shell).
# Usage: emu-probe.sh <variant> <gpu-mode>
set -uo pipefail
VARIANT="$1"; GPU="$2"
APP=com.probe.render
OUT=out; mkdir -p "$OUT"
APK=$(ls apk/*.apk 2>/dev/null | head -1)
echo "variant=$VARIANT gpu=$GPU apk=$APK"

"$ANDROID_HOME/emulator/emulator" -version 2>/dev/null | head -1 > "$OUT/emulator-version.txt" || true
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

for t in 8 16 26; do
  sleep $(( t - ${prev:-0} )); prev=$t
  adb exec-out screencap -p > "$OUT/shot-${t}s.png" 2>/dev/null || true
  echo "screenshot at ${t}s: $(stat -c%s "$OUT/shot-${t}s.png" 2>/dev/null) bytes"
done

PID=$(adb shell pidof "$APP" 2>/dev/null | tr -d '\r\n')
[ -n "$PID" ] && echo alive > "$OUT/app-state.txt" || echo dead > "$OUT/app-state.txt"
adb logcat -d > "$OUT/logcat.txt" 2>/dev/null || true
grep -E 'PROBE_|OpenGL API|Vulkan API|godot' "$OUT/logcat.txt" | grep -vE 'shader_gles3' | head -40 || true
exit 0
