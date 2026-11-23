# 로비(거점) v3 — 다크 고딕 도트: 3/4 원근 실내, 단일 창 빛, 저채도, 거친 질감, 짙은 어둠.
# 칼 받침대 = 캐릭터 전환 오브젝트 (세실리아: 사슬이 은빛 / 아델라: 칼날이 금빛).
# 원본 640x360, 미리보기 x3. 실행: python Docs/Design/Lobby/make_lobby_v3.py
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H = 640, 360
OUT = Path(__file__).parent / 'v3'
(OUT / 'props').mkdir(parents=True, exist_ok=True)
VPX, VPY, F, ZB = 380.0, 150.0, 1480.0, 10.0
HC = (VPY - 40) * ZB / F    # 천장 높이
HF = (252 - VPY) * ZB / F   # 눈높이 아래 바닥
XL = (VPX - 232) * ZB / F   # 왼벽
XR = (520 - VPX) * ZB / F   # 오른벽
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)


# ---------------------------------------------------------------- 노이즈·도구
def vnoise(scale, seed):
    r = np.random.RandomState(seed)
    gh, gw = int(H / scale) + 3, int(W / scale) + 3
    im = Image.fromarray((r.rand(gh, gw) * 255).astype(np.uint8)).resize((int(gw * scale), int(gh * scale)), Image.BICUBIC)
    return np.asarray(im, np.float32)[:H, :W] / 255


def fbm(seed, scales=(80, 30, 10, 3), weights=(0.4, 0.3, 0.18, 0.12)):
    return sum(w * vnoise(s, seed * 10 + i) for i, (s, w) in enumerate(zip(scales, weights)))


def hash2(a, b):
    return np.modf(np.abs(np.sin(a * 12.9898 + b * 78.233) * 43758.5453))[0]


def poly(pts):
    m = Image.new('L', (W, H), 0)
    ImageDraw.Draw(m).polygon(pts, fill=255)
    return np.asarray(m) > 0


