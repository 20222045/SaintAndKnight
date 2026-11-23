# 로비(거점) v6 (v5 + 외부 CC0 에셋: GothicVania Church Pack) — 다크 고딕 도트: 3/4 원근 실내, 단일 창 빛, 저채도, 거친 질감, 짙은 어둠.
# 칼 받침대 = 캐릭터 전환 오브젝트 (세실리아: 사슬이 은빛 / 아델라: 칼날이 금빛).
# 원본 640x360, 미리보기 x3. 실행: python Docs/Design/Lobby/make_lobby_v3.py
import math
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

W, H = 640, 360
OUT = Path(__file__).parent / 'v6'
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

# ---------------------------------------------------------------- [v6] 외부 에셋 (GothicVania Church Pack, ansimuz, CC0/퍼블릭 도메인)
# 원본: external/gothicvania_church/. 채도를 빼고 밝기를 우리 고유색 수준으로 맞춘 뒤 같은 조명·팔레트를 거친다.
EXT = Path(__file__).parent / 'external' / 'gothicvania_church'


def ext_sprite(fname, box, key=(39, 38, 56)):
    a = np.asarray(Image.open(EXT / fname).convert('RGBA').crop(box)).astype(np.float32)
    keym = np.all(a[..., :3] == key, axis=-1) | np.all(a[..., :3] == (255, 0, 255), axis=-1)
    a[keym, 3] = 0
    rgb = a[..., :3] / 255
    warm = (rgb[..., 0] > 0.5) & (rgb[..., 0] - rgb[..., 2] > 0.3) & (a[..., 3] > 0)
    lum = (rgb * (0.3, 0.59, 0.11)).sum(-1, keepdims=True)
    alb_ = np.clip((lum * 0.8 + rgb * 0.2) * 1.55 * np.array([1.0, 0.98, 1.03]), 0, 1)
    alb_[warm] = rgb[warm]
    out = np.concatenate([alb_ * 255, a[..., 3:4]], -1).astype(np.uint8)
    return Image.fromarray(out, 'RGBA'), warm


def place_ext(name, fname, box, x, y, light=None, crop_after=None):
    spr, warm = ext_sprite(fname, box)
    PROP.alpha_composite(spr, (x, y))
    m = Image.new('L', (W, H), 0); m.paste(spr.getchannel('A'), (x, y))
    if name in MASKS:
        MASKS[name] = Image.fromarray(np.maximum(np.asarray(MASKS[name]), np.asarray(m)))
    else:
        MASKS[name] = m
    sa = np.asarray(spr)
    for j, i in zip(*np.nonzero(warm)):
        r_, g_, b_ = sa[j, i, :3]
        EMI.putpixel((x + int(i), y + int(j)), (min(255, int(r_ * 1.25)), min(255, int(g_ * 1.2)), int(b_), 255))
    if light and warm.any():
        ys_, xs_ = np.nonzero(warm)
        LIGHTS.append((x + float(xs_.mean()), y + float(ys_.mean()), light[0], light[1], WARM))


# 해골 얼굴 장식 기둥 + 랜턴 (왼쪽 뒤 모서리 기둥 대체, 오른쪽 랜턴은 창을 가려 잘라냄)
place_ext('ext_column', 'column.png', (0, 66, 85, 190), 176, 130, light=(80, 0.95))
# 가고일 석상과 묘 십자가 (왼쪽 중간 바닥)
place_ext('ext_gargoyle', 'backgrounds.png', (469, 104, 540, 192), 134, 206)
# 가고일 발치 촛불 무더기
place_ext('ext_candles', 'backgrounds.png', (105, 148, 156, 192), 150, 256, light=(100, 1.25))
LIGHTS.append((172, 246, 85, 1.5, WARM))   # 촛불이 석상 몸통을 비춤

# ---------------------------------------------------------------- [v4] 뒷벽 장식·뒤쪽 가구·융단
rs4 = np.random.RandomState(41)
BOOKS = [c(.32, .14, .12), c(.18, .2, .14), c(.3, .24, .16), c(.14, .14, .18), c(.4, .34, .24), c(.22, .1, .1), c(.26, .22, .2)]
BONE, BONED = c(.72, .69, .6), c(.42, .4, .35)
GLASS, GLASSH = c(.26, .3, .28), c(.72, .78, .74)


def skull(p, x, y):
    p.rect(x, y, x + 4, y + 3, BONE); p.rect(x + 1, y + 4, x + 3, y + 5, BONE)
    p.pt(x + 1, y + 2, c(.05, .05, .05)); p.pt(x + 3, y + 2, c(.05, .05, .05)); p.pt(x + 2, y + 5, BONED)
    p.line([x, y + 3, x, y + 3], BONED)


