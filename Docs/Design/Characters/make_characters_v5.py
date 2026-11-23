# 세실리아·아델라 전투 도트 v5 — 게임 연결용 최종 시안(사용자 요청 2026-11-10 "다 하고 말해줘").
# v4 대비: (1) 세실리아는 큰 원본을 중간값 필터로 잡티를 눌러 색 덩어리를 만든 뒤 축소(모양은 원본에서 따서 사슬 유지),
#          따뜻한 색(피부·금)과 차가운 색(베일·흰옷)을 벌리고 14색으로 정리. 아델라는 v4 값 유지(어두운 갑옷 보존).
#          (2) 달리기·준비·공격·피격은 시트 1의 작은 인게임 도트에서 잘라 같은 크기·같은 팔레트로 맞춤.
#          (3) 대기 프레임 얼굴 손질(POLISH_UNUSED)은 시안만 남김.
# 2026-11-10 사용자 선택 "v3/v4 정도가 괜찮다" → 세실리아도 v4 값(필터 없음·24색·대비 0.55)으로 되돌리고 얼굴 손질 제거.
# 출력 v5/: {이름}_{idle|run|windup|attack|hit}.png, {이름}_stage0~4.png, {이름}_collapse_f00~f11.png
#          128x96, 발 기준선 y=92(Unity 피벗 = 아래에서 3px), 발 중심 x=64, 오른쪽 바라봄.
# 실행: python Docs/Design/Characters/make_characters_v5.py
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

HERE = Path(__file__).parent
ROOT = HERE.parents[2]
OUT = HERE / 'v5'
OUT.mkdir(exist_ok=True)
CW, CH, BASE, AX = 128, 96, 92, 64
BG_DIST = 60
LABEL = (110, 52)
LUMA = np.array([0.299, 0.587, 0.114], np.float32)

CHAR = {
    'cecilia': dict(
        target=55,                 # 화면 높이(360px)의 약 15% = 참고 게임 캐릭터 비율
        pre=0,                     # 큰 원본 중간값 필터 크기(0=없음). 5로 잡티를 누르면 사용자가 원치 않는 평평한 느낌(2026-11-10)
        colors=24, despeckle=1,
        wb=True,                   # 시트의 분홍기 제거
        tone=0.55,                 # 명암 대비 세기
        sat=(1.35, 1.12, 1.12),    # 채도 배율: 금 / 따뜻한 색(피부) / 나머지 — v4와 같은 값
        top_cols=[(42, 348), (366, 630), (648, 903), (921, 1186), (1204, 1493)], top_rows=(88, 261),
        cols=[(147, 494), (498, 826), (830, 1167), (1171, 1515)], rows=[(297, 491), (516, 702), (726, 911)],
        # 시트 1 '인게임 도트' 줄: idle(배율 기준), run, windup, attack, hit
        small=[(134, 845, 187, 909), (199, 845, 251, 909), (267, 845, 330, 909), (339, 845, 429, 909), (429, 845, 485, 909)]),
    'adela': dict(
        target=62, pre=0, colors=24, despeckle=1, wb=False, tone=0.25, sat=(1.35, 1.12, 1.12),
        top_cols=[(20, 333), (351, 621), (639, 910), (928, 1198), (1216, 1519)], top_rows=(101, 280),
        cols=[(119, 467), (471, 817), (821, 1166), (1170, 1519)], rows=[(323, 508), (531, 715), (739, 924)],
        small=[(128, 914, 185, 990), (193, 914, 254, 990), (260, 914, 335, 990), (338, 914, 429, 990), (431, 914, 487, 990)]),
}
MOTIONS = ['run', 'windup', 'attack', 'hit']

# 대기 프레임 손질: (x, y) → 팔레트에 없는 색도 허용. 128x96 캔버스 좌표.
SK_L, SK_M, SK_D = (242, 206, 190), (226, 176, 160), (190, 128, 120)   # 피부(승인 흉상 기준)
POLISH_UNUSED = {   # 얼굴 손질 시안(사용자 선택: 손질 없는 v4 느낌) — 적용 안 함
    # 세실리아: 얼굴을 피부색으로 베일·머리와 분리, 파란 눈(승인 #3d6fb0) 2개(3/4 측면), 입 그림자
    ('cecilia', 'idle'): [(x, y, SK_L) for x in (68, 69, 70, 71) for y in (44, 45, 46, 47)]
                         + [(67, 44, SK_M), (67, 45, SK_M), (67, 46, SK_M), (67, 47, SK_M), (68, 48, SK_M), (69, 48, SK_M), (70, 48, SK_M)]
                         + [(67, 45, (42, 35, 64)), (67, 46, (61, 111, 176)), (70, 45, (42, 35, 64)), (70, 46, (61, 111, 176)),
                            (71, 47, SK_D)],
    # 아델라: 호박색 눈(승인 #d99a2b) 2개, 입 그림자
    ('adela', 'idle'): [(x, y, SK_L) for x in (66, 67, 68, 69) for y in (39, 40, 41)]
                       + [(66, 39, (58, 26, 18)), (66, 40, (217, 154, 43)), (69, 39, (58, 26, 18)), (69, 40, (217, 154, 43)),
                          (68, 42, SK_D), (65, 41, SK_M)],
}


