# 로비(거점) 도트 시안 생성기. 원본 480x270을 그린 뒤 x4(1920x1080)로 최근접 확대한다.
# 실행: python Docs/Design/Lobby/make_lobby.py
import math, random
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont
import numpy as np

W, H, FLOOR = 480, 270, 212
OUT = Path(__file__).parent
rng = random.Random(7)

P = dict(
    void='#0a0910', mortar='#0e0c15', wall1='#1e1a2b', wall2='#262134', wall3='#302a42',
    stone0='#2c2738', stone1='#3b3449', stone2='#4d4560', stone3='#625879',
    floor0='#120f19', floor1='#1d1928', floor2='#262132', floorhi='#3a3350',
    moon='#a9cdf2', moonhi='#e2f1ff', glass1='#16305a', glass2='#25508a', glass3='#3f76b8', glass4='#80b2e6',
    crim='#7d1626', crimhi='#b8303f', gold='#d9a840', goldd='#8c6420', goldhi='#f4d27a',
    flame='#ffd36e', flamehi='#fff4c8', ember='#ff8a2a', wax='#e6dcc4', waxd='#a99e86',
    wood0='#24150f', wood1='#3d2419', wood2='#573626', wood3='#764a31',
    parch='#cbb48a', parchd='#9c8660', ink='#5a2a20',
    steel0='#3d4152', steel1='#6c7288', steel2='#a1a8bd', steel3='#d6dbea', iron='#211f29',
    cloth='#6e1320', clothd='#470b15',
    ui_bg='#0d0b14', ui_txt='#eadfc4', ui_dim='#8f8699', hp='#c23a3a', mind='#7a5cc8',
)
C = {k: tuple(int(v[i:i + 2], 16) for i in (1, 3, 5)) + (255,) for k, v in P.items()}

img = Image.new('RGBA', (W, H), C['void'])
d = ImageDraw.Draw(img)


def rect(x0, y0, x1, y1, c):
    d.rectangle([x0, y0, x1, y1], fill=C[c])


def px(x, y, c):
    d.point((x, y), fill=C[c])


def glow(cx, cy, r, color, alpha, rings=6):
    """계단식 원형 빛. 링 수만큼 띠가 생겨 도트 느낌을 유지한다."""
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ld = ImageDraw.Draw(layer)
    for i in range(rings):
        rr = r * (1 - i / rings)
        ld.ellipse([cx - rr, cy - rr * 0.85, cx + rr, cy + rr * 0.85], fill=C[color][:3] + (alpha,))
    img.alpha_composite(layer)


def poly_light(points, color, alpha):
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(layer).polygon(points, fill=C[color][:3] + (alpha,))
    img.alpha_composite(layer)


def flame(x, y, big=False):
    """x,y = 심지 위치."""
    px(x, y - 1, 'ember'); px(x, y - 2, 'flame'); px(x, y - 3, 'flamehi')
    if big:
        px(x - 1, y - 2, 'ember'); px(x + 1, y - 2, 'ember'); px(x, y - 4, 'flame'); px(x, y - 5, 'flamehi')


def candle(x, top, h):
    rect(x - 1, top, x, top + h, 'wax'); px(x, top + 1, 'waxd'); px(x, top + h, 'waxd')
    flame(x, top)


# ---------- 벽 ----------
rect(0, 0, W, FLOOR, 'mortar')
for row, y in enumerate(range(0, FLOOR, 8)):
    off = 8 if row % 2 else 0
    for x in range(-16 + off, W, 16):
        v = rng.choice(['wall1', 'wall1', 'wall2', 'wall2', 'wall3'])
        rect(x + 1, y + 1, x + 15, y + 7, v)
        if rng.random() < 0.45:
            d.line([x + 1, y + 1, x + 14, y + 1], fill=C['wall3' if v != 'wall3' else 'stone1'])
        if rng.random() < 0.07:
            px(x + rng.randint(3, 12), y + rng.randint(3, 6), 'mortar')


