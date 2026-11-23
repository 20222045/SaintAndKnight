"""Boss spit 23-cell sheets built from the SpriteCook slime material (6 stages, 3x2) instead of code-drawn slime.
Staging only (Docs). Character pixels are copied 1:1 from the Day 6 atlas; only the slime material is resampled.
Run: python make_boss_spit_from_material.py -> staging-spritecook-v2/ (keeps v1).
Pass --sync-drafts to copy only the two revised PNG/JSON pairs into Drafts.
Does not change runtime art, scenes, character atlases, or Unity .meta files.
"""
import argparse
import math
import os
import shutil
from PIL import Image, ImageChops, ImageFilter
import make_boss_pattern_dots as m

MATERIAL = os.path.join(m.ROOT, "Docs/Design/Stage/spritecook/1_boss_spit_slime_6stage_v1.png")
OUT = os.path.join(m.HERE, "staging-spritecook-v2")
W, H, GROUND = m.W, m.H, m.GROUND
FRAC = [.10, .30, .50, .70, 1, 1]          # how far down the body each stage reaches
MIN_BLOB = 10                               # source px; smaller specks (stray marks, dust) are dropped


def clean(cell):
    """Hard alpha + drop tiny connected specks."""
    cell = cell.copy(); px = cell.load(); w, h = cell.size; seen = set()
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]; px[x, y] = (r, g, b, 255) if a >= 128 else (0, 0, 0, 0)
    for y in range(h):
        for x in range(w):
            if (x, y) in seen or not px[x, y][3]: continue
            comp, todo = [], [(x, y)]; seen.add((x, y))
            while todo:
                cx, cy = todo.pop(); comp.append((cx, cy))
                for nx, ny in ((cx+1, cy), (cx-1, cy), (cx, cy+1), (cx, cy-1)):
                    if 0 <= nx < w and 0 <= ny < h and (nx, ny) not in seen and px[nx, ny][3]: seen.add((nx, ny)); todo.append((nx, ny))
            if len(comp) < MIN_BLOB:
                for p in comp: px[p] = (0, 0, 0, 0)
    return cell.crop(cell.getbbox())


def stages():
    sheet = Image.open(MATERIAL).convert("RGBA"); cw, ch = sheet.width / 3, sheet.height / 2
    return [clean(sheet.crop((round(c * cw), round(r * ch), round((c + 1) * cw), round((r + 1) * ch)))) for r in range(2) for c in range(3)]


def scaled(src, scale, squash=0):
    return src.resize((max(1, round(src.width * scale)), max(1, round(src.height * scale) - squash)), Image.NEAREST)


