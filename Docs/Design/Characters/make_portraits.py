# 측면 HUD 전신 일러스트(사용자 제작, 2026-11-10) → 흰 배경 제거 PNG.
# 입력: Docs/Design/Characters/reference/portraits/cecilia_stage{0-4}.jpg (사용자 제공 순서: 4단계→0단계)
# 출력: Assets/SaintKnightDay06/Art/Portraits/cecilia_portrait_stage{0-4}.png + 미리보기 Docs/Design/Characters/portraits_preview.png
# 방법: 테두리에서 이어진 거의 흰 영역만 배경으로 보고,
#       경계 2px은 흰색을 알파로 환산해 어두운 패널 위에서 흰 테두리가 남지 않게 한다.
# 실행: python Docs/Design/Characters/make_portraits.py
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

HERE = Path(__file__).parent
ROOT = HERE.parents[2]
SRC = HERE / 'reference' / 'portraits'
OUT = ROOT / 'Assets/SaintKnightDay06/Art/Portraits'
OUT.mkdir(parents=True, exist_ok=True)
BG_MIN = 244          # 이 값 이상(모든 채널)이고 채도가 낮으면 배경 후보
PURE_MIN = 250        # 닫힌 영역도 이 값 이상이고 넓으면 배경(팔·다리 사이)
PURE_AREA = 600


def flood(cand, seeds):
    seen = seeds & cand
    while True:
        g = seen.copy()
        g[1:] |= seen[:-1]; g[:-1] |= seen[1:]; g[:, 1:] |= seen[:, :-1]; g[:, :-1] |= seen[:, 1:]
        g &= cand
        if (g == seen).all():
            return seen
        seen = g


def components(mask):
    from collections import deque
    lab = np.zeros(mask.shape, np.int32)
    sizes = [0]
    for y0, x0 in zip(*np.nonzero(mask)):
        if lab[y0, x0]:
            continue
        n = len(sizes); sizes.append(0)
        q = deque([(y0, x0)]); lab[y0, x0] = n
        while q:
            y, x = q.popleft(); sizes[n] += 1
            for yy, xx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
                if 0 <= yy < mask.shape[0] and 0 <= xx < mask.shape[1] and mask[yy, xx] and not lab[yy, xx]:
                    lab[yy, xx] = n; q.append((yy, xx))
    return lab, np.array(sizes)


def cutout(path):
    rgb = np.asarray(Image.open(path).convert('RGB')).astype(np.float32)
    lo, hi = rgb.min(-1), rgb.max(-1)
    cand = (lo >= BG_MIN) & (hi - lo <= 8)
    seeds = np.zeros_like(cand); seeds[0] = seeds[-1] = True; seeds[:, 0] = seeds[:, -1] = True
    bg = flood(cand, seeds)
    # 닫힌 흰 영역은 지우지 않음: 흰 드레스·베일도 완전한 흰색이라 구멍이 뚫림(1차 시도 결과).
    # 경계 띠: 배경에서 2px 안쪽까지는 흰색을 알파로 환산(흰 테두리 제거)
    band = np.asarray(Image.fromarray(np.uint8(bg) * 255).filter(ImageFilter.MaxFilter(5))) > 0
    band &= ~bg
    alpha = np.where(bg, 0.0, 1.0)
    edge_a = np.clip((255 - lo) / (255 - 150), 0, 1)
    alpha = np.where(band, np.minimum(alpha, edge_a), alpha)
    col = np.where(alpha[..., None] > 0, (rgb - 255 * (1 - alpha[..., None])) / np.maximum(alpha[..., None], 1e-3), 0)
    out = np.dstack([np.clip(col, 0, 255), alpha * 255]).astype(np.uint8)
    return Image.fromarray(out, 'RGBA')


frames = []
for stage in range(5):
    im = cutout(SRC / f'cecilia_stage{stage}.jpg')
    im.save(OUT / f'cecilia_portrait_stage{stage}.png')
    frames.append(im)
# 미리보기: HUD 패널 색(어두운 배경) 위
W, H = frames[0].size
prev = Image.new('RGBA', (W * 5 // 2, H // 2), (22, 18, 24, 255))
for i, f in enumerate(frames):
    prev.alpha_composite(f.resize((W // 2, H // 2), Image.LANCZOS), (i * W // 2, 0))
prev.save(HERE / 'portraits_preview.png')
print('ok', frames[0].size)
