# Claude 인계서 — 세실리아·아델라의 2D 보스 애니메이션

**2026-11-20 · SaintAndKnight · Unity 6000.0.81f1 · Windows**

이 작업의 기준은 게임에서 이미 쓰는 세실리아와 아델라다. 얼굴, 복장, 장비를 식별할 수 있게 두고 보스 공격마다 몸을 지탱하는 손과 발을 다르게 그렸다. 보스 휩쓸기는 기존 연출을 쓴다. 최근 수정한 부분은 촉수 잡기 첫 1초다. **먼저 실제 게임의 전후 GIF를 보고, 같은 동작을 새로 그릴 때만 프롬프트를 사용한다.** [전후 GIF](GrabTransition/grab-comparison.gif) · [복사 가능한 실제 프롬프트 원문](PROMPTS_2D.md)

## 지금 남은 작업 · 2026-11-20

**현재 적용본**에서 세실리아와 아델라는 보스 사망 7종을 모두 재생한다. 14시트·280프레임이며, 촉수 잡기의 첫 1초에는 인물마다 새로 그린 자세 네 장이 들어갔다. 휩쓸기는 요청대로 기존 연출을 사용한다. 이 버전의 보스방 그래픽 검사는 17/17, 전체 Play Mode는 198개 통과·26개 제외·실패 0개였다. Windows 빌드의 기동 확인 시간은 15초다.

| 순서 | 남은 일 | 확인할 화면과 끝나는 기준 |
| --- | --- | --- |
| 1. 최종 시각 검수 | 두 캐릭터의 일곱 장면을 원래 재생 속도로 보고 참고 자료와 대조한다. | ZIP의 `Preview/boss-patterns.gif`, `Preview/boss-finishes.gif`, `Private/UserReferences/`를 연다. 촉수 잡기는 뒷무릎이 먼저 닿고 손끝이 이마에 닿는지 본다. 각 장면에 **채택** 또는 **수정할 동작과 시점**을 기록한다. 요청한 95% 유사도는 정량 측정한 값이 아니다. |
| 2. Windows 수동 플레이 | 세실리아·아델라로 실제 보스전에서 피격부터 재시도까지 직접 조작한다. | `Builds/Win64_NativeDefeat/SaintAndKnight.exe`에서 치명타 → 해당 공격의 사망 동작 → 공통 마무리 → R 복귀를 본다. 두 인물의 보스 방향, 손과 장화 접점, 복귀 후 이동을 확인해 결과를 남긴다. 자동 검사와 별도로 수동 플레이 완료 기록이 남아야 한다. |
| 3. 검수 결과에 따른 선택 보강 | 추가 전신 중간 자세를 그린 장면은 현재 촉수 잡기다. 나머지 6종도 이미 게임에서 움직인다. | 움직임이 급하게 보인 장면만 새 포즈를 검토한다. 기존 요청 순서에 따라 **바닥 촉수 → 점액 뱉기 → 공통 4종**으로 본다. 예를 들어 바닥 촉수는 체중이 내려가고 손이 바닥을 짚는 사이, 점액은 어깨가 들리고 팔뚝이 눈을 가리는 사이를 본다. 수정한 경우 같은 시점의 전후 GIF를 다시 찍는다. |

Claude가 이어받을 때는 1번의 검수 기록부터 확인한다. 작업 폴더의 `Inbetween/run.py`는 별도 중간 자세를 만들기 위한 실험 스크립트다. 확인 시점에 해당 폴더에 출력 PNG는 없었으며, 이를 실행하는 것은 현재 적용본을 사용하는 데 필요한 단계가 아니다.

## 몬스터 자신의 모션 · 2026-11-20

1스테이지와 예배당 씬의 스프라이트 슬롯을 실제 표시 코드와 대조했다. 위의 사망 7종은 세실리아·아델라가 쓰러지는 장면이다. 아래 장수는 몬스터 자신에게 연결된 그림 수이며, 우선순위는 기존 요청 순서와 현재 동작의 빈 곳을 기준으로 제안한다.

