# 세실리아·아델라 전투 도트 v2 — 사용자 제공 시트(reference/character_design_sheet.webp)의
# '인게임 도트' 줄에서 6동작을 잘라 배경만 지운다. 원본 화질 유지: 축소·감색·외곽선 없음(사용자 피드백 2026-11-08).
# 출력 v2/: {이름}_{동작}.png 128x96, 발 기준선 y=92, 발 중심 x=64, 오른쪽 바라봄. 실행: python Docs/Design/Characters/make_characters_v2.py
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

HERE = Path(__file__).parent
ROOT = HERE.parents[2]
OUT = HERE / 'v2'
OUT.mkdir(exist_ok=True)
SRC = np.asarray(Image.open(HERE / 'reference/character_design_sheet.webp').convert('RGB')).astype(np.float32)
CW, CH, BASE, AX = 128, 96, 92, 64   # 세실리아 약 61px·아델라 약 74px (640x360 화면 높이의 약 17%·20%)
HALO = 90            # 가장자리에 남은 배경 섞인 어두운 픽셀 제거 기준(배경과의 색 거리 합)
MOTIONS = ['idle', 'run', 'windup', 'attack', 'hit', 'down']
BOXES = {            # 시트 원본 좌표 (x0, y0, x1, y1)
    'cecilia': [(134, 845, 187, 909), (199, 845, 251, 909), (267, 845, 330, 909),
                (339, 845, 429, 909), (429, 845, 485, 909), (494, 845, 569, 909)],
    'adela': [(128, 914, 185, 990), (193, 914, 254, 990), (260, 914, 335, 990),
              (338, 914, 429, 990), (431, 914, 487, 990), (488, 914, 571, 986)],
}


def nb_count(on):
    p = np.pad(on, 1).astype(np.int8)
    return sum(p[1 + dy:p.shape[0] - 1 + dy, 1 + dx:p.shape[1] - 1 + dx] for dy in (-1, 0, 1) for dx in (-1, 0, 1) if dy or dx)


def cutout(box):
    """테두리에서 이어진 어두운 배경을 지우고(안쪽 어두운 갑옷은 유지), 배경 섞인 가장자리 2겹을 벗긴다."""
    x0, y0, x1, y1 = box
    rgb = SRC[y0:y1, x0:x1]
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
        fg &= ~((nb_count(fg) < 8) & (dist < HALO))
    for _ in range(2):                       # 외톨이 픽셀 제거(배경 잔여·이웃 그림 조각)
        fg &= nb_count(fg) >= 2
    return rgb.astype(np.uint8), fg


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
for name, boxes in BOXES.items():
    for m, b in zip(MOTIONS, boxes):
        frames[(name, m)] = Image.fromarray(place(*cutout(b)), 'RGBA')
        frames[(name, m)].save(OUT / f'{name}_{m}.png')

# 시트 x3
Z = 3
sheet = Image.new('RGBA', (CW * Z * 6 + 70, (CH * Z + 20) * 2 + 20), (34, 28, 38, 255))
dr = ImageDraw.Draw(sheet)
for r, name in enumerate(BOXES):
    for c, m in enumerate(MOTIONS):
        x, y = 10 + c * (CW * Z + 10), 10 + r * (CH * Z + 20)
        sheet.alpha_composite(frames[(name, m)].resize((CW * Z, CH * Z), Image.NEAREST), (x, y))
        dr.line([(x, y + (BASE + 1) * Z), (x + CW * Z, y + (BASE + 1) * Z)], (110, 80, 60, 255))
        dr.text((x + 4, y + 4), f'{name} {m}', fill=(200, 180, 150, 255))
sheet.save(OUT / 'characters_v2_sheet.png')

# 로비 배경 합성(원 크기, 걷는 선 y=310) x3
for k in BOXES:
    bg = Image.open(ROOT / f'Docs/Design/Lobby/v6/lobby_v6_{k}_bg_native.png').convert('RGBA')
    bg.alpha_composite(frames[('cecilia', 'idle')], (300 - AX, 310 - BASE - 1))
    bg.alpha_composite(frames[('adela', 'idle')].transpose(Image.FLIP_LEFT_RIGHT), (390 - (CW - AX), 310 - BASE - 1))
    bg.resize((1920, 1080), Image.NEAREST).save(OUT / f'in_lobby_{k}.png')
print('ok', {k: frames[(k, 'idle')].getbbox() for k in BOXES})
