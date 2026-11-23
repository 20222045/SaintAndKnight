# 세실리아·아델라 전투 도트 v1 — Idle 1프레임 시안 (오른쪽 바라봄, 몸 48px, 캔버스 64x64, 발 기준선 y=60).
# 승인 흉상(Assets/SaintKnightDay01/Art/*_approved_cropped.png)의 색·소품만 따름. 하반신·무기 형태는 시안.
# 실행: python Docs/Design/Characters/make_characters_v1.py
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).parent / 'v1'
OUT.mkdir(parents=True, exist_ok=True)
S, BASE = 64, 60
OUTLINE = (27, 19, 32, 255)


def hx(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5)) + (255,)


def canvas():
    im = Image.new('RGBA', (S, S), (0, 0, 0, 0))
    return im, ImageDraw.Draw(im)


def outline(im):
    a = np.array(im)
    on = a[..., 3] > 0
    nb = np.zeros_like(on)
    nb[1:] |= on[:-1]; nb[:-1] |= on[1:]; nb[:, 1:] |= on[:, :-1]; nb[:, :-1] |= on[:, 1:]
    a[nb & ~on] = OUTLINE
    return Image.fromarray(a)


def px(d, pts, c):
    for p in pts:
        d.point(p, c)


# ---------------------------------------------------------------- 세실리아
def cecilia():
    HD, HM, HL = hx('#9c8f86'), hx('#d9cfc2'), hx('#f2ece0')       # 백금발
    SD, SM, SL = hx('#d9a08c'), hx('#f2c9b5'), hx('#fbe2d4')       # 피부
    WD, WM, WL = hx('#a9a7b8'), hx('#dcdbe6'), hx('#f7f6fb')       # 흰 베일·드레스
    GD, GM, GL = hx('#8a6324'), hx('#c99a3e'), hx('#f0d27a')       # 금장식
    CD, CL = hx('#6d7280'), hx('#c9ced8')                          # 은 사슬
    EY, ED, BT, CK = hx('#3d6fb0'), hx('#1e3558'), hx('#4a3b40'), hx('#3a2c3a')
    im, d = canvas()
    # 베일 뒷자락 (금 테두리)
    d.polygon([(25, 15), (32, 14), (31, 22), (28, 31), (24, 41), (20, 45), (18, 41), (21, 28)], WD)
    d.polygon([(26, 16), (31, 15), (29, 24), (26, 33), (22, 41), (21, 38), (23, 27)], WM)
    d.line([(21, 28), (18, 41), (20, 45)], GM)
    # 뒤로 흐르는 머리
    d.polygon([(27, 17), (33, 16), (33, 25), (30, 34), (27, 42), (24, 44), (25, 32), (26, 24)], HM)
    d.line([(28, 22), (27, 33), (25, 42)], HD)
    d.line([(31, 24), (29, 34)], HD)
    d.line([(30, 19), (29, 27)], HL)
    # 치마 (금 밑단, 주름)
    d.polygon([(27, 37), (36, 37), (39, 57), (23, 57)], WM)
    d.line([(30, 39), (28, 56)], WD); d.line([(34, 40), (35, 56)], WD)
    d.line([(32, 38), (32, 50)], WL)
    d.line([(37, 44), (38, 56)], WD)
    d.rectangle([24, 56, 38, 57], GM); d.line([(24, 57), (38, 57)], GD)
    # 부츠
    d.rectangle([28, 58, 31, 60], BT); d.rectangle([32, 58, 36, 60], BT); d.point((36, 58), (0, 0, 0, 0))
    # 상체: 어깨 드러난 드레스
    d.rectangle([28, 29, 36, 36], WM)
    d.rectangle([29, 29, 36, 30], SM); d.point((29, 30), SD)
    d.line([(28, 31), (36, 31)], GM)
    d.line([(29, 32), (35, 32)], WL)
    d.line([(28, 36), (36, 36)], GM); d.point((32, 36), GL)
    # 목·초커
    d.rectangle([31, 26, 33, 27], SM); d.line([(31, 28), (34, 28)], CK); d.point((33, 28), GL)
    # 머리: 얼굴·앞머리
    d.rectangle([28, 16, 37, 26], HM)
    d.rectangle([31, 19, 37, 26], SM)
    d.line([(37, 22), (37, 24)], SL); d.point((31, 26), SD)
    d.line([(28, 16), (36, 16)], HL); d.rectangle([30, 17, 37, 18], HL)
    px(d, [(33, 19), (36, 19), (31, 20), (31, 21), (31, 22)], HM)
    px(d, [(30, 20), (30, 23), (30, 25), (29, 26)], HD)
    d.line([(34, 20), (36, 20)], ED); d.point((35, 21), EY); d.point((35, 22), ED)
    d.point((36, 25), SD)
    # 베일 위쪽 + 금 머리띠·십자
    d.rectangle([27, 14, 35, 15], WL); d.line([(26, 15), (26, 19)], WM)
    d.line([(27, 16), (35, 16)], GM); d.point((31, 16), GL)
    px(d, [(28, 13), (27, 14), (28, 14), (29, 14), (28, 15)], GL)
    # 앞팔 + 사슬
    d.line([(35, 31), (36, 37)], SM); d.point((34, 31), SD); d.rectangle([36, 37, 37, 39], SM)
    for i, (x, y) in enumerate([(37, 40), (38, 41), (38, 42), (39, 43), (39, 44), (39, 45), (40, 46), (40, 47), (40, 48)]):
        d.point((x, y), CL if i % 2 else CD)
    d.line([(40, 49), (40, 54)], GM); d.line([(39, 50), (41, 50)], GM); d.point((40, 50), GL); d.point((40, 54), GD)
    return outline(im)


