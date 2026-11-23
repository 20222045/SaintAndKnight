# 세실리아·아델라 전투 도트 v3 — 사용자 제공 큰 콘셉트(reference/{이름}_pixel_collapse.webp)를 실제 픽셀로 다시 찍는다.
# 원본은 격자 없는 AI 콘셉트(의사 픽셀 약 2px). 프레임별로 배경·바닥선·라벨 제거 → 같은 배율로 축소(LANCZOS)
# → 캐릭터별 공용 팔레트(디더 없음)로 색 정리 → 외톨이 픽셀 제거. 외곽선 없음.
# 출력 v3/: {이름}_stage{0-4}.png(손상 0~4단계 자세), {이름}_collapse_f00~f11.png(쓰러짐 동작),
#          128x96, 발 기준선 y=92, 발 중심 x=64, 오른쪽 바라봄. 실행: python Docs/Design/Characters/make_characters_v3.py
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).parent
ROOT = HERE.parents[2]
OUT = HERE / 'v3'
OUT.mkdir(exist_ok=True)
CW, CH, BASE, AX = 128, 96, 92, 64
COLORS = 48          # 캐릭터별 공용 팔레트 색 수
BG_DIST = 60         # 배경색과의 색 거리 합이 이보다 작으면 배경
LABEL = (110, 52)    # 격자 칸 왼쪽 위 'F00' 라벨 가림 영역(w, h)

# 칸 정의: (x0, x1), (y0, 바닥선 y) — 원본 시트 좌표. 바닥선 바로 위까지만 자른다.
SHEETS = {
    'cecilia': dict(
        target=62,   # 단계 0 키 → 62px (v2 세실리아 약 61px와 맞춤)
        top_cols=[(42, 348), (366, 630), (648, 903), (921, 1186), (1204, 1493)], top_rows=(88, 261),  # 칸 사이 ▸ 화살표 제외
        cols=[(147, 494), (498, 826), (830, 1167), (1171, 1515)], rows=[(297, 491), (516, 702), (726, 911)]),
    'adela': dict(
        target=70,   # 단계 0 키 → 70px (시트상 아델라가 더 큼)
        top_cols=[(20, 333), (351, 621), (639, 910), (928, 1198), (1216, 1519)], top_rows=(101, 280),
        cols=[(119, 467), (471, 817), (821, 1166), (1170, 1519)], rows=[(323, 508), (531, 715), (739, 924)]),
}


