#!/usr/bin/env python3
"""Merge every Render Lab artifact into one phone-friendly HTML page."""
import json, glob, os, html, shutil, sys
src, dst = sys.argv[1], sys.argv[2]
os.makedirs(dst, exist_ok=True)
rows, cards = [], []
order = {"PASS_3D_AND_2D": 0, "PARTIAL_2D_ONLY": 1, "PARTIAL_3D_ONLY": 1, "BLACK_OR_WRONG": 2,
         "CRASH_OR_DEAD": 3, "INSTALL_FAILED": 3, "EMULATOR_DID_NOT_RUN": 4}
results = []
for rj in glob.glob(os.path.join(src, "*", "result.json")):
    r = json.load(open(rj)); r["_dir"] = os.path.dirname(rj); results.append(r)
results.sort(key=lambda r: (order.get(r.get("verdict"), 9), r.get("variant", ""), r.get("gpu", "")))
colour = {0: "#1f9d55", 1: "#d69e2e", 2: "#c53030", 3: "#9b2c2c", 4: "#555"}
for r in results:
    name = os.path.basename(r["_dir"])
    os.makedirs(os.path.join(dst, name), exist_ok=True)
    imgs = ""
    for s in r.get("screenshots", []):
        shutil.copy(os.path.join(r["_dir"], s), os.path.join(dst, name, s))
        imgs += f'<img src="{name}/{s}" loading="lazy">'
    for extra in ("logcat.txt", "result.json", "props.txt", "run.log"):
        if os.path.exists(os.path.join(r["_dir"], extra)):
            shutil.copy(os.path.join(r["_dir"], extra), os.path.join(dst, name, extra))
    p = r.get("pixels") or {}
    c = colour[order.get(r.get("verdict"), 4)]
    rows.append(f'<tr><td>{html.escape(r.get("variant",""))}</td><td>{html.escape(r.get("gpu",""))}</td>'
                f'<td style="color:{c};font-weight:bold">{html.escape(r.get("verdict",""))}</td>'
                f'<td>{r.get("shader_link_errors","")}</td><td>{p.get("black_pct","")}</td>'
                f'<td>{r.get("fps_last","")}</td></tr>')
    cards.append(f'<div class="card"><h3>{html.escape(name)} — <span style="color:{c}">{html.escape(r.get("verdict",""))}</span></h3>'
                 f'<pre>{html.escape(r.get("probe_ready",""))}\n{html.escape(r.get("engine_api_line",""))}\n'
                 f'pixels: {html.escape(json.dumps(p))}\nemulator: {html.escape(str(r.get("emulator_version","")))}</pre>'
                 f'<div class="g">{imgs}</div><a href="{name}/logcat.txt">logcat</a> · <a href="{name}/result.json">result.json</a></div>')
open(os.path.join(dst, "index.html"), "w").write(f"""<!doctype html><html><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>Render Lab</title>
<style>body{{background:#0b0b16;color:#e6e6f0;font-family:system-ui,sans-serif;margin:0;padding:14px}}
table{{border-collapse:collapse;width:100%;font-size:13px}}td,th{{border:1px solid #333;padding:5px}}
.card{{background:#15152a;border-radius:10px;padding:10px;margin:14px 0}}pre{{white-space:pre-wrap;font-size:11px}}
.g{{display:grid;grid-template-columns:repeat(3,1fr);gap:6px}}img{{width:100%;border-radius:6px}}a{{color:#6cf}}</style>
</head><body><h1>Render Lab — emulator + desktop</h1>
<table><tr><th>variant</th><th>gpu / mode</th><th>verdict</th><th>shader errors</th><th>black %</th><th>fps</th></tr>
{''.join(rows)}</table>{''.join(cards)}</body></html>""")
print(f"report: {len(results)} runs")
for r in results: print(f'{r.get("variant"):8} {r.get("gpu"):22} {r.get("verdict")}')
