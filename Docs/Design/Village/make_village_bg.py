"""Village street background.

2026-11-19: NovelAI 1216x832 (source/village_novelai_51001.png) -> 640x360, one screen, mirrored, sky cut out.
2026-11-22 (user: extend the existing village to the sides with NovelAI): that mirrored crop, scaled to 910x512, was
put in the middle of a 1792x512 canvas and NovelAI V4.5 inpainted only the two sides (seed 777413539,
source/village_extended_777413539.png, prompt in source/prompt_extend.txt). Here: whole canvas -> 1260x360
(the middle becomes exactly 640 px), sides quantized to the old 640 art's palette, sky cut the same way, and the
old 640 px art (source/village_640_20261119/) pasted back unchanged at x = MID. Doors keep their old px + MID.
"""
import colorsys
from pathlib import Path
from PIL import Image

here = Path(__file__).resolve().parent
old_town = Image.open(here / "source/village_640_20261119/village_bg.png").convert("RGBA")
old_lights = Image.open(here / "source/village_640_20261119/village_lights.png").convert("RGBA")
src = Image.open(here / "source/village_extended_777413539.png").convert("RGB")
scale = 640 / 910
W, H, MID = round(src.width * scale), 360, round(441 * scale)          # 1260, 360, 310
small = src.resize((W, H), Image.LANCZOS)
palette = old_town.convert("RGB").quantize(colors=128, method=Image.MEDIANCUT)
pixel = small.quantize(palette=palette, dither=Image.NONE).convert("RGB")
px = pixel.load()

# sky: per column it ends where 10 of the next 12 pixels are silhouette (same rule as the 640 art)
def solid(x, y):
    r, g, b = px[x, y]
    _, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
    return s < 0.24 or v < 0.36 or (r > 200 and g > 150 and b < 110)
town = pixel.convert("RGBA")
tp = town.load()
lights = Image.new("RGBA", (W, H), (0, 0, 0, 0))
lp = lights.load()
raw = []
for x in range(W):
    y = 0
    while y < H - 12 and sum(solid(x, y + k) for k in range(12)) < 10:
        y += 1
    raw.append(y)
skyline = []
for x in range(W):
    med = sorted(raw[max(0, x - 15):x + 16])[len(raw[max(0, x - 15):x + 16]) // 2]
    y = min(raw[x], med + 3)            # thin columns cut down into a red roof: back to the neighbours' line
    # ponytail: hand range for painted clouds left standing as grey blocks over the new left houses
    if 158 <= x <= 240: y = max(y, 88)
    skyline.append(y)
for x in range(W):
    y = skyline[x]
    for yy in range(y): tp[x, yy] = (0, 0, 0, 0)
    for yy in range(y, 322):
        r, g, b = px[x, yy]
        if r > 190 and g > 120 and b < 140 and r - b > 90: lp[x, yy] = (r, g, b, 255)

town.paste(old_town, (MID, 0))
lights.paste(old_lights, (MID, 0))
art = here.parents[2] / "Assets/SaintKnightLobby/Art"
town.save(art / "village_bg.png")
lights.save(art / "village_lights.png")
print("wrote", art / "village_bg.png", (W, H), "old art at x =", MID)
