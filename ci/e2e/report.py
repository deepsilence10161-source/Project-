#!/usr/bin/env python3
"""Build the phone-friendly HTML report.  Usage: report.py <out_dir> <site_dir> <run_no> <sha>"""
import html, json, os, shutil, sys

out, site, run_no, sha = sys.argv[1:5]
e2e = os.path.join(site, "e2e")
os.makedirs(e2e, exist_ok=True)
res = json.load(open(os.path.join(out, "result.json")))
for f in sorted(os.listdir(out)):
    if f.endswith((".png", ".mp4", ".txt", ".json")) and f != "logcat.txt":
        shutil.copy(os.path.join(out, f), e2e)
# logcat can be large: keep only the game's lines + errors
lp = os.path.join(out, "logcat.txt")
with open(os.path.join(e2e, "logcat-game.txt"), "w") as dst:
    if os.path.exists(lp):
        for line in open(lp, errors="replace"):
            if "godot" in line or "neondash" in line or " E " in line:
                dst.write(line)
    else:
        dst.write("logcat was not captured\n")

ok = res["verdict"] == "PASS"
rows = "".join(
    f"<tr><td>{'✅' if c['ok'] else '❌'}</td><td>{html.escape(c['label'])}</td>"
    f"<td><code>{html.escape(str(c['evidence']))[:220]}</code></td></tr>" for c in res["checks"])
shots = "".join(
    f"<figure><img src='{n}.png' loading='lazy'><figcaption>{n}</figcaption></figure>"
    for n, v in res["shots"].items() if isinstance(v, dict) and v.get("present"))
video = ("<video controls muted playsinline src='e2e.mp4' style='width:100%;max-width:432px;border-radius:10px'></video>"
         if res["video_bytes"] else "<p>video नहीं बना</p>")
rec = "<br>".join(html.escape(r) for r in res["recoveries"]) or "कोई नहीं"
fps = res["fps"]
page = f"""<!doctype html><html lang="hi"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1"><title>Neon Dash — E2E</title>
<style>body{{background:#0a0a1f;color:#e8e8ff;font-family:system-ui,sans-serif;margin:0;padding:16px}}
h1{{color:#00f0ff;font-size:22px}}h2{{color:#00f0ff;font-size:17px;margin-top:26px}}
.v{{font-size:20px;font-weight:700;padding:12px;border-radius:10px;background:{'#0f3d24' if ok else '#4a1020'}}}
table{{border-collapse:collapse;width:100%;font-size:14px}}td{{border-bottom:1px solid #26264a;padding:6px;vertical-align:top}}
code{{font-size:11px;color:#9a9ac8;word-break:break-all}}
.g{{display:grid;grid-template-columns:repeat(auto-fill,minmax(150px,1fr));gap:10px}}
figure{{margin:0;background:#14142b;border-radius:10px;overflow:hidden}}img{{width:100%;display:block}}
figcaption{{padding:6px;font-size:12px;color:#9a9ac8}}a{{color:#00f0ff}}</style></head><body>
<h1>⚡ Neon Dash — Emulator E2E</h1>
<div class="v">{'✅ पास' if ok else '❌ फ़ेल'} — {res['passed']}/{res['total']} जाँचें</div>
<p>Run #{html.escape(run_no)} · commit <code>{html.escape(sha[:7])}</code> · screen {html.escape(res['screen'])} ·
FPS (software GPU) min {fps['min']} / औसत {fps['avg']} / max {fps['max']} · events {res['events']}</p>
<h2>जाँचें</h2><table>{rows}</table>
<h2>Video</h2>{video}
<h2>Screenshots</h2><div class="g">{shots}</div>
<h2>Recoveries (emulator की दिक़्क़तें, छिपाई नहीं गईं)</h2><p><code>{rec}</code></p>
<h2>फ़ाइलें</h2><p><a href="events.txt">events.txt</a> · <a href="steps.txt">steps.txt</a> ·
<a href="result.json">result.json</a> · <a href="logcat-game.txt">logcat-game.txt</a></p>
<p style="color:#777;font-size:12px">Emulator: Android 34 AOSP x86_64, -gpu lavapipe (software GPU)। FPS असली phone जैसी नहीं है।</p>
</body></html>"""
open(os.path.join(e2e, "index.html"), "w").write(page)

hub = """<!doctype html><html lang="hi"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Neon Dash — Reports</title><style>body{background:#0a0a1f;color:#e8e8ff;font-family:system-ui,sans-serif;padding:20px}
a{display:block;color:#00f0ff;font-size:19px;margin:14px 0}</style></head><body><h1>⚡ Neon Dash — Reports</h1>
<a href="e2e/">🎮 Emulator E2E (हर push पर)</a><a href="render-lab/">🔬 Render Lab</a><a href="gpu-lab/">🖥️ GPU Lab (macOS असली GPU)</a>
<a href="https://github.com/deepsilence10161-source/Project-/releases">📦 APK Downloads</a></body></html>"""
open(os.path.join(site, "index.html"), "w").write(hub)
print("report written", e2e)