def jar(p, x, ybot, h):
    p.rect(x, ybot - h, x + 3, ybot, GLASS); p.rect(x, ybot - h - 1, x + 3, ybot - h - 1, WDD)
    p.pt(x + 1, ybot - h + 1, GLASSH); p.line([x + 1, ybot - 2, x + 2, ybot - 2], c(.4, .44, .3))


# 찢긴 초상화
P = Pen(PROP, 'painting')
P.rect(334, 70, 384, 134, c(.42, .32, .14))
P.rect(336, 72, 382, 132, GOLD); P.line([336, 72, 382, 72], GOLDL); P.line([336, 72, 336, 132], GOLDL)
P.rect(339, 75, 379, 129, c(.3, .22, .1))
for x in range(337, 382, 4):
    P.pt(x, 73, c(.95, .85, .55)); P.pt(x + 2, 131, c(.3, .22, .1))
P.rect(341, 77, 377, 127, c(.11, .1, .1))
P.ell([353, 84, 365, 98], c(.2, .18, .17)); P.poly([(346, 127), (350, 106), (359, 99), (368, 106), (372, 127)], c(.17, .15, .15))
P.ell([355, 86, 363, 96], c(.26, .23, .21))
for x0, y0, x1, y1 in ((344, 82, 372, 118), (350, 78, 366, 104), (362, 100, 376, 124)):  # 칼자국
    P.line([x0, y0, x1, y1], c(.36, .35, .38), 2); P.line([x0 + 1, y0, x1 + 1, y1], c(.5, .48, .45))
P.rect(358, 64, 360, 69, IRONL)
P.done()

# 꺼진 벽 촛대 (기둥)
P = Pen(PROP)
for cx in (233, 519):
    P.rect(cx - 1, 150, cx + 1, 158, IRON); P.rect(cx - 4, 148, cx + 4, 150, IRONL); P.rect(cx - 1, 142, cx, 147, c(.6, .57, .5))
    P.pt(cx - 2, 151, c(.6, .57, .5)); P.pt(cx - 2, 152, c(.6, .57, .5))
P.done()

# 거미줄 (천장 모서리)
P = Pen(PROP)
WEB = c(.62, .62, .66)
for (cx, cy, d0, d1) in ((232, 40, 0, 90), (520, 40, 90, 180), (254, 70, 10, 80)):
    rays = []
    for i, ang in enumerate(np.linspace(d0, d1, 6)):
        r_ = 30 + rs4.randint(-4, 6)
        ex, ey = cx + r_ * math.cos(math.radians(ang)), cy + r_ * math.sin(math.radians(ang))
        rays.append((ang, r_))
        for t in np.linspace(0, 1, int(r_)):
            if rs4.rand() < 0.8:
                P.pt(int(cx + (ex - cx) * t), int(cy + (ey - cy) * t), WEB)
    for k in (0.3, 0.5, 0.7, 0.9):
        prev = None
        for ang, r_ in rays:
            pt_ = (int(cx + r_ * k * math.cos(math.radians(ang))), int(cy + r_ * k * math.sin(math.radians(ang))))
            if prev and rs4.rand() < 0.85:
                mx, my = (prev[0] + pt_[0]) // 2, (prev[1] + pt_[1]) // 2 + 1
                P.line([prev, (mx, my), pt_], WEB)
            prev = pt_
    for _ in range(3):
        x = cx + (rs4.randint(4, 24) if d0 < 90 else -rs4.randint(4, 24))
        P.line([x, cy + 4, x, cy + 4 + rs4.randint(6, 18)], WEB)
P.done()

# 책장 (뒷벽 오른쪽)
P = Pen(PROP, 'bookcase')
x0, x1, y0, y1 = 460, 508, 146, 258
P.rect(x0 - 3, y0 - 7, x1 + 3, y0 - 1, WD); P.line([x0 - 3, y0 - 7, x1 + 3, y0 - 7], WDL)
for x in range(x0 - 2, x1 + 3, 4):
    P.pt(x, y0 - 4, WDL)
P.rect(x0, y0, x1, y1, c(.08, .06, .05))
P.rect(x0 - 2, y0, x0 + 1, y1, WD); P.line([x0 - 2, y0, x0 - 2, y1], WDL)
P.rect(x1 - 1, y0, x1 + 2, y1, WD); P.line([x1 + 2, y0, x1 + 2, y1], WDD)
shelves = [146, 168, 190, 212, 234]
for sy in shelves[1:]:
    P.rect(x0, sy, x1, sy + 2, WD); P.line([x0, sy, x1, sy], WDL)
