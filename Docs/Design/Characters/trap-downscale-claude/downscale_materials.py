"""함정 소재 시트(1536x1024, 3x2 단계)를 게임 해상도로 줄이는 미리보기 (Claude, 2026-11-12).

단순 최근접 축소 대신:
  1. 소재의 각 픽셀을 게임 함정 그림의 팔레트로 먼저 바꾸고(반투명 번짐은 버림),
  2. 줄어든 한 픽셀이 덮는 영역에서 가장 많이 나온 색을 고른다(외곽선 색은 가중치를 줘서 살림).
그 결과를 기존 캐릭터 도트 위에 얹어 비교 그림을 만든다. Assets는 건드리지 않는다.

    python downscale_materials.py
"""
from pathlib import Path
import numpy as np
from PIL import Image

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
SRC = HERE.parent / "trap-dedicated-motion/source"
ART = ROOT / "Assets/SaintKnightDay10/Art"
ATLAS = ROOT / "Assets/SaintKnightDay06/Resources/SaintKnightDay06"
CW, CH, COLS, FLOOR = 128, 96, 12, 93
ALPHA = 128          # 이보다 옅은 번짐은 버린다

# 소재: (팔레트를 가져올 게임 그림, 6단계의 목표 높이 px, 가장 어두운 색(외곽선)의 투표 가중치)
# 촉수는 가닥이 가늘어 외곽선을 세게 살리고, 덩어리인 점액·벽은 약하게 둔다.
MATERIALS = {
    "tentacle": ("floor_tentacle_rise_f01.png", 66, 1.6),
    "slime": ("slime_pool_f01.png", 66, 1.2),
    "wall": ("flesh_wall_idle_f01.png", 104, 1.2),
}
# 캐릭터: (버티기 자세 칸)
CHARS = {"cecilia": 40, "adela": 49}


def palette_of(path, limit=7):
    a = np.array(Image.open(path).convert("RGBA")).reshape(-1, 4)
    a = a[a[:, 3] >= ALPHA][:, :3]
    colors, counts = np.unique(a, axis=0, return_counts=True)
    colors = colors[np.argsort(-counts)][:limit].astype(np.int32)
    return colors[np.argsort(colors.sum(axis=1))]          # 어두움 -> 밝음


def to_palette(rgb, pal):
    d = ((rgb[..., None, :].astype(np.int32) - pal[None, None]) ** 2).sum(-1)
    return d.argmin(-1)


def stages(name):
    """6단계를 같은 틀(전체 단계의 합친 경계)로 잘라 돌려준다."""
    sheet = np.array(Image.open(SRC / f"{name}_material.png").convert("RGBA"))
    cells = [sheet[r * 512:(r + 1) * 512, c * 512:(c + 1) * 512] for r in range(2) for c in range(3)]
    # 단계마다 바닥 높이가 조금씩 달라서, 각 단계의 맨 아래 줄을 칸 바닥에 맞춘다.
    cells = [np.roll(c, 511 - np.where(c[..., 3] >= ALPHA)[0].max(), axis=0) for c in cells]
    solid = np.any([c[..., 3] >= ALPHA for c in cells], axis=0)
    ys, xs = np.where(solid)
    return [c[ys.min():ys.max() + 1, xs.min():xs.max() + 1] for c in cells]


def shrink_vote(cell, pal, height, line_weight):
    h0, w0 = cell.shape[:2]
    scale = height / h0
    h, w = height, max(1, round(w0 * scale))
    idx = to_palette(cell[..., :3], pal)
    solid = cell[..., 3] >= ALPHA
    ye = np.linspace(0, h0, h + 1).round().astype(int)
    xe = np.linspace(0, w0, w + 1).round().astype(int)
    weight = np.ones(len(pal))
    weight[0] = line_weight
    out = np.zeros((h, w, 4), np.uint8)
    for y in range(h):
        for x in range(w):
            s = solid[ye[y]:ye[y + 1], xe[x]:xe[x + 1]]
            if s.mean() < .5:
                continue
            votes = np.bincount(idx[ye[y]:ye[y + 1], xe[x]:xe[x + 1]][s], minlength=len(pal)) * weight
            out[y, x, :3] = pal[votes.argmax()]
            out[y, x, 3] = 255
    return Image.fromarray(out)


def shrink_nearest(cell, height):
    img = Image.fromarray(cell)
    img = img.resize((max(1, round(cell.shape[1] * height / cell.shape[0])), height), Image.NEAREST)
    a = np.array(img)
    a[..., 3] = np.where(a[..., 3] >= ALPHA, 255, 0)
    return Image.fromarray(a)


def char_cell(name):
    atlas = Image.open(ATLAS / f"{name}_motion.png").convert("RGBA")
    c = CHARS[name]
    return atlas.crop((c % COLS * CW, c // COLS * CH, c % COLS * CW + CW, c // COLS * CH + CH))


def over(char, material):
    img = char.copy()
    box = char.crop((0, 0, CW, 70)).getbbox()                # 머리·몸통 기준 중심
    cx = (box[0] + box[2]) // 2
    img.alpha_composite(material, (cx - material.width // 2, FLOOR + 1 - material.height))
    return img


def sheet(rows, crop=(20, 20, 108, 96), zoom=4):
    cw, ch = crop[2] - crop[0], crop[3] - crop[1]
    out = Image.new("RGBA", (cw * len(rows[0]), ch * len(rows)), (40, 40, 48, 255))
    for j, row in enumerate(rows):
        for i, img in enumerate(row):
            out.alpha_composite(img.crop(crop), (i * cw, j * ch))
    return out.resize((out.width * zoom, out.height * zoom), Image.NEAREST)


def main():
    for name, (ref, height, line_weight) in MATERIALS.items():
        pal = palette_of(ART / ref)
        cells = stages(name)
        voted = [shrink_vote(c, pal, height, line_weight) for c in cells]
        nearest = [shrink_nearest(c, height) for c in cells]
        out = HERE / name
        out.mkdir(exist_ok=True)
        for i, img in enumerate(voted):
            img.save(out / f"{name}_stage{i + 1}.png")
        if name == "wall":
            blank = Image.new("RGBA", (CW, CH + 16))
            def alone(m):
                b = blank.copy(); b.alpha_composite(m, ((CW - m.width) // 2, CH + 16 - m.height)); return b
            sheet([[alone(m) for m in nearest], [alone(m) for m in voted]], (0, 0, CW, CH + 16), 3).save(HERE / "wall_compare.png")
            continue
        rows = [[over(char_cell("cecilia"), m) for m in nearest],
                [over(char_cell("cecilia"), m) for m in voted],
                [over(char_cell("adela"), m) for m in voted]]
        sheet(rows).save(HERE / f"{name}_compare.png")
        print(name, "size", voted[-1].size)


if __name__ == "__main__":
    main()
