# 영상 속 도트 도구 시험 (2026-11-19, Claude)

사용자가 보낸 '오늘코딩' 쇼츠 2편(AI 게임 리소스 오픈소스 8가지)의 도구를 이 프로젝트에 대 봤다.
**게임 파일(Assets·씬·코드)은 바꾸지 않았다.** 시험은 모두 임시 폴더와 이 폴더에서만 했다.

| 도구 | 실행 | 결과 |
|---|---|---|
| PixelOE 1.0.0 | 실행함 (로컬, 임시 가상환경) | 2배 축소·윤곽 1·색 줄이기 없음 설정이 현재 HUD 도트보다 선이 또렷하고 눈 색이 살아 있다. 3배 이상은 지저분하다 |
| codixel | 실행함 (Node, 외부 통신 없음) | 게임 도트 그대로 뼈대 3개(몸·머리·사슬 팔)를 붙여 흔들기 8프레임. 디자인은 유지되지만 통째로 돌리는 방식이라 큰 자세 변화(사망·포획)에는 맞지 않는다 |
| 2dimg2motion | Codex에 설치함 (`~/.codex/skills/2dimg2motion`) | Codex 내장 이미지 생성이 필요하다. Claude Code에서는 돌릴 수 없어 사용자가 Codex에서 실행해야 한다 |
| sprite-gen | 실행 못 함 | `codex` CLI·`grok` 로그인·ffmpeg가 이 PC에 없다 |
| agent-sprite-forge | 실행 못 함 | 이미지·영상 생성 API 키나 Codex·Grok CLI, ffmpeg 5.1+가 필요하다 |
| SSD (Sprite Sheet Diffusion) | 실행 못 함 | 그래픽카드가 MX450 2GB라 확산 모델을 돌릴 수 없다 |
| pixelbolt | 받지 않음 | 코드로 도트를 찍는 도구다. 코드 대체 그림은 이전에 사용자가 기각했다 |

## PixelOE

- 스크립트: [pixeloe_portrait_trial.py](pixeloe_portrait_trial.py) (`pip install pixeloe` 후 실행)
- 비교 그림(Git 제외, 원본 일러스트가 비공개라서): `Docs/Design/Characters/reference/tool-trial-20261119/`
  - `stage0_*`: 왼쪽부터 현재 게임 / PixelOE 2배 / 3배 / 3배+현재 팔레트 (밝은 배경, 색 줄이기 없음)
  - `stage3_*`: 첫 시도. 회색 배경에 색을 128/64/48개로 줄였더니 전체가 회색으로 바래서 버렸다
- 판단: 애니 일러스트를 도트화하는 단계(살롱·에필로그 CG, HUD)에 쓸 수 있다. 그림이 PC 밖으로 나가지 않는다.

## codixel

- 스크립트: [codixel_rig_trial.js](codixel_rig_trial.js) / 결과: [rig_sheet.png](rig_sheet.png), [sway.gif](sway.gif)
- 판단: 숨쉬기·흔들림·사슬 휘두르기 같은 작은 반복 동작에 쓸 만하다. 새 자세는 그릴 수 없다.

## 2dimg2motion 쓰는 법 (Codex)

Codex에서 이렇게 요청한다: `/2dimg2motion use <세실리아 원본 그림> to generate <동작> animation`.
결과는 HD 14프레임이므로 PixelOE로 55px 도트로 줄인 뒤, 원본 도트와 실제 게임 크기로 비교하고 나서 적용한다.