| 제작 우선순위 | 현재 게임에서 보이는 동작 | 이어서 할 일 |
| --- | --- | --- |
| 1. 장미창의 성모 | 본체는 1·2페이즈 각각 2장 반복, 처치 시 1장이다. 촉수와 점액은 별도 효과로 움직인다. 공격이나 피격 전용 본체 포즈는 연결돼 있지 않다. | 바닥 촉수는 힘을 모았다가 솟는 순간 몸을 펴고, 촉수 잡기는 뻗었다 당기고, 점액 뱉기는 고개를 젖혔다 내미는 순서로 그린다. 휩쓸기는 기존 연출을 쓴다. 이후 피격 반응과 처치 자세로 무너지는 과정을 보강한다. |
| 2. 수도사 | 본체 14장. 대기 2장, 걷기 4장, 할퀴기 준비·타격·회복 각 1장, 잡기 준비·헛침 각 1장, 피격 1장, 쓰러짐 2장이다. 두 주인공과의 잡힘 합성은 각각 별도 8장이다. | 할퀴는 손이 가장 멀리 나간 순간과 피격 판정이 맞는지 먼저 본다. 동작이 끊겨 보이면 팔을 뻗고 거두는 사이와 피격 후 몸이 돌아오는 자세부터 보강한다. |
| 3. 처형인 | 본체 16장. 대기 2장, 걷기 4장, 낫 공격 준비 3장·타격 2장·회복 1장, 피격·아머 파괴 각 1장, 쓰러짐 2장이다. | 기존 낫 궤적을 기준으로 타격 직후 무게가 실리는 발과 낫을 회수하는 팔을 검수한다. 아머 파괴와 일반 피격이 게임 크기에서 구별되는지도 확인한다. |

일반 적의 PNG와 프레임 목록은 [Day09 몬스터 원화](../../Enemies/Day09/README.md), 보스 전신 원화는 [장미창의 성모 v4](../../Boss/RoseMadonna-v4/README.md)에 있다. 이번 확인은 씬과 코드 기준이다. 새 몬스터 포즈의 제작·연결과 실제 속도 촬영은 후속 작업이다.

## Claude에 바로 붙여 넣을 작업 요청

```text
SaintAndKnight의 CLAUDE.md와 Docs/Design/DeathScenes-20261117/BossReference/CLAUDE_2D_HANDOFF.md를 읽어 줘. 보스 사망 장면은 2D 도트로 이어간다. 먼저 GrabTransition/grab-comparison.gif에서 이전과 현재를 비교하고, CharacterPoses의 두 전신 디자인 시트 및 GrabTransition/input의 해당 인물 기준 PNG를 확인해 줘. 동작을 수정할 때는 PROMPTS_2D.md의 공통 본문과 그 캐릭터 문단을 합쳐 사용해 줘. 앞발·활동하는 손·반대쪽 손·옷이나 머리가 멈추는 순서를 구체적으로 적어 줘. 채택한 원화를 128×96칸으로 만들고 손과 장화 접점을 갱신한 뒤, 보스방 실제 카메라 GIF로 결과를 확인해 줘. 기존 씬과 .meta GUID를 유지해 줘. 사용자 참고 사진은 Private에 보관해 줘.
```

## 어떤 그림을 열어야 하나

| 필요한 것 | 파일과 용도 |
| --- | --- |
| 원래 얼굴과 게임 전신 | 프로젝트 루트의 `Assets/SaintKnightDay01/Art/{cecilia,adela}_approved_cropped.png`와 `Docs/Design/DeathScenes-20261117/{cecilia,adela}_start.png`. 전신 원화 생성 때 이 순서로 같은 인물의 두 장을 입력했다. |
| 완성 디자인 | [세실리아 8자세](CharacterPoses/cecilia.png) · [아델라 8자세](CharacterPoses/adela.png). 왼쪽 위부터 대기, 바닥 촉수, 촉수 잡기, 점액 뱉기, 뿌리 고치, 진홍 꽃받침, 장미와 사슬, 겹고리다. |
| 중간 자세 생성 입력 | [세실리아 기준](GrabTransition/input/cecilia-standard.png) · [아델라 기준](GrabTransition/input/adela-standard.png). 실제 모델에 입력한 투명 PNG이며, 원화를 다시 자를 필요가 없다. |
| 중간 자세 출력 | [세실리아 키포즈 4장](GrabTransition/cecilia.png) · [아델라 키포즈 4장](GrabTransition/adela.png). 1254×1254 녹색 원화에서 왼쪽 위 → 오른쪽 위 → 왼쪽 아래 → 오른쪽 아래 순서다. |
| Unity 베이크 입력 | `Assets/SaintKnightSpriteCook/Editor/`의 `*_boss_pose_keys.png` 2장, `*_grab_entrance.png` 2장, `BossPoseMarks.json`, `GrabEntranceMarks.json`. |
| 게임이 실제로 읽는 파일 | `Assets/SaintKnightSpriteCook/Resources/SaintKnightSpriteCook/DefeatMotion/`. 세 공격은 `<인물>_boss_{spikes,grab,spit}_defeat.png`, 공통 마무리는 `<인물>_boss1.png`부터 `boss4.png`까지다. 전체 14시트·280프레임이다. |
| 전달 ZIP | 프로젝트의 `Private/Claude-Boss2D/SaintAndKnight-Claude-2D.zip`. `START_HERE.md`에서 시작한다. `Preview/`에는 이번 버전 전체 GIF, `Private/UserReferences/`에는 사용자 원본 7장과 GIF 1개가 있다. 프로젝트의 Git 제외 규칙에 따라 원본 참고 자료를 비공개 폴더에 보관한다. |

