# Anime Series — `anime` शाखा (Neon Dash से अलग)

रोमांटिक / भावनात्मक 3D anime वीडियो सीरीज़।
- किरदार: VRoid के मुफ़्त मॉडल (व्यावसायिक उपयोग और बदलाव की अनुमति), बदले हुए रूप में।
- बनाना: Blender 4.2 (मुफ़्त) + VRM Add-on (MIT) — GitHub Actions पर अपने-आप render।
- आवाज़: Kokoro (Apache-2.0) या असली आवाज़।
- `proof/` — sandbox में बना पहला परीक्षण वीडियो।

## अब तक के नतीजे
- `proof/couple-first-still.jpg` — लड़की (बदला हुआ AvatarSample_B) + लड़का (AvatarSample_C), पहली जोड़ी वाली तस्वीर।
- GitHub पर मापी गई रफ़्तार (4 CPU, Cycles 16 samples): 720×1280 ≈ 6.5 सेकंड/फ़्रेम, 1080×1920 ≈ 11.5 सेकंड/फ़्रेम।
- `voices/` — 4 हिंदी आवाज़ों का नमूना।
- `tools/restyle_heroine.py` — लड़की का रूप (चाँदी-नीले बाल, नीली आँखें) बनाने वाला script। VRM फ़ाइलें repo में नहीं रखीं; CI में दोबारा बनती हैं।