# ---------- 천장 아치 ----------
def arch_y(x, x0, x1, spring, apex):
    hw = (x1 - x0) / 2
    t = min(1.0, abs(x - (x0 + x1) / 2) / hw)
    return spring - math.sqrt(max(0.0, 1 - t)) * (spring - apex)


bays = [(-120, 120, 46, 14), (120, 360, 46, 4), (360, 600, 46, 14)]
for x in range(W):
    for x0, x1, sp, ap in bays:
        if x0 <= x <= x1:
            y = int(arch_y(x, x0, x1, sp, ap))
            rect(x, 0, x, y - 1, 'void')
            rect(x, y, x, y + 2, 'stone1'); px(x, y, 'stone2'); px(x, y + 3, 'stone0')

# ---------- 기둥 ----------
for cx in (120, 360):
    rect(cx - 8, 0, cx + 8, FLOOR, 'stone1')
    rect(cx - 8, 0, cx - 7, FLOOR, 'stone2')
    rect(cx + 5, 0, cx + 8, FLOOR, 'stone0')
    for gx in (cx - 3, cx + 2):
        d.line([gx, 50, gx, FLOOR - 12], fill=C['stone0'])
    rect(cx - 12, 40, cx + 12, 48, 'stone2'); d.line([cx - 12, 40, cx + 12, 40], fill=C['stone3'])
    rect(cx - 12, 47, cx + 12, 48, 'stone0')
    rect(cx - 12, FLOOR - 10, cx + 12, FLOOR, 'stone2'); d.line([cx - 12, FLOOR - 10, cx + 12, FLOOR - 10], fill=C['stone3'])