사용자 사진 1은 바닥 촉수, 2는 촉수 잡기, 3은 점액 뱉기, 4~7은 공통 마무리 1~4에 대응한다. 별도 참고 GIF는 촉수 잡기의 4초 박자와 2.4초 부근의 두 번째 숙임을 잡는 데 썼다. 사용자가 제공한 원본의 캡처 프로그램은 확인되지 않았다. 아래 촬영 절차는 우리가 Unity에서 만든 플레이 GIF에 해당한다. 게임 프레임에 들어가는 인물은 프로젝트 캐릭터로 그렸다.

## 무엇으로 만들었나

| 단계 | 실제 방법 |
| --- | --- |
| 새 전신 자세 그리기 | Codex 내장 ImageGen에 기준 그림과 영어 프롬프트를 함께 보냈다. 2dimg2motion으로 중간 동작 기준 PNG를 표준화하고 화면 기준 좌우, 앞발, 몸이 낮아지는 순서를 정했다. Claude 쪽 이미지 생성 도구가 다를 경우에도 같은 기준 PNG와 본문을 입력할 수 있다. |
| 기본 7종을 도트로 옮기기 | [기본 포즈 pack.py](CharacterPoses/pack.py)가 배경 분리, 축소, 포즈당 64색 양자화와 투명 아틀라스 배치를 담당한다. |
| 새 촉수 잡기 4자세를 도트로 옮기기 | [중간 포즈 pack.py](GrabTransition/pack.py)가 녹색 배경을 분리한다. 게임 목표 크기의 2배로 준비한 뒤 PixelOE의 `pixel_size=2`, `thickness=1`, `mode="contrast"`, `do_quant=False`로 축소한다. 알파 경계는 128에서 이진화하고 Codixel이 네 칸을 한 줄에 묶는다. 세실리아와 아델라의 원화 축척은 각각 10과 9를 사용했다. |
| 보스와 연결 | `SKBossReferenceArt.cs`에서 캐릭터 레이어와 보스 재질을 합성한다. 잡기의 접점은 올라가는 팔, 버티는 손목, 앞 장화에 맞춘다. 런타임은 `contacts.json`으로 실제 보스에서 캐릭터까지 촉수를 잇는다. PixelFX 0.3.1의 `water/drip`은 점액 장면의 낙숫방울과 바닥 물결에 사용했다. |
| 시각 검사 | Pixelbolt의 onion 보기로 앞뒤 포즈를 겹쳤다. Agent Sprite Forge의 sheet_qc로 시트 경계를, sprite-gen으로 중복·바닥선을 확인했다. 아래 6px 여백에서 Forge의 기본 15% 안전 여백 경고가 있었고 실제 잘림은 0이다. ‘agentsprite’는 프로젝트의 기존 시험과 같은 Agent Sprite Forge로 해석했다. SSD는 이번 그림의 생성 경로에 포함하지 않았다. |

## 실제 타이밍과 수정 지점

| 0부터 센 프레임 | 보스 촉수 잡기에서 보이는 동작 |
| --- | --- |
| 0 | 기존 게임 피격 자세다. |
| 1–2 | 두 손은 낮고 무릎이 풀린다. |
| 3–4 | 몸이 내려가고 한 손은 무릎, 반대 손은 가슴 쪽으로 움직인다. |
| 5–6 | 뒷무릎이 닿는다. 올라오는 손과 이마 사이에 공간이 남는다. |
| 7 | 손끝이 이마에 닿는다. |
| 8–31 | 기존 무릎 자세로 이어진다. 17~21번에서 다시 숙인다. 치명타의 32칸은 8fps에서 4초다. |

원화의 포즈를 변경하면 해당 `pack.py`의 `MARKS`도 그림을 보고 다시 잡는다. 중간 포즈는 **올라가는 쪽 윗팔, 버티는 손목, 앞 장화, 얼굴** 순서다. 기준점은 원화 왼쪽 위가 (0,0)인 픽셀 좌표다. 4장 모두 앞 장화가 같은 게임 위치를 향하도록 정렬하고, 런타임 좌우 반전은 게임에 맡긴다. 도트는 48 PPU, Point, 무압축이다.

### 원화 변경 후 재생성