def nb_count(on):
    p = np.pad(on, 1).astype(np.int8)
    return sum(p[1 + dy:p.shape[0] - 1 + dy, 1 + dx:p.shape[1] - 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)


def components(on):
    """4-연결 성분 라벨(작은 칸이라 단순 BFS)."""
    lab = np.zeros(on.shape, np.int32)
    n = 0
    for y0, x0 in zip(*np.nonzero(on)):
        if lab[y0, x0]:
            continue
        n += 1
        stack = [(y0, x0)]
        lab[y0, x0] = n
        while stack:
            y, x = stack.pop()
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < on.shape[0] and 0 <= xx < on.shape[1] and on[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = n
                    stack.append((yy, xx))
    return lab, n


def cut(src, x0, x1, y0, y1, label):
    rgb = src[y0:y1, x0:x1]
    bg = np.median(np.concatenate([rgb[0], rgb[:, 0], rgb[:, -1]]), 0)
    fg = np.abs(rgb - bg).sum(-1) > BG_DIST
    if label:
        fg[:LABEL[1], :LABEL[0]] = False
    lab, n = components(fg)
    sizes = np.bincount(lab.ravel())[1:]
    # 본체(가장 큰 조각) 둘레 안의 조각만 유지(철퇴·떨어진 칼 등). 칸 경계 화살표·먼지·바닥선 조각(납작한 가로줄)은 버림.
    main = lab == 1 + sizes.argmax()
    ys, xs = np.nonzero(main)
    y0, y1, x0, x1 = ys.min() - 120, main.shape[0], xs.min() - 100, xs.max() + 100
    keep = main.copy()
    for i, sl in enumerate(np.ndarray.nonzero(sizes >= 20)[0]):
        part = lab == sl + 1
        py, px = np.nonzero(part)
        flat = np.ptp(py) <= 4 and np.ptp(px) >= 12
        if not flat and py.min() >= y0 and py.max() <= y1 and px.min() >= x0 and px.max() <= x1:
            keep |= part
    return rgb, keep


def shrink(rgb, fg, k):
    h, w = fg.shape
    size = (max(1, round(w * k)), max(1, round(h * k)))
    a = fg.astype(np.float32)
    prem = Image.fromarray(np.uint8(rgb * a[..., None])).resize(size, Image.LANCZOS)
    al = np.asarray(Image.fromarray(np.uint8(a * 255)).resize(size, Image.LANCZOS), np.float32) / 255
    col = np.asarray(prem, np.float32) / np.maximum(al[..., None], 1e-3)
    on = al >= 0.5
    for _ in range(2):
        on &= nb_count(on) >= 2
    lab, _ = components(on)                  # 축소 후 6px 미만 부스러기(돌 조각 등) 제거
    on &= np.bincount(lab.ravel())[lab] >= 6
    return np.clip(col, 0, 255).astype(np.uint8), on


def place(col, on):
    ys, xs = np.nonzero(on)
    bottom = ys.max()
    feet = xs[ys >= bottom - 3]
    ox, oy = AX - round((feet.min() + feet.max()) / 2), BASE - bottom
    out = np.zeros((CH, CW, 4), np.uint8)
    for y, x in zip(ys, xs):
        if 0 <= y + oy < CH and 0 <= x + ox < CW:
            out[y + oy, x + ox] = (*col[y, x], 255)
    return out


frames = {}
for name, s in SHEETS.items():
    src = np.asarray(Image.open(HERE / f'reference/{name}_pixel_collapse.webp').convert('RGB')
                     .filter(ImageFilter.UnsharpMask(1.2, 60, 2))).astype(np.float32)
    cells = [(f'stage{i}', *c, *s['top_rows'], False) for i, c in enumerate(s['top_cols'])]
    cells += [(f'collapse_f{r * 4 + c:02d}', *s['cols'][c], *s['rows'][r], True) for r in range(3) for c in range(4)]
    cut0 = cut(src, *cells[0][1:])                              # 단계 0(서 있는 자세) 키로 배율 결정
    k = s["target"] / (np.ptp(np.nonzero(cut0[1].any(1))[0]) + 1)
    raw = {key: place(*shrink(*cut(src, *c), k)) for key, *c in cells}
    opaque = np.concatenate([f[f[..., 3] > 0][:, :3] for f in raw.values()])[None]
    pal = Image.fromarray(opaque).quantize(COLORS, Image.Quantize.MEDIANCUT)
    for key, f in raw.items():
        q = np.asarray(Image.fromarray(f[..., :3]).quantize(palette=pal, dither=Image.Dither.NONE).convert('RGB'))
        img = np.dstack([q, f[..., 3]])
        img[f[..., 3] == 0] = 0
        frames[(name, key)] = Image.fromarray(img, 'RGBA')
        frames[(name, key)].save(OUT / f'{name}_{key}.png')
    print(name, 'scale', round(k, 3))

# 시트 x3: 캐릭터별 위 줄 단계 0~4, 아래 쓰러짐 F00~F11
Z, keys = 3, [f'stage{i}' for i in range(5)] + [f'collapse_f{i:02d}' for i in range(12)]
per_row = 6
rows = -(-len(keys) // per_row)
sheet = Image.new('RGBA', (CW * Z * per_row + 70, (CH * Z + 10) * rows * 2 + 30), (34, 28, 38, 255))
dr = ImageDraw.Draw(sheet)
for r0, name in enumerate(SHEETS):
    for i, key in enumerate(keys):
        x = 10 + (i % per_row) * (CW * Z + 10)
        y = 10 + (r0 * rows + i // per_row) * (CH * Z + 10)
        sheet.alpha_composite(frames[(name, key)].resize((CW * Z, CH * Z), Image.NEAREST), (x, y))
        dr.line([(x, y + (BASE + 1) * Z), (x + CW * Z, y + (BASE + 1) * Z)], (110, 80, 60, 255))
        dr.text((x + 4, y + 4), f'{name} {key}', fill=(200, 180, 150, 255))
sheet.save(OUT / 'characters_v3_sheet.png')

for k2 in SHEETS:
    bg = Image.open(ROOT / f'Docs/Design/Lobby/v6/lobby_v6_{k2}_bg_native.png').convert('RGBA')
    bg.alpha_composite(frames[('cecilia', 'stage0')], (300 - AX, 310 - BASE - 1))
    bg.alpha_composite(frames[('adela', 'stage0')].transpose(Image.FLIP_LEFT_RIGHT), (390 - (CW - AX), 310 - BASE - 1))
    bg.resize((1920, 1080), Image.NEAREST).save(OUT / f'in_lobby_{k2}.png')
print('ok')
