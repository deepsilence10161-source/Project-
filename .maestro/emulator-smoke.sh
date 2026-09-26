#!/usr/bin/env bash
# Neon Dash - Android emulator smoke test.
#
# Runs INSIDE the reactivecircus/android-emulator-runner action, on a real
# booted emulator with adb already pointing at emulator-5554.
#
# This lives in its own file on purpose: android-emulator-runner feeds the
# `script:` block to the shell one line at a time (visible in the CI log as a
# separate `/usr/bin/sh -c <line>` per line), so shell variables set on one
# line are EMPTY on the next. The old inline script therefore ran
# `adb install -r ""` and died with "filename doesn't end .apk or .apex".
# A single script file gives us one real shell with persistent state.
#
# Exit codes:
#   0  script ran to completion; see .app-state / .crash-state for the verdict
#   1  hard infrastructure failure (no APK, install failed, no device)

set -uo pipefail

APP=com.neondash.game
APK=build/NeonDash-debug.apk

mkdir -p screenshots
: > .maestro-rc
: > .install-rc
echo unknown > .app-state
echo unknown > .crash-state

banner() { echo; echo "=== $* ==="; }

# ---------------------------------------------------------------- install ----
banner "installing APK"
if [ ! -f "$APK" ]; then
  echo "::error::APK not found at $APK"
  ls -la build/ 2>/dev/null || true
  exit 1
fi
echo "APK: $APK ($(stat -c%s "$APK") bytes)"

adb install -r "$APK" 2>&1 | tee install.log
echo "${PIPESTATUS[0]}" > .install-rc
INSTALL_RC=$(cat .install-rc)
echo "install rc=$INSTALL_RC"
if [ "$INSTALL_RC" != "0" ]; then
  echo "::error::adb install failed (rc=$INSTALL_RC)"
  cat install.log
  exit 1
fi

# ---------------------------------------------------------------- maestro ----
banner "Maestro"
if ! command -v maestro > /dev/null 2>&1; then
  echo "installing Maestro..."
  curl -fsSL "https://get.maestro.mobile.dev" | bash
  export PATH="$HOME/.maestro/bin:$PATH"
fi
command -v maestro || { echo "::error::maestro not on PATH"; exit 1; }
maestro --version

# Keep going even if Maestro reports failures: the report and screenshots are
# the deliverable, and the verdict is recorded in .maestro-rc.
set +e
maestro test .maestro/flows/smoke.yaml \
  --format junit --output maestro-report.xml
MAESTRO_RC=$?
set -e 2>/dev/null || true
echo "$MAESTRO_RC" > .maestro-rc
echo "maestro exit code: $MAESTRO_RC"

# ------------------------------------------------------------- artefacts ----
banner "collecting artefacts"
find .maestro -name '*.png' -exec cp {} screenshots/ \; 2>/dev/null || true
adb exec-out screencap -p > screenshots/09-final-device-screen.png 2>/dev/null || true
adb logcat -d > logcat.txt 2>/dev/null || true
ls -la screenshots/ | head -20

# ------------------------------------------------------------- liveness ------
banner "liveness: is the game process alive right now?"
PID=$(adb shell pidof "$APP" 2>/dev/null | tr -d '\r\n')
if [ -n "$PID" ]; then
  echo "ALIVE: $APP pid=$PID"
  echo alive > .app-state
else
  echo "DEAD: $APP is not running"
  echo dead > .app-state
fi

banner "crash scan in logcat"
# Godot logs unhandled C# exceptions as "ERROR: System.<ExceptionType>". Those
# do NOT raise a Java FATAL EXCEPTION and do not always kill the process, so a
# plain crash grep misses them. A real NullReferenceException in
# ObstacleView.Configure shipped this way and only the logcat revealed it.
if grep -qE 'FATAL EXCEPTION' logcat.txt 2>/dev/null; then
  echo "CRASH SIGNATURE FOUND:"
  grep -nE 'FATAL EXCEPTION' logcat.txt | head -20
  echo crashed > .crash-state
elif grep -qE "Process: ${APP}, PID" logcat.txt 2>/dev/null; then
  echo "APP PROCESS DEATH FOUND:"
  grep -nE "Process: ${APP}, PID" logcat.txt | head -20
  echo crashed > .crash-state
elif grep -qE 'ERROR: System\.[A-Za-z]+Exception' logcat.txt 2>/dev/null; then
  echo "UNHANDLED C# EXCEPTION FOUND:"
  grep -nE 'ERROR: System\.[A-Za-z]+Exception' logcat.txt | head -10
  echo "--- first stack frame ---"
  grep -A3 -m1 'ERROR: System\.[A-Za-z]+Exception' logcat.txt | head -5
  echo crashed > .crash-state
else
  echo "no fatal / app-death / C# exception signatures in logcat"
  echo clean > .crash-state
fi

# ---- renderer / GPU environment noise -------------------------------------
# Separated on purpose, and NEVER treated as a crash. The GitHub emulator runs
# swiftshader (software GL), whose GL_MAX_FRAGMENT_UNIFORM_VECTORS is far
# smaller than a real phone GPU's, so Godot's GLES3 shader programs fail to
# link there:
#     "Fragment shader active uniforms exceed GL_MAX_FRAGMENT_UNIFORM_VECTORS"
# That is a limitation of the emulator's software renderer, not a defect in
# the game, and it must not mask a real failure - so it is counted and
# published instead of being silently ignored.
RENDERER_ERRORS=$(grep -cE 'E godot.*ERROR: (Scene|Canvas)ShaderGLES3|GL_MAX_FRAGMENT_UNIFORM_VECTORS|Program linking failed' logcat.txt 2>/dev/null || true)
echo "renderer/gpu environment errors (software GL): ${RENDERER_ERRORS:-0}"
echo "${RENDERER_ERRORS:-0}" > .renderer-errors
if [ "${RENDERER_ERRORS:-0}" -gt 0 ]; then
  echo "NOTE: rendering on this emulator is degraded by swiftshader's shader"
  echo "      limits. The APK cannot be visually verified here; only liveness,"
  echo "      installability and the absence of exceptions are proven."
fi

banner "summary"
echo "install rc : $(cat .install-rc)"
echo "maestro rc : $(cat .maestro-rc)"
echo "app state  : $(cat .app-state)"
echo "crash scan : $(cat .crash-state)"

# The report must still be published even on a bad verdict, so the caller
# decides pass/fail from these state files.
exit 0
