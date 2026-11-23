"""Boss pattern dots that have no corridor equivalent: Grab (lifted), Sweep (knocked down), boss death.
Staging only (Docs). Character pixels are copied 1:1 from the Day 6 atlas; lying poses are exact 90-degree turns.
Run: python make_boss_pattern_dots.py   -> staging/<character>_boss_{grab,sweep,death}.png + *_contact.png
"""
import math, os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "../../../.."))
OUT = os.path.join(HERE, "staging")
W, H, GROUND = 128, 96, 93
PAL = [(18,10,22),(46,16,34),(84,22,44),(138,32,58),(196,70,92),(238,150,160)]  # = BossTentaclePalette
THORN = (232,220,196)
SHARDS = [(196,48,64),(72,96,188),(226,188,96),(236,228,214)]
CELLS = {  # atlas cells: held x2, resist x2, escape, hurt x2
    "cecilia": dict(hold=[39,41], resist=[52,54], escape=63, hurt=[27,28]),
    "adela":   dict(hold=[48,50], resist=[62,64], escape=73, hurt=[33,34]),
}

def body(atlas, cell):
    return atlas.crop(((cell % 12) * W, (cell // 12) * H, (cell % 12 + 1) * W, (cell // 12 + 1) * H))

def bezier(p0, p1, p2, n=60):
    return [((1-t)**2*p0[0]+2*(1-t)*t*p1[0]+t*t*p2[0], (1-t)**2*p0[1]+2*(1-t)*t*p1[1]+t*t*p2[1]) for t in (i/n for i in range(n+1))]

def tube(img, pts, r0, r1, thorns=True):
    """Shaded round tube along pts, radius r0 -> r1. Outline, body, upper-left highlight."""
    px = img.load()
    for k, (cx, cy) in enumerate(pts):
        r = r0 + (r1 - r0) * k / max(1, len(pts) - 1)
        for y in range(int(cy - r - 1), int(cy + r + 2)):
            for x in range(int(cx - r - 1), int(cx + r + 2)):
                if not (0 <= x < W and 0 <= y < H): continue
                d = math.hypot(x - cx, y - cy)
                if d > r + .5: continue
                if d > r - .6:
                    if px[x, y][3] == 0 or px[x, y][:3] == PAL[0]: px[x, y] = PAL[0] + (255,)
                    continue
                lit = (cx - x) + (cy - y)          # light from upper left
                px[x, y] = (PAL[5] if lit > r * .9 else PAL[4] if lit > r * .3 else PAL[3] if lit > -r * .5 else PAL[2]) + (255,)
        if thorns and k % 9 == 4 and r >= 2:
            x, y = int(cx), int(cy - r - 1)
            if 0 <= x < W and 1 <= y < H: px[x, y] = THORN + (255,); px[x, max(0, y - 1)] = THORN + (255,)

def bbox(img): return img.getbbox()

def axis(pose):
    """Body axis = median x of the head rows (bbox also holds hair, cape and weapon)."""
    l, t, r, b = bbox(pose); px = pose.load()
    xs = sorted(x for y in range(t, t + 9) for x in range(l, r) if px[x, y][3])
    return xs[len(xs) // 2]

def grab_frame(pose, lift, sway, taut):
    """One thick tentacle from the boss side (upper right) coils the waist and lifts the body off the floor."""
    out = Image.new("RGBA", (W, H)); l, t, r, b = bbox(pose)
    dx, dy = sway, -lift
    ty = t + dy + int((b - t) * .45)                       # waist line
    cx = axis(pose) + dx
    back = Image.new("RGBA", (W, H))
    tube(back, bezier((127, 4 + taut), (cx + 30, ty + 6 - taut * 2), (cx + 8, ty)), 4.5, 3.5)
    tube(back, bezier((cx - 8, ty + 3), (cx - 1, ty - 4), (cx + 8, ty + 5)), 3, 3, thorns=False)            # far side of the coil
    out.alpha_composite(back); out.paste(pose, (dx, dy), pose)
    front = Image.new("RGBA", (W, H))
    tube(front, bezier((cx + 8, ty), (cx, ty + 5), (cx - 8, ty + 3)), 3.2, 3, thorns=False)                 # coil over the waist
    tube(front, bezier((cx + 8, ty + 5), (cx + 1, ty + 11), (cx - 7, ty + 9)), 3, 2.4, thorns=False)        # second turn
    tube(front, bezier((cx - 7, ty + 9), (cx - 12, ty + 11), (cx - 11, ty + 17)), 2.2, 1)                   # tip curls off
    out.alpha_composite(front)
    return out

def lying(pose):
    """Exact 90-degree turn: head to the left (thrown backwards), resting on the ground row."""
    l, t, r, b = bbox(pose); p = pose.crop((l, t, r, b)).rotate(90, expand=True)
    return p

def place(p, cx, bottom):
    out = Image.new("RGBA", (W, H)); out.paste(p, (cx - p.width // 2, bottom - p.height + 1), p); return out

def shards(seed):
    out = Image.new("RGBA", (W, H)); px = out.load()
    for i in range(14):
        a = i * 2.399 + seed; d = 8 + (i * 7 % 13) + seed * 6
        x, y = int(64 + math.cos(a) * d * 1.5), int(58 + math.sin(a) * d)
        c = SHARDS[i % 4] + (255,)
        for ox, oy in ((0,0),(1,0),(0,1),(-1,0),(0,-1)) if i % 3 == 0 else ((0,0),(1,-1)):
            if 0 <= x+ox < W and 0 <= y+oy < H: px[x+ox, y+oy] = c
    return out

def death_frame(lay, phase):
    """Thorned tentacles close low over the fallen body; the tips breathe by one pixel."""
    out = place(lay, 64, GROUND); l, t, r, b = bbox(out); front = Image.new("RGBA", (W, H)); p = phase
    top = lambda peak: 2 * peak - GROUND                   # bezier control y for a given arc peak
    px = out.load()                                        # straps hug the body mass, not the raised weapon
    t = next(y for y in range(H) if sum(px[x, y][3] > 0 for x in range(W)) >= 14) + 3
    tube(front, bezier((l + 2, GROUND), (l + 14, top(t + 2 + p)), (l + 20, GROUND)), 2.4, 2)
    tube(front, bezier((64 - 12, GROUND), (64 - 4, top(t - 2 - p)), (64 + 12, GROUND)), 2.8, 2.4)
    tube(front, bezier((r - 24, GROUND), (r - 16, top(t + 1 + p)), (r - 4, GROUND)), 2.4, 1.8)
    tube(front, bezier((r + 14, GROUND - 1), (r + 6, GROUND - 5 - p), (r - 2, GROUND - 9)), 2, 1)
    tube(front, bezier((l - 14, GROUND - 1), (l - 8, GROUND - 4), (l, GROUND - 3 - p)), 1.8, 1, thorns=False)
    out.alpha_composite(front); return out

# ---------------------------------------------------------------- boss-only 23-cell sheets (not corridor recolours)
SLIME = [(36,20,32),(74,36,54),(120,58,78),(168,90,104),(212,136,138),(242,196,184)]
SWALLOW = {"cecilia": [43,45,47,39,53,56], "adela": [53,55,57,48,63,66]}
RESIST = {"cecilia": [44,45,50,51,52,53,54,55,56,57,57,58], "adela": [54,55,60,61,62,63,64,65,66,67,67,68]}
ESCAPE = {"cecilia": [60,63,67], "adela": [70,73,77]}

def put(pose, dx, dy):
    out = Image.new("RGBA", (W, H)); out.paste(pose, (dx, dy), pose); return out

def rose(img, x, y):
    px = img.load()
    for ox, oy, c in ((0,0,PAL[5]),(-1,0,PAL[4]),(1,0,PAL[4]),(0,-1,PAL[4]),(0,1,PAL[3]),(-2,0,PAL[3]),(2,0,PAL[3]),
                      (-1,-1,PAL[3]),(1,-1,PAL[3]),(-1,1,PAL[2]),(1,1,PAL[2]),(0,-2,PAL[3]),(0,2,PAL[1])):
        if 0 <= x+ox < W and 0 <= y+oy < H: px[x+ox, y+oy] = c + (255,)

def thorn_frame(pose, stage, phase, motion):
    """Boss floor tentacles: thorned vines wind up the body as a helix and hoist it; ends as a wound pillar with a rose."""
    consumed, escape = motion == "Consumed", motion == "Escape"
    lift = [0, 1, 4, 7, 10, 12][stage]; frac = [.14, .36, .56, .76, .93, 1.08][stage]; pitch = [9, 9, 8, 8, 7, 5][stage]
    sway = 0 if motion != "Resist" else (-2 if phase == 0 else 2)
    if escape: lift, sway = 0, [-2, -6, -10][stage]
    body = put(pose, sway, -lift); l, t, r, b = bbox(body); cx = axis(pose) + sway
    back, front = Image.new("RGBA", (W, H)), Image.new("RGBA", (W, H)); hw = 8
    base = cx - sway                                         # roots stay planted while the body sways
    if escape:
        if stage < 2:                                        # snapped stumps sink back, one loose coil falls away
            for s_, hgt in ((-13, 14 - stage * 8), (12, 10 - stage * 6)):
                tube(back, bezier((base + s_, GROUND), (base + s_ + 3, GROUND - hgt // 2), (base + s_ - 2, GROUND - hgt)), 2.6, 1.2)
            if stage == 0: tube(front, bezier((cx + 9, b - 16), (cx + 2, b - 10), (cx - 8, b - 13)), 2.2, 1.2)
        out = Image.new("RGBA", (W, H)); out.alpha_composite(back); out.alpha_composite(body); out.alpha_composite(front); return out
    top = b - int((b - t) * frac); y = b + 1 + (phase if motion != "Swallow" else 0); k = 0
    tube(back, bezier((base - 14, GROUND), (base - 12, (GROUND + y) // 2), (cx - hw, y)), 3.2, 2.8)        # two roots
    tube(back, bezier((base + 13, GROUND), (base + 14, (GROUND + y) // 2 + 2), (cx + hw, y - pitch)), 3, 2.6)
    while y - pitch >= top:
        last = y - 2 * pitch < top; r0, r1 = 2.7, (1.3 if last else 2.7)
        a_, b_ = ((cx - hw, y), (cx + hw, y - pitch)) if k % 2 == 0 else ((cx + hw, y), (cx - hw, y - pitch))
        tube(front if k % 2 == 0 else back, bezier(a_, (cx, y - pitch / 2 + (3 if k % 2 == 0 else -3)), b_), r0, r1, thorns=k % 2 == 0)
        y -= pitch; k += 1
    if stage >= 3:                                           # side whips pin the arms
        tube(front, bezier((base - 19, GROUND), (cx - 20, t + 34), (cx - 7, t + 17 + phase)), 2.2, 1.1)
        tube(back, bezier((base + 19, GROUND), (cx + 21, t + 30), (cx + 8, t + 14 - phase)), 2.2, 1.1)
    out = Image.new("RGBA", (W, H)); out.alpha_composite(back)
    if not consumed:
        if stage == 5: body = body.crop((cx - hw, 0, cx + hw + 1, H)); out.alpha_composite(body, (cx - hw, 0))   # only seen between coils
        else: out.alpha_composite(body)
    out.alpha_composite(front)
    if consumed or stage == 5: rose(out, cx + (phase if consumed else 0), max(3, y - 2))
    return out

def hsh(a, b=0): return (a * 73856093 ^ b * 19349663 ^ 0x5bd1e995) >> 3 & 0xffff

def slime_coat(mask, keep, phase):
    """Shade slime as running streams: each 3px stream is lit on its left, dark on its right, with a dark outer rim."""
    img = Image.new("RGBA", (W, H)); px = img.load()
    for (x, y) in keep:
        if not (0 <= x < W and 0 <= y < H): continue
        m = lambda dx, dy: (x + dx, y + dy) not in mask
        if m(-1, 0) and m(1, 0): i = 4 if m(0, 1) else 3                 # one-pixel drip, bright drop at its end
        elif m(0, -1) or m(-1, 0) and m(-1, -1): i = 5 if hsh(x // 3, phase) % 2 else 4   # top edge catches the light
        elif m(1, 0) or m(0, 1): i = 1                                    # dark rim on the right and underneath
        else:
            i = ((4, 3, 3), (3, 3, 2), (3, 3, 3), (3, 3, 3))[hsh(x // 3, 5) % 4][x % 3]   # irregular streams, mostly smooth
            if x % 3 == 0 and (y + hsh(x // 3) % 13 + phase) % 13 < 2: i = 5              # a wet glint sliding down
            if m(0, 2) or m(2, 0): i = 2
        px[x, y] = SLIME[i] + (255,)
    return img

def spit_frame(pose, stage, phase, motion):
    """Boss spit: slime lands on the head and runs down in streams; ends as a slime-coated figure, then a closed mound."""
    from PIL import ImageFilter
    consumed, escape = motion == "Consumed", motion == "Escape"
    frac = [.08, .20, .36, .55, .78, 1.05][stage]; sway = 0 if motion != "Resist" else (-1 if phase == 0 else 1)
    if escape: frac, sway = [.22, .08, 0][stage], [-2, -6, -10][stage]
    body = put(pose, sway, 0); l, t, r, b = bbox(body); cx = axis(pose) + sway
    grow = 1 if stage < 5 or escape else 2
    alpha = body.getchannel("A")
    for _ in range(grow + (2 if consumed else 0)): alpha = alpha.filter(ImageFilter.MaxFilter(3))
    a = alpha.load(); mask = set()
    for x in range(W):
        g = x // 3; run = .3 if hsh(g, 11) % 4 == 0 and stage < 4 else .7 + (hsh(g, 7) % 90) / 100   # uneven streams, some short
        depth = t + (b - t) * frac * run + (phase if hsh(g) % 2 else 0)
        if consumed or stage == 5 and motion != "Swallow": depth = H
        for y_ in range(H):
            if a[x, y_] and y_ <= depth and y_ <= GROUND: mask.add((x, y_))
    if consumed:                                              # close the figure into one mound: fill each row between its ends
        rows = {}
        for (x, y_) in mask: rows.setdefault(y_, []).append(x)
        for y_, xs in rows.items(): mask.update((x, y_) for x in range(min(xs), max(xs) + 1))
        mask = {(x, y_ + (phase if y_ < GROUND - 4 else 0)) for (x, y_) in mask}
    keep = set(mask)
    for x in range(l, r + 1):                                 # hanging drips below the streams
        ys = [y_ for (xx, y_) in mask if xx == x]
        if ys and hsh(x, 3) % 5 == 0:
            for d in range(1, 3 + hsh(x, phase) % 5):
                if max(ys) + d < GROUND: keep.add((x, max(ys) + d))
    pw = 0 if escape and stage == 2 else 5 + stage * 5 - (stage * 3 if escape else 0)           # puddle grows on the floor
    for x in range(cx - pw, cx + pw + 1):
        for y_ in (GROUND - 1, GROUND):
            if y_ == GROUND or abs(x - cx) < pw - 2: mask.add((x, y_)); keep.add((x, y_))
    if motion == "Swallow" and stage < 2:                     # the spit itself still arriving from above
        w_ = 3 - stage
        for y_ in range(0, t + 3):
            for x in range(cx - w_ + (hsh(y_ // 3) % 2), cx + w_ + 1): mask.add((x, y_)); keep.add((x, y_))
    out = Image.new("RGBA", (W, H))
    if not consumed: out.alpha_composite(body)
    out.alpha_composite(slime_coat(mask, keep, phase)); return out

def trap_sheet(atlas, ch, name, fn):
    cells = []
    for i in range(23):
        motion = "Swallow" if i < 6 else "Resist" if i < 18 else "Escape" if i < 21 else "Consumed"
        stage = i if i < 6 else (i - 6) // 2 if i < 18 else i - 18 if i < 21 else 5
        phase = 0 if i < 6 else (i - 6) % 2 if i < 18 else 0 if i < 21 else i - 21
        cell = SWALLOW[ch][stage] if i < 6 else RESIST[ch][i - 6] if i < 18 else ESCAPE[ch][stage] if i < 21 else RESIST[ch][-1]
        cells.append(fn(body(atlas, cell), stage, phase, motion))
    s = Image.new("RGBA", (W * 8, H * 3))
    for i, f in enumerate(cells): s.paste(f, (i % 8 * W, i // 8 * H))
    full = f"{ch}_boss_{name}"; s.save(os.path.join(OUT, full + ".png"))
    c = Image.new("RGBA", s.size, (40, 40, 48, 255)); c.alpha_composite(s)
    c.resize((s.width * 3, s.height * 3), Image.NEAREST).save(os.path.join(OUT, full + "_contact.png"))
    with open(os.path.join(OUT, full + ".json"), "w") as f:   # same manifest shape as the corridor trap sheets
        f.write('{\n  "schemaVersion":1,\n  "character":"%s",\n  "trap":"boss_%s",\n  "frameWidth":128,\n  "frameHeight":96,\n  "columns":8,\n  "rows":3,\n  "usedFrames":23,\n  "ppu":48,\n  "pivotX":0.5,\n  "pivotY":0.26625,\n  "swallowStart":0, "swallowCount":6,\n  "resistStart":6, "resistStages":6, "resistFramesPerStage":2,\n  "escapeStart":18, "escapeCount":3,\n  "consumedStart":21, "consumedCount":2\n}\n' % (ch, name))
    assert len({f.tobytes() for f in cells}) == 23, full + ": duplicate cells"
    return cells[:6]

def sheet(frames, name):
    s = Image.new("RGBA", (W * len(frames), H))
    for i, f in enumerate(frames): s.paste(f, (i * W, 0))
    s.save(os.path.join(OUT, name + ".png"))
    c = Image.new("RGBA", s.size, (40, 40, 48, 255)); c.alpha_composite(s)
    c.resize((s.width * 3, s.height * 3), Image.NEAREST).save(os.path.join(OUT, name + "_contact.png"))

def main():
    os.makedirs(OUT, exist_ok=True)
    for ch, c in CELLS.items():
        atlas = Image.open(os.path.join(ROOT, f"Assets/SaintKnightDay06/Resources/SaintKnightDay06/{ch}_motion.png")).convert("RGBA")
        g = [grab_frame(body(atlas, c["hold"][0]), 12, 0, 0), grab_frame(body(atlas, c["hold"][1]), 13, 0, 1),
             grab_frame(body(atlas, c["resist"][0]), 12, -2, 0), grab_frame(body(atlas, c["resist"][1]), 11, 2, 2),
             place(body(atlas, c["escape"]).crop(bbox(body(atlas, c["escape"]))), 54, GROUND)]
        sheet(g, ch + "_boss_grab")                       # hold 2, resist 2, escape 1
        hurt = [body(atlas, k) for k in c["hurt"]]; lay = lying(hurt[1])
        hit = Image.new("RGBA", (W, H)); hit.paste(hurt[0], (-4, -3), hurt[0])
        s = [hit, place(lay, 50, GROUND - 14), place(lay, 40, GROUND - 3), place(lay, 38, GROUND), shards(0), shards(1)]
        sheet(s, ch + "_boss_sweep")                      # hit, airborne, landing, down, shards x2
        sheet([death_frame(lay, 0), death_frame(lay, 1)], ch + "_boss_death")
        for name, fn in (("tentacle", thorn_frame), ("slime", spit_frame)):
            row = trap_sheet(atlas, ch, name, fn); prev = Image.new("RGBA", (W * 6, H), (40, 40, 48, 255))
            for i, f in enumerate(row): prev.alpha_composite(f, (i * W, 0))
            prev.resize((W * 24, H * 4), Image.NEAREST).save(os.path.join(OUT, f"{ch}_swallow_boss_{name}_preview.png"))
        assert all(f.size == (W, H) for f in g + s)

if __name__ == "__main__":
    main()
