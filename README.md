# Neon Dash 🏃‍♂️⚡

A neon endless runner for **Android**, built with **Godot 4.7** and **C#**.

Three lanes. Swipe to dodge, swipe up to jump. Speed ramps up the longer you
survive. Beat your best score.

---

## Why this project exists

This repo is also a working example of **how to test a native Android game
properly**, because that question is genuinely hard to answer well.

The short version of what was verified along the way:

| Question | Answer |
|---|---|
| Can Playwright test a native APK? | **No.** Playwright drives browser engines (Chromium/Firefox/WebKit). It cannot see or touch a native Android UI tree. It *can* drive Chrome on Android and WebViews — not an installed APK. |
| Can GitHub Pages "run" an APK? | **No.** Pages serves static files only. An APK there is a download, never an executable. |
| Can Maestro / Appium automate in-game UI? | **No.** A game canvas is one opaque `SurfaceView` to the OS. Maestro and Appium see the screen, not the game. |
| Then what *does* work? | **Pure-logic tests + emulator smoke tests + real-device clouds.** See below. |

---

## Testing architecture

```
┌─ Layer 1 ── Pure logic (no engine, no device) ──────────────────┐
│  Scripts/Core/*.cs          -> GameStateMachine, ScoreManager, │
│                                SpeedController, LaneController, │
│                                JumpController, CollisionDetector│
│                                SpawnDirector, DifficultyCurve  │
│  tests/NeonDash.Core.Tests  -> xUnit, 92 tests                 │
│  Runs: locally + CI. Needs only .NET 9.                       │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌─ Layer 2 ── APK build ─────────────────────────────────────────┐
│  godot --headless --export-debug "Android"                    │
│  Verified with aapt / apksigner / unzip.                      │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌─ Layer 3 ── Emulator E2E (GitHub Actions, KVM) ────────────────┐
│  API 34 AOSP x86_64 · Nexus 4 · -gpu lavapipe (proven setup)  │
│  PLAYS the game via adb: start, swipe, jump, crash, retry.    │
│  The debug build logs its own events (ND_EVT ...) and         │
│  ci/e2e/judge.py asserts 16 checks + screenshots + video.     │
│  Report: GitHub Pages /e2e/                                   │
└────────────────────────────────────────────────────────────────┘
                              ↓
┌─ Layer 4 ── Real device (optional) ────────────────────────────┐
│  Install the release APK on a phone and play it.              │
└────────────────────────────────────────────────────────────────┘
```

### How the emulator test sees inside the game

Maestro and Appium cannot see inside a Godot game: it renders into a single
native surface. So the game reports on itself instead. `Scripts/TestProbe.cs`
prints one line per event in **debug builds only** (`ND_EVT state from=Menu
to=Playing`, `ND_EVT lane dir=Left to=0`, `ND_EVT crash score=... newbest=...`,
plus a 2-second heartbeat with score and FPS). `ci/e2e/run.sh` sends real
touch input with `adb` and waits for the matching event; `ci/e2e/judge.py`
turns the evidence into a pass/fail verdict. Release builds print nothing.

---

## Local development

```bash
# 1. Toolchain
#    Godot 4.7.2 (.NET/mono build), .NET 9 SDK, JDK 17, Android SDK

# 2. Run the logic tests (fast, no engine needed)
dotnet test tests/NeonDash.Core.Tests/NeonDash.Core.Tests.csproj

# 3. Import + run the game headlessly
godot --headless --path . --import
godot --headless --path . --quit-after 600

# 4. Build the APK
mkdir -p build
godot --headless --path . --export-debug "Android" build/NeonDash-debug.apk
```

## Controls

| Input | Action |
|---|---|
| Swipe left / right | Change lane |
| Swipe up | Jump |
| Tap on title / game over | Start / retry |

## CI workflows

| Workflow | What it does |
|---|---|
| `unit-tests.yml` | Runs the 92 pure-logic tests on every push |
| `build-apk.yml` | Builds + signs + verifies the APK |
| `emulator-e2e.yml` | Boots an emulator, plays the game with adb touch input, asserts 16 checks from game events + screenshots, publishes report + video to Pages `/e2e/` |

## Notes

- `Scripts/Core/` is intentionally free of any `Godot` types. That is what
  makes the whole suite runnable without an engine, a display, or a device.
- The xUnit project compiles those same files directly out of `Scripts/Core/`,
  so there is exactly one source of truth for the game rules.
- Godot 4.7's C# Android export requires **.NET 9** (the export templates are
  built against it).