for i in range(len(shelves) - 1):
    top, bot = shelves[i] + (3 if i else 1), shelves[i + 1] - 1
    x = x0 + 2
    while x < x1 - 3:
        r_ = rs4.rand()
        if r_ < 0.62:
            w, h = rs4.randint(2, 4), rs4.randint(bot - top - 7, bot - top + 1)
            col = BOOKS[rs4.randint(len(BOOKS))]
            P.rect(x, bot - h, x + w - 1, bot, col)
            P.line([x + w - 1, bot - h, x + w - 1, bot], c(.06, .05, .05))
            P.pt(x, bot - h + 2, GOLD if rs4.rand() < .5 else c(.5, .45, .35))
            x += w
        elif r_ < 0.7:
            h = bot - top - 4
            P.poly([(x, bot), (x + 3, bot), (x + 8, bot - h + 2), (x + 5, bot - h)], BOOKS[rs4.randint(len(BOOKS))])
            x += 7
        elif r_ < 0.78:
            jar(P, x, bot, rs4.randint(4, 8)); x += 5
        elif r_ < 0.83:
            skull(P, x, bot - 5); x += 6
        elif r_ < 0.88:
            P.rect(x, bot - 7, x + 1, bot, c(.7, .66, .58)); P.pt(x - 1, bot - 3, c(.7, .66, .58)); x += 3
        elif r_ < 0.93:
            P.rect(x, bot - 3, x + 6, bot, c(.6, .55, .44)); P.line([x, bot - 3, x + 6, bot - 3], c(.72, .66, .54)); x += 8
        else:
            x += 3