# ---------------------------------------------------------------- 아델라
def adela():
    HD, HM, HL = hx('#5e1420'), hx('#9c2433'), hx('#cf4a4f')       # 진홍 머리
    SD, SM, SL = hx('#d49a86'), hx('#f0c4ae'), hx('#f9dccb')
    AD, AM, AL, AH = hx('#2c2f3c'), hx('#4f5566'), hx('#8a91a3'), hx('#c4cad6')  # 강철
    UD, UM = hx('#1f1a24'), hx('#2f2834')                          # 검은 속옷
    GD, GM, GL = hx('#8a6324'), hx('#c99a3e'), hx('#f0d27a')
    RD, RM, RL = hx('#4a0f18'), hx('#7a1a26'), hx('#a8303a')       # 붉은 망토
    EY, ED, LB = hx('#d99a2b'), hx('#5a2a10'), hx('#4a3426')
    im, d = canvas()
    # 망토
    d.polygon([(26, 28), (32, 28), (31, 40), (28, 52), (21, 55), (19, 48), (23, 36)], RM)
    d.line([(24, 34), (21, 48), (21, 54)], RD); d.line([(28, 36), (26, 50)], RD); d.line([(30, 30), (28, 42)], RL)
    # 등 뒤 연 모양 방패 (금 테·십자)
    d.polygon([(20, 29), (27, 29), (27, 39), (24, 45), (20, 39)], AM)
    d.line([(20, 29), (20, 39), (24, 45)], GM); d.line([(20, 29), (27, 29)], GM)
    d.line([(23, 31), (23, 41)], GL); d.line([(21, 34), (25, 34)], GL)
    # 포니테일
    d.polygon([(27, 15), (30, 15), (29, 20), (25, 26), (21, 30), (20, 27), (23, 21)], HM)
    d.line([(27, 17), (23, 23), (21, 28)], HD); d.line([(28, 16), (25, 22)], HL)
    # 다리: 정강이 갑옷·무릎
    d.rectangle([28, 42, 31, 57], AD); d.rectangle([32, 42, 35, 57], AM)
    d.line([(33, 43), (33, 56)], AL); d.rectangle([32, 47, 35, 48], AL); d.point((33, 47), AH)
    d.rectangle([28, 47, 31, 48], AM)
    # 철제 신발
    d.rectangle([27, 58, 31, 60], AD); d.rectangle([32, 58, 37, 60], AM); d.line([(32, 58), (35, 58)], AL)
    # 허리 갑옷·벨트
    d.rectangle([27, 39, 36, 42], AM); d.line([(28, 40), (35, 40)], AL); d.line([(27, 42), (36, 42)], AD)
    d.rectangle([27, 37, 36, 38], LB); d.rectangle([32, 37, 33, 38], GM)
    # 흉갑
    d.rectangle([27, 27, 36, 36], AM)
    d.rectangle([33, 29, 35, 34], AL); d.point((34, 30), AH)
    d.line([(28, 28), (28, 35)], AD); d.line([(29, 35), (36, 35)], GM)
    d.rectangle([30, 26, 34, 27], AD)                               # 목가리개
    # 목·얼굴
    d.point((32, 25), SM); d.point((33, 25), SM)
    d.rectangle([28, 14, 37, 24], HM)
    d.rectangle([31, 18, 37, 24], SM)
    d.line([(37, 21), (37, 23)], SL); d.point((31, 24), SD)
    d.rectangle([29, 14, 36, 16], HL); d.line([(30, 17), (37, 17)], HM)
    px(d, [(36, 18), (32, 18), (31, 19), (31, 20), (31, 21)], HM)
    px(d, [(30, 19), (30, 22), (29, 23), (28, 15)], HD)
    d.line([(34, 19), (36, 19)], ED); d.point((35, 20), EY); d.point((35, 21), ED)
    d.point((36, 23), SD)
    # 앞어깨 견갑 (금 테·리벳)
    d.ellipse([30, 26, 38, 33], AM)
    d.line([(31, 27), (36, 27)], AH); d.line([(32, 28), (36, 28)], AL)
    d.line([(31, 32), (37, 32)], GM); d.point((37, 31), GL); d.point((34, 30), GL)
    # 앞팔·건틀릿
    d.line([(36, 33), (36, 37)], AD); d.line([(37, 34), (37, 37)], AL)
    d.rectangle([36, 38, 38, 40], AM); d.point((38, 38), AL)
    # 장검: 손잡이 위, 칼날 앞-아래
    d.point((37, 36), GL)
    d.line([(35, 41), (40, 41)], GM); d.point((37, 41), GL)
    d.line([(38, 42), (42, 57)], AH); d.line([(39, 42), (43, 56)], AL)
    return outline(im)


