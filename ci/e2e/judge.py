#!/usr/bin/env python3
"""Decide pass/fail for the Neon Dash emulator E2E from collected evidence.

Usage: judge.py <out_dir>   -> writes <out_dir>/result.json, exit 0 always
Every check records its evidence so the verdict can be re-checked by eye.
"""
import json, os, re, sys

out = sys.argv[1]

def read(name, default=""):
    p = os.path.join(out, name)
    return open(p, errors="replace").read() if os.path.exists(p) else default

events = [l.strip() for l in read("events.txt").splitlines() if l.strip()]
log = read("logcat.txt")

def find(rx):
    return [e for e in events if re.search(rx, e)]

def kv(line):
    return dict(re.findall(r"(\w+)=([^\s]+)", line))

ticks = [kv(e) for e in find(r"^ND_EVT tick")]
play_scores = [int(t["score"]) for t in ticks if t.get("state") == "Playing" and t.get("score", "").isdigit()]
fps = [int(t["fps"]) for t in ticks if t.get("state") == "Playing" and t.get("fps", "").isdigit()]
crashes = [kv(e) for e in find(r"^ND_EVT crash")]

crash_sig = re.findall(r"FATAL EXCEPTION|Process: com\.neondash\.game, PID|ERROR: System\.[A-Za-z]+Exception|Fatal signal \d+", log)

# ---- screenshots ------------------------------------------------------------
shots = {}
try:
    from PIL import Image, ImageChops, ImageStat
    def load(n):
        p = os.path.join(out, n + ".png")
        if not os.path.exists(p) or os.path.getsize(p) < 1000:
            return None
        im = Image.open(p).convert("RGB")
        im.thumbnail((270, 450))
        return im
    for n in ["01-title", "02-playing", "03-after-left", "04-after-right", "05-jump", "06-game-over", "07-retry-running"]:
        im = load(n)
        if im is None:
            shots[n] = {"present": False}
            continue
        px = list(im.getdata())
        bright = sum(1 for r, g, b in px if max(r, g, b) > 90) / len(px) * 100
        shots[n] = {"present": True, "bright_pct": round(bright, 2), "_im": im}
    def diff(a, b):
        A, B = shots.get(a, {}).get("_im"), shots.get(b, {}).get("_im")
        if A is None or B is None or A.size != B.size:
            return None
        return round(sum(ImageStat.Stat(ImageChops.difference(A, B)).mean) / 3, 2)
    d_title_play = diff("01-title", "02-playing")
    d_play_over = diff("05-jump", "06-game-over")
except Exception as e:  # noqa
    shots["error"] = str(e)
    d_title_play = d_play_over = None
for v in shots.values():
    if isinstance(v, dict):
        v.pop("_im", None)

# ---- checks -----------------------------------------------------------------
C = []
def check(key, label_hi, ok, evidence):
    C.append({"key": key, "label": label_hi, "ok": bool(ok), "evidence": evidence})

install_rc = read("install-rc.txt", "?").strip()
check("install", "APK install हुआ", install_rc == "0", f"adb install rc={install_rc}")
ready = find(r"^ND_EVT ready")
check("ready", "Game खुला (ready event)", ready, ready[:1] or "ready event नहीं मिला")
title = shots.get("01-title", {})
check("title_visible", "Title screen दिख रहा है (काला नहीं)", title.get("bright_pct", 0) > 0.3,
      f"bright pixels {title.get('bright_pct')}%")
st = find(r"state from=Menu to=Playing")
check("start", "Tap से game शुरू हुआ", st, st[:1] or "नहीं मिला")
check("screen_changes", "शुरू होने पर screen बदली", d_title_play is not None and d_title_play > 1.0,
      f"title↔playing अंतर = {d_title_play}")
l = find(r"lane dir=Left")
check("swipe_left", "बाएँ swipe से lane बदली", l, l[:1] or "नहीं मिला")
r = find(r"lane dir=Right")
check("swipe_right", "दाएँ swipe से lane बदली", r, r[:2] or "नहीं मिला")
j = find(r"^ND_EVT jump")
check("jump", "ऊपर swipe से कूदा", j, j[:1] or "नहीं मिला")
check("score_grows", "दौड़ते हुए score बढ़ा", len(play_scores) >= 2 and max(play_scores) > min(play_scores),
      f"scores {play_scores[:8]}")
check("crash_event", "रुकावट से टकराकर game over हुआ", crashes and int(crashes[0].get("score", 0)) > 0,
      [f"score={c.get('score')} best={c.get('best')} newbest={c.get('newbest')}" for c in crashes[:3]] or "crash नहीं हुआ")
check("new_best", "पहली बार पर 'NEW BEST' (bug fix)", crashes and crashes[0].get("newbest") == "True",
      f"पहला crash newbest={crashes[0].get('newbest') if crashes else None}")
go = find(r"state from=Playing to=GameOver")
check("game_over_state", "Game Over state आया", go, go[:1] or "नहीं मिला")
check("game_over_screen", "Game Over पर screen बदली", d_play_over is not None and d_play_over > 0.5,
      f"jump↔game-over अंतर = {d_play_over}")
rt = find(r"state from=GameOver to=Playing")
check("retry", "Tap से दोबारा खेल शुरू हुआ", rt, rt[:1] or "नहीं मिला")
check("no_crash", "कोई crash / C# exception नहीं", not crash_sig, crash_sig[:5] or "साफ़")
alive = read("app-state.txt", "?").strip()
check("alive", "अंत में game चालू था", alive == "alive", f"pidof → {alive}")

passed = all(c["ok"] for c in C)
res = {
    "verdict": "PASS" if passed else "FAIL",
    "passed": sum(c["ok"] for c in C), "total": len(C),
    "checks": C,
    "fps": {"min": min(fps) if fps else None, "max": max(fps) if fps else None,
            "avg": round(sum(fps) / len(fps), 1) if fps else None},
    "events": len(events),
    "screen": read("screen.txt").strip(),
    "emulator": read("emulator-version.txt").strip(),
    "recoveries": [x for x in read("recoveries.txt").splitlines() if x.strip()],
    "shots": shots,
    "video_bytes": os.path.getsize(os.path.join(out, "e2e.mp4")) if os.path.exists(os.path.join(out, "e2e.mp4")) else 0,
    "renderer_noise": len(re.findall(r"ShaderGLES3|Program linking failed", log)),
}
json.dump(res, open(os.path.join(out, "result.json"), "w"), indent=2, ensure_ascii=False)
for c in C:
    print(("PASS " if c["ok"] else "FAIL ") + c["key"] + " :: " + str(c["evidence"]))
print(f"VERDICT {res['verdict']} {res['passed']}/{res['total']}  fps={res['fps']}  recoveries={len(res['recoveries'])}")