def nb_count(on):
    p = np.pad(on, 1).astype(np.int8)
    return sum(p[1 + dy:p.shape[0] - 1 + dy, 1 + dx:p.shape[1] - 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)


def components(on):
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


def cut_big(raw, x0, x1, y0, y1, label):
    """큰 시트 칸: 배경·라벨·칸 경계 조각 제거 후 본체 둘레 조각만 유지."""
    rgb = raw[y0:y1, x0:x1]
    bg = np.median(np.concatenate([rgb[0], rgb[:, 0], rgb[:, -1]]), 0)
    fg = np.abs(rgb - bg).sum(-1) > BG_DIST
    if label:
        fg[:LABEL[1], :LABEL[0]] = False
    lab, _ = components(fg)
    sizes = np.bincount(lab.ravel())[1:]
    main = lab == 1 + sizes.argmax()
    ys, xs = np.nonzero(main)
    ty, lx, rx = ys.min() - 120, xs.min() - 100, xs.max() + 100
    keep = main.copy()
    for sl in np.nonzero(sizes >= 20)[0]:
        part = lab == sl + 1
        py, px = np.nonzero(part)
        flat = np.ptp(py) <= 4 and np.ptp(px) >= 12
        if not flat and py.min() >= ty and px.min() >= lx and px.max() <= rx:
            keep |= part
    return keep


def cut_small(raw, box):
    """시트 1 작은 도트: 테두리에서 이어진 배경만 지움(안쪽 어두운 갑옷 유지) + 배경 섞인 가장자리 2겹 제거."""
    x0, y0, x1, y1 = box
    rgb = raw[y0:y1, x0:x1]
    bg = np.median(np.concatenate([rgb[0], rgb[-1], rgb[:, 0], rgb[:, -1]]), 0)
    dist = np.abs(rgb - bg).sum(-1)
    like = dist < 46
    seen = np.zeros_like(like)
    seen[0], seen[-1], seen[:, 0], seen[:, -1] = like[0], like[-1], like[:, 0], like[:, -1]
    while True:
        g = seen.copy()
        g[1:] |= seen[:-1]; g[:-1] |= seen[1:]; g[:, 1:] |= seen[:, :-1]; g[:, :-1] |= seen[:, 1:]
        g &= like
        if (g == seen).all():
            break
        seen = g
    fg = ~seen
    for _ in range(2):
        fg &= ~((nb_count(fg) < 8) & (dist < 90))
    for _ in range(2):
        fg &= nb_count(fg) >= 2
    return fg


def height(mask):
    return np.ptp(np.nonzero(mask.any(1))[0]) + 1


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
    lab, _ = components(on)
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


def grade(col, on, wb, tone, levels, sat):
    c = col.astype(np.float32) / 255
    if wb is not None:
        c = c * wb
    lum = c @ LUMA
    lo, hi = levels
    nl = np.clip((lum - lo) / (hi - lo), 0, 1)
    nl = nl * nl * (3 - 2 * nl) * 0.55 + nl * 0.45
    c = c * (1 - tone + tone * nl / np.maximum(lum, 1e-3))[..., None]
    hsv = np.asarray(Image.fromarray(np.uint8(np.clip(c, 0, 1) * 255)).convert('HSV'), np.float32)
    h, s = hsv[..., 0] * 360 / 255, hsv[..., 1]
    gold = (h > 25) & (h < 64) & (s > 50)
    warm = ((h < 50) | (h > 340)) & (s > 30)
    hsv[..., 1] = np.minimum(s * np.where(gold, sat[0], np.where(warm, sat[1], sat[2])), 255)
    out = np.asarray(Image.fromarray(np.uint8(hsv), 'HSV').convert('RGB'))
    return np.where(on[..., None], out, 0).astype(np.uint8)


def despeckle(idx, on):
    h, w = idx.shape
    out = idx.copy()
    for y in range(1, h - 1):
        for x in range(1, w - 1):
            if not on[y, x]:
                continue
            nb = [idx[y + dy, x + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if (dy or dx) and on[y + dy, x + dx]]
            if len(nb) >= 6 and idx[y, x] not in nb:
                vals, cnt = np.unique(nb, return_counts=True)
                if cnt.max() >= 4:
                    out[y, x] = vals[cnt.argmax()]
    return out


frames = {}
SHEET1 = np.asarray(Image.open(HERE / 'reference/character_design_sheet.webp').convert('RGB')).astype(np.float32)
for name, s in CHAR.items():
    img = Image.open(HERE / f'reference/{name}_pixel_collapse.webp').convert('RGB')
    raw = np.asarray(img).astype(np.float32)                                   # 모양(마스크)용
    colsrc = np.asarray((img.filter(ImageFilter.MedianFilter(s['pre'])) if s['pre'] else
                         img.filter(ImageFilter.UnsharpMask(1.2, 60, 2)))).astype(np.float32)   # 색용
    cells = [(f'stage{i}', *c, *s['top_rows'], False) for i, c in enumerate(s['top_cols'])]
    cells += [(f'collapse_f{r * 4 + c:02d}', *s['cols'][c], *s['rows'][r], True) for r in range(3) for c in range(4)]
    k = s['target'] / height(cut_big(raw, *cells[0][1:]))
    rawf = {}
    for key, x0, x1, y0, y1, label in cells:
        rawf[key] = place(*shrink(colsrc[y0:y1, x0:x1], cut_big(raw, x0, x1, y0, y1, label), k))
    ks = s['target'] / height(cut_small(SHEET1, s['small'][0]))                 # 작은 도트 idle 키 → 같은 목표 키
    for m, box in zip(MOTIONS, s['small'][1:]):
        x0, y0, x1, y1 = box
        rawf[m] = place(*shrink(SHEET1[y0:y1, x0:x1], cut_small(SHEET1, box), ks))
    allpx = np.concatenate([f[f[..., 3] > 0][:, :3] for f in rawf.values()]).astype(np.float32)
    wb = None
    if s['wb']:
        lum = allpx @ LUMA
        white = allpx[lum >= np.percentile(lum, 97)].mean(0)
        wb = (white.mean() / white).astype(np.float32)
    lum = (allpx / 255 * (wb if wb is not None else 1)) @ LUMA
    levels = (np.percentile(lum, 2), np.percentile(lum, 99.5))
    graded = {key: np.dstack([grade(f[..., :3], f[..., 3] > 0, wb, s['tone'], levels, s['sat']), f[..., 3]]) for key, f in rawf.items()}
    opaque = np.concatenate([f[f[..., 3] > 0][:, :3] for f in graded.values()])[None]
    pal = Image.fromarray(opaque).quantize(s['colors'], Image.Quantize.MEDIANCUT, kmeans=4)
    for key, f in graded.items():
        on = f[..., 3] > 0
        idx = np.asarray(Image.fromarray(f[..., :3]).quantize(palette=pal, dither=Image.Dither.NONE))
        for _ in range(s['despeckle']):
            idx = despeckle(idx, on)
        qi = Image.fromarray(idx.astype(np.uint8), 'P'); qi.putpalette(pal.getpalette())
        img = np.dstack([np.asarray(qi.convert('RGB')), f[..., 3]])
        img[~on] = 0
        frames[(name, key)] = img
    frames[(name, 'idle')] = frames[(name, 'stage0')].copy()
    print(name, 'scale big', round(k, 3), 'small', round(ks, 3))

POLISH = {}
for (name, key), pts in POLISH.items():
    for x, y, c in pts:
        frames[(name, key)][y, x] = (*c, 255)

for (name, key), f in frames.items():
    Image.fromarray(f, 'RGBA').save(OUT / f'{name}_{key}.png')

# 시트 x3
keys = ['idle'] + MOTIONS + [f'stage{i}' for i in range(1, 5)] + [f'collapse_f{i:02d}' for i in range(12)]
Z, per = 3, 7
rows = -(-len(keys) // per)
sheet = Image.new('RGBA', (CW * Z * per + 80, (CH * Z + 10) * rows * 2 + 30), (34, 28, 38, 255))
dr = ImageDraw.Draw(sheet)
for r0, name in enumerate(CHAR):
    for i, key in enumerate(keys):
        x = 10 + (i % per) * (CW * Z + 10)
        y = 10 + (r0 * rows + i // per) * (CH * Z + 10)
        sheet.alpha_composite(Image.fromarray(frames[(name, key)], 'RGBA').resize((CW * Z, CH * Z), Image.NEAREST), (x, y))
        dr.line([(x, y + (BASE + 1) * Z), (x + CW * Z, y + (BASE + 1) * Z)], (110, 80, 60, 255))
        dr.text((x + 4, y + 4), f'{name} {key}', fill=(200, 180, 150, 255))
sheet.save(OUT / 'characters_v5_sheet.png')

for k2 in CHAR:
    bg = Image.open(ROOT / f'Docs/Design/Lobby/v6/lobby_v6_{k2}_bg_native.png').convert('RGBA')
    bg.alpha_composite(Image.fromarray(frames[('cecilia', 'idle')], 'RGBA'), (300 - AX, 310 - BASE - 1))
    bg.alpha_composite(Image.fromarray(frames[('adela', 'idle')], 'RGBA').transpose(Image.FLIP_LEFT_RIGHT), (390 - (CW - AX), 310 - BASE - 1))
    bg.resize((1920, 1080), Image.NEAREST).save(OUT / f'in_lobby_{k2}.png')
print('ok')