# ---------------------------------------------------------------- 출력
spr = {'cecilia': cecilia(), 'adela': adela()}
for k, v in spr.items():
    v.save(OUT / f'{k}_idle_native.png')

Z = 8
sheet = Image.new('RGBA', (S * Z * 2 + 48, S * Z + 32), (36, 32, 40, 255))
for i, v in enumerate(spr.values()):
    sheet.alpha_composite(v.resize((S * Z, S * Z), Image.NEAREST), (16 + i * (S * Z + 16), 16))
    ImageDraw.Draw(sheet).line([(16 + i * (S * Z + 16), 16 + BASE * Z + Z), (16 + i * (S * Z + 16) + S * Z, 16 + BASE * Z + Z)], (90, 70, 60, 255))
sheet.save(OUT / 'characters_v1_sheet.png')

# 로비 배경(640x360, 걷는 선 y=310)에 원 크기로 올려 비율 확인, x3 미리보기
for k, v in spr.items():
    bg = Image.open(ROOT / f'Docs/Design/Lobby/v6/lobby_v6_{k}_bg_native.png').convert('RGBA')
    bg.alpha_composite(v, (300 - 32, 310 - BASE - 1))
    bg.alpha_composite(spr['adela' if k == 'cecilia' else 'cecilia'].transpose(Image.FLIP_LEFT_RIGHT), (380 - 32, 310 - BASE - 1))
    bg.resize((1920, 1080), Image.NEAREST).save(OUT / f'in_lobby_{k}.png')
print('ok', OUT)
