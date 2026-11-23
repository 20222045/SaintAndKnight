# 원본 도트를 유지하는 장면별 모션 프롬프트

> **2026-11-18 적용 결과:** [남은 네 종류의 원본 도트 동작과 실제 GIF](StageRevision/RESULT_20261118.md). 아래 문구는 추가 제작용 이력이며 최신 게임 결과는 링크에서 확인한다.

> **최신 기준(2026-11-18):** 생성한 전신 포즈는 미적용으로 전환했다. 원래 게임의 전신 도트로 관절·손·장비를 맞추며, 보스 공통6개 키 모두 마지막 사진의 무릎 자세를 쓴다. 아래 공통1~6의 서로 다른 자세 설명은 과거 요청 이력이다. 현재 마무리 기준은 아래 문구와 `BossRevision/CONTACT_RESULT_20261118.md`다.

```text
Use the original full-body native game sprite, including its actual face, hair, costume, armour and pixel clusters. Do not replace the body with generated anatomy or merely palette-match a different drawing. The fully clothed character collapses onto bent knees, leans forward away from the boss behind her, raises one hand near her forehead, and bends the other arm across the front of her clothes. The boss's roots stay immediately behind her and hold the outside of her shoulder, upper arm and wrist. Keep the knees close and supported at floor level. Match the existing original character style and equipment. Keep visible contacts attached through every loop frame. No intimate contact or suggestive expression. The six common resource keys are phase variants of this same final posture, reached after the distinct boss attack introductions.
```

2026-11-17. 아래 문구는 이번 레퍼런스 순서를 따르는 추가 제작용이다. 현재 게임에 넣는 합성 시트와 별도로 보관한다. 생성 결과를 게임 적용 완료로 간주하지 않는다. 사진과의 정량적인 90% 싱크로율은 측정하지 않았다.

## 입력과 공통 조건

- 캐릭터 입력은 이 폴더의 `cecilia_start.png` / `adela_start.png` 또는 현재 게임 도트의 해당 포즈를 쓴다. 외부 게임 사진·GIF를 생성 도구에 올리지 않는다.
- 적과 보스는 현재 게임의 `Assets/SaintKnightDay09/Art/Enemies/`, `Assets/SaintKnightDay09/Art/Boss/` 원본을 참조한다.
- 아래 공통 문구 + 캐릭터 문구 하나 + 장면 문구 하나를 붙여 사용한다.
- 초안은 18칸: 도입 8칸, 마지막 자세의 연속 동작 10칸. 128×96px 셀, 투명 배경, Point/48 PPU. 벽 합성은 현재 벽 원본의 160×112px 셀을 유지한다. 도구가 규격을 달리 출력하면 원본 인물을 축소하는 방식으로 맞추지 않는다.
- 노출·의상 파손을 늘리지 않는다. 캐릭터를 다시 디자인하거나 장식을 없앤 결과는 사용하지 않는다.

```text
Animate the exact existing adult game character in the input image. The input is the authoritative identity and pixel-art style reference. Preserve the same face, hair length and arrangement, head ornament, body proportions, clothing coverage, costume layers, trim, weapon and palette. Keep the input character's native pixel scale. Do not redraw the character in a different style, simplify the outfit, shorten hair, change anatomy, add exposed skin, or increase resolution by painting new details. Use a fixed side-on 2D gameplay camera and stable floor anchor. Every frame must preserve the same character identity.

Create a readable nonsexual dark-fantasy combat capture and defeat animation. Use impact, loss of balance, gravity, delayed hair/cloth movement and clearly directed monster motion. Serious strain and fatigue; no suggestive expressions. No intimate contact, sexual acts, forced leg spreading, nudity, penetration or graphic injury. Roots contact outer sleeves, armour, upper arms and boots. Clothes remain intact.

Produce one transparent animation strip with 18 complete equal cells, no empty cells, text, labels, panels, background, glow haze or motion blur. Frames 1-8 show contact, resistance and loss of balance. Frames 9-18 are a seamless continuing-motion loop at the final pose. The heroine does not repeatedly return to the starting stance. Animate the monster, root tips, hanging chain, hair and cape with small delayed motions. No repeated whole-body thrusting or large rhythmic bouncing. Do not crop the heroine's head, hair, hands or equipment.
```

