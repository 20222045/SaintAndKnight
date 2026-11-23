# 실제 이미지 생성 프롬프트

각 캐릭터를 별도 호출로 생성했다. 아래 **공통 본문 뒤에 해당 캐릭터 문단 하나**를 붙여 한 번에 입력한다. 영어 본문은 제작 당시 사용한 문구다. 같은 입력으로 다시 생성해도 그림은 달라질 수 있다. 기존 게임을 재현하려면 인계본에 들어 있는 확정 PNG를 사용한다.

## 1. 대기와 보스 장면 7종의 전신 디자인

입력 1은 해당 인물의 `Assets/SaintKnightDay01/Art/<이름>_approved_cropped.png`다. 입력 2는 `Docs/Design/DeathScenes-20261117/<이름>_start.png`다. 이름은 `cecilia` 또는 `adela`다. 내장 ImageGen에 이 순서로 이미지를 첨부했다. 불투명 배경 출력. 채택 원화는 [세실리아](CharacterPoses/cecilia.png)와 [아델라](CharacterPoses/adela.png)다.

**공통 본문**

```text
Create a polished production CHARACTER ACTING AND COSTUME model sheet for a gothic side-scrolling action game. Use the first image solely as the approved FACE, hair colour and trim identity reference; use the second image as the authoritative head-to-toe gameplay CLOTHING, silhouette, accessory and proportion reference. Show exactly the SAME adult character in eight distinct full-body key drawings on one detailed landscape 3:2 sheet, fixed four columns by two rows with ample gutters and aligned feet within each row. Every figure visible head to toe, all cells clearly separated. Uniform muted slate background, flat solid color, no panels, text, numbers, lines, borders or shadows on ground. Hand-crafted high-end gothic pixel art, anatomically precise mature proportions, crisp stepped pixel clusters, dramatic readable silhouette at thumbnail size, restrained light edges for cloth or steel. Maintain exact dress length, armor coverage and palette across all eight views. A non-erotic costume and movement study for animators. No monster, spell particles or vines; hands, face and boots should be visible. Each cell is ONE keypose, vastly different orientation, convincing center of gravity and new limb drawing. View from side or 3/4. TOP row: 1 standing idle as native sprite, signature gear, this is the identity key. 2 floor-strike recoil: dropping into a very low asymmetric crouch, one shoe planted out in front, one arm thrown low and out to balance. 3 staggered one-knee stance, one elbow flexed high, that palm touches her own forehead, far hand braces on the forward knee, bowed head. 4 standing recoiling from a splash, shoulder high, near forearm crossing in front of brow like a visor, other hand out and low, one foot stepping back. BOTTOM row: 5 curled tightly on one hip in profile, nearly horizontal diagonal folded torso, knees drawn close, forearms shelter the face, dramatic compact low silhouette; 6 both arms open diagonally, lifted from behind into a weightless sitting posture, feet dangling asymmetrically, thin air visible between arms and torso; 7 seated upright on invisible high support, one elbow reaches backward to brace on the seat, other hand by knee, feet forward and head looking sideways; 8 compressed into a narrow, side-facing one-knee huddle, elbows tight, one forearm pressed to opposite shoulder, face raised, markedly different from side-curled cell 5. Keep occupied hands free of equipment in cells 2-8. Avoid repeated standing figures. Give each key powerful restrained acting with its own facial expression.
```

**세실리아에만 덧붙인 문단**

```text
Cecilia: pale silver hair, blue eyes, ivory liturgical veil with a thin gold embroidered border and small gold clasp, layered ivory and warm gray vestments, long skirt, dark boots, recognizable silver flail/censer on chain in idle. Softer silhouette, folds compress over knee, veil drifts a beat after head.
```

**아델라에만 덧붙인 문단**

```text
Adela: red high ponytail, amber eyes, dark blued articulated steel armor, layered shoulder plates with antique gold cross ornament, red cloak/scarf, gauntlets, armored greaves, sword and tall dark shield in idle. Square strong posture, show planted greave taking weight and different shoulder plate articulation, crimson cloth or ponytail trailing motion. Maintain armour and clothing from gameplay sprite.
```

## 2. 촉수 잡기의 중간 동작

캐릭터마다 기준 그림 **한 장만** 첨부했다. [세실리아 입력 PNG](GrabTransition/input/cecilia-standard.png) · [아델라 입력 PNG](GrabTransition/input/adela-standard.png). 이 그림은 1단계 시트의 위쪽 세 번째 칸에서 배경을 분리하고 2dimg2motion의 `standardize_baseline.py`로 전신의 긴 변을 360px에 맞춘 결과다. 실제 옵션은 `--margin-ratio 0.26`이었다. 불투명한 녹색 배경으로 생성한 뒤 추출 단계에서 투명화했다. [세실리아 출력](GrabTransition/cecilia.png) · [아델라 출력](GrabTransition/adela.png).

