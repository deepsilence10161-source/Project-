#!/usr/bin/env python3
"""Judge one Render Lab run objectively from its screenshot + logcat.

Usage: analyze.py <out_dir> <variant> <gpu>   -> writes <out_dir>/result.json
Colour probes (see Probe.tscn): red box, blue sphere, green ground = 3D;
yellow bar = 2D canvas. Thresholds are deliberately loose; the numbers are
all recorded so the verdict can be re-checked by eye in the report.
"""
import json, os, re, sys, glob

out, variant, gpu = sys.argv[1], sys.argv[2], sys.argv[3]
res = {"variant": variant, "gpu": gpu}

def read(name, default=""):
    p = os.path.join(out, name)
    return open(p, errors="replace").read().strip() if os.path.exists(p) else default

res["emulator_version"] = read("emulator-version.txt", "unknown")
res["install_rc"] = read("install-rc.txt", "unknown")
res["app_state"] = read("app-state.txt", "unknown")
res["immersive_setting"] = read("immersive-setting.txt", "")
props = read("props.txt")
res["props"] = props

log = read("logcat.txt")
res["probe_ready"] = (re.findall(r"PROBE_READY (.*)", log) or [""])[-1].strip()
ticks = re.findall(r"PROBE_TICK sec=(\d+) fps=(\d+)", log)
res["probe_ticks"] = len(ticks)
res["fps_last"] = int(ticks[-1][1]) if ticks else None
res["engine_api_line"] = (re.findall(r"((?:OpenGL|Vulkan) API[^\r\n]*)", log) or [""])[-1].strip()
res["shader_link_errors"] = len(re.findall(r"Program linking failed", log))
res["uniform_limit_errors"] = len(re.findall(r"GL_MAX_FRAGMENT_UNIFORM_VECTORS", log))
res["crash"] = bool(re.search(r"FATAL EXCEPTION|Process: com\.probe\.render, PID|ERROR: System\.[A-Za-z]+Exception|Fatal signal", log))

shots = sorted(glob.glob(os.path.join(out, "shot-*.png")), key=lambda p: int(re.findall(r"(\d+)s", p)[0]))
res["screenshots"] = [os.path.basename(s) for s in shots]
res["pixels"] = None
if shots:
    try:
        from PIL import Image
        im = Image.open(shots[-1]).convert("RGB")
        im.thumbnail((360, 800))
        px = list(im.getdata()); n = len(px)
        def frac(f): return round(100.0 * sum(1 for p in px if f(*p)) / n, 2)
        res["pixels"] = {
            "black_pct":  frac(lambda r, g, b: r < 18 and g < 18 and b < 18),
            "red_pct":    frac(lambda r, g, b: r > 150 and g < 90 and b < 90),
            "blue_pct":   frac(lambda r, g, b: b > 110 and r < 90 and b > g + 30),
            "green_pct":  frac(lambda r, g, b: g > 90 and r < 110 and b < 110 and g > r + 30),
            "yellow_pct": frac(lambda r, g, b: r > 190 and g > 170 and b < 90),
            "size": list(Image.open(shots[-1]).size),
        }
    except Exception as e:  # noqa
        res["pixels"] = {"error": str(e)}

p = res["pixels"] or {}
ok3d = bool(p) and p.get("red_pct", 0) > 0.3 and p.get("green_pct", 0) > 1.0
ok2d = bool(p) and p.get("yellow_pct", 0) > 2.0
res["render_3d"] = ok3d
res["render_2d"] = ok2d
if res["install_rc"] != "0":
    v = "INSTALL_FAILED"
elif res["crash"] or res["app_state"] != "alive":
    v = "CRASH_OR_DEAD"
elif ok3d and ok2d:
    v = "PASS_3D_AND_2D"
elif ok2d:
    v = "PARTIAL_2D_ONLY"
elif ok3d:
    v = "PARTIAL_3D_ONLY"
else:
    v = "BLACK_OR_WRONG"
res["verdict"] = v
json.dump(res, open(os.path.join(out, "result.json"), "w"), indent=2)
print(json.dumps({k: res[k] for k in ("variant", "gpu", "verdict", "probe_ready", "engine_api_line",
                                       "shader_link_errors", "pixels", "fps_last")}, indent=2))