### 세실리아

```text
Cecilia Noel, age 22. Keep the exact approved long silver hair, original face and proportions, veil/head ornament, ornate ivory/lavender and gold layered long dress, long skirt panels and chain flail with its original weight. Native standing height is 55 pixels. Keep all costume ornaments, sleeves and folds. A generic short white robe or short-haired replacement is unacceptable.
```

### 아델라

```text
Adela Crow, age 26. Keep the exact approved long crimson ponytail, original face and proportions, ornate dark armour, shoulder plates, tall boots, long crimson cape, longsword and pointed shield. Native standing height is 62 pixels. Keep armour coverage, ornaments and all equipment. A simplified black bodysuit, generic dress, loose hair or changed shield is unacceptable.
```

## 1. 수도사 — 사진 1

```text
Use the game's original thin brown-hooded monk. He closes both hands around the heroine's clothed outer upper arms from behind. Keep a compact, enclosed silhouette with pressure from both sides of her shoulders, as if two sleeve-covered arms frame her. She braces, tries to pull forward, then her knees buckle and her head drops. Preserve the original costume and visible weapon. End in a low, forward-slumped held pose, the monk upright immediately behind and slightly to one side. Contact is confined to the upper arms; keep the bodies and hips separate. The final loop moves the monk's sleeves, her long hair, cape and hanging weapon slightly; the heroine never stands up again.
```

## 2. 처형인 — 사진 2

```text
Use the game's original broad hooded executioner and polearm. Keep the encounter compact and low: his heavy outer arm and blunt weapon shaft overpower the heroine's guard from behind/above, and her body folds sideways toward the floor. Show the guard failing, a short drop, then a fully clothed side-lying collapse next to his crouched stance. His bulk frames the fallen figure without straddling or lying on her. Keep her face, hair and original weapon readable. Continue subtle arm/shoulder weight shifts, cloth settling and chain slack in the final loop, without lifting the heroine repeatedly.
```

## 3. 점액 — 그대로

새 프롬프트·새 캐릭터 그림으로 교체하지 않는다. 현재 회랑 점액의 진행·흡수 표시를 유지한다.

## 4. 회랑 바닥 촉수 — 사진 3

```text
Thin dark crimson tentacles rise vertically from a floor crack. First catch the boots, then spiral around the outside of the clothing and outer forearms. Draw the arms upward as the body is lifted a little, with legs naturally together and original dress/armour intact. Keep the long, vertical wrapped silhouette. The heroine makes one failed attempt to resist; her head and shoulders then droop. The spiral closes from the legs toward the head until an opaque native-textured root cocoon contains her. In the final loop, coils tighten and loosen slightly, root tips flex and the base stays connected to the floor. No body parts remain floating outside the closed cocoon.
```

## 5. 살덩이 벽 — 사진 4

```text
Use the current game's rough ribbed organic wall, not a human assailant. Rooted folds reach around the heroine's outer upper arms from behind and pull her sideways into the wall. She sinks to one knee and leans forward, then her shoulder, torso, cape/skirt and finally her head are drawn into the same side opening. The direction is horizontal toward the wall, never a fall into a floor hole. Keep the original costume and face while visible. The last frame is a closed rough wall fold, with no detached hair, hand or armour fragments outside. The final loop flexes the folds and trailing root tips.
```

## 6. 보스 휩쓸기 — 그대로

현재 휩쓸기 피격·쓰러짐을 유지한다. 다른 잡힘 모션으로 바꾸지 않는다.

## 7. 보스 바닥 촉수 — 사진 5

```text
The boss's thicker dark crimson roots rise from below and behind, then close around both outer upper arms and the boots. Keep the heroine compact and forward-slumped inside a heavy low cradle, with a thick rear mass supporting the capture and two branches around the shoulders. Her face remains readable above the roots. She makes one short resistance movement, loses balance and slumps lower. Distinguish this from the corridor's tall thin spiral: this is a low, heavy clamp anchored to the floor. Rose thorns point away from her. Continue small root flexes, hair/cape settling and weight shifts in the final loop.
```

## 8. 보스 촉수 잡기 — 사진 6