사용한 도구를 다른 PC에 준비할 때 참고할 버전이다. [2dimg2motion d4d73e2](https://github.com/WU-HAOTIAN34/2dimg2motion/tree/d4d73e2) · [PixelOE 1.0.0](https://github.com/KohakuBlueleaf/PixelOE) · [Codixel 604a9dc](https://github.com/mankidev/codixel/tree/604a9dc) · [Pixelbolt fa27bd7](https://github.com/skulitom/Pixelbolt/tree/fa27bd7) · [Agent Sprite Forge 25203d0](https://github.com/0x0funky/agent-sprite-forge/tree/25203d0) · [sprite-gen 35a27e1](https://github.com/aldegad/sprite-gen/tree/35a27e1).

프로젝트 루트에서 다음을 실행한다. `$OePython`에는 PixelOE·PyTorch·Pillow·NumPy가 설치된 Python의 경로를, `$Codixel`에는 Codixel 체크아웃의 `core/pixel.js` 파일 경로를 넣는다. Node도 실행 가능해야 한다. 이미 전달된 게임용 PNG는 이 도구 설치 없이 Unity에서 바로 읽힌다.

```powershell
$art = 'Docs/Design/DeathScenes-20261117/BossReference'
python "$art/CharacterPoses/pack.py"
& $OePython "$art/GrabTransition/pack.py" $Codixel
```

그다음 Unity 메뉴 `Tools / SaintKnight / Art / Bake Native Defeat Motions`를 실행한다. 촉수 잡기 첫 1초만 바꾸면 두 인물의 `*_boss_grab_defeat.png`에서 1~7번 칸이 바뀌는지 확인한다. 베이크는 함께 쓰는 매니페스트와 접점 JSON도 다시 기록한다.

## 실제 게임을 무엇으로 찍었나

촬영은 **Unity Play Mode의 예배당 씬과 게임의 Main Camera**로 했다. 테스트가 두 플레이어를 차례로 배치하고 치명타 원인을 지정해 원하는 장면을 재현한다. `Assets/Tests/PlayMode/NativeDefeatTests.cs`의 `CaptureAllNativeScenes`, `CaptureClip`, `RenderCurrentGame`이 촬영 코드다.

1. 테스트가 `Day09_BossChapel`에서 실제 스프라이트 키와 텍스처를 확인한다. 게임 카메라와 HUD를 **1280×720 RenderTexture**로 그린다.
2. 게임 시간 기준 **0.125초마다** 캐릭터 주위 **384×256 PNG**를 저장한다. 패턴 중 바닥·점액은 18샘플, 잡기와 공통 장면은 32샘플로 찍는다. 공통 시트 자체는 18칸이며 촬영에는 반복 구간도 들어간다.
3. [review.py](review.py)가 촬영 PNG를 재배치하고 120ms와 130ms를 번갈아 써서 평균 8fps GIF를 만든다. [전후 비교 스크립트](GrabTransition/review.py)는 이전과 현재의 같은 프레임 번호를 좌우로 붙인다. 녹화나 GIF 보간 툴은 필요하지 않다.

**촬영 재현 명령**. PowerShell에서 프로젝트 루트를 현재 폴더로 열고 실행한다. 그래픽 촬영 단계에는 그래픽 장치가 필요하다.

```powershell
$unity = 'C:/Program Files/Unity/Hub/Editor/6000.0.81f1/Editor/Unity.exe'
$project = (Resolve-Path '.').Path
$run = Join-Path $project ('Logs/ClaudeBoss2D-' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $run | Out-Null
$env:SK_NATIVE_CAPTURE_ROOT = $run
$argsForUnity = @('-batchmode','--burst-disable-compilation','-projectPath',('"{0}"' -f $project),
    '-screen-width','1280','-screen-height','720','-runTests','-testPlatform','PlayMode',
    '-testFilter','NativeDefeat','-testResults',('"{0}/graphics.xml"' -f $run),
    '-logFile',('"{0}/graphics.log"' -f $run))
$job = Start-Process $unity -ArgumentList $argsForUnity -WindowStyle Hidden -Wait -PassThru
if ($job.ExitCode -ne 0) { throw "Unity capture failed. See $run/graphics.log" }
python Docs/Design/DeathScenes-20261117/BossReference/review.py "$run/Screens" "$run"
```

촬영 후 `$run/Screens/index.csv`의 요청한 장면과 표시된 장면, `graphics.xml`의 실패 수, 완성 GIF의 양손과 앞발을 함께 확인한다. 이번 그래픽 검사는 17/17 통과했다. 직전 버전과 게임 PNG를 픽셀 단위로 비교한 결과 두 캐릭터 모두 1~7번 칸만 바뀌었다. 8번 이후 48개 프레임에서 양팔과 촉수 사이의 최대 간격은 0px였다. 캐릭터 원화만 필요한 작업은 ZIP의 PNG로 바로 시작할 수 있다.