같은 원화에서 직접 기준 그림을 추출하려면 좌상단 원점의 픽셀 좌표로 세실리아는 (800,112)–(1135,490), 아델라는 (789,80)–(1148,482)를 자른다. 우측과 하단 좌표는 포함하지 않는다. 원화 왼쪽 위 64×12 영역의 RGB 중앙값을 배경으로 잡고, 채널별 최대 차이가 14를 넘는 픽셀을 남긴다. 마스크에 3×3 중앙값 필터를 적용해 투명 PNG로 저장한다. 그 PNG를 아래 명령에 넣는다. 최종 기준 입력의 크기는 세실리아 481×548, 아델라 504×548이다.

```powershell
python "$HOME/.codex/skills/2dimg2motion/scripts/standardize_baseline.py" cecilia-raw.png --output cecilia-standard.png --subject-max 360 --margin-ratio 0.26
python "$HOME/.codex/skills/2dimg2motion/scripts/standardize_baseline.py" adela-raw.png --output adela-standard.png --subject-max 360 --margin-ratio 0.26
```

**공통 본문**

```text
Create exactly four consecutive NEW action drawings, one 2-by-2 storyboard sheet, same original game character as supplied. It is the ONE-KNEE IMPACT transition BEFORE the reference pose. Production side-on gothic 2D action game sprite keys, tasteful fully dressed serious combat, single solid saturated flat pure chroma green #00ff00 everywhere outside figures. Each cell 50% of width and height. No text, UI, borders, shadows or VFX. Full bodies including all feet, with empty gutters. Orthographic camera locked, consistent head size across all four drawings. Match the reference's painterly pixel clusters and whole costume including lengths, proportions and colors exactly. ALWAYS facing screen-right, same screen-right FRONT boot in precisely the same position relative to cell, back knee on screen-left. The screen-right hand rises to temple and the screen-left hand goes to knee; never swap. No weapon held. Show four clearly different silhouettes from top left to bottom right: 1. Upright stumble, knees just begin flexing, BOTH hands below waist and torso still nearly vertical, rear knee far from floor. 2. Mid-descent with rear knee halfway down and front thigh angled, one hand reaching toward knee, right hand at chest height well below chin. 3. Rear knee strikes ground, torso and head pitched slightly forward, left palm lands on forward knee; right forearm halfway up, rising wrist level with mouth, with a visible gap between fingertips and forehead. 4. Same kneel as exact supplied reference, right fingertips meet forehead and left hand braces knee, head bowed; trailing fabric/hair settling. Silhouette should descend progressively, character occupies less of cell height as she kneels. Upper body, eyes and brow readable at small game scale.
```

**세실리아에만 덧붙인 문장**

```text
Silver-haired Cecilia, embroidered gold-trim ivory veil and long ivory layered dress; gravity carries the veil after her shoulder movement.
```

**아델라에만 덧붙인 문장**

```text
Red ponytail knight Adela, dark plate mail, crimson cape, gold-trim shoulder plates and dark armored shins; ponytail arcs upward on landing and settles behind her.
```

## 다음 동작에 맞게 프롬프트를 바꾸는 법

1. 새 포즈와 가장 가까운 확정 전신 PNG를 한 장 고른다. 캐릭터 이름, 앞발, 화면 기준 좌우 손, 장비를 쥔 손, 소재 색은 고정한다.
2. 동작은 네 문장으로 쓴다. **무게가 이동하는 순간 → 먼저 닿는 부위 → 그 직후 아직 따라오는 부위 → 최종 자세** 순으로 쓴다. 각 문장에 머리·양손·앞발 중 적어도 두 곳의 위치를 명시한다.
3. 예를 들어 점액 방어라면 “뒷발을 내딛는다 → 어깨가 올라가고 팔꿈치가 접힌다 → 팔뚝이 눈앞에 도착한다 → 체중이 뒷발에 실리고 베일 끝이 내려온다”가 동작 본문이다. 이는 다음 작업용 예시다.
4. 게임에서 구분해야 할 것을 눈으로 판정한다. 앞발이 미끄러지는지, 손이 좌우를 바꾸는지, 두 중간 자세가 축소 후 같은 윤곽이 되는지 살핀다. 그림이 바뀌면 원화 픽셀 좌표를 기록한 `MARKS`와 분할 칸도 새 출력에 맞춰 갱신한다.
