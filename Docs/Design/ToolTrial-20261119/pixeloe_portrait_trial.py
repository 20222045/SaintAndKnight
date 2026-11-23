"""PixelOE trial on the Cecilia HUD illustration vs the current in-game pixel portrait."""
import sys, os
import numpy as np, torch
from PIL import Image
from pixeloe.torch.pixelize import pixelize

ROOT = r"C:\UnityProject\SaintAndKnight"
OUT = os.path.dirname(os.path.abspath(__file__)) + r"\oe_out"
os.makedirs(OUT, exist_ok=True)
stage = sys.argv[1] if len(sys.argv) > 1 else "0"
src = Image.open(rf"{ROOT}\Docs\Design\Characters\reference\portraits\original_png\cecilia_portrait_stage{stage}.png").convert("RGBA")
cur = Image.open(rf"{ROOT}\Assets\SaintKnightDay06\Art\Portraits\cecilia_portrait_stage{stage}.png").convert("RGBA")
W, H = src.size
print("size", src.size, cur.size)

# PixelOE works on RGB: flatten on a mid grey, keep alpha separately
bg = Image.new("RGBA", src.size, (250, 248, 250, 255)); bg.alpha_composite(src)
t = torch.from_numpy(np.asarray(bg.convert("RGB"))).permute(2, 0, 1)[None].float() / 255

def run(ps, thick, colors):
    o = pixelize(t, pixel_size=ps, thickness=thick, mode="contrast", do_quant=colors > 0,
                 num_colors=max(colors, 2), no_post_upscale=True, backend="torch")
    small = Image.fromarray((o[0].permute(1, 2, 0).clamp(0, 1).numpy() * 255).astype(np.uint8))
    a = src.split()[3].resize(small.size, Image.BOX).point(lambda v: 255 if v >= 128 else 0)
    small = small.convert("RGBA"); small.putalpha(a)
    big = small.resize((small.width * ps, small.height * ps), Image.NEAREST)
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0)); canvas.paste(big, (0, 0))
    return canvas

variants = [("current", cur), ("oe_px2_t1", run(2, 1, 0)), ("oe_px3_t1", run(3, 1, 0)), ("oe_px3_t1_pal", None)]
from PIL import Image as _I
pal = cur.convert("RGB").quantize(128, method=_I.Quantize.MAXCOVERAGE)
v = variants[2][1]; a = v.split()[3]; q = v.convert("RGB").quantize(palette=pal, dither=_I.Dither.NONE).convert("RGBA"); q.putalpha(a)
variants[3] = ("oe_px3_t1_pal", q)
gap = 8
sheet = Image.new("RGBA", (len(variants) * (W + gap), H), (40, 40, 48, 255))
for i, (name, im) in enumerate(variants):
    im.save(rf"{OUT}\stage{stage}_{name}.png")
    sheet.alpha_composite(im, (i * (W + gap), 0))
sheet.save(rf"{OUT}\stage{stage}_compare.png")
# face crop at 2x for detail
fx, fy, fw, fh = W // 4, 0, W // 2, H // 3
crops = Image.new("RGBA", (len(variants) * (fw * 2 + gap), fh * 2), (40, 40, 48, 255))
for i, (name, im) in enumerate(variants):
    c = im.crop((fx, fy, fx + fw, fy + fh)).resize((fw * 2, fh * 2), Image.NEAREST)
    crops.alpha_composite(c, (i * (fw * 2 + gap), 0))
crops.save(rf"{OUT}\stage{stage}_face.png")
print("ok", OUT)