# ---------- 스테인드글라스 창 ----------
WX0, WX1, WSP, WAP, WBOT = 206, 274, 74, 40, 158
inside = lambda x, y, m=0: WX0 + m <= x <= WX1 - m and arch_y(x, WX0 + m, WX1 - m, WSP, WAP + m) <= y <= WBOT - m
moon_x, moon_y = 252, 70
for y in range(WAP - 2, WBOT + 1):
    for x in range(WX0, WX1 + 1):
        if not inside(x, y):
            continue
        if not inside(x, y, 3):
            px(x, y, 'stone3' if x < 240 else 'stone2'); continue
        dist = math.hypot(x - moon_x, (y - moon_y) * 0.8)
        cross = (abs(x - 240) <= 3 and 80 <= y <= 150) or (104 <= y <= 110 and abs(x - 240) <= 16)
        cross_edge = (abs(x - 240) <= 4 and 79 <= y <= 151) or (103 <= y <= 111 and abs(x - 240) <= 17)
        rose = math.hypot(x - 240, y - 62)
        if cross:
            c = 'crimhi' if (x + y) % 7 else 'crim'
        elif cross_edge:
            c = 'gold'
        elif 9.5 <= rose <= 11:
            c = 'gold'
        elif rose < 9.5:
            ang = (math.degrees(math.atan2(y - 62, x - 240)) + 360) % 360
            c = 'goldhi' if rose < 2.5 else ('crimhi' if int(ang // 45) % 2 else 'glass3')
        else:
            band = dist + ((x + y) % 2) * 3  # 체커 디더로 경계를 섞는다
            c = 'glass4' if band < 14 else 'glass3' if band < 30 else 'glass2' if band < 52 else 'glass1'
            if (x + y) % 9 == 0 or (x - y) % 9 == 0:
                c = {'glass4': 'glass3', 'glass3': 'glass2', 'glass2': 'glass1', 'glass1': 'iron'}[c]
        if x in (223, 257) or y in (96, 128):
            if not cross and rose > 11:
                c = 'iron'
        px(x, y, c)
rect(WX0 - 6, WBOT + 1, WX1 + 6, WBOT + 5, 'stone2'); d.line([WX0 - 6, WBOT + 1, WX1 + 6, WBOT + 1], fill=C['stone3'])
rect(WX0 - 6, WBOT + 5, WX1 + 6, WBOT + 6, 'stone0')

# ---------- 바닥 ----------
rect(0, FLOOR, W, H, 'floor1')
d.line([0, FLOOR, W, FLOOR], fill=C['floorhi']); d.line([0, FLOOR + 1, W, FLOOR + 1], fill=C['floor2'])
for i, (y0, y1) in enumerate([(FLOOR + 2, 226), (227, 244), (245, 270)]):
    d.line([0, y1 + 1, W, y1 + 1], fill=C['floor0'])
    step = 28 + i * 10
    for x in range((i * 13) % step, W, step):
        d.line([x, y0, x, y1], fill=C['floor0'])
        d.line([x + 1, y0, x + 1, y0 + 1], fill=C['floor2'])

# ---------- 달빛 ----------
poly_light([(210, 76), (270, 76), (236, FLOOR), (146, FLOOR)], 'moon', 22)
poly_light([(226, 100), (256, 100), (222, FLOOR), (176, FLOOR)], 'moon', 18)
layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
ImageDraw.Draw(layer).ellipse([140, FLOOR - 1, 240, FLOOR + 14], fill=C['moon'][:3] + (34,))
ImageDraw.Draw(layer).ellipse([166, FLOOR, 222, FLOOR + 8], fill=C['moonhi'][:3] + (30,))
img.alpha_composite(layer)
for _ in range(34):  # 먼지
    y = rng.randint(90, FLOOR - 4)
    t = (y - 76) / (FLOOR - 76)
    x = rng.randint(int(210 + (146 - 210) * t), int(270 + (236 - 270) * t))
    a = rng.choice([90, 140, 200])
    img.putpixel((x, y), C['moonhi'][:3] + (a,)) if a < 200 else px(x, y, 'moonhi')

# ---------- 왼쪽: 제단과 성호 ----------
glow(64, 120, 34, 'gold', 7)
rect(62, 90, 65, 152, 'gold'); rect(52, 105, 75, 108, 'gold')
rect(65, 90, 65, 152, 'goldd'); rect(52, 108, 75, 108, 'goldd'); rect(62, 90, 62, 152, 'goldhi'); rect(52, 105, 75, 105, 'goldhi')
rect(60, 88, 67, 90, 'goldhi'); rect(60, 150, 67, 153, 'goldd')
rect(36, 185, 92, FLOOR - 1, 'stone1'); rect(36, 185, 37, FLOOR - 1, 'stone2'); rect(89, 185, 92, FLOOR - 1, 'stone0')
rect(32, 180, 96, 185, 'stone2'); d.line([32, 180, 96, 180], fill=C['stone3']); d.line([32, 185, 96, 185], fill=C['stone0'])
rect(50, 186, 78, 206, 'cloth'); rect(50, 186, 51, 206, 'clothd'); rect(77, 186, 78, 206, 'clothd')
d.line([50, 206, 78, 206], fill=C['gold']); d.line([50, 186, 78, 186], fill=C['gold'])
rect(63, 190, 64, 201, 'gold'); rect(60, 193, 67, 194, 'gold')
for x, h in ((38, 9), (44, 5), (84, 6), (90, 10)):
    candle(x, 180 - h, h - 1)
    glow(x, 178 - h, 9, 'flame', 22, 3)
# 바닥 촛대
rect(17, 168, 18, FLOOR - 1, 'iron'); rect(13, 208, 22, FLOOR - 1, 'iron'); rect(11, 166, 24, 167, 'iron')
for x in (12, 18, 23):
    candle(x, 160, 5)
glow(18, 158, 16, 'flame', 18, 3)

# ---------- 기둥 횃대 ----------
for cx in (120, 360):
    rect(cx - 2, 122, cx + 2, 124, 'iron'); rect(cx - 3, 119, cx + 3, 121, 'iron'); px(cx, 125, 'iron')
    flame(cx, 119, big=True); flame(cx - 1, 119)
    glow(cx, 116, 22, 'ember', 9)

# ---------- 중앙: 지도 탁자 ----------
rect(240, 192, 243, FLOOR - 1, 'wood1'); rect(289, 192, 292, FLOOR - 1, 'wood1')
rect(240, 192, 240, FLOOR - 1, 'wood2'); rect(289, 192, 289, FLOOR - 1, 'wood2')
rect(243, 203, 289, 204, 'wood0')
rect(236, 183, 296, 189, 'wood2'); d.line([236, 183, 296, 183], fill=C['wood3'])
rect(236, 189, 296, 193, 'wood1'); d.line([236, 193, 296, 193], fill=C['wood0'])
rect(248, 182, 284, 188, 'parch'); d.line([248, 188, 284, 188], fill=C['parchd'])
rect(248, 189, 252, 194, 'parch'); rect(280, 189, 284, 192, 'parchd')  # 앞으로 늘어진 지도 끝
for x, y in ((254, 184), (258, 185), (262, 184), (266, 186), (270, 185)):
    px(x, y, 'ink')
px(274, 184, 'crimhi'); px(275, 185, 'crimhi'); px(275, 184, 'crimhi'); px(274, 185, 'crimhi')  # 목표 표식
rect(241, 184, 246, 187, 'parchd'); rect(241, 184, 241, 187, 'parch')  # 말린 두루마리
candle(291, 175, 7)
glow(291, 172, 20, 'flame', 20)

# ---------- 오른쪽: 무기 거치대 ----------
rect(376, 162, 378, FLOOR - 1, 'wood1'); rect(416, 162, 418, FLOOR - 1, 'wood1')
rect(376, 162, 376, FLOOR - 1, 'wood2'); rect(416, 162, 416, FLOOR - 1, 'wood2')
rect(374, 166, 420, 168, 'wood2'); d.line([374, 166, 420, 166], fill=C['wood3'])
rect(376, 198, 418, 200, 'wood1')
# 장검 (칼끝 아래)
rect(388, 147, 390, 149, 'gold'); rect(389, 150, 389, 157, 'wood1'); rect(388, 150, 388, 157, 'wood2')
rect(383, 158, 395, 159, 'gold'); px(383, 159, 'goldd'); px(395, 159, 'goldd')
rect(388, 160, 390, 196, 'steel2'); rect(389, 160, 389, 194, 'steel3'); rect(390, 160, 390, 196, 'steel1')
px(389, 197, 'steel2')
# 연 방패
shield = [(397, 170), (413, 170), (413, 184), (405, 198), (397, 184)]
d.polygon(shield, fill=C['steel0'], outline=C['gold'])
rect(404, 173, 406, 192, 'crim'); rect(400, 178, 410, 180, 'crim'); px(405, 179, 'crimhi')
d.line([398, 171, 398, 183], fill=C['steel1'])
# 성유물 사슬 (오른쪽 기둥 고리에 걸림)
px(420, 165, 'iron'); px(421, 166, 'iron')
for i, y in enumerate(range(168, 196, 2)):
    xx = 421 + (1 if 4 < i < 10 else 0)
    px(xx, y, 'steel2' if i % 2 else 'steel1'); px(xx, y + 1, 'steel0')
rect(421, 197, 421, 202, 'gold'); rect(419, 198, 423, 198, 'gold')

# ---------- 오른쪽 끝: 훈련장 문 ----------
DX0, DX1, DTOP = 434, 470, 150
for y in range(DTOP - 4, FLOOR):
    for x in range(DX0 - 3, DX1 + 4):
        r = math.hypot(x - 452, y - 168)
        inner = DX0 <= x <= DX1 and (y >= 168 or r <= 18)
        outer = DX0 - 3 <= x <= DX1 + 3 and (y >= 168 or r <= 21)
        if inner:
            c = 'wood1' if (x - DX0) % 6 == 0 else ('wood2' if (x - DX0) % 6 < 3 else 'wood3')
            if y in (176, 177, 200, 201):
                c = 'iron'
            px(x, y, c)
        elif outer:
            px(x, y, 'stone3' if x < 452 else 'stone2')
d.ellipse([438, 187, 442, 191], outline=C['goldd'])
poly_light([(434, FLOOR - 1), (470, FLOOR - 1), (476, FLOOR + 3), (428, FLOOR + 3)], 'ember', 40)
d.line([435, FLOOR - 1, 469, FLOOR - 1], fill=C['ember'])
rect(424, 140, 425, 146, 'iron'); rect(422, 138, 427, 139, 'iron')
flame(424, 138, big=True)
glow(424, 134, 18, 'ember', 10)

# ---------- 캐릭터 (승인 이미지 색만 따른 로비용 시안, 최종 스프라이트 아님) ----------
SPR_C = {  # 세실리아: 백금발, 흰 베일·금 장식, 파란 눈, 흰 수녀복
    'o': '#17121f', 'W': '#ece9f4', 'w': '#a8a3c2', 'G': '#d9a840', 'H': '#eadcb8', 'h': '#b29e74',
    'S': '#f2cbb4', 's': '#c99580', 'E': '#3f78c8', 'D': '#e0dcea', 'd': '#9c97b4', 'K': '#2b2538',
}
CECILIA = """\
.....oooooo.....
....oWWWWWWo....
...oWWWWWWWWo...
...oWGGGGGGGo...
..oWwHHHHHHHHo..
..oWHHHHHHSSSo..
..oWHHHHHSSESo..
..oWHhHHHSSSSo..
..oWHhHHHHSsSo..
..oWHhHHHhSSo...
..oWHhHHhoSSo...
.oWwHhHoKGGKo...
.oWwHhoDDDDDDo..
.oWwHhoDDGDDDDo.
.oWwHoDDGGGDDSo.
.oWwHoDDDGDDDSo.
.oWwhoDdDGDDdSo.
.oWwhoDdDGDDdso.
.oWwoDDdDGDDdo..
.oWwoDDdDDDDdo..
..oooDDGGGGGdo..
....oDDdDDDDdo..
....oDDdDDDDDdo.
...oDDdDDDDDDdo.
...oDDdDDDDDDdo.
...oDdDDDDDDDdo.
...oDdDDDDDDDDdo
..oDDdDDDDDDDDdo
..oDDdDDDDDDDDdo
..oDdDDDDDDDDDdo
..oDdDDDDDDDDDdo
..oDdDDDDDDDDDdo
.oDDdDDDDDDDDDdo
.oDDdDDDDDDDDDdo
.oGGGGGGGGGGGGGo
..oKKKo..oKKKo..
..oKKo....oKKKo.
..ooo.....oooo.."""

SPR_A = {  # 아델라: 진홍 포니테일, 호박색 눈, 어두운 강철 갑옷·금 장식, 붉은 망토
    'o': '#17121f', 'R': '#a8203a', 'r': '#6a1228', 'S': '#efc2a8', 's': '#c48a74', 'Y': '#e0a030',
    'A': '#6c7288', 'a': '#3d4152', 'L': '#b4bacc', 'G': '#d9a840', 'g': '#8c6420',
    'M': '#8a1a2a', 'm': '#520e1a', 'B': '#221d2c',
}
ADELA = """\
......oooooo......
.....oRRRRRRo.....
....oRRRRRRRRo....
...oRRRRRRRRRRo...
...oRRRRRRRRrRo...
...oSSRRRRRrRRRoo.
..oSYSSRRRRrRRrRRo
..oSSSSSRRrRRo.oRo
...oSsSSRrRRo..oRo
...oSSSsoRRo...oRo
....osSSoRo...oRro
...ooBBBBoo...oro.
..oLAAAAAAAMo.oro.
.oLLAAGAAAAAMMo...
.oLAAGGGAAAAMMMo..
.oLAAAGAAAAaMMMo..
.oLAAAGAAAAaMMMo..
.oSoAAGAAAAaMMMMo.
.oSoAAAAAAAaMMMMo.
..ooBBBBBBBBmMMMo.
...oGgGGGGGgmMMMo.
...oAAAaAAAAamMMMo
...oAAAaAAAAamMMMo
...oaAAaoAAAamMMMo
...oaAAo.oAAamMMMo
...oAAAo.oAAAomMMo
...oLAAo.oLAAomMMo
...oAAAo.oAAAo.mMo
...oAAAo.oAAAo.mMo
...oaAAo.oaAAo.mmo
...oAAAo.oAAAo..o.
...oLAAo.oLAAo....
...oAAAo.oAAAo....
...oAAAo.oAAAo....
...oaAAo.oaAAo....
...oAAAo.oAAAo....
..oaaaao.oaaaao...
..ooooo..ooooo...."""


def sprite(text, pal, x, y):
    rows = text.splitlines()
    assert len({len(r) for r in rows}) == 1, [len(r) for r in rows]
    for j, row in enumerate(rows):
        for i, ch in enumerate(row):
            if ch != '.':
                c = pal[ch]
                d.point((x + i, y + j), fill=tuple(int(c[k:k + 2], 16) for k in (1, 3, 5)) + (255,))
    return len(rows[0]), len(rows)


def shadow(x0, x1):
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(layer).ellipse([x0, FLOOR - 2, x1, FLOOR + 2], fill=(0, 0, 0, 120))
    img.alpha_composite(layer)


ch = len(CECILIA.splitlines())
shadow(207, 225)
sprite(CECILIA, SPR_C, 209, FLOOR - ch)
# 손에 늘어뜨린 사슬
for i, y in enumerate(range(FLOOR - ch + 18, FLOOR - ch + 30)):
    px(223 + (i // 6), y, 'steel2' if i % 2 else 'steel0')
px(224, FLOOR - ch + 30, 'gold'); px(223, FLOOR - ch + 31, 'gold'); px(225, FLOOR - ch + 31, 'gold'); px(224, FLOOR - ch + 32, 'gold')

ah = len(ADELA.splitlines())
shadow(305, 325)
sprite(ADELA, SPR_A, 306, FLOOR - ah)
# 허리의 칼집
rect(318, FLOOR - ah + 21, 319, FLOOR - ah + 34, 'iron'); px(318, FLOOR - ah + 20, 'gold'); px(319, FLOOR - ah + 20, 'gold')

# ---------- 비네트 (계단식) ----------
arr = np.asarray(img).astype(np.float32)
yy, xx = np.mgrid[0:H, 0:W]
r = np.hypot((xx - 240) / 260, (yy - 150) / 170)
f = 1 - 0.6 * np.clip((r - 0.5) / 0.6, 0, 1)
f = np.round(f / 0.1) * 0.1
arr[..., :3] *= f[..., None]
img = Image.fromarray(arr.clip(0, 255).astype(np.uint8), 'RGBA')
d = ImageDraw.Draw(img)
scene = img.copy()

# ---------- UI ----------
d.fontmode = '1'
F = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 11)
FS = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 10)


def panel(x0, y0, x1, y1):
    layer = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(layer).rectangle([x0, y0, x1, y1], fill=C['ui_bg'][:3] + (215,))
    img.alpha_composite(layer)
    d.rectangle([x0, y0, x1, y1], outline=C['goldd'])
    for cx, cy in ((x0, y0), (x1, y0), (x0, y1), (x1, y1)):
        px(cx, cy, 'gold')


def key(x, y, label, font=FS):
    w = int(d.textlength(label, font=font)) + 5
    d.rectangle([x, y, x + w, y + 11], fill=C['ui_txt'])
    d.text((x + 3, y - 1), label, font=font, fill=C['ui_bg'])
    return x + w


panel(6, 6, 134, 23)
d.text((12, 6), '거점 · 폐허 예배당', font=F, fill=C['ui_txt'])
px(10, 14, 'gold')

panel(330, 6, 474, 45)
for i, (name, hp, mind, active) in enumerate((('세실리아', 1.0, 0.85, True), ('아델라', 1.0, 0.6, False))):
    y = 9 + i * 18
    d.text((336, y), ('▶' if active else ' ') + name, font=F, fill=C['ui_txt' if active else 'ui_dim'])
    rect(398, y + 4, 468, y + 7, 'iron'); rect(398, y + 4, 398 + int(70 * hp), y + 7, 'hp')
    rect(398, y + 10, 468, y + 11, 'iron'); rect(398, y + 10, 398 + int(70 * mind), y + 11, 'mind')

# 상호작용 말풍선 (지도 탁자)
panel(244, 154, 288, 169)
x = key(248, 156, 'W', F)
d.text((x + 4, 154), '출정', font=F, fill=C['ui_txt'])
d.polygon([(263, 170), (269, 170), (266, 173)], fill=C['goldd'])

# 하단 조작 안내
panel(-1, 254, W, H)
x = 8
for k, label in (('A/D', '이동'), ('Space', '점프'), ('W', '상호작용'), ('Tab', '전환'), ('Esc', '메뉴')):
    x = key(x, 257, k)
    d.text((x + 4, 255), label, font=FS, fill=C['ui_txt'])
    x += int(d.textlength(label, font=FS)) + 16

img.save(OUT / 'lobby_concept_v1_native.png')
big = img.resize((W * 4, H * 4), Image.NEAREST)
big.save(OUT / 'lobby_concept_v1.png')
scene.resize((W * 4, H * 4), Image.NEAREST).save(OUT / 'lobby_concept_v1_noUI.png')

# ---------- 설명용 번호 표기본 ----------
spots = [
    (64, 168, '제단 — 기도: 저장 · HP 회복 (Day 20~21 연결)'),
    (266, 150, '지도 탁자 — 출정: 스테이지 선택 / 월드맵'),
    (397, 142, '무기 거치대 — 무장: 캐릭터 정보 · 장비 (후속)'),
    (452, 128, '문 — 훈련장: 현재 Day 5 전투 씬으로 이동'),
    (402, 52, '상태 패널 — HP(빨강) / 정신(보라) 분리 표시'),
    (240, 248, '조작 안내 — 현재 입력 키 기준, 상호작용 W는 제안'),
]
LEG = 300
ann = Image.new('RGBA', (W * 4, H * 4 + LEG), C['ui_bg'])
ann.paste(big, (0, 0))
ad = ImageDraw.Draw(ann)
FA = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 30)
FL = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 30)
for n, (x, y, text) in enumerate(spots, 1):
    cx, cy = x * 4, y * 4
    ad.ellipse([cx - 22, cy - 22, cx + 22, cy + 22], fill=C['gold'], outline=C['ui_bg'], width=4)
    ad.text((cx, cy), str(n), font=FA, fill=C['ui_bg'], anchor='mm')
    col, row = (n - 1) // 3, (n - 1) % 3
    lx, ly = 50 + col * 940, H * 4 + 40 + row * 80
    ad.ellipse([lx, ly, lx + 44, ly + 44], fill=C['gold'])
    ad.text((lx + 22, ly + 22), str(n), font=FA, fill=C['ui_bg'], anchor='mm')
    ad.text((lx + 64, ly + 22), text, font=FL, fill=C['ui_txt'], anchor='lm')
ann.save(OUT / 'lobby_concept_v1_annotated.png')
print('ok', ch, ah)