def bottom_drips(src):
    """Bend the cropped lower band into drips using only existing material pixels."""
    w, h = src.size
    band = min(9, h)
    start = h - band
    out = Image.new("RGBA", (w, h + 5))
    for x in range(w):
        extension = round(2 + 1.7 * math.sin(x * .61) + .9 * math.sin(x * 1.19 + .8))
        extension = max(0, min(5, extension))
        for y in range(h + extension):
            sy = y if y < start else start + min(band - 1, (y - start) * band // (band + extension))
            out.putpixel((x, y), src.getpixel((x, sy)))
    return out.crop(out.getbbox())


def top_axis(src):
    px = src.load()
    xs = sorted(x for y in range(min(3, src.height)) for x in range(src.width) if px[x, y][3])
    return xs[len(xs) // 2]


def taper_neck(src):
    """Remove the rectangular top cut before joining the narrow incoming stream."""
    src = src.copy()
    axis = top_axis(src)
    for y in range(min(8, src.height)):
        radius = 2 + y
        for x in range(src.width):
            if abs(x - axis) > radius:
                src.putpixel((x, y), (0, 0, 0, 0))
    return src


def pouring_stream(layer, x, bottom, phase):
    """Continuous curved neck: no repetition of an entire clipped source row."""
    # Olive/rose colours sampled from the original SpriteCook material.
    palette = [(21, 18, 13, 255), (51, 46, 26, 255), (83, 79, 42, 255),
               (133, 77, 86, 255), (62, 61, 31, 255), (32, 29, 11, 255)]
    for y in range(min(H, bottom + 1)):
        center = x + round(1.25 * math.sin((y - bottom) / 9 + phase * .6))
        radius = 2 + (1 if math.sin(y / 7 + phase) > .65 else 0)
        for dx in range(-radius, radius + 1):
            xx = center + dx
            if not 0 <= xx < W:
                continue
            if abs(dx) == radius:
                color = palette[0]
            elif dx == -radius + 1:
                color = palette[2 if (y + phase * 3) % 14 < 5 else 1]
            elif dx == round(math.sin(y / 11 + phase) * .8):
                color = palette[3]
            else:
                color = palette[4 if dx < 0 else 5]
            layer.putpixel((xx, y), color)


def lean_mound(src, lean):
    """One-pixel resistance sway, with the mound's base kept on its anchor."""
    out = Image.new("RGBA", (src.width + 4, src.height))
    for y in range(src.height):
        shift = round(lean * (1 - y / max(1, src.height - 1)))
        out.paste(src.crop((0, y, src.width, y + 1)), (2 + shift, y))
    return out


def envelope(layer):
    """Allow the body in interior openings, never outside the mound outline."""
    mask = Image.new("L", layer.size)
    pixels, alpha = mask.load(), layer.getchannel("A").load()
    # Detached material flecks must not create a bridge that exposes a hand/cape.
    remaining = {(x, y) for y in range(H) for x in range(W) if alpha[x, y]}
    largest = set()
    while remaining:
        start = remaining.pop(); component = {start}; todo = [start]
        while todo:
            x, y = todo.pop()
            for neighbor in ((x-1, y), (x+1, y), (x, y-1), (x, y+1)):
                if neighbor in remaining:
                    remaining.remove(neighbor); component.add(neighbor); todo.append(neighbor)
        if len(component) > len(largest):
            largest = component
    for y in range(H):
        xs = [x for x in range(W) if (x, y) in largest]
        if xs:
            for x in range(xs[0], xs[-1] + 1):
                pixels[x, y] = 255
    return mask.filter(ImageFilter.MinFilter(3))


def make_spit_frame(mats, scale, diagnostics=None):
    def spit_frame(pose, stage, phase, motion):
        consumed, escape = motion == "Consumed", motion == "Escape"
        sway = 0 if motion != "Resist" else (-1 if phase == 0 else 1)
        mat = stage
        if escape: mat, sway = [2, 0, None][stage], [-2, -6, -10][stage]
        body = m.put(pose, sway, 0); l, t, r, b = m.bbox(body); cx = m.axis(pose) + sway
        out = Image.new("RGBA", (W, H))
        layer = Image.new("RGBA", (W, H))
        allowed = Image.new("L", (W, H), 255)
        top = 0
        if mat is not None:
            squash = 0 if mat < 4 or escape or motion == "Swallow" else (3 if consumed else 1) + phase
            s = scaled(mats[mat], scale, squash=squash)
            if 1 <= mat <= 3:
                s = bottom_drips(taper_neck(s))
            if mat >= 4:
                s = lean_mound(s, (phase * 2 - 1) if motion == "Resist" else phase if consumed else 0)
            bottom = GROUND if mat >= 4 else round(t + (GROUND - t) * FRAC[mat]) + (phase if motion == "Resist" else 0)
            if mat == 0:
                bottom = t + 3  # The real drop tip, not its canvas centre, lands on the head.
                tips = [x for x in range(s.width) if s.getpixel((x, s.height - 1))[3]]
                x0 = cx - tips[len(tips) // 2]
            else:
                x0 = cx - s.width // 2
            top = bottom - s.height + 1
            if 1 <= mat <= 3 and not escape and top > 0:
                pouring_stream(layer, x0 + top_axis(s), top + 3, phase)
            layer.alpha_composite(s, (x0, top))
            if mat >= 4:
                allowed = envelope(layer)
        if not consumed:
            visible = body.copy()
            visible.putalpha(ImageChops.multiply(visible.getchannel("A"), allowed))
            out.alpha_composite(visible)
        out.alpha_composite(layer)
        if diagnostics is not None:
            diagnostics.append(dict(motion=motion, stage=stage, phase=phase, material=mat,
                                    body=body, layer=layer, envelope=allowed, output=out,
                                    stream_end=top, head_top=t))
        return out
    return spit_frame


def previews(frames, destination):
    """Reproducible two-character GIFs; preview files stay outside Assets."""
    routes = {
        "consumed": list(range(18)) + [21, 22] * 3,
        "escape": list(range(5)) + [14, 15] * 2 + [18, 19, 20],
    }
    for name, route in routes.items():
        images = []
        for index in route:
            image = Image.new("RGBA", (W * 2, H), (40, 40, 48, 255))
            for column, ch in enumerate(m.CELLS):
                image.alpha_composite(frames[ch][index], (column * W, 0))
            images.append(image.resize((W * 6, H * 3), Image.NEAREST).convert("RGB"))
        images[0].save(os.path.join(destination, f"boss_slime_{name}_preview.gif"),
                       save_all=True, append_images=images[1:], duration=180, loop=0, disposal=2)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--out", default=OUT)
    parser.add_argument("--sync-drafts", action="store_true")
    args = parser.parse_args()
    m.OUT = os.path.abspath(args.out)
    os.makedirs(m.OUT, exist_ok=True); mats = stages(); all_frames = {}
    for ch in m.CELLS:
        atlas = Image.open(os.path.join(m.ROOT, f"Assets/SaintKnightDay06/Resources/SaintKnightDay06/{ch}_motion.png")).convert("RGBA")
        t0 = m.bbox(m.body(atlas, m.SWALLOW[ch][0]))[1]
        scale = (GROUND - t0 + 6) / mats[5].height             # closed mound stands a little taller than the character
        details = []
        row = m.trap_sheet(atlas, ch, "slime", make_spit_frame(mats, scale, details))
        all_frames[ch] = [d["output"] for d in details]
        prev = Image.new("RGBA", (W * 6, H), (40, 40, 48, 255))
        for i, f in enumerate(row): prev.alpha_composite(f, (i * W, 0))
        prev.resize((W * 18, H * 3), Image.NEAREST).save(os.path.join(m.OUT, f"{ch}_swallow_boss_slime_preview.png"))
        print(ch, "scale %.3f" % scale, "mound", scaled(mats[5], scale).size)
        if args.sync_drafts:
            drafts = os.path.join(m.ROOT, "Assets/SaintKnightSpriteCook/Drafts")
            for suffix in (".png", ".json"):
                name = f"{ch}_boss_slime{suffix}"
                shutil.copyfile(os.path.join(m.OUT, name), os.path.join(drafts, name))
    previews(all_frames, m.OUT)


if __name__ == "__main__":
    main()
