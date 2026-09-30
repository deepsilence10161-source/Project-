#!/usr/bin/env python3
"""Restyle VRoid AvatarSample_B (license: edits + commercial allowed) into our heroine:
silver-white hair fading to icy-blue tips, fair skin, deep blue eyes, pale blue-white
jacket, soft lilac skirt, cat-ear lobes hidden. Textures downscaled to 2048.
Usage: restyle_heroine.py in.vrm out.vrm"""
import io, json, struct, sys
import numpy as np
from PIL import Image

src, dst = sys.argv[1], sys.argv[2]
raw = open(src, "rb").read()
jl = struct.unpack_from("<I", raw, 12)[0]
js = json.loads(raw[20:20 + jl]); binb = bytearray(raw[20 + jl + 8:])

def img_bytes(i):
    bv = js["bufferViews"][js["images"][i]["bufferView"]]
    o = bv.get("byteOffset", 0); return bytes(binb[o:o + bv["byteLength"]])

def rgb_to_hsv(a):
    r, g, b = a[..., 0], a[..., 1], a[..., 2]
    mx = a[..., :3].max(-1); mn = a[..., :3].min(-1); d = mx - mn
    h = np.zeros_like(mx); m = d > 1e-6
    rr = m & (mx == r); gg = m & (mx == g) & ~rr; bb_ = m & ~rr & ~gg
    h[rr] = ((g - b)[rr] / d[rr]) % 6; h[gg] = ((b - r)[gg] / d[gg]) + 2; h[bb_] = ((r - g)[bb_] / d[bb_]) + 4
    return h / 6.0, np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0), mx

def hsv_to_rgb(h, s, v):
    i = np.floor(h * 6) % 6; f = h * 6 - np.floor(h * 6)
    p = v * (1 - s); q = v * (1 - f * s); t = v * (1 - (1 - f) * s)
    out = np.zeros(h.shape + (3,), np.float32)
    for k, (R, G, B) in enumerate([(v, t, p), (q, v, p), (p, v, t), (p, q, v), (t, p, v), (v, p, q)]):
        m = i == k; out[m, 0] = R[m]; out[m, 1] = G[m]; out[m, 2] = B[m]
    return out

def lum(a): return 0.299 * a[..., 0] + 0.587 * a[..., 1] + 0.114 * a[..., 2]
def ramp(L, dark, light, gamma=1.0):
    t = np.clip(L, 0, 1)[..., None] ** gamma
    return np.array(dark, np.float32) * (1 - t) + np.array(light, np.float32) * t
def region(a, x0, y0, x1, y1):
    H, W = a.shape[:2]; m = np.zeros((H, W), bool)
    m[int(y0 * H):int(y1 * H), int(x0 * W):int(x1 * W)] = True; return m

def fair_skin(a, mask):
    h, s, v = rgb_to_hsv(a)
    skin = mask & (h > 0.0) & (h < 0.11) & (s > 0.12) & (s < 0.75) & (v > 0.25)
    new = hsv_to_rgb(np.clip(h - 0.008, 0, 1), s * 0.52, np.clip(v * 1.22 + 0.06, 0, 1))
    a[..., :3][skin] = new[skin]

def hair(a, m, y0, y1):
    H = a.shape[0]; L = (lum(a) - 0.12) / 0.62
    base = ramp(L, (0.72, 0.76, 0.88), (1.0, 1.0, 1.0), 0.7)
    yy = (np.arange(H)[:, None] / H - y0) / (y1 - y0)
    tip = np.clip((yy - 0.55) / 0.45, 0, 1)[..., None] * np.ones_like(base[..., :1])
    blue = ramp(L, (0.18, 0.40, 0.85), (0.55, 0.80, 1.0), 0.9)
    col = base * (1 - tip * 0.85) + blue * (tip * 0.85)
    a[..., :3][m] = col[m]

def process(i, face):
    a = np.asarray(Image.open(io.BytesIO(img_bytes(i))).convert("RGBA").resize((2048, 2048), Image.LANCZOS), np.float32) / 255.0
    a = a.copy()
    if face:
        fm = region(a, 0, 0, 0.5, 0.5); h, s, v = rgb_to_hsv(a)
        purple = fm & (h > 0.62) & (h < 0.85) & (s > 0.15)
        tmp = a.copy(); hair(tmp, purple, 0, 1); a[purple] = tmp[purple]
        fair_skin(a, fm)
        bm = region(a, 0.52, 0.17, 0.97, 0.22); h, s, v = rgb_to_hsv(a); brow = bm & (s > 0.1)
        a[..., :3][brow] = ramp(lum(a), (0.45, 0.48, 0.62), (0.80, 0.82, 0.92))[brow]
        im = region(a, 0.0, 0.78, 0.5, 1.0); iris = im & (a[..., 3] > 0.05)
        a[..., :3][iris] = ramp(lum(a), (0.03, 0.06, 0.30), (0.50, 0.78, 1.0), 0.85)[iris]
    else:
        hair(a, region(a, 0.5, 0.125, 1.0, 0.375), 0.125, 0.375)
        sm = region(a, 0.75, 0.5, 0.95, 0.75)
        a[..., :3][sm] = ramp(lum(a), (0.35, 0.60, 0.95), (0.85, 0.95, 1.0))[sm]
        jm = region(a, 0.0, 0.0, 0.48, 0.35); L = lum(a); dark = jm & (L < 0.55)
        a[..., :3][dark] = ramp(L, (0.70, 0.78, 0.92), (0.97, 0.98, 1.0), 0.7)[dark]
        km = region(a, 0.5, 0.41, 0.75, 0.5); h, s, v = rgb_to_hsv(a)
        a[..., :3][km] = hsv_to_rgb(np.full_like(h, 0.88), s * 0.55, np.clip(v * 1.05, 0, 1))[km]
        fair_skin(a, region(a, 0, 0.5, 0.5, 1.0))
        a[..., 3][region(a, 0.75, 0.75, 1.0, 1.0)] = 0.0   # hide cat-ear lobes (MASK materials)
    buf = io.BytesIO(); Image.fromarray((np.clip(a, 0, 1) * 255).astype(np.uint8), "RGBA").save(buf, "PNG"); return buf.getvalue()

new = {0: process(0, True), 7: process(7, True), 3: process(3, False), 11: process(11, False)}
repl = {js["images"][i]["bufferView"]: d for i, d in new.items()}
nb = bytearray()
for vi, bv in enumerate(js["bufferViews"]):
    o = bv.get("byteOffset", 0); data = repl.get(vi, bytes(binb[o:o + bv["byteLength"]]))
    while len(nb) % 4: nb.append(0)
    bv["byteOffset"] = len(nb); bv["byteLength"] = len(data); nb.extend(data)
while len(nb) % 4: nb.append(0)
js["buffers"][0]["byteLength"] = len(nb)
js["extensions"]["VRM"]["meta"]["title"] = "Heroine (restyled from VRoid AvatarSample_B)"
jb = json.dumps(js, separators=(",", ":")).encode()
while len(jb) % 4: jb += b" "
with open(dst, "wb") as f:
    f.write(struct.pack("<III", 0x46546C67, 2, 12 + 8 + len(jb) + 8 + len(nb)))
    f.write(struct.pack("<II", len(jb), 0x4E4F534A)); f.write(jb)
    f.write(struct.pack("<II", len(nb), 0x004E4942)); f.write(nb)
print("wrote", dst)