```text
Thick roots extend horizontally from the Rose Madonna's mantle at the side of the screen. Catch the heroine's clothed upper arms, lift her feet a little and pull her diagonally sideways toward the boss. Preserve a clearly horizontal dragging direction, with weapon, long hair and cape trailing behind/downward. Her knees remain naturally together. After the failed resistance, the mantle/root folds begin to close around her from that side. Keep the boss's violet/gold garment design and the heroine's original costume. The final loop moves the supporting roots and loose cloth; avoid a repeated vertical bouncing motion.
```

## 9. 보스 점액 뱉기 — 사진 7

```text
Use the current boss-slime material. A thick, opaque stream falls from above onto the heroine's head ornament, shoulder and raised guard. She ducks, tries to shield her face, then sinks under the weight. The coating travels distinctly from top to bottom over the OUTSIDE of the original clothes and armour; it is never transparent or body-contouring. Long drips connect the upper coating to the floor puddle. Continue downflow until the figure is enclosed in an opaque mass. The final loop keeps droplets sliding down and the base rippling while the defeated character stays still inside.
```

## 10. 보스 공통 1 — 최신 사진1

```text
Keep the fully clothed heroine upright in a front three-quarter view. Thin dark roots rise vertically from the floor behind her and curl around her outer sleeves and forearms. Keep the face and most of the torso clear. Her head bows as the arms are held low and slightly apart. The final loop flexes the root arches and hanging clothes subtly; avoid thick horizontal bars across her chest.
```

## 11. 보스 공통 2 — 최신 사진2

```text
The heroine curls into a compact crouch while a native-textured root cocoon closes from below. Leave an oval opening for her original face and clothed shoulders; opaque roots contain the rest of the body. Keep a rounded vertical pod silhouette with overlapping organic coils, a stable floor base and small root-tip flexion. No detached body parts outside the cocoon.
```

## 12. 보스 공통 3 — 최신 사진3

```text
Dark red roots form a radial flower-like enclosure around the fully clothed heroine. The branches curve outward before returning toward her sleeves, leaving clear gaps around her face and ornate torso. Lift her slightly upright with arms bent near shoulder height and legs together. The final loop moves different branches with small phase delays, preserving a readable centred figure.
```

## 13. 보스 공통 4 — 최신 사진4

```text
Two long dark tentacles catch the outer wrists and draw BOTH ARMS clearly outward to the left and right, slightly above shoulder level. Lift the upright heroine by about13 native pixels. Keep fully clothed legs together and naturally hanging, with her long hair and original cape or dress falling under gravity. Each tentacle must remain visibly connected to its own wrist. The shield follows the right forearm. Keep the face clear; no horizontal coil pasted across her head or chest.
```

## 14. 보스 공통 5 — 최신 사진5의 비성적 구도

```text
The fully clothed heroine sinks into a compact seated pose with knees together under her original dress or armour. Tall rooted branches form a high backrest behind and around her. An overhead canopy curls slowly downward to either side of her head, leaving her face clear. Foreground roots lie over the outside of the clothing and arms. Preserve a tall enclosing silhouette and the seated weight. No exposure or suggestive expression.
```

## 15. 보스 공통 6 — 최신 사진6의 무릎 자세

```text
The heroine drops onto her covered knees, lowers her head and leans forward. A large curved root behind her braces the outer shoulder and catches the clothed upper arms. Preserve the low forward-slumped silhouette and close-together knees, with the cape or dress resting on the floor. The final loop flexes the rear root and sleeve contacts subtly without standing her up or bouncing the whole body.
```

## 실제 사용한 신규 자세 지시문과 검수

보스 자세 후보의 실제 입력·사용 도구·팔레트·얼굴 처리 범위는 [POSE_PROMPTS.md](BossRevision/POSE_PROMPTS.md)에 있다. 원본과 같은 게임 크기로 얼굴·머리·의상·무기·키를 비교한다. 동작은 잡는 위치, 당기는 방향, 마지막 자세, 반복 구간 순으로 확인한다. 게임 연결 후 포획·저항·HP0·R 복구·좌우 방향과 기존 회귀 검사를 확인한다. AI 생성 결과가 있다는 이유만으로 적용 완료로 간주하지 않는다.