def smooth(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def blocks(u, v, bw, bh, mort, seed):
    row = np.floor(v / bh)
    off = hash2(row, seed) * bw
    col = np.floor((u + off) / bw)
    fu, fv = (u + off) / bw - col, v / bh - row
    tone = 0.72 + 0.4 * hash2(row * 3.1 + seed, col * 1.7 + seed)
    edge = np.minimum(np.minimum(fu, 1 - fu) * bw, np.minimum(fv, 1 - fv) * bh)
    return tone, edge < mort, np.clip(edge / (mort * 4), 0, 1), fv


N1, N2, N3, N4 = fbm(1), fbm(2), fbm(3), fbm(4, (6, 2), (0.6, 0.4))
GRAIN = vnoise(1.5, 99)

# ---------------------------------------------------------------- 공간 (벽·바닥·천장)
alb = np.zeros((H, W, 3), np.float32)
back = (xx >= 232) & (xx <= 520) & (yy >= 40) & (yy <= 252)
left = (xx < 232) & ~back
right = (xx > 520) & ~back
floor_m = (yy > 252) & poly([(232, 252), (520, 252), (700, 384), (700, 400), (-60, 400), (-60, 446)])
ceil_m = (yy < 40) | poly([(-80, -190), (232, 40), (520, 40), (700, -100)])
left &= ~floor_m & ~ceil_m
right &= ~floor_m & ~ceil_m

STONE = np.array([1.0, 0.97, 1.03]) * 0.56
FLOORC = np.array([1.0, 0.96, 0.92]) * 0.52
WOOD = np.array([1.0, 0.82, 0.66]) * 0.42
warp = (N1 - 0.5) * 0.03


def wall_value(u, v, bw, bh, mort, seed):
    tone, mortar, bevel, fv = blocks(u + warp, v + warp * 0.6, bw, bh, mort, seed)
    val = tone * (0.55 + 0.45 * bevel) * (0.7 + 0.6 * N2) * (0.85 + 0.3 * GRAIN)
    val = tone * (0.75 + 0.25 * bevel) * (0.7 + 0.6 * N2) * (0.85 + 0.3 * GRAIN)
    val = np.where(mortar, 0.3 + 0.12 * N3, val)
    val *= np.where(fv < 0.18, 1.08, 1.0)
    # 회반죽이 남은 곳과 떨어져 돌이 드러난 곳
    pm = N3 + 0.2 * N4
    plaster = (0.62 + 0.3 * N1 + 0.12 * (GRAIN - 0.5)) * (1 - 0.35 * smooth(0.3, 0.7, N2))
    val = np.where(pm < 0.47, plaster, val)
    val = np.where((pm >= 0.47) & (pm < 0.49), val * 0.55, val)
    val *= 1 - 0.4 * smooth(0.6, 0.85, N4)           # 얼룩
    return val


# 뒷벽
u, v = (xx - VPX) * ZB / F, (yy - VPY) * ZB / F
val = wall_value(u, v, 0.15, 0.072, 0.007, 1)
val *= 1 - 0.35 * smooth(0.25, 0.7, v)               # 아래쪽 습기
alb[back] = (val[..., None] * STONE)[back]
# 옆벽
for m, sign, xw in ((left, -1, XL), (right, 1, XR)):
    z = F * xw / np.maximum(1e-3, sign * (xx - VPX))
    vv = (yy - VPY) * z / F
    val = wall_value(z, vv, 0.32, 0.072, 0.009, 2 if sign < 0 else 3)
    val *= 1 - 0.35 * smooth(0.25, 0.7, vv)
    alb[m] = (val[..., None] * STONE * 0.9)[m]
# 바닥 (닳은 판석)
z = F * HF / np.maximum(1e-3, yy - VPY)
fu = (xx - VPX) * z / F
tone, mortar, bevel, _ = blocks(fu + warp, z + warp * 3, 0.36, 0.5, 0.012, 5)
val = tone * (0.6 + 0.4 * bevel) * (0.65 + 0.7 * N2) * (0.85 + 0.3 * GRAIN)
val = np.where(mortar, 0.27 + 0.1 * N3, val) * (1 - 0.4 * smooth(0.6, 0.85, N4))
alb[floor_m] = (val[..., None] * FLOORC)[floor_m]
# 천장 (나무 판재)
z = F * HC / np.maximum(1e-3, VPY - yy)
cu = (xx - VPX) * z / F
pl = np.modf(np.abs(cu) / 0.11)[0]
val = (0.5 + 0.3 * hash2(np.floor(cu / 0.11), 7)) * (0.7 + 0.5 * N1) * np.where(pl < 0.08, 0.4, 1)
alb[ceil_m] = (val[..., None] * WOOD * 0.7)[ceil_m]

# ---------------------------------------------------------------- 소품 그리기 도구
EMI = Image.new('RGBA', (W, H), (0, 0, 0, 0))   # 발광 (빛 계산 안 함)
PROP = Image.new('RGBA', (W, H), (0, 0, 0, 0))  # 고유색 소품
FG = Image.new('RGBA', (W, H), (0, 0, 0, 0))    # 전경 실루엣
MASKS = {}


def c(r, g, b, a=1.0):
    return (int(r * 255), int(g * 255), int(b * 255), int(a * 255))


class Pen:
    def __init__(s, im, name=None):
        s.im, s.g, s.name = im, ImageDraw.Draw(im), name
        s.m = Image.new('L', (W, H), 0) if name else None
        s.mg = ImageDraw.Draw(s.m) if name else None

    def poly(s, pts, col):
        s.g.polygon(pts, fill=col)
        if s.mg: s.mg.polygon(pts, fill=255)

    def rect(s, x0, y0, x1, y1, col):
        s.g.rectangle([x0, y0, x1, y1], fill=col)
        if s.mg: s.mg.rectangle([x0, y0, x1, y1], fill=255)

    def line(s, pts, col, w=1):
        s.g.line(pts, fill=col, width=w)
        if s.mg: s.mg.line(pts, fill=255, width=w)

    def pt(s, x, y, col):
        s.g.point((x, y), fill=col)
        if s.mg: s.mg.point((x, y), fill=255)

    def ell(s, box, col, outline=None):
        s.g.ellipse(box, fill=col, outline=outline)
        if s.mg: s.mg.ellipse(box, fill=255)

    def done(s):
        if s.name:
            MASKS[s.name] = MASKS.get(s.name, Image.new('L', (W, H), 0))
            MASKS[s.name].paste(255, (0, 0), s.m)


E = Pen(EMI)
LIGHTS = []
COOL, WARM = np.array([0.78, 0.88, 1.0]), np.array([1.0, 0.66, 0.36])
S_ST, S_STL, S_STD = c(.5, .49, .53), c(.66, .65, .69), c(.28, .27, .3)
WD, WDL, WDD = c(.36, .27, .2), c(.5, .38, .27), c(.2, .14, .1)
IRON, IRONL = c(.16, .16, .19), c(.32, .32, .36)
CRIM, CRIMD = c(.4, .14, .15), c(.24, .08, .09)
GOLD, GOLDL = c(.68, .52, .24), c(.88, .74, .42)


def flame(x, y, big=False, pen=None):
    pts = [(x, y - 1, c(1, .55, .2)), (x, y - 2, c(1, .8, .45)), (x, y - 3, c(1, .96, .82))]
    if big:
        pts += [(x, y - 4, c(1, .9, .6)), (x - 1, y - 2, c(1, .6, .25)), (x + 1, y - 2, c(1, .6, .25))]
    for px_, py_, col in pts:
        E.pt(px_, py_, col)
        if pen: pen.pt(px_, py_, col)


def candle(pen, x, top, h, w=2):
    pen.rect(x, top, x + w - 1, top + h, c(.78, .74, .66))
    pen.rect(x + w - 1, top, x + w - 1, top + h, c(.55, .5, .44))
    flame(x + (w - 1) // 2, top, h > 6, pen)


# ---------------------------------------------------------------- 뾰족 창 (뒷벽 왼쪽)
def arch_y(x, x0, x1, spring, apex):
    t = min(1.0, abs(x - (x0 + x1) / 2) / ((x1 - x0) / 2))
    return spring - math.sqrt(max(0.0, 1 - t)) * (spring - apex)


B = Pen(PROP, 'window')
WX0, WX1, WSP, WAP, WBOT = 260, 320, 122, 74, 210
rs = np.random.RandomState(8)
missing = {(0, 9), (5, 14), (6, 15), (2, 5), (4, 3)}
for y in range(WAP - 6, WBOT + 6):
    for x in range(WX0 - 6, WX1 + 7):
        out = x >= WX0 - 6 and x <= WX1 + 6 and y >= arch_y(x, WX0 - 6, WX1 + 6, WSP, WAP - 6) and y <= WBOT + 5
        inn = WX0 <= x <= WX1 and y >= arch_y(x, WX0, WX1, WSP, WAP) and y <= WBOT
        if not out:
            continue
        if not inn:
            B.pt(x, y, S_STL if (x < WX0 - 2 or y > WBOT + 2) else S_ST)
            continue
        if x < WX0 + 5:                     # 안쪽 측면 (깊이)
            B.pt(x, y, S_STD); continue
        a, b = (x - WX0) // 8, (y - WAP) // 8
        lead = (x - WX0) % 8 == 0 or (y - WAP) % 8 == 0 or x in (290,)
        if lead:
            E.pt(x, y, c(.05, .05, .06))
        elif (a, b) in missing:
            E.pt(x, y, c(.05, .07, .1))
        else:
            k = 0.6 + 0.4 * ((a * 7 + b * 13 + a * b) % 7) / 6
            k *= 1.0 - 0.18 * max(0, (y - 120) / 90)
            k *= 0.93 + 0.07 * ((x * 3 + y * 5) % 3) / 2
            E.pt(x, y, c(.9 * k, .96 * k, 1.0 * k))
B.done()
# 창틀 위로 자란 마른 덩굴
rs = np.random.RandomState(3)
V = Pen(PROP, 'window')
for start in ((252, 70), (258, 96), (324, 84)):
    x, y = start
    for i in range(70):
        V.pt(x, y, c(.12, .14, .1))
        if i % 5 == 0:
            V.pt(x + 1, y, c(.2, .23, .16)); V.pt(x - 1, y + 1, c(.17, .2, .13))
        x += rs.choice([-1, 0, 1, 1]) if start[0] < 300 else rs.choice([-1, -1, 0, 1])
        y += rs.choice([0, 1, 1, 2])
V.done()
LIGHTS += [(290, 140, 150, 0.45, COOL), (390, 318, 230, 0.4, COOL * 0.8), (290, 80, 150, 0.35, COOL)]

# ---------------------------------------------------------------- 기둥 (방 모서리)
P = Pen(PROP)
for x0 in (226, 512):
    for x in range(x0, x0 + 14):
        t = (x - x0) / 13
        k = 0.35 + 0.35 * math.sin(t * math.pi * 0.9 + 0.3)
        P.line([x, 40, x, 252], c(k, k * 0.98, k * 1.04))
    P.rect(x0 - 3, 40, x0 + 16, 46, S_ST); P.rect(x0 - 4, 244, x0 + 17, 254, S_ST)

# ---------------------------------------------------------------- 벽감·머리 없는 성녀상·제단
P = Pen(PROP, 'altar')
NX0, NX1 = 396, 432
for y in range(140, 226):
    for x in range(NX0 - 4, NX1 + 5):
        if y >= arch_y(x, NX0 - 4, NX1 + 4, 166, 136) and NX0 - 4 <= x <= NX1 + 4:
            inside = NX0 <= x <= NX1 and y >= arch_y(x, NX0, NX1, 168, 142)
            P.pt(x, y, c(.07, .07, .08) if inside else S_STL)
# 성상 (머리 부서짐, 두 손 모음)
P.poly([(406, 224), (422, 224), (419, 186), (420, 172), (408, 172), (409, 186)], c(.58, .56, .58))
P.poly([(404, 176), (424, 176), (421, 170), (407, 170)], c(.62, .6, .62))
P.rect(412, 166, 416, 170, c(.5, .48, .5)); P.pt(413, 165, c(.4, .38, .4))      # 부러진 목
P.rect(411, 184, 417, 190, c(.68, .66, .68))                                     # 모은 손
for yv in range(192, 224, 4):
    P.line([410 + (yv % 8) // 4, yv, 412, yv + 3], c(.4, .39, .42))
P.line([419, 186, 421, 224], c(.38, .37, .4))
P.rect(400, 224, 428, 228, S_ST)
# 제단
P.poly([(376, 226), (452, 226), (458, 232), (370, 232)], S_STL)
P.rect(370, 232, 458, 262, S_ST)
P.line([370, 232, 458, 232], S_STD)
for px0 in (376, 432):
    P.rect(px0, 238, px0 + 18, 256, S_STD); P.rect(px0 + 2, 240, px0 + 16, 254, c(.42, .41, .45))
P.rect(404, 232, 424, 260, CRIM); P.line([404, 232, 404, 260], CRIMD); P.line([424, 232, 424, 260], CRIMD)
for x in range(404, 425, 2):
    P.pt(x, 261, GOLD)
P.rect(413, 238, 415, 250, GOLD); P.rect(409, 242, 419, 243, GOLD)
for x, h in ((374, 9), (378, 5), (382, 12), (386, 7), (440, 6), (444, 13), (448, 8), (452, 4), (396, 4), (432, 5)):
    candle(P, x, 225 - h, h)
for x, y, h in ((360, 268, 6), (365, 271, 3), (466, 266, 8), (472, 270, 4)):
    candle(P, x, y - h, h)
P.done()
LIGHTS += [(380, 214, 80, 1.15, WARM), (446, 214, 80, 1.15, WARM), (414, 230, 60, 0.6, WARM), (366, 262, 40, 0.6, WARM), (468, 260, 40, 0.6, WARM)]

# ---------------------------------------------------------------- 왼벽 깃발 (원근)
P = Pen(PROP, 'banners')
rs = np.random.RandomState(12)
for z0, z1 in ((4.7, 5.25), (6.1, 6.6)):
    xs0, xs1 = int(VPX - F * XL / z0), int(VPX - F * XL / z1)
    jag = 0.0
    for x in range(xs0, xs1 + 1):
        z = F * XL / (VPX - x)
        t = (x - xs0) / max(1, xs1 - xs0)
        jag = max(-0.06, min(0.06, jag + rs.uniform(-0.03, 0.03)))
        notch = 0.08 * max(0, 1 - abs(t - 0.5) * 4)
        y0 = int(VPY + (-0.62) * F / z)
        y1 = int(VPY + (0.22 + jag - notch) * F / z)
        f = math.sin(t * math.pi * 4)
        k = 0.85 + 0.3 * f
        for y in range(y0, y1):
            if rs.rand() < 0.015:
                continue
            col = c(.4 * k, .14 * k, .15 * k)
            if abs(t - 0.06) < 0.05 or abs(t - 0.94) < 0.05:
                col = c(.5 * k, .4 * k, .2 * k)
            P.pt(x, y, col)
        P.pt(x, y0 - 1, IRONL)
    cx, cz = (xs0 + xs1) // 2, (z0 + z1) / 2
    cy = int(VPY + (-0.35) * F / cz)
    r = int(0.07 * F / cz)
    P.g.ellipse([cx - r // 2, cy - r, cx + r // 2, cy + r], outline=GOLD)
    P.line([cx, cy - r + 2, cx, cy + r - 2], GOLDL)
P.done()

# ---------------------------------------------------------------- 천장 들보·향로
P = Pen(PROP)
for z in (3.6, 4.6, 6.0, 8.0):
    y = int(VPY - F * HC / z)
    x0, x1 = int(VPX - F * XL / z), int(VPX + F * XR / z)
    th = max(2, int(0.06 * F / z))
    P.rect(x0, y, x1, y + th, WD)
    P.line([x0, y + th, x1, y + th], WDL)
    P.line([x0, y, x1, y], WDD)
P.done()
P = Pen(PROP, 'censer')
cz = 6.0
ytop = int(VPY - F * HC / cz) + 6
for y in range(ytop, 118, 2):
    P.pt(452, y, IRONL); P.pt(452, y + 1, IRON)
P.ell([447, 118, 457, 127], IRON); P.line([447, 121, 457, 121], IRONL); P.pt(452, 128, IRONL)
for y in range(ytop, 70, 2):  # 끊어진 사슬
    P.pt(340, y, IRON)
P.done()

# ---------------------------------------------------------------- 지도 탁자 (오른쪽)
P = Pen(PROP, 'map_table')
for lx, top, bot in ((461, 292, 318), (555, 292, 318), (472, 290, 300), (544, 290, 300)):
    P.rect(lx, top, lx + 3, bot, WD); P.line([lx, top, lx, bot], WDL)
P.poly([(470, 283), (546, 283), (560, 292), (458, 292)], WDL)
P.rect(458, 292, 560, 297, WD); P.line([458, 297, 560, 297], WDD)
P.poly([(480, 284), (532, 284), (538, 290), (474, 290)], c(.62, .56, .44))
P.line([(480, 284), (532, 284)], c(.72, .66, .54))
for x, y in ((486, 286), (492, 287), (499, 286), (505, 288), (511, 287), (518, 286)):
    P.pt(x, y, c(.25, .2, .16))
P.pt(524, 287, c(.6, .2, .18)); P.pt(525, 288, c(.6, .2, .18)); P.pt(524, 288, c(.6, .2, .18))
P.rect(512, 276, 513, 286, c(.62, .64, .7)); P.rect(510, 275, 515, 275, GOLD); P.rect(511, 271, 514, 274, WD)
P.rect(472, 278, 482, 283, c(.3, .18, .16)); P.rect(473, 276, 481, 278, c(.36, .3, .24))
P.rect(542, 282, 546, 283, GOLD)
candle(P, 543, 272, 9, 2)
P.done()
LIGHTS += [(544, 266, 90, 1.2, WARM), (510, 296, 90, 0.5, WARM)]

# ---------------------------------------------------------------- 훈련장 문 (오른벽, 원근)
P = Pen(PROP, 'door_training')
zA, zB = 8.0, 6.4
xA, xB = int(VPX + F * XR / zA), int(VPX + F * XR / zB)
for x in range(xA - 3, xB + 4):
    z = F * XR / (x - VPX)
    t = (x - xA) / (xB - xA)
    yb = VPY + F * HF / z
    ht = 0.5 + 0.1 * math.sqrt(max(0, 1 - (2 * t - 1) ** 2))
    yt = yb - ht * F / z
    if x < xA or x > xB:
        P.line([x, yt - 3, x, yb], S_STL); continue
    P.line([x, yt - 3, x, yt - 1], S_STL)
    plank = (x - xA) % 5
    col = WDD if plank == 0 else (WDL if plank == 1 else WD)
    P.line([x, yt, x, yb - 1], col)
    for fr in (0.25, 0.75):
        yy_ = int(yb - fr * ht * F / z)
        P.pt(x, yy_, IRON); P.pt(x, yy_ + 1, IRON)
    E.pt(x, int(yb) - 1, c(1, .62, .3)); E.pt(x, int(yb), c(.8, .45, .2))
kx = xA + 5
kz = F * XR / (kx - VPX)
ky = int(VPY + F * HF / kz - 0.24 * F / kz)
P.g.ellipse([kx - 2, ky - 2, kx + 2, ky + 2], outline=GOLD)
E.pt(kx + 1, ky + 6, c(1, .7, .35))
P.done()
LIGHTS.append((574, 300, 50, 0.85, WARM))

# ---------------------------------------------------------------- 바닥 잔해
P = Pen(PROP)
rs = np.random.RandomState(21)
for _ in range(70):
    x, y = rs.randint(150, 620), rs.randint(258, 352)
    s = 1 + int(rs.rand() * 3 * (y - 250) / 100)
    k = rs.uniform(.35, .6)
    P.rect(x, y, x + s, y + max(1, s - 1), c(k, k * .98, k * 1.02)); P.line([x, y + s, x + s, y + s], c(.12, .12, .13))
P.rect(330, 300, 341, 303, c(.3, .18, .16)); P.rect(331, 299, 340, 299, c(.6, .56, .48))  # 떨어진 책
P.done()

# ---------------------------------------------------------------- 칼 받침대 (캐릭터 전환)
def sword_layers(state):
    A_ = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    Em = Image.new('RGBA', (W, H), (0, 0, 0, 0))
    p, e = Pen(A_, 'switch_sword'), Pen(Em)
    # 받침대
    OUTL = c(.03, .03, .04)
    p.rect(187, 314, 247, 322, OUTL); p.rect(188, 315, 246, 321, c(.4, .39, .43)); p.line([188, 315, 246, 315], c(.7, .69, .73))
    p.poly([(197, 289), (235, 289), (240, 295), (192, 295)], OUTL)
    p.poly([(198, 290), (234, 290), (238, 295), (194, 295)], c(.78, .77, .8))
    p.rect(193, 295, 239, 315, OUTL)
    p.rect(194, 296, 238, 314, c(.52, .51, .55)); p.line([194, 296, 238, 296], c(.84, .83, .86))
    p.rect(198, 299, 234, 312, c(.36, .35, .39)); p.rect(199, 300, 233, 311, c(.46, .45, .49))
    p.poly([(239, 295), (243, 292), (243, 312), (239, 315)], c(.3, .29, .33))
    for x, y in ((199, 302), (230, 309), (210, 314)):
        p.line([x, y, x + 3, y + 2], S_STD)
    cec, ade = state == 'cecilia', state == 'adela'
    # 전면 문양: 왼쪽 사슬 성호 / 오른쪽 장검
    sym_on, sym_off = c(.85, .93, 1.0), c(.3, .3, .33)
    sym_w = c(1, .82, .45)
    for i in range(3):
        (e if cec else p).pt(204, 301 + i * 2, sym_on if cec else sym_off)
    (e if cec else p).rect(203, 307, 205, 312, sym_on if cec else sym_off)
    (e if cec else p).rect(201, 308, 207, 308, sym_on if cec else sym_off)
    (e if ade else p).rect(228, 301, 228, 312, sym_w if ade else sym_off)
    (e if ade else p).rect(225, 303, 231, 303, sym_w if ade else sym_off)
    # 장검
    sx = 216
    p.ell([sx - 2, 234, sx + 2, 238], GOLD)
    p.rect(sx - 1, 239, sx + 1, 248, WD); p.line([sx - 1, 239, sx - 1, 248], WDL)
    p.rect(sx - 10, 249, sx + 10, 251, GOLD); p.line([sx - 10, 249, sx + 10, 249], GOLDL)
    for y in range(252, 293):
        p.rect(sx - 1, y, sx + 1, y, c(.62, .64, .7)); p.pt(sx - 2, y, c(.4, .42, .47)); p.pt(sx + 2, y, c(.3, .31, .35))
        if ade:
            e.pt(sx - 2, y, c(1, .78, .42)); e.pt(sx, y, c(1, .93, .75)); e.pt(sx + 2, y, c(.95, .55, .25))
    # 감긴 사슬 + 매달린 성호
    on, off_, dark = c(.88, .95, 1.0), c(.55, .57, .62), c(.25, .26, .3)
    for i in range(20):
        t = i * 0.62
        x, y = sx + int(round(4.5 * math.sin(t))), 254 + i * 2
        front = math.cos(t) > 0
        if front:
            (e if cec else p).pt(x, y, on if cec and i % 2 == 0 else (c(.7, .82, .95) if cec else (off_ if i % 2 == 0 else dark)))
    for i, y in enumerate(range(252, 280, 2)):
        x = sx + 10 + (1 if 3 < i < 10 else 0)
        (e if cec else p).pt(x, y, on if cec else (off_ if i % 2 else dark))
    cross = [(sx + 11, y) for y in range(280, 290)] + [(x, 283) for x in range(sx + 8, sx + 15)]
    for x, y in cross:
        (e if cec else p).pt(x, y, on if cec else GOLD)
    p.done()
    light = (216, 280, 95, 1.35, COOL) if cec else (216, 280, 95, 1.35, np.array([1.0, 0.72, 0.4]))
    return A_, Em, light


# ---------------------------------------------------------------- 전경 실루엣
G = Pen(FG)
K, KR = c(.015, .015, .02), c(.07, .07, .08)
G.poly([(120, 360), (128, 318), (136, 318), (130, 360)], K)                      # 부서진 의자 등받이
G.poly([(128, 322), (300, 334), (298, 344), (126, 332)], K)
G.poly([(132, 338), (296, 348), (294, 360), (130, 360)], K)
G.line([(128, 322), (300, 334)], KR); G.line([(128, 318), (136, 318)], KR)
G.poly([(300, 334), (306, 328), (310, 338), (304, 346), (298, 344)], K)        # 부러진 끝
G.poly([(520, 360), (528, 338), (560, 332), (600, 338), (612, 360)], K)          # 쓰러진 기둥 토막
G.g.ellipse([590, 330, 640, 360], fill=K)
G.line([(528, 338), (560, 332), (600, 338)], KR)
G.rect(626, 230, 629, 360, K); G.rect(618, 228, 637, 231, K)                     # 꺼진 촛대
for x in (619, 627, 635):
    G.rect(x, 220, x + 1, 228, KR)
for x in range(0, 120, 9):                                                         # 앞 난간
    G.rect(x, 330 + (x % 3), x + 3, 360, K)
G.rect(0, 328, 120, 332, K); G.line([0, 328, 120, 328], KR)

# ---------------------------------------------------------------- 렌더
PAL = []
for i in range(24):
    v = 0.012 + 0.93 * (i / 23) ** 1.6
    PAL.append((v * .97, v * .98, v * 1.04))
PAL += [(.2, .13, .08), (.35, .22, .12), (.52, .34, .18), (.7, .5, .28), (.86, .7, .45), (.98, .88, .66), (1, .97, .86),
        (.45, .33, .12), (.7, .54, .22), (.9, .76, .4),
        (.12, .05, .06), (.22, .08, .09), (.34, .12, .13), (.48, .18, .18),
        (.1, .13, .17), (.18, .22, .27), (.3, .36, .42), (.55, .64, .7), (.72, .82, .88), (.86, .93, .97), (.95, .99, 1.0),
        (.14, .16, .12), (.22, .25, .18), (.45, .4, .31), (.62, .56, .44)]
PAL_IMG = Image.new('P', (1, 1))
flat = [int(min(1, x) * 255) for col in PAL for x in col]
PAL_IMG.putpalette(flat + flat[-3:] * (256 - len(PAL)))


def render(state):
    sw_alb, sw_emi, sw_light = sword_layers(state)
    pa = Image.alpha_composite(PROP, sw_alb)
    pa_np = np.asarray(pa, np.float32) / 255
    grit = (0.8 + 0.4 * N2[..., None]) * (0.88 + 0.24 * GRAIN[..., None])
    a = pa_np[..., 3:4]
    base = alb * (1 - a) + pa_np[..., :3] * grit * a
    light = np.empty((H, W, 3), np.float32); light[:] = (0.09, 0.095, 0.115)
    for x, y, r, inten, col in LIGHTS + [sw_light]:
        f = np.clip(1 - np.hypot(xx - x, (yy - y) * 1.25) / r, 0, 1) ** 2 * inten
        light += f[..., None] * col
    beam = Image.new('L', (W, H), 0)
    bd = ImageDraw.Draw(beam)
    bd.polygon([(262, 96), (318, 80), (474, 300), (330, 322)], fill=120)
    bd.polygon([(276, 110), (312, 100), (440, 300), (360, 312)], fill=90)
    beam = np.asarray(beam.filter(ImageFilter.GaussianBlur(7)), np.float32)[..., None] / 255
    pool = Image.new('L', (W, H), 0)
    ImageDraw.Draw(pool).ellipse([318, 286, 488, 326], fill=255)
    ImageDraw.Draw(pool).ellipse([360, 294, 452, 318], fill=255)
    pool = np.asarray(pool.filter(ImageFilter.GaussianBlur(9)), np.float32)[..., None] / 255
    light += pool * COOL * 1.5 + beam * COOL * 0.35
    col = base * light
    col += beam * np.array([0.24, 0.27, 0.3]) * (0.7 + 0.6 * N1[..., None])   # 공기 중 빛줄기
    em = np.asarray(Image.alpha_composite(EMI, sw_emi), np.float32) / 255
    col = col * (1 - em[..., 3:4]) + em[..., :3] * em[..., 3:4]
    rs = np.random.RandomState(4)
    for _ in range(70):  # 먼지
        y = rs.randint(110, 300); t = (y - 90) / 220
        x = int(rs.uniform(270 + 60 * t, 316 + 140 * t))
        col[y, x] += 0.25
    bright = np.clip(col - 0.55, 0, None)
    bloom = np.asarray(Image.fromarray((np.clip(bright, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(10)), np.float32) / 255
    bloom2 = np.asarray(Image.fromarray((np.clip(bright, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(3)), np.float32) / 255
    col += bloom * 1.6 + bloom2 * 0.6                                # 빛 번짐
    lum = (col * (0.3, 0.59, 0.11)).sum(-1, keepdims=True)
    col = lum + (col - lum) * 0.6                                   # 채도 낮춤
    col = np.clip(col, 0, 1.2) ** 1.12
    r = np.hypot((xx - 380) / 330, (yy - 215) / 210)
    col *= (1 - 0.82 * smooth(0.42, 1.2, r))[..., None]            # 짙은 비네트
    col *= (0.45 + 0.55 * smooth(40, 220, xx))[..., None]           # 왼쪽 HUD 자리 어둡게
    fg = np.asarray(FG, np.float32) / 255
    col = col * (1 - fg[..., 3:4]) + fg[..., :3] * fg[..., 3:4]
    col += (GRAIN[..., None] - 0.5) * 0.025
    img = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8), 'RGB')
    return img.quantize(palette=PAL_IMG, dither=Image.Dither.FLOYDSTEINBERG).convert('RGB')


imgs = {}
for state in ('cecilia', 'adela'):
    im = render(state)
    imgs[state] = im
    im.save(OUT / f'lobby_v3_{state}_native.png')
    im.resize((W * 3, H * 3), Image.NEAREST).save(OUT / f'lobby_v3_{state}.png')

# 소품 개별 PNG (세실리아 상태 기준, 조명 구움) + 칼 두 상태
for name, m in MASKS.items():
    if name == 'switch_sword':
        continue
    box = m.getbbox()
    piece = imgs['cecilia'].convert('RGBA'); piece.putalpha(m)
    piece.crop(box).save(OUT / 'props' / f'{name}.png')
sw_mask = sword_layers('cecilia')[0].getchannel('A')
for state in ('cecilia', 'adela'):
    piece = imgs[state].convert('RGBA'); piece.putalpha(sw_mask)
    piece.crop(sw_mask.getbbox()).save(OUT / 'props' / f'switch_sword_{state}.png')

# 칼 전환 비교 시트
font = ImageFont.truetype('C:/Windows/Fonts/malgunbd.ttf', 30)
crop = (150, 205, 290, 335)
S = 5
cw, ch = (crop[2] - crop[0]) * S, (crop[3] - crop[1]) * S
sheet = Image.new('RGB', (cw * 2 + 60, ch + 90), (8, 8, 10))
sd = ImageDraw.Draw(sheet)
for i, (state, label) in enumerate((('cecilia', '세실리아 선택 — 사슬이 은빛'), ('adela', '아델라 선택 — 칼날이 금빛'))):
    part = imgs[state].crop(crop).resize((cw, ch), Image.NEAREST)
    sheet.paste(part, (20 + i * (cw + 20), 20))
    sd.text((20 + i * (cw + 20) + cw // 2, ch + 55), label, font=font, fill=(225, 220, 210), anchor='mm')
sheet.save(OUT / 'switch_sword_states.png')
print('ok', sorted(MASKS))