P.rect(x0, 236, x1, y1, WD)
for dx in (x0 + 2, (x0 + x1) // 2 + 1):
    P.rect(dx, 238, dx + 20, 256, WDD); P.rect(dx + 2, 240, dx + 18, 254, WD); P.line([dx + 2, 240, dx + 18, 240], WDL)
    P.pt(dx + (18 if dx == x0 + 2 else 2), 247, GOLD)
for y in range(152, 258, 7):  # 기대 놓은 사다리
    t = (y - 150) / 108
    xl, xr = int(498 + 12 * t), int(504 + 12 * t)
    P.line([xl, y, xr, y], WDL)
P.line([498, 150, 510, 258], WD); P.line([504, 150, 516, 258], WD)
P.done()

# 세워 둔 관 (오른쪽 모서리)
P = Pen(PROP, 'coffin')
pts = [(527, 194), (541, 194), (546, 210), (543, 264), (525, 264), (522, 210)]
P.poly(pts, c(.24, .16, .12))
P.poly([(528, 197), (540, 197), (543, 210), (540, 261), (528, 261), (525, 210)], WD)
P.line([(527, 194), (522, 210), (525, 264)], WDL)
P.rect(533, 210, 535, 238, c(.16, .1, .08)); P.rect(528, 216, 540, 218, c(.16, .1, .08))
for y in (226, 248):
    for x in range(522, 547, 2):
        P.pt(x, y + (x % 4 == 0), IRONL if x % 4 else IRON)
for x, y in ((529, 200), (539, 200), (527, 258), (541, 258)):
    P.pt(x, y, IRONL)
P.done()

# 통·상자·자루 (왼쪽 뒤 모서리)
P = Pen(PROP, 'crates')
P.poly([(236, 236), (258, 236), (262, 232), (240, 232)], c(.5, .38, .27))
P.rect(236, 236, 258, 262, WD); P.rect(236, 236, 258, 262, WD)
for y in range(240, 262, 5):
    P.line([236, y, 258, y], WDD)
P.line([236, 237, 258, 261], WDL); P.line([258, 237, 236, 261], WDL)
P.poly([(258, 236), (262, 232), (262, 258), (258, 262)], c(.26, .19, .14))
P.rect(240, 222, 254, 236, WD); P.poly([(240, 222), (254, 222), (257, 219), (243, 219)], WDL)
for y in range(225, 236, 4):
    P.line([240, y, 254, y], WDD)
bx = 266
for y in range(238, 265):
    t = (y - 238) / 26
    w = int(7 + 2 * math.sin(t * math.pi))
    P.line([bx - w, y, bx + w, y], WD)
    for sx in range(bx - w + 2, bx + w, 3):
        P.pt(sx, y, WDD)
for hy in (242, 258):
    P.line([bx - 9, hy, bx + 9, hy], IRON); P.line([bx - 9, hy - 1, bx + 9, hy - 1], IRONL)
P.ell([bx - 7, 235, bx + 7, 240], c(.3, .22, .16)); P.ell([bx - 5, 236, bx + 5, 239], c(.18, .12, .09))
candle(P, 247, 211, 7)
LIGHTS.append((248, 205, 65, 0.85, WARM))
P.poly([(276, 264), (278, 252), (284, 248), (290, 254), (291, 264)], c(.4, .36, .28))
P.line([(278, 252), (284, 248), (290, 254)], c(.52, .47, .37)); P.line([283, 250, 285, 256], c(.28, .25, .2))
P.done()

# 바닥 융단 (제단 앞, 원근)
X0r, X1r, Z0r, Z1r = 0.04, 0.42, 6.4, 9.1
zz = F * HF / np.maximum(1e-3, yy - VPY)
Xw = (xx - VPX) * zz / F
uu, ww_ = (Xw - X0r) / (X1r - X0r), (zz - Z0r) / (Z1r - Z0r)
inside = (uu > 0) & (uu < 1) & (ww_ > 0) & (ww_ < 1) & (yy > 252) & (N4 > 0.18)
fray = (np.minimum(np.minimum(uu, 1 - uu), np.minimum(ww_, 1 - ww_)) < 0.03) & (GRAIN > 0.55)
inside &= ~fray & ~(N3 + 0.3 * GRAIN > 0.95)
border = (uu < 0.1) | (uu > 0.9) | (ww_ < 0.07) | (ww_ > 0.93)
dia = (np.abs(np.modf(uu * 4)[0] - 0.5) + np.abs(np.modf(ww_ * 7)[0] - 0.5)) < 0.22
rug = np.zeros((H, W, 4), np.float32)
rug[..., :3] = np.where(border[..., None], [.42, .32, .18], np.where(dia[..., None], [.42, .15, .15], [.3, .09, .1]))
rug[..., :3] *= np.where((border & (np.modf(uu * 30 + ww_ * 30)[0] < 0.3))[..., None], 1.3, 1.0)
rug[..., 3] = inside
PROP.alpha_composite(Image.fromarray((rug * 255).astype(np.uint8), 'RGBA'))

# ---------------------------------------------------------------- [v5] 파이프 오르간·바닥 묘석
def ws(X, z):
    return (VPX + F * X / z, VPY + F * HF / z)


P = Pen(PROP, 'organ')
for i in range(13):
    x = 456 + int(i * 4.6)
    h = 46 + int(34 * abs(i - 6) / 6)
    top = 132 - h
    if i == 9:                                 # 부러진 파이프
        top = 112
        P.line([x, top - 1, x + 3, top - 4], c(.4, .4, .44))
    if i == 3:                                 # 휘어진 파이프
        P.poly([(x, top + 10), (x + 3, top + 10), (x + 7, top - 2), (x + 4, top - 2)], c(.52, .52, .56))
        top += 10
    P.rect(x, top, x + 3, 132, c(.48, .48, .52))
    P.line([x, top, x, 132], c(.7, .7, .74)); P.line([x + 3, top, x + 3, 132], c(.28, .28, .32))
    P.rect(x, top, x + 3, top, c(.22, .22, .25))
    my = 120 - (i % 3) * 3
    P.poly([(x, my), (x + 3, my), (x + 3, my + 3), (x, my + 4)], c(.08, .08, .09))
    P.pt(x + 1, my - 1, c(.62, .62, .66))
P.rect(452, 128, 519, 139, WD); P.line([452, 128, 519, 128], WDL); P.line([452, 139, 519, 139], WDD)
for x in range(455, 518, 6):
    P.rect(x, 131, x + 3, 136, WDD)
P.done()

P = Pen(PROP, 'tomb_slab')
corners = [ws(-0.5, 7.7), ws(-0.25, 7.7), ws(-0.25, 6.9), ws(-0.5, 6.9)]
P.poly(corners, c(.34, .33, .36))
inner = [ws(-0.48, 7.62), ws(-0.27, 7.62), ws(-0.27, 6.97), ws(-0.48, 6.97)]
P.poly(inner, c(.5, .49, .52))
P.line([ws(-0.375, 7.55), ws(-0.375, 7.05)], c(.24, .23, .26))
P.line([ws(-0.44, 7.38), ws(-0.31, 7.38)], c(.24, .23, .26))
for zz_ in (7.2, 7.13):
    a_, b_ = ws(-0.45, zz_), ws(-0.3, zz_)
    for t in np.linspace(0, 1, 12):
        if int(t * 11) % 3:
            P.pt(int(a_[0] + (b_[0] - a_[0]) * t), int(a_[1]), c(.3, .29, .32))
P.line([ws(-0.29, 7.6), ws(-0.33, 7.3), ws(-0.3, 7.0)], c(.18, .17, .2))
P.done()

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

# ---------------------------------------------------------------- [v4] 독서대·샹들리에·철창·잡동사니
# 독서대 (기록·갤러리)
P = Pen(PROP, 'lectern')
P.rect(300, 270, 304, 304, WD); P.line([300, 270, 300, 304], WDL)
for y in (278, 290):
    P.rect(298, y, 306, y + 2, WD); P.line([298, y, 306, y], WDL)
P.poly([(292, 306), (312, 306), (314, 310), (290, 310)], WDD); P.line([292, 306, 312, 306], WD)
P.poly([(288, 262), (318, 257), (320, 263), (290, 269)], WD); P.line([(288, 262), (318, 257)], WDL)
P.poly([(290, 261), (303, 258), (304, 263), (291, 266)], c(.8, .76, .66))
P.poly([(304, 258), (317, 256), (318, 261), (305, 263)], c(.72, .68, .58))
for i in range(4):
    P.line([292, 262 + i, 301, 260 + i], c(.4, .36, .3)) if i % 2 == 0 else None
    P.line([306, 258 + i, 315, 257 + i], c(.36, .33, .28)) if i % 2 == 0 else None
P.line([304, 263, 305, 272], c(.5, .16, .16))
candle(P, 316, 249, 7)
P.done()
LIGHTS.append((317, 242, 60, 0.85, WARM))

# 샹들리에 (꺼짐, 빛줄기 안에 매달림)
P = Pen(PROP, 'chandelier')
for y in range(0, 146, 2):
    P.pt(290, y, IRONL); P.pt(290, y + 1, IRON)
for k in (-1, 1):
    P.line([290, 138, 290 + 22 * k, 150], IRON)
P.g.ellipse([264, 148, 316, 158], outline=IRON, width=2); P.mg.ellipse([264, 148, 316, 158], outline=255, width=2)
P.line([266, 152, 314, 152], IRONL)
for i in range(7):
    a = i / 7 * 2 * math.pi
    cx, cy = int(290 + 25 * math.cos(a)), int(153 + 5 * math.sin(a))
    P.rect(cx - 2, cy - 1, cx + 2, cy, IRONL)
    h = 3 + (i * 5) % 6
    P.rect(cx - 1, cy - 1 - h, cx, cy - 2, c(.66, .62, .54)); P.pt(cx + 1, cy - 2, c(.66, .62, .54))
    P.pt(cx - 2, cy + 1, c(.66, .62, .54)); P.pt(cx, cy - 2 - h, c(.1, .1, .1))
P.line([290, 158, 290, 170], IRON); P.poly([(287, 164), (293, 164), (290, 172)], IRON)
P.done()

# 매달린 철창
P = Pen(PROP, 'cage')
for y in range(0, 98, 2):
    P.pt(486, y, IRONL); P.pt(486, y + 1, IRON)
P.g.arc([474, 96, 498, 112], 180, 360, fill=IRONL, width=2); P.mg.arc([474, 96, 498, 112], 180, 360, fill=255, width=2)
for x in range(474, 499, 4):
    P.line([x, 102 if x in (474, 498) else 98, x, 140], IRON)
    P.pt(x - 1, 112, IRONL)
P.line([474, 140, 498, 140], IRONL); P.line([474, 122, 498, 122], IRON)
P.poly([(478, 140), (494, 140), (490, 144), (482, 144)], IRON)
skull(P, 478, 133); candle(P, 489, 130, 9, 2)
P.done()
LIGHTS.append((490, 124, 75, 1.0, WARM))

# 바닥 잡동사니: 흩어진 종이, 뼈, 촛농, 쓰러진 촛대, 의자
P = Pen(PROP)
for x, y, w in ((430, 312, 6), (444, 318, 5), (416, 324, 7), (536, 322, 5), (500, 330, 6)):
    P.poly([(x, y), (x + w, y - 1), (x + w + 1, y + 2), (x + 1, y + 3)], c(.62, .58, .48))
    P.line([x + 1, y + 1, x + w - 1, y], c(.4, .36, .3))
for x, y in ((352, 284), (474, 284), (396, 270)):
    P.ell([x - 4, y - 1, x + 4, y + 1], c(.7, .66, .58))
P.line([340, 292, 348, 290], BONE); P.pt(339, 291, BONE); P.pt(349, 289, BONE); P.pt(339, 293, BONE); P.pt(349, 291, BONE)
skull(P, 352, 286)
P.line([560, 326, 590, 312], IRON, 2); P.line([586, 308, 594, 316], IRON); P.line([556, 324, 560, 330], IRON)
P.rect(590, 304, 591, 308, c(.66, .62, .54))
# 부서진 의자 (탁자 옆)
P.rect(568, 286, 584, 289, WD); P.line([568, 286, 584, 286], WDL)
P.rect(569, 289, 571, 304, WD); P.rect(581, 289, 583, 300, WD)
P.rect(581, 262, 583, 286, WD); P.rect(569, 266, 571, 286, WD); P.line([569, 268, 583, 266], WD); P.line([569, 276, 583, 274], WDL)
P.done()

# ---------------------------------------------------------------- [v5] 세례반·키 큰 촛대·무릎 꿇는 대·쓰러진 종·성가책
STONE_L, STONE_M, STONE_D = c(.64, .62, .66), c(.48, .47, .5), c(.28, .27, .3)
P = Pen(PROP, 'font')
P.poly([(258, 286), (280, 286), (283, 290), (255, 290)], STONE_M); P.line([258, 286, 280, 286], STONE_L)
P.rect(264, 266, 274, 286, STONE_M); P.line([264, 266, 264, 286], STONE_L); P.line([274, 266, 274, 286], STONE_D)
for y in (270, 282):
    P.line([263, y, 275, y], STONE_D)
P.poly([(254, 254), (284, 254), (281, 266), (257, 266)], STONE_M)
P.line([(254, 254), (257, 266)], STONE_L); P.line([(284, 254), (281, 266)], STONE_D)
P.ell([253, 250, 285, 257], STONE_L)
P.ell([256, 251, 282, 256], c(.1, .12, .14)); P.pt(262, 253, c(.6, .66, .7)); P.pt(263, 253, c(.45, .5, .55))
for cx_ in (261, 269, 277):
    P.line([cx_, 257, cx_, 263], STONE_D); P.line([cx_ - 2, 259, cx_ + 2, 259], STONE_D)
P.line([(279, 255), (277, 261), (279, 265)], c(.15, .15, .17))
P.done()

P = Pen(PROP, 'candelabra_tall')
for (bx, lit, lean) in ((350, True, 0), (466, False, 5)):
    top = 222
    P.line([bx, 270, bx + lean, top + 6], IRON, 2); P.line([bx - 1, 270, bx - 1 + lean, top + 6], IRONL)
    P.poly([(bx - 6, 272), (bx + 6, 272), (bx + 3, 266), (bx - 3, 266)], IRON)
    for k in (-6, 6):
        P.line([bx, 271, bx + k, 274], IRON)
    tx = bx + lean
    P.line([tx - 9, top + 6, tx + 9, top + 6 + lean // 2], IRON)
    P.line([tx, 240, tx - 2, 244], IRONL)
    for k in (-8, 0, 8):
        cx_ = tx + k
        P.rect(cx_ - 2, top + 4 + (lean // 2 if k > 0 else 0), cx_ + 2, top + 5 + (lean // 2 if k > 0 else 0), IRONL)
        if lit:
            candle(P, cx_ - 1, top - 6 + (k == 0) * -3, 9 + (k == 0) * 3)
        elif k != 8:
            P.rect(cx_ - 1, top - 1, cx_, top + 3, c(.62, .58, .5))
LIGHTS.append((350, 212, 80, 1.05, WARM))
P.done()

P = Pen(PROP, 'kneeler')
P.rect(394, 272, 434, 275, WD); P.line([394, 272, 434, 272], WDL)
P.rect(396, 269, 432, 271, CRIM); P.line([396, 269, 432, 269], c(.52, .2, .2))
for lx in (396, 430):
    P.rect(lx, 275, lx + 2, 281, WDD)
P.done()


def bell_sprite():
    b = Image.new('RGBA', (48, 50), (0, 0, 0, 0))
    g = ImageDraw.Draw(b)
    BR, BRD, BRL, VER = c(.44, .34, .2), c(.24, .18, .11), c(.66, .53, .32), c(.3, .4, .34)
    g.ellipse([20, 0, 28, 8], outline=BRD, width=2)
    g.ellipse([12, 5, 36, 17], fill=BR)
    g.polygon([(13, 11), (35, 11), (37, 30), (42, 40), (45, 45), (3, 45), (6, 40), (11, 30)], fill=BR)
    g.line([(13, 11), (11, 30), (6, 40), (3, 45)], fill=BRD); g.line([(35, 11), (37, 30), (42, 40), (45, 45)], fill=BRD)
    g.line([16, 12, 14, 42], fill=BRL); g.line([17, 12, 15, 42], fill=BRL)
    for y in (19, 37):
        g.line([9, y, 39, y], fill=BRD); g.line([9, y - 1, 39, y - 1], fill=BRL)
    g.rectangle([3, 42, 45, 45], fill=BRD); g.line([3, 42, 45, 42], fill=BRL)
    g.ellipse([3, 43, 45, 49], fill=c(.04, .04, .05)); g.ellipse([21, 43, 27, 49], fill=c(.2, .16, .1))
    rr = np.random.RandomState(7)
    for _ in range(60):
        x, y = rr.randint(10, 40), rr.randint(12, 42)
        if b.getpixel((x, y))[3] and rr.rand() < 0.6:
            g.point((x, y), fill=VER); g.point((x, y + 1), fill=VER)
    g.line([(31, 45), (28, 36), (30, 28)], fill=c(.06, .05, .04), width=1)
    return b.rotate(-14, expand=True, resample=Image.NEAREST)


bs = bell_sprite()
BELL_POS = (350, 262)
PROP.alpha_composite(bs, BELL_POS)
bm = Image.new('L', (W, H), 0); bm.paste(bs.getchannel('A'), BELL_POS)
MASKS['bell'] = bm

P = Pen(PROP)
for x, y, col in ((444, 300, c(.3, .14, .12)), (430, 306, c(.18, .2, .14)), (520, 314, c(.3, .14, .12))):
    P.poly([(x, y), (x + 7, y - 2), (x + 8, y + 1), (x + 1, y + 3)], col); P.line([x + 1, y + 1, x + 7, y - 1], c(.62, .58, .48))
for _ in range(16):  # 깨진 유리 조각
    x, y = rs4.randint(250, 340), rs4.randint(262, 282)
    P.pt(x, y, c(.7, .8, .86)); P.pt(x + 1, y, c(.4, .48, .52))
P.done()

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


# ---------------------------------------------------------------- [v5] 전경 장애물: 부서진 신도석 등받이 · 성체 난간 · 기둥 토막
G = Pen(FG)
K0, K1, K2, KR = c(.03, .03, .035), c(.06, .058, .062), c(.1, .095, .1), c(.2, .19, .2)
# 신도석 등받이 (왼쪽~가운데), 가운데가 부러짐
G.poly([(150, 320), (236, 324), (236, 360), (150, 360)], K1)
G.poly([(262, 325), (334, 328), (334, 360), (262, 360)], K1)
G.poly([(148, 315), (238, 320), (238, 326), (148, 321)], K2); G.line([(148, 315), (238, 320)], KR)
G.poly([(260, 321), (336, 324), (336, 330), (260, 327)], K2); G.line([(260, 321), (336, 324)], KR)
for x in range(158, 236, 12):
    G.line([x, 326, x, 360], K0); G.line([x + 1, 326, x + 1, 360], K2)
for x in range(270, 334, 12):
    G.line([x, 330, x, 360], K0); G.line([x + 1, 330, x + 1, 360], K2)
G.poly([(236, 320), (240, 316), (243, 324), (246, 318), (248, 330), (236, 334)], K2)           # 부러진 끝
G.poly([(262, 321), (258, 317), (256, 326), (253, 320), (252, 333), (262, 336)], K2)
G.poly([(240, 336), (244, 333), (268, 352), (264, 356)], K2); G.line([(244, 333), (268, 352)], KR)  # 떨어진 판자
for ex in (144, 334):                                                                              # 끝 기둥 + 공 장식
    G.rect(ex, 306, ex + 7, 360, K1); G.line([ex, 306, ex, 360], KR)
    G.ell([ex - 1, 297, ex + 8, 306], K2); G.pt(ex + 2, 299, KR); G.pt(ex + 1, 300, KR)
    G.rect(ex + 2, 291, ex + 5, 297, K2)
G.rect(144 + 3, 284, 144 + 4, 291, K2); G.rect(144 + 1, 286, 144 + 6, 287, K2)                       # 작은 성호
# 성체 난간 (가운데~오른쪽): 돌림 기둥, 일부 빠지고 한 칸은 무너짐
rail_y = 330
G.rect(366, rail_y - 2, 486, rail_y + 3, K2); G.line([366, rail_y - 2, 486, rail_y - 2], KR); G.line([366, rail_y - 1, 486, rail_y - 1], c(.14, .13, .14))
G.rect(362, rail_y - 10, 369, 360, K1); G.ell([361, rail_y - 18, 370, rail_y - 9], K2); G.pt(363, rail_y - 16, KR); G.line([362, rail_y - 10, 362, 360], KR)
for i, x in enumerate(range(374, 486, 10)):
    if i in (4, 8):
        continue                                                     # 빠진 기둥
    stop = 344 if i == 6 else 360
    for y in range(rail_y + 4, stop):
        t = (y - rail_y - 4) / 26
        w = 1 + int(2.4 * math.sin(min(1, t * 1.3) * math.pi) + (1 if 0.1 < t < 0.16 else 0))
        G.line([x - w, y, x + w, y], K1)
        G.pt(x - w, y, KR if y % 3 else K2)
    if i == 6:
        G.poly([(x - 2, 344), (x + 3, 341), (x + 1, 347)], K2)
G.poly([(486, rail_y - 2), (492, rail_y), (540, 352), (534, 356), (486, rail_y + 4)], K2)            # 무너진 난간대
G.line([(492, rail_y), (540, 352)], KR)
G.poly([(500, 354), (503, 340), (507, 342), (505, 360), (499, 360)], K1)                              # 쓰러진 기둥
# 오른쪽: 기둥 토막과 꺼진 촛대
G.poly([(548, 360), (556, 338), (590, 332), (626, 338), (636, 360)], K1)
G.ell([600, 328, 640, 360], K1); G.ell([606, 333, 634, 355], K2)
G.line([(556, 338), (590, 332), (626, 338)], KR)
for x in range(560, 630, 7):
    G.line([x, 340, x + 2, 360], K0)
G.rect(626, 230, 629, 360, K1); G.rect(618, 228, 637, 231, K2); G.line([618, 228, 637, 228], KR)
for x in (619, 627, 635):
    G.rect(x, 220, x + 1, 228, K2)
# 왼쪽 (HUD 자리): 낮은 난간
for x in range(0, 130, 9):
    G.rect(x, 330 + (x % 3), x + 3, 360, K1)
G.rect(0, 328, 130, 332, K2); G.line([0, 328, 130, 328], KR)
# 질감
fga = np.asarray(FG, np.float32)
fga[..., :3] *= (0.7 + 0.6 * N2[..., None]) * (0.85 + 0.3 * GRAIN[..., None])
FG = Image.fromarray(fga.clip(0, 255).astype(np.uint8), 'RGBA')

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


def render(state, with_fg=True):
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
    for a_, b_ in ((0, 9), (2, 5), (4, 3), (5, 14), (6, 15), (1, 2), (3, 8)):   # 유리 틈 사이 가는 빛줄기
        px_, py_ = WX0 + a_ * 8 + 4, WAP + b_ * 8 + 4
        L_ = 300 - py_
        bd.polygon([(px_ - 2, py_), (px_ + 2, py_), (px_ + 6 + 0.73 * L_, 300), (px_ - 4 + 0.73 * L_, 300)], fill=150)
    beam = np.asarray(beam.filter(ImageFilter.GaussianBlur(7)), np.float32)[..., None] / 255
    pool = Image.new('L', (W, H), 0)
    ImageDraw.Draw(pool).ellipse([318, 286, 488, 326], fill=255)
    ImageDraw.Draw(pool).ellipse([360, 294, 452, 318], fill=255)
    pool = np.asarray(pool.filter(ImageFilter.GaussianBlur(9)), np.float32)[..., None] / 255
    light += pool * COOL * 1.5 + beam * COOL * 0.35
    col = base * light
    am = pa_np[..., 3] > 0.5                                   # 빛 받는 모서리 하이라이트
    rim = am & ~(np.roll(am, 1, 0) & np.roll(am, 1, 1))
    col += rim[..., None] * np.clip(light - 0.15, 0, None) * 0.22
    col += beam * np.array([0.24, 0.27, 0.3]) * (0.7 + 0.6 * N1[..., None])   # 공기 중 빛줄기
    em = np.asarray(Image.alpha_composite(EMI, sw_emi), np.float32) / 255
    em[..., 3] *= 1 - (np.asarray(MASKS['chandelier'], np.float32) / 255)   # 창 앞 샹들리에는 유리를 가림
    col = col * (1 - em[..., 3:4]) + em[..., :3] * em[..., 3:4]
    rs = np.random.RandomState(4)
    for _ in range(190):  # 먼지
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
    if with_fg:
        col = col * (1 - fg[..., 3:4]) + fg[..., :3] * fg[..., 3:4]
    col += (GRAIN[..., None] - 0.5) * 0.025
    img = Image.fromarray((np.clip(col, 0, 1) * 255).astype(np.uint8), 'RGB')
    return img.quantize(palette=PAL_IMG, dither=Image.Dither.FLOYDSTEINBERG).convert('RGB')


imgs = {}
for state in ('cecilia', 'adela'):
    im = render(state)
    imgs[state] = im
    im.save(OUT / f'lobby_v6_{state}_native.png')
    im.resize((W * 3, H * 3), Image.NEAREST).save(OUT / f'lobby_v6_{state}.png')
    bg_only = render(state, with_fg=False)
    bg_only.save(OUT / f'lobby_v6_{state}_bg_native.png')   # Unity: 플레이어 뒤 배경
    # Unity: 걷는 선(310px)보다 앞에 놓인 소품 = 플레이어 앞에 그림
    front = np.zeros((H, W), bool)
    for n_ in ('switch_sword', 'map_table', 'bell'):
        front |= np.asarray(MASKS[n_]) > 0
    piece = bg_only.convert('RGBA'); piece.putalpha(Image.fromarray((front * 255).astype(np.uint8)))
    piece.save(OUT / f'front_props_{state}_native.png')
FG.save(OUT / 'foreground_native.png')                                              # Unity: 플레이어 앞 전경

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
# 소품 시트
items = [(n_, Image.open(OUT / 'props' / f'{n_}.png')) for n_ in sorted(m.stem for m in (OUT / 'props').glob('*.png'))]
S2 = 3
cells = [(n_, im_.resize((im_.width * S2, im_.height * S2), Image.NEAREST)) for n_, im_ in items]
cols = 5
cw = max(c_.width for _, c_ in cells) + 40
rh = max(c_.height for _, c_ in cells) + 70
sheet = Image.new('RGBA', (cw * cols, rh * math.ceil(len(cells) / cols)), (10, 10, 12, 255))
sd = ImageDraw.Draw(sheet)
f2 = ImageFont.truetype('C:/Windows/Fonts/malgun.ttf', 22)
for i, (n_, c_) in enumerate(cells):
    x, y = (i % cols) * cw, (i // cols) * rh
    sheet.alpha_composite(c_, (x + (cw - c_.width) // 2, y + rh - 50 - c_.height))
    sd.text((x + cw // 2, y + rh - 25), n_, font=f2, fill=(225, 220, 210), anchor='mm')
sheet.save(OUT / 'props_sheet.png')
print('ok', sorted(MASKS))
