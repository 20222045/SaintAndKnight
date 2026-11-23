# Claude 구현 인계서 — 바로 이어서 작업하기

기준: 2026-11-07 (Asia/Seoul). 이 문서는 현재 코드·씬·마지막 성공한 검사 결과를 읽고 작성했다.
먼저 루트 [CLAUDE.md](../CLAUDE.md)를 읽는다. 이 두 문서로 전체 재분석 없이 작업 범위와 수정 위치를 정할 수 있다. 실제 수정 대상 코드와 새로 제공된 자료의 국소 확인은 생략하지 않는다.

## 1. 지금 프로젝트를 여는 방법

**회랑 3D 실험 (2026-11-19)**. `Tools > SaintKnight > Preview > Open Corridor 3D Preview`로 분리 씬을 열어 Play. `Toggle 2D / 3D Corridor`로 전환. 640×360 렌더와 카메라 스냅은 실험 씬의 `SKCorridorPreview`에 있다. 그래픽 1/1, 세실리아 걷기 32쌍. Play Mode 191 통과, 실패 0, 전체 213, Explicit 22. [기록·비교·재촬영](Design/Stage/CORRIDOR_3D_PREVIEW.md)

| 항목 | 위치/방법 |
| --- | --- |
| 프로젝트 | `C:/UnityProject/SaintAndKnight` |
| Unity | `C:/Program Files/Unity/Hub/Editor/6000.0.81f1/Editor/Unity.exe` |
| 버전 근거 | [ProjectVersion.txt](../ProjectSettings/ProjectVersion.txt) |
| 시작 씬 (로비) | [Lobby_Sanctuary.unity](../Assets/SaintKnightLobby/Scenes/Lobby_Sanctuary.unity), 생성 코드 [SKLobbySceneBuilder.cs](../Assets/SaintKnightLobby/Editor/SKLobbySceneBuilder.cs), 동작 [SKLobby.cs](../Assets/SaintKnightLobby/Runtime/SKLobby.cs) |
| 최신 플레이 씬 | [Day08_Scrolling.unity](../Assets/SaintKnightDay08/Scenes/Day08_Scrolling.unity). [Day 5 훈련 씬](../Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity)은 회귀 비교용 |
| 시작 씬 등록 | [EditorBuildSettings.asset](../ProjectSettings/EditorBuildSettings.asset), 로비가 첫 번째, 그다음 Day 8 |
| 플레이 입력 | A/D·방향키 이동, Space 점프, J 공격, Tab 전환, R 훈련장 초기화. 로비: W/↑ 상호작용 |
| 개발용 입력 | Q 피해 / E 회복. 제품 입력 설정 완료 상태가 아님 |
| 패키지 | [manifest.json](../Packages/manifest.json). Input System 1.19.0, uGUI 2.0.0, Test Framework 1.6.0, URP 17.0.4 |
| 런타임 어셈블리 | [SaintKnight.Runtime.asmdef](../Assets/SaintKnight.Runtime.asmdef), Input System·uGUI 참조 |
| 테스트 어셈블리 | [SaintKnight.PlayModeTests.asmdef](../Assets/Tests/PlayMode/SaintKnight.PlayModeTests.asmdef) |

Unity Hub에서 이 폴더를 기존 프로젝트로 열면 된다. 새 프로젝트 생성·패키지 전체 업그레이드·Library 삭제는 인계 절차에 필요하지 않다. 이전 Day 1/2/4 씬은 회귀 비교용으로 남아 있다.

## 2026-11-20 촉수 잡기 2D 중간 자세

두 캐릭터의 촉수 잡기 첫 1초에 2D 자세 네 장씩을 넣어 뒷무릎 착지와 이마에 손이 닿는 순간을 나눴다. 프롬프트 원문과 그림을 담은 PDF·비공개 ZIP도 제작했다. 기존 32칸에서 1~7번을 교체했다. Play Mode 198개 통과·26개 제외·실패 0개, 보스방 그래픽 17/17. Windows 빌드·15초 기동 확인. [프롬프트·그림·촬영 절차](Design/DeathScenes-20261117/BossReference/CLAUDE_2D_HANDOFF.md).

## 2026-11-19 보스 사망 캐릭터 그림 적용

세실리아와 아델라의 동작별 캐릭터 원화를 일곱 보스 사망 연출에 연결했다. 두 캐릭터 합계 14시트·280프레임이고, 고정 자세 168프레임에서 양팔과 촉수 접점 오차는 0px였다. Play Mode 191개 통과·23개 제외·실패 0개, 별도 그래픽 16/16. Windows 빌드와 15초 기동을 확인했다. 원화 분리는 `CharacterPoses/pack.py`, 프레임 베이크는 `SKNativeDefeatBuilder.Bake`다. [원화·실제 게임 촬영](Design/DeathScenes-20261117/BossReference/CharacterPoses/README.md).

## 보스 사망 레퍼런스 7장 · 2026-11-19

바닥 둥지·이마 손 잡기·세로 점액과 공통 마무리 4종을 두 캐릭터에 적용했다. 14시트·280칸. 잡기는 4초이며 실제 보스까지 촉수가 이어진다. 공통은 뿌리 고치 → 진홍 꽃받침 → 장미와 사슬 → 겹고리 순환이다. 옛 공통 5·6은 호환 리소스로만 남아 있다. 휩쓸기와 살아 있는 포획은 원래 그림을 사용한다.

제작은 `SKBossReferenceArt.cs`, 베이크는 `SKNativeDefeatBuilder.Bake`다. 살아 있는 포획은 `LoadHeld`, 사망 전용 패턴은 `_defeat` 시트를 쓴다. `contacts.json` 버전 3이 각 자세의 팔·장화 위치를 실제 보스와 연결한다. Play Mode 185개 통과(실패 0), 관련 그래픽 16/16, Windows 빌드·15초 기동 확인. `Logs/BossReference20261118/`에 XML과 실제 카메라 캡처가 있다. [결과와 재현](Design/DeathScenes-20261117/BossReference/RESULT.md).

## 1-0-stage-defeat. 2026-11-18 남은 수도사·처형인·회랑·벽 모션 (Codex)

- 남은 수도사·처형인·회랑 바닥·살덩이 벽을 두 캐릭터 모두 새 원본 도트 동작으로 마무리했다. 양팔 붙듦/옆으로 접힘/수직 삼키기/수평 벽 흡수를 구분했다. 보스와 일반 점액 PNG25개는 해시 불변. Play Mode 185통과/실패0(전체204/Explicit19), 관련 그래픽 16/16. 실제 촬영·Windows 빌드·15초 기동 확인. [최신 결과](Design/DeathScenes-20261117/StageRevision/RESULT_20261118.md).
- `SKNativeStageDefeatBuilder`는 기존 베이크 클래스의 partial이다. 수도사의 원본 Pounced/Hurt/Held/Crouch 단계와 두 소매 접점, 처형인의 옆으로 접힌 원본 도트/받치는 두 팔, 회랑 수직 삼키기, 벽 수평 흡수를 각각8시트/144칸에 구현했다. 장비는 바닥에 남는다. 런타임 판정·기존 씬·GUID 유지.
- 접점 `DefeatMotion/stage_contacts.json`:144프레임/붙듦82/164접촉부, 픽셀 간격 최대0.00px. 원본 의상 크롭과 실제 두 캐릭터×두 적×양방향/R 복구 검사를 포함했다. 보스 기존 접점160프레임은 유지한다.
- 입력은 원본 아틀라스/적/함정13개로 정리(기존36개 중 미사용 합성 입력 제외), 원본41 PNG 및 완료 보스/점액25 PNG 해시 불변. 새 그림 생성/참고 사진 업로드 없음.
- 증빙 `Logs/EnemyDefeat20261118/final-graphics.xml`, `final-full.xml`, `final-build.log`, `player-smoke.log`. 신규12사례/36전체/216동작, 전체32사례/96전체/576동작. [적 두 종류 GIF](Design/DeathScenes-20261117/StageRevision/stage-enemies.gif), [바닥·벽 GIF](Design/DeathScenes-20261117/StageRevision/stage-traps.gif). 정량95% 일치율·창 수동 조작감 미검수.

## 1-0-carry. 2026-11-18 실제 보스까지 공통 사망 운반 (Codex)

- 보스 공통 무릎 자세를 실제 보스 앞으로 들어 옮기는 연출을 연결했다. 원본 전신 도트·복장 유지, 넓은 촉수 날개를 짧은 팔/무릎 감김과 실제 보스에서 이어지는 세 가닥으로 교체. 두 캐릭터×좌우4사례 도착 확인. Play Mode 182통과/실패0(전체201/Explicit19), 관련 그래픽 13/13. Windows 빌드·15초 기동 확인. [최신 결과](Design/DeathScenes-20261117/BossRevision/CARRY_RESULT_20261118.md).
- `SKBossDefeatCarryView` + `SKCharacterSpriteView`: 사망 원인의 실제 보스를 기억하고 공통 무릎 자세에서 `Core_Heart` 앞으로 그림 레이어만 운반한다. 0.75초 준비 후1.5~3.5초 이동, 최종 자세 반복. 원본 Rigidbody/충돌체/HP/AI/씬은 그대로다. `SKTrapEnvelopeView`는 운반 중 중복 그림을 숨긴다.
- `SKNativeDefeatBuilder`: 넓은 촉수 꼬리를 짧은 윗팔/손목/무릎 감김으로 교체. 런타임 세 가닥은 실제 접점에 붙는다. 원본41 PNG 해시 보존, 현재36입력(기존 보스 질감 추가), 생성 캐릭터0. 낙하 장비는 원래 바닥에 남는다. 26시트/468칸 규격 유지.
- 증빙 `Logs/BossCarry20261118/final-graphics.xml`, `final-full.xml`, `final-build.log`, `player-smoke.log`. 운반4사례/동작192/전체16, 기존32사례/동작576/전체96. [양방향 실제 GIF](Design/DeathScenes-20261117/boss-delivery.gif). 정량95% 사진 일치율·창 수동 조작감 미검수.

## 1-0-contact. 2026-11-18 원본 전신·최신 사진의 공통 무릎 자세 (Codex)

- 사용자 최신 사진에 맞춰 보스 공통6키의 마지막 자세를 무릎·이마 손·앞으로 굽힌 팔·뒤에서 붙듦으로 통일했다. 생성 포즈를 제외하고 원본 전신·팔·손·장비를 사용한다.26시트/468칸, 반복160프레임의 윗팔·손목 접점 검사. Play Mode 180/180(전체198/Explicit18/실패0), 그래픽10/10. 실제32사례 캡처·Windows 빌드·15초 기동 확인. [최신 결과](Design/DeathScenes-20261117/BossRevision/CONTACT_RESULT_20261118.md).
- `SKNativeDefeatBuilder.Rig/NativeActor/MirroredActor/KneelingBossScene`: Day06 원본 전신을 사용하고 팔·손·장비만 관절 배치한다. 생성 포즈 경로는 manifest에서0개다. 원본35개 해시 보존. 공통6개 키는 같은 무릎 자세의 작은 위상 변형이며, 최신 사진 지시가 이전6종의 다른 최종 자세를 대체한다.
- 실제로 그린 성장 곡선과 손·팔 접점을 맞춘다. `contacts.json`은 두 캐릭터×8가시 포획×10반복=160프레임의 붙잡힌 부위와 촉수 픽셀 간격. 별도 원본 의상 크롭 검사를 포함한다. 캐릭터는 보스 반대 방향으로 숙이고, 뒤쪽 촉수를 등 가까이 모았다.
- 증빙 `Logs/TentacleFit20261118/final-full.xml`, `finish-graphics.xml`, `finish-build.log`, `player-smoke.log`. 실제 캡처96장+동작576장, [공통 자세 GIF](Design/DeathScenes-20261117/boss-common-kneeling.gif). 씬·전투 판정·기존 GUID 보존. 수동 창 조작감·정량95% 사진 일치율은 검증하지 않았다.

## 1-0-boss-revision. 2026-11-17 보스 후속 수정 (Codex)

- 26시트/468칸(두 캐릭터×13종), 보스3패턴+공통6종. 원본 PNG·머리·팔레트 보존, AI 보조 신규6자세·곡선 촉수. Play Mode 178/178(전체196/Explicit18/실패0), 그래픽8/8. 실제26사망+6포획 캡처96장/동작576장, Windows 빌드·15초 기동 확인. 수동 조작감 미검수. [최신 결과](Design/DeathScenes-20261117/BossRevision/RESULT.md).
- `SKNativeDefeatBuilder.BossPose/BossScene`: 별도 후보 원본은 `Art/DefeatPoses/`, 서기/들림55·62px·원본 Idle 팔레트·머리 픽셀 적용. 기존 가로 촉수 조각을 연속 곡선으로 교체. 새 사진6장을 공통 `boss1`~`boss6`에 대응한다. 점액·휩쓸기·나머지 적별 모션 유지.
- `SKNativeDefeatLibrary.CommonBossCount=6`. 패턴→공통은 완성된 반복8번 칸부터 이어져 다시 서는 도입을 반복하지 않는다. 사망 표시용 Unlit 소재는 Resources 의존성으로 빌드에 포함하며 R/회복/비활성 때 원래 전투 소재·정렬 복구.
- `Logs/BossRevision20261117/full-final.xml`, `graphics-display-final.xml`, `build-final.log`, `player-smoke.log`. 그래픽 촬영은 코루틴의 Update 이후 색상 덮임을 피하도록 화면 최종 표시를 다시 적용한 후 렌더링한다. 런타임은 기존 LateUpdate가 이 역할을 한다. 씬·전투·HP·탈출 판정 변경 없음.

## 1-0-native. 2026-11-17 원본 도트 포획·사망 모션 (Codex)

- [설계·적용·재현](Design/DeathScenes-20261117/DESIGN.md). 현재 게임 원본을 새 Resources `SaintKnightSpriteCook/DefeatMotion/`에 24시트/432칸으로 합성했다. 벽160×112, 나머지128×96, 48 PPU/Point. 캐릭터 재생성·리사이즈·재채색·씬 재생성 없음.
- `SKNativeDefeatLibrary`/`SKCharacterSpriteView`: HP 비율 포획, 치명타 순간 원인 보존, 보스3패턴→2초 뒤 공통5종 순환. `SKFloorSpike.OnHitAccepted`: 휩쓸기 콤보 중 실제 촉수 치명타 구분. `SKFleshWall`: 기존 벽 렌더러에서 단독 합성 표시. `SKTrapEnvelopeView`: 흡수 후 숨겨진 본체 대신 표시. `SKGameOverScreen`: 새 모션은 재시도키까지 대기하고 사망 도트만 세계의 어두운 막 앞에 그린다. R/회복/비활성 때 원래 정렬 복구. 점액·휩쓸기·전투·탈출 시간 유지.
- Play Mode **180/180**(전체197/Explicit17/실패0), `Logs/NativeDefeat20261117/full-final.xml`. 그래픽 **8/8** `graphics-final.xml`. 24사망+6실제포획의 전체화면90장/동작크롭540장은 `Screens/index.csv`; 실제 GIF는 적용서 폴더. 수동 조작감 검수와 구분한다.
- Windows 빌드 `Builds/Win64_NativeDefeat/SaintAndKnight.exe`, `build2.log` 성공. 합성·빌드 도구는 새 `SaintKnight.NativeDefeat.Editor.asmdef`로 Editor에만 포함한다. Unity의 배치 캡처는 end-of-frame을 기다리지 않고 기존 그래픽 프로브와 같은 Play Mode 카메라 렌더를 사용한다.
- 별도 Rest 작업의 `SKHotSpring.cs`에서 빠진 `using UnityEngine.UI;` 한 줄만 보완했다. 다른 Unity 작업은 종료하지 않고 기다렸다. 현재 포즈 범위의 조합본이며 정밀 신규 관절 포즈는 [프롬프트](Design/DeathScenes-20261117/MOTION_PROMPTS.md)를 별도로 제공한다. 외형 불일치 AI 생성분은 미적용.

## 1-0. 2026-11-16 Day 17~30 (Claude)

- 빌드 첫 씬은 타이틀 `Assets/SaintKnightDay26/Scenes/Title.unity`(`SKTitleSceneBuilder.Create`로 재생성). 흐름: 타이틀 → 로비 → 문(새 출격)/지도 탁자(이어하기) → `Day09_Stage1`(체크포인트 `Checkpoint_Nave` x1340, `Checkpoint_Forecourt` x1840, `SKCheckpointPlacer`) → 보스 예배당 → 처치 2.5초 뒤 입구 빛, W로 성소.
- `SKRunDirector`가 플레이어가 있는 씬마다 자동 생성된다: 이어받기(`SKCarry`, 문·메뉴가 씬을 열 때만), 쓰러짐 기록, 보스 클리어, 이펙트(`SKHitFx`), Esc 일시정지(`SKPauseMenu`), 화면 글자 숨김(`SKDay01HUD.HideStatusText`). 씬을 직접 열면(검사·에디터) 예전처럼 처음부터 시작한다.
- 저장: `SKSave` → `persistentDataPath/save.json`, 배치 실행(검사·빌드 확인)은 `save_batch.json`.
- 씬에 저장되는 MonoBehaviour는 반드시 클래스 이름과 같은 파일에 둔다(`SKTitleMenu.cs` 분리 사유).
- 검사 명령은 아래 절과 같다. 단 `Start-Process -Wait`는 Unity 자식 프로세스까지 기다려 멈출 수 있으니 `-PassThru` 후 `$p.WaitForExit()`를 쓴다. 그래픽 캡처는 `-nographics` 없이 `-testFilter Day17To30Tests.CaptureNewScreens`.
- 상세·남은 일: [DAY17_30.md](DAY17_30.md).

## 1-1. 2026-11-08 업데이트

2차 분석 업그레이드 적용: [REFERENCE_UPGRADE_20261108.md](REFERENCE_UPGRADE_20261108.md). 회귀 테스트 50개(기존 37 + `ReferenceUpgradeTests` 13), 측정용 `PlaytestProbe` 4개(Explicit, `-testFilter PlaytestProbe`로 실행, `PLAYTEST|` 로그). 적 `hurtRecoverySeconds`/`strikeArmored`/`grabWindupArmored` 추가. 새 파일 `SKGrabState.cs`(잡힘 상태·연타 미터). `SKEnemyAgent`의 `grab*` 필드, `SKDay01Player`의 `jumpReleaseVelocityScale`/`airSpeedMultiplier`/`faceAttackerOnHit`, `SKEnemyTrainingSession.autoRetrySeconds`는 Day 5 씬에서만 켜져 있다. 이 문서의 아래 수치 표·37개 표현은 그 이전 기준이다. 사본: `Logs/ClaudeUpgrade/Before/`.

로비(거점) 도트 콘셉트 시안 v1~v6, 최신은 v6(전경 장애물·폐교회 소품 + CC0 외부 에셋, 출처 `Design/Lobby/external/gothicvania_church/SOURCE.md`). v3(다크 고딕, 로비의 칼 = 캐릭터 전환 — 사용자 결정): [Design/Lobby/LOBBY_CONCEPT.md](Design/Lobby/LOBBY_CONCEPT.md). 2026-11-08 사용자 요청("이걸로 로비해줘")으로 v6를 Unity 로비 씬으로 구현. Day 5 씬에서 플레이어·HUD를 가져와 적을 빼고 만든 새 씬이며 Day 5 파일은 저장하지 않음. 배경 640×360 @48 PPU = 게임 카메라 화면, 배경 z=+1/앞 소품 z=−0.5/전경 z=−1 + sortingOrder −10/15/20. 앞 소품(`lobby_front_*.png` = 칼·지도 탁자·종)과 전경은 플레이어 앞에 그림. 상호작용 지점은 아트 픽셀 x 기준(칼 216, 독서대 304, 제단 414, 지도 탁자 509, 문 575). `LobbyTests` 6개 + `CaptureLobbyInPlayMode`(Explicit, 그래픽 있는 배치에서만 `Logs/LobbyPlayCapture_*.png`). 편집 모드 cam.Render 캡처는 그리기 순서가 틀려 신뢰하지 말 것. 검사 Logs/ClaudeRuns/20261108_023502: 56/56.

## 1-2. 2026-11-10 Day 6 다중 프레임 적용

- 실제 적용 대상은 기존 로비·Day 5 `SKCharacterSpriteView`. 씬 재생성과 재부착 메뉴 실행은 필요 없다. 기존 씬과 `.meta` GUID를 유지했다. [다중 프레임 아틀라스와 시간표](../Assets/SaintKnightDay06/Resources/SaintKnightDay06/)는 `output/imagegen/day06-native-motion/`에서 가져왔다. `SKCharacterMotionLibrary`가 128×96 셀, 48 PPU, Point, 기존 피벗으로 읽는다.
- `SKPlayerCombat`이 준비·타격 진행률과 취소 버전을 제공한다. 준비/타격/회수 그림만 바뀌며 피해, 판정, 쿨타임, 히트스톱은 전투 코드가 소유한다. `SKGrabState`는 잡기 시작·연타·성공 탈출 시각을 기록한다. 잡기 실패나 내던지기에 성공 탈출 그림을 쓰지 않는다. 브라우저 시연의 위치 이동은 적용하지 않고 실제 점프·탈출 물리를 쓴다.
- HP 손상 단계별 대기, 쓰러짐 12장, 2D HUD는 기존 에셋을 사용한다. 변경 전 파일 사본은 `Logs/Day06MotionApply/Before/`에 있다. 검수와 다음 작업은 [현재 구현 상태](IMPLEMENTATION_STATUS.md)를 참조한다.
- 세실리아 달리기 발 미끄러짐: `cecilia_motion_frames.json` Run 시간을 500→412ms로 줄여 수정(그림 변경 없음, 사본 `Logs/Day06FootSlide/Before/`). 아트를 다시 뽑아 JSON을 덮어쓰면 이 값이 되돌아가니 접지 발끝 이동 속도가 172.8px/초에 맞는지 다시 잰다.
- 미결 결정 처리(2026-11-10): 적 반격 `counterArmorAfterHit`, `jumpDuringAttack` 끔, `crouchHeight` 0.72, B `attackHeight` 0.475. 상세 [REFERENCE_UPGRADE_20261108.md](REFERENCE_UPGRADE_20261108.md) 7절. 미뤄 둔 것(적 종류가 늘면 재검토): 지상 공격 발 묶기, 잡힘 중 Tab 탈출, 적 타이밍 무작위.
- Play Mode 70/70 통과(실패 0, Explicit 9 건너뜀, `Logs/ClaudeRuns/20261110_134818_decisions2/`). [실행 파일](../Builds/Win64_Day06Motion/SaintAndKnight.exe)은 이 버전의 Windows 빌드이며, 그래픽 없는 배치 기동을 15초 확인했다. 로비에서 문 앞에 접근해 W/↑로 Day 5에 진입한다. 이전 `Builds/Win64/`는 이 적용 전 버전이다.

## 1-3. 2026-11-10 Day 8 횡스크롤 카메라

- Day 5를 복제한 [Day08_Scrolling](../Assets/SaintKnightDay08/Scenes/Day08_Scrolling.unity)을 빌드 목록에 등록했다. 로비의 문은 Day 8로 이동한다. Day 7의 아머 파괴와 사슬 견제 수치도 반영했다. 전투 프로필을 수정할 때는 Day 8도 별도 직렬화 값을 갖는 점을 확인한다.
- [SKHorizontalCamera](../Assets/SaintKnightDay08/Runtime/SKHorizontalCamera.cs)는 플레이어 X만 따라간다. 경계 `[-6.6667, 20]`에 화면 전체가 들어가도록 카메라 중심을 제한한다. 원래 카메라 영역 안에 16:9를 맞추며 HUD는 화면 좌표를 사용한다.
- 기존 배경을 오른쪽에 좌우 반전해 붙이고, 바닥 콜라이더를 연장하고, 오른쪽 벽을 옮겼다. Day 8 플레이어의 Rigidbody2D 보간을 켰다. Day 9에서 지형을 교체할 때 배경 범위, 물리 벽과 `horizontalBounds`를 함께 맞춘다.
- [Day08CameraTests](../Assets/Tests/PlayMode/Day08CameraTests.cs)는 추적, 점프, R 초기화와 좌우 화면·물리 경계를 검증한다. Play Mode **75/75 통과**(Explicit 9, 종료 코드 0). [결과 XML](../Logs/ClaudeRuns/20261110_140233_day07/playmode-results.xml). 변경 전 사본은 `Logs/Day08/Before/`에 있다. [Day 8 Windows 빌드](../Builds/Win64_Day08/SaintAndKnight.exe) · [빌드 로그](../Logs/Day08/Win64/build.log).

## 1-4. 2026-11-10 Day 7 캐릭터별 공격 특성

- `SKPlayerCombat.AttackProfile`에 `breaksArmor`(아델라 켬)·`interruptDelay`(세실리아 0.2, 11-10 0.3→0.2). `SKHitbox.Begin` 끝 인수 두 개 → `SKHealth.TryTakeDamage`가 `LastHitBreaksArmor`/`LastHitInterruptDelay`로 기록 → `SKEnemyAgent.OnDamaged`가 읽는다.
- 아머 파괴는 `strikeArmored`·`grabWindupArmored`만 무시하고 맞은 뒤 반격(`windupArmored`)은 유지한다. 지연은 준비/타격/잡기 준비를 실제로 끊었을 때만 `staggeredUntil`에 걸리고, 그 대기 중 들어온 타격은 시간을 줄이지도 늘리지도 못한다(늘어나면 끊고 연타로 반격 전에 처치됨, `CuttingTheFirstTellThenMashingStillMeetsTheCounter`). `ResetAgent`가 지운다.
- 값 위치는 Day 5·Day 8·로비 씬과 `ConfigureReferenceCombat`. 검사 `CombatDecisionTests` 3개 + 세 씬 프로필 일치 1개. 프로필을 바꾸면 세 씬을 함께 바꾼다. 결과 `Logs/ClaudeRuns/20261110_143306_day07_day08`.

## 1-5. 2026-11-10 Day 9 첫 스테이지 기본 지형

- [Day09_Stage1](../Assets/SaintKnightDay09/Scenes/Day09_Stage1.unity): 무너진 대성당 4화면(2560×360px = 53.3유닛, 왼끝 −6.667). 로비 문이 이 씬으로 간다. Day 8은 카메라 비교용으로 남김.
- 배경(2026-11-10 최종): 사용자 선택으로 `Docs/Design/Stage/pixel-redraw-v1/stage1_bg.png`(충돌선에 맞춰 다시 그린 도트, 불투명). 아래 v2 원경·전경층은 삭제했고 `make_day09_art.py`는 `stage1_layout.json` 원본으로만 쓴다(그 v2 그림을 다시 복사하지 말 것).
- 보스 예배당(2026-11-10): 스테이지 x2500 문에 닿으면(Day 11부터 키 입력 없음, [SKSceneDoor](../Assets/SaintKnightDay09/Runtime/SKSceneDoor.cs)) → [Day09_BossChapel](../Assets/SaintKnightDay09/Scenes/Day09_BossChapel.unity)(그림 `boss-chapel-v1`, 640×360 한 화면 고정 카메라, 바닥 y310, 시작 x72, 장미창의 성모). 같은 메뉴 `Tools > SaintKnight > Day 09 > Create Stage 1 and Boss Chapel Scenes`가 두 씬을 만든다. 씬 이동 시 HP·선택 캐릭터는 초기화(Day 20~21에서 처리).
- (이전) 배경 v2: 원경 `stage1_far.png`(1440px, [SKParallaxLayer](../Assets/SaintKnightDay09/Runtime/SKParallaxLayer.cs) — 빌더가 계수·오프셋을 계산해 카메라 양끝에서 화면을 딱 덮음, 정렬 −20), 본경 `stage1_bg.png`(아치·지붕 틈은 투명, 정렬 −10), 전경 `stage1_front.png`(정렬 20, 화면 위아래 가장자리만). v1 생성기는 `make_day09_art_v1.py`로 보존.
- 원본은 하나: [make_day09_art.py](Design/Stage/make_day09_art.py)가 그림(원경·본경·전경)과 `stage1_layout.json`(충돌 상자·시작점·적 위치, 그림 픽셀 단위)을 같이 만든다. 지형을 바꾸면 이 스크립트를 고치고 `v1/`을 `Assets/SaintKnightDay09/Art/`로 복사한 뒤 `Tools > SaintKnight > Day 09 > Create Stage 1 Scene`([SKStage1SceneBuilder](../Assets/SaintKnightDay09/Editor/SKStage1SceneBuilder.cs))을 실행한다. 이 메뉴는 Day 9 씬만 덮어쓰고 Day 8 파일은 저장하지 않는다. `Day09StageTests`의 픽셀 상수(바닥 310, 위층 250, 천장 170)도 함께 맞춘다.
- 구역: 묘지 A1 → 회랑 B1(Day 10 점액 웅덩이 자리 1120~1300px) → 계단(280px)·위층(250px)·낮은 천장(170px, 위층에서 점프하면 막혀 A2를 넘지 못함, 계단 칸 아래는 A2 시야 사각) → 예배당 앞 A3+B2, 닫힌 문 2500px(Day 11). 적 A·B는 Day 8의 A·B를 복제해 전투 수치가 같다. R/전투 불능 재시도는 스테이지 시작점으로 돌아간다(체크포인트는 Day 20).
- 적 그림(2026-11-10 사용자 도트): [Design/Enemies/Day09](Design/Enemies/Day09/README.md) → `split_day09_frames.py` → `Assets/SaintKnightDay09/Art/Enemies/<set>/<motion>_fNN.png` → 빌더가 피벗(x64, 발 = 충돌 상자 바닥)으로 임포트하고 [SKEnemySpriteView](../Assets/SaintKnightDay09/Runtime/SKEnemySpriteView.cs)에 넣는다. 표시 전용: 상태만 읽고, 잡는 동안 수도사가 합성 그림을 그리며 플레이어 SpriteRenderer를 끈다(풀리면/비활성화되면 켬). `SKEnemyAgent.ArmorBrokenAt`·`RecoveringFromGrabMiss`는 그림용 읽기 값이다. 합성 탈출(0.15초)·내던지기(0.25초)는 풀린 직후 그 시간만 보이고 각자 그림으로 돌아간다(그 사이 실제 몸 간격이 그림 간격과 맞음). 탈출 판정은 `Time.time - EscapedAt < 0.1`(`ForceRelease`가 `HeldSince`를 지우므로 시각 비교 금지). 그림을 바꾸면 분할 스크립트 → 빌더 순서로 다시 실행. 처형인 그림(키 80px)은 판정 상자(55px)보다 크다. 임시 실루엣 PNG는 남아 있으나 미사용.
- Play Mode **86/86 통과**(전체 96, Explicit 10) [결과 XML](../Logs/ClaudeRuns/20261110_200446_day09_boss_final/playmode-results.xml).
- 검사: `Day09StageTests` 9개(지형 4 + 적 그림 4 + 보스방 문 1) + `CaptureEachSection`(Explicit, 그래픽 배치에서 `-testFilter Day09StageTests.CaptureEachSection` → `Logs/Day09Capture_0~3.png`). Play Mode **81/81 통과**(전체 90, Explicit 9) [결과 XML](../Logs/ClaudeRuns/20261110_160139_day09/playmode-results.xml). Windows 빌드·수동 조작감 미실시. 사본 `Logs/Day09/Before/`.

## 1-6. 2026-11-10 첫 보스 방향

- 사용자가 첫 보스로 **장미창의 성모**를 선택했다. Day 22~23 전투는 아래처럼 앞당겨 구현했다. Day 10 바닥 촉수·점액 웅덩이는 보스가 그대로 부를 부품(위치·예고·판정을 받는 컴포넌트)으로 만든다. [보스 설계·도트 요청](Design/Boss/BOSS_ROSE_MADONNA.md)

## 1-7. 2026-11-10 첫 보스 구현 및 전신 도트 적용

- [SKBossAgent](../Assets/SaintKnightDay09/Runtime/SKBossAgent.cs): 상태 Intro → Idle/Tell/Strike/Recovery/Holding/Exposed/Transition/Dead. 시간은 scaled time, 히트스톱 중 정지, 끊긴 공격 재발동 없음. 심장 피해 배율은 `SKHealth.DamageScale`(닫힘 0.2). 촉수 끊기는 별도 `Tentacle_Target` SKHealth(맞으면 즉시 회복)의 `Damaged`에서 `LastHitInterruptDelay`(휘두르기)·`LastHitBreaksArmor`(잡기)로 판단. 잡기는 `SKGrabState` 그대로(연타·내던지기). 점액은 `SKDay01Player.SpeedMultiplier`. R/자동 재시도는 `SKEnemyTrainingSession.TrainingReset`으로 보스·소환 수도사 초기화(세션 적 목록은 비움 — 한 번도 깨어나지 않은 적은 `ResetAgent`에서 body가 null).
- 수치는 [설계서](Design/Boss/BOSS_ROSE_MADONNA.md) 3~5절과 같고 SKBossAgent의 직렬화 기본값이다. 빌더 `BuildBoss`가 오브젝트(심장·휘두르기·바닥 촉수 3·끊기 대상·경고·그림·체력바)를 만든다. 테스트용 `ForceNext(SKBossAttack)`.
- 그림 v4는 사용자 첨부 일러스트를 바탕으로 만든 전신 도트다. 베일·금빛 후광·묵주·드레스가 보이는 1페이즈 대기 240/242px 2장, 후광과 옷에 붉은 빛이 도는 2페이즈 2장, 부서진 후광 아래 무릎을 꿇은 처치 1장을 기존 `Art/Boss/boss_body_*_NOT_FINAL.png`에 적용했다. 몸 칸 256×300, 48 PPU, 중심 x=128, 바닥은 셀 하단이다. 심장 그림은 바닥 위 36px에서 치맛자락에 겹친다. 씬과 스프라이트 GUID는 기존 연결을 쓴다. [원본과 가져오기 방법](Design/Boss/RoseMadonna-v4/README.md)
- 게임 그림은 v4를 [fit_to_game.py](Design/Boss/RoseMadonna-v4/fit_to_game.py)로 맞춘 결과(색 보정·살덩이 뿌리·심장 자리·2페이즈 오염·처치 뿌리). 심장·촉수·점액은 [make_boss_sprites.py](Design/Boss/make_boss_sprites.py)(이제 몸은 출력 안 함), 살덩이 함수는 `flesh_tools.py`. `import_rose_madonna.py`는 보정 없는 원본 칸(되돌리기용). 처치 색은 `SKBossAgent.Update`(실행 순서 100)가 체력 코드의 회색을 되돌린다.
- 검사 `Day09BossTests` 7개 + `CaptureBossFight`(Explicit). 이전 v3 Play Mode **93/93**(전체 104, Explicit 11). [이전 결과](../Logs/ClaudeRuns/20261110_212043_boss_v3/playmode-results.xml)

## 1-7b. 2026-11-10 첫 보스 약화

- HP 360(빌더 `SetHealth`), 닫힌 껍질 ×0.3(`SKBossAgent.shellDamageScale` 기본값), 키 130px(`fit_to_game.py` `STAND`; 위치 값은 240px 기준 측정을 `K`로 축소). 심장 그림 반지름 12px, 판정은 그대로. 검사는 보스 실제 값으로 계산. Play Mode 94/94 [결과](../Logs/ClaudeRuns/20261110_223954_boss_130/playmode-results.xml).

## 1-8. 2026-11-10 세 번째 플레이 캐릭터(성모) 계획

- 결정: 가시 후광 원거리(피해 14·준비 0.18·속도 9·사거리 6·간격 0.65 제안), 보스 처치 시 해금(`PlayerPrefs` `SK.Unlocked.Madonna`), Tab은 해금된 캐릭터만 순환, HP 공유. 그림(노출 없음) 도착 후 구현. [MADONNA_PLAYABLE.md](Design/Characters/MADONNA_PLAYABLE.md). 두 캐릭터 고정 코드: `SwitchCharacter`의 `1 - selected`, `SKPlayerCombat` 프로필 선택, `SKCharacterSpriteView`, `SKDay01HUD`, `SKLobby`.

## 1-9. 2026-11-10~11 Day 10 함정

- 공용 부품 [SKFloorSpike](../Assets/SaintKnightDay10/Runtime/SKFloorSpike.cs)(`Arm(바닥 위치, 지연)`/`Cancel`, `period`>0이면 스스로 반복, `TrainingReset`에 리듬 재시작)와 [SKSlimePool](../Assets/SaintKnightDay10/Runtime/SKSlimePool.cs)(`Spawn(위치, 초)`/`Clear`, `permanent`; 모든 웅덩이가 정적 목록에서 같은 `SpeedMultiplier`를 계산). 피해 대상·넉백 기준은 촉수 히트박스의 owner(보스 = 보스 체력, 스테이지 = 각 틈의 적 팀 SKHealth, 맞을 일 없음).
- 보스: `spikes`(SKFloorSpike[3])·`puddle`(SKSlimePool). 예고·활성·피해는 각 부품 값(0.7/0.3/15), 간격·거리·회복은 보스 값.
- 스테이지: 빌더 `BuildTraps`가 `trapDay10`에서 점액(양끝 15px 안쪽)과 틈 2개(양끝 50px 안쪽, 2.4초 주기, 1.2초 엇갈림)를 만든다. 수치를 바꾸면 메뉴로 두 씬을 다시 만든다.
- 점프 최고 약 1.13유닛이라 높이 1.4유닛인 촉수를 피하려면 옆으로 비켜야 한다. [SKFleshWall](../Assets/SaintKnightDay10/Runtime/SKFleshWall.cs)은 스테이지 x1945px(예배당 앞, A3 전)에 있다. 바닥에 선 플레이어가 좌우 1.2유닛 안에 들어오면 보라 예고 0.5초 뒤 `SKGrabState`로 잡는다(연타 탈출, 0.5초마다 피해 4, 4초 뒤 내던지기 12). 잡기·탈출 뒤 2.5초, 헛잡기 뒤 1.5초 쉰다. 예고를 유도하고 물러섰다가 쉬는 동안 지나갈 수 있다. 플레이어 잡힘 상태는 `SKPlayerCombat.Awake`가 만드므로 `Start`에서 찾는다.
- 정식 도트(2026-11-11) 24장, 48 PPU, 바닥 가운데 피벗. [make_day10_floor_art.py](Design/Stage/make_day10_floor_art.py)는 바닥 촉수 예고 2·솟음 3·들어감 2장(32×96)과 점액 반복 3장(64×12)을 만든다. 스테이지와 예배당 씬에서 공유한다. [make_flesh_wall.py](Design/Stage/make_flesh_wall.py)는 벽 대기 3·뻗음 1장과 캐릭터별 삼켜짐 2·버팀 2·탈출 1장(160×112)을 만든다. 합성은 잡힌 방향에 따라 반전하고 플레이어 단독 그림을 잠시 가린다. 연타 탈출 뒤 0.15초간 탈출 그림을 표시하고, R·비활성화 때는 즉시 단독 그림을 복원한다. 출력은 `Assets/SaintKnightDay10/Art/`에 있으며 기존 벽 도트 세 장의 GUID를 보존했다. 미리보기 [바닥 촉수·점액](../Logs/Day10Art/floor-trap-preview.png), [살덩이 벽](../Logs/Day10Art/flesh-wall-preview.png). 변경 전 사본 `Logs/Day10Art/Before/`.
- Day 14(2026-11-12): 플레이어 반응 도트(촉수 찔림 `SpikeHurt`·점액 위 걷기 `SlimeWalk`/대기 `SlimeIdle`)를 **임시 합성 그림**으로 연결했다. 아틀라스 아래 한 줄(세실리아 72~80, 아델라 84~92), `SKCharacterSpriteView`가 `SKFloorSpike.AnyStrikingAt`·`SKSlimePool.AnyCovers`를 읽는다. 정식 도트는 [제작 안내](Design/Characters/TRAP_REACTION_GUIDE.md)대로 같은 칸에 덮어쓴다(그 뒤 `make_trap_reactions.py` 재실행 금지). 점액 묻음은 미구현. 전체 123/123. 상세 [DAY14.md](DAY14.md). Day 17에는 HUD 일러스트 상태 변형(촉수 찔림·점액 덮임·살덩이 잠식·살덩이 벽에 삼켜져 쓰러짐)이 예정되어 있다. 요청서 [SPRITE_REQUEST_DAY09.md](Design/Enemies/SPRITE_REQUEST_DAY09.md) 4-1·5절.
- 정식 도트 적용 후 전체 Play Mode **103/103 통과**(전체 114, Explicit 11, 종료 코드 0). `Day10TrapTests` 9개로 두 씬의 촉수·점액 재생, 두 캐릭터의 벽 합성, 연타 탈출·R 후 단독 그림 복원을 확인했다. [결과 XML](../Logs/Day10Art/Full/playmode-results.xml).
- 불안정 검사 정리(같은 날): 연타 반복은 `EscapeReady`에서 멈춘다(넘치면 놓인 뒤 J가 공격이 됨). `ReferenceUpgradeTests`·`Day06SpriteViewTests` 대기는 게임 시간 기준(편집기 멈춤 무시). 보스 껍질은 `Update`에서도 닫는다. 이후 전체 2회 연속 99/99. 다른 검사의 `Until`은 아직 실제 시간 기준이니 비슷한 실패가 나면 같은 방식으로 바꾼다. 클래스 이름만으로 `-testFilter`를 주면 Explicit 캡처까지 실행돼 `-nographics`에서 Unity가 죽으니 테스트 전체 이름을 쓴다.

- 화면 검수(2026-11-11): `Day10TrapTests.CaptureTrapScenes`(Explicit, 그래픽 배치)로 `Logs/Day10Capture_*.png` 11장. 바닥 촉수는 판정 표시를 투명으로 켠다(사용자 요청, 적 공격·보스 휘두르기 표시는 그대로). 남은 판단: 촉수 예고 그림이 작아 점액 위에서 잘 안 보임. 빌드 `Builds/Win64_Day10/`.

## 1-10. 2026-11-11 Day 11 문

- [SKSceneDoor](../Assets/SaintKnightDay09/Runtime/SKSceneDoor.cs): 플레이어가 문 좌우 `reach`(0.6유닛) 안에 오면 키 입력 없이 대상 씬을 부른다. 문구·W 입력 없음. 전투 불능·잡힘 중에는 넘어가지 않는다.
- 사용자 결정(2026-11-11): 레버·잠금은 넣지 않는다. 길목의 적(A3+B2)이 관문이다. 레버 `SKSwitch`는 만들었다가 제거했으니 다시 제안하지 않는다.
- 스테이지에서 x2471px보다 동쪽에 플레이어를 놓는 검사는 씬이 넘어간다. 오른쪽 벽을 재는 검사는 문 오브젝트를 끈다.
- Play Mode **103/103 통과**(전체 115, Explicit 12) [결과 XML](../Logs/ClaudeRuns/20261111_230942_day11_autodoor_full/playmode-results.xml). 캡처 `Logs/Day09Capture_7~8.png`. 빌드 `Builds/Win64_Day11/`. 사본 `Logs/Day11/Before/`. 수동 조작감 미실시.

## 1-11. 2026-11-11 Day 12 캐릭터 능력 및 전환 검증

- [Day 12 기록](DAY12.md). 새 `Day12AbilityTests` 3개는 최신 스테이지에서 실제 Tab/J 입력으로 양방향 특성 교체(B 고유 아머 절단), A 일반 예고 취소 후 세실리아 추가 지연 0.2초, 전환 후에도 B 반격 아머 유지 여부를 검사한다.
- 새 `Day12SwitchTests` 7개는 스테이지·보스방에서 동시 Tab+J, 양 캐릭터의 준비/타격 취소·원래 쿨타임 보존, 공유 HP·손상 단계·HUD, 웅크림/공중, 점액 감속, 피격/히트스톱/사망/비활성 차단, 실제 살덩이 벽 잡힘 차단·히트스톱 중 R 복구를 검사한다. `CombatDecisionTests`의 씬 프로필 비교에도 보스방을 포함했다.
- 런타임·씬·아트·능력 수치는 바꾸지 않았다. 공유 HP와 잡힘 중 전환 불가 정책은 유지한다. 최초 점액 검사 실패는 마찰 후 실제 속도를 설정 속도와 비교한 검사 가정 문제였으며, 전환 전후 실제 속도 보존과 감속 배율을 따로 확인하도록 고쳤다.
- 전체 Play Mode **113/113 통과**(전체 125, Explicit 12, 실패 0, 종료 코드 0), [결과 XML](../Logs/Day12/Full/playmode-results.xml). 새 검사 10개 모두 통과. 검사 방법은 아래 7절과 같으며 Day 12만 실행하려면 `-testFilter Day12`를 추가한다(새 클래스에는 Explicit 없음).
- 기존 Day 11 빌드가 프로젝트를 점유한 동안은 종료까지 기다렸다. 변경 전 사본 `Logs/Day12/Before/`, 최신 Day 11 문서 보존 사본 `Logs/Day12/BeforeLatest/`. 새 캡처·Windows 빌드·수동 조작감 검수는 하지 않았다. Day 12 종료 당시 다음 작업은 Day 13이었다. 이후 공용 손상 판정은 아래 1-12절, Day 14 임시 그림 연결은 1-9절·1-13절을 따른다.

## 1-12. 2026-11-11~12 Day 13 외형 손상 0~4단계 판정

- [Day 13 기록](DAY13.md). `SKHealth.DamageStage`와 공용 함수 `DamageStageFor(currentHp, maximumHp)`가 현재 HP를 최대 HP의 75%·50%·25%와 직접 비교한다. 단계는 75% 초과=0, 50% 초과~75% 이하=1, 25% 초과~50% 이하=2, 0 초과~25% 이하=3, HP 0 이하=4(`INCAPACITATED`). 표시 숫자 반올림·정신 상태와 분리된다.
- `SKDay01HUD`와 `SKCharacterSpriteView`는 체력의 같은 읽기 속성을 쓴다. 기존 `SKDay01HUD.StageFor(percent)`는 호환 메서드로 남겼다. 별도 단계 저장·타이머·컴포넌트·씬 재생성은 없다. 일반 피격·잡힘 `Drain`·회복·R 초기화·공유 HP 전환에 같은 현재 HP를 반영하며, 회복하면 단계도 역으로 돌아간다.
- 신규 `Day13DamageStageTests.cs` 8개: 최대 HP 100/200/360과 경계 직전/정확/직후, 극소 양수 HP, 승인/거부 피해와 무적 중 Drain, 회복/Reset, 최신 두 씬 양 캐릭터 단계 0~4 HUD·기존 손상 대기/쓰러짐, 실제 살덩이 벽 경계 통과·R 복구. 전용 **8/8 통과**(`Logs/Day13/TargetedHost/`). 기존 표시 우선순위와 전투 수치·아트·씬은 유지했다. 후속 Day 14 표시 변경은 별도 기록을 따른다.
- 첫 전체에서는 기존 보스 점프 검사 하나가 실패했다(새 8개는 통과). 런타임을 바꾸지 않고 검사에서 일정한 시간 진행과 실제 상승·타격 순간 발높이·공격 종료·착지 관측을 보완했다. 최종 **Day 13·14 통합 전체 123/123 통과**(기존 113 + Day 13 신규 8 + Day 14 신규 2, 전체 135, Explicit 12, 실패 0, 종료 코드 0), [결과 XML](../Logs/Day14/Full/playmode-results.xml). 새 8개와 보완한 보스 검사 모두 통과했고 C# 컴파일 오류·경고는 없었다.
- 소스 증빙 `Logs/Day13/IntegratedValidationInputs.json`의 5개 SHA256가 종료 직후 파일과 일치했다. 사본 `Logs/Day13/Before/`·`BeforeLatest/`, 최종 Day 13 기록 직전 사본 `BeforeLatest2/`, 동시 Day 14 기록을 포함한 공유 6문서 사본 `BeforeLatest3/`. 기존 Day 11·12 확정 기록을 보존했다. 새 캡처·빌드·수동 조작감/렌더링 검수는 하지 않았다.

## 1-13. 2026-11-12 Day 14 임시 함정 반응 표시

- 별도 Day 14 작업의 `SpikeHurt`·`SlimeWalk`·`SlimeIdle` 임시 합성 그림 연결은 완료됐다. 기존 1-9절의 적용 위치·셀·교체 규칙과 [DAY14.md](DAY14.md)·[정식 도트 제작 안내](Design/Characters/TRAP_REACTION_GUIDE.md)를 따른다. 정식 아트·점액 묻음·수동 검수는 남아 있다.
- 위 초기 `Logs/Day14/Full/` 통합 실행에서 Day 14 신규 검사 2개도 통과했다. 이후 사용자 요청에 따른 실제 원본 도트 기반 포획·흡수 구현은 아래 1-14절을 따른다. 다음 로드맵은 **Day 15 정신 상태**다.

## 1-14. 2026-11-12 기존 도트 기반 함정 포획·전신 흡수

- 최신 사용자 요청은 기존 캐릭터 도트에 점액·촉수가 붙어 잡아먹는 표현과 살덩이 벽에서 일정 시간 저항하지 않으면 완전히 삼켜지는 동작이다. 사용자가 문서를 읽고 세부 설계를 위임했다. 상세 [TRAP_CONSUMPTION.md](TRAP_CONSUMPTION.md), 후속 아트 정책은 [DAY14.md](DAY14.md)·[제작 안내](Design/Characters/TRAP_REACTION_GUIDE.md)를 따른다.
- **기존 도트 포즈 + `SKTrapEnvelopeView` 런타임 픽셀 레이어**를 사용한다. 실제 포즈 위를 점액·촉수·살덩이가 덮고, 저항→진행→완료에 맞춰 감싸는 범위를 바꾼다. 생성 AI의 새 인물 시안과 `packed-v2`는 Assets에 적용하지 않았다. 원본 PNG 2개·해당 `.meta` 2개·프레임 JSON 2개 및 기존 씬·관련 GUID 등 총 21개가 보존됐다([해시 증빙](../Logs/TrapConsumption/asset-preservation.json)).
- 점액: 기존 걷기 ×0.5 유지, 같은 웅덩이 위 **연속 접지 1.25초** 후 포획. 이탈·점프하면 누적 시간을 초기화한다. 촉수: 기존 예고·유효 구간을 유지하고 **실제 승인된 15 피해**의 `SKHitbox.HitAccepted` 직후 포획한다. 무적 등으로 거부된 피해는 포획하지 않으며 승인 콜백은 공격당 대상별 1회다.
- 공통 소유자는 함정 측 `SKEnvironmentalCapture`다. `SKDay01Player`→`SKGrabState`의 기존 연타 입력·잡힘 중 이동/공격/전환 차단은 유지한다. 마지막 유효 저항(`LastStruggleAt`, 아직 없으면 `HeldSince`)부터 **4초**간 저항이 없으면 완전 흡수한다. 드레인은 **4 HP / 0.75초**, 탈출 미터 **6**·초당 감소 **2.5**, 성공 탈출 무적 **0.6초**·재포획 휴식 **1.5초**다. 히트스톱은 함정 진행도 함께 멈춘다.
- 살덩이 벽은 기존 **4 HP / 0.5초** 드레인을 유지하고, 4초 뒤 내던지던 흐름을 마지막 저항 기준 무저항 흡수로 바꿨다. 연타로 탈출할 수 있고 유효 저항은 흡수 기한을 연장한다.
- 완전 흡수는 **HP 0 + 잡힘 해제 + 캐릭터 단독 SpriteRenderer 숨김 + 닫힌 덩어리 유지**다. `INCAPACITATED`와 HP 손상 단계·별도 정신 상태 정책은 유지한다. R·자동 재시도 **1.6초**·함정/플레이어 비활성·초기화·회복 시 소유권과 숨김을 정리한다. 시간제 웅덩이는 포획 중 수명이 끝나도 유지하며 Clear/Spawn/disable/Destroy에서 해제한다. 복수 함정은 한 플레이어를 중복 소유하지 않는다.
- 새 `FleshWallAbsorptionTests` **6개** + `EnvironmentalCaptureTests` **7개**, Explicit `TrapConsumptionVisualTests` **1개**를 추가했다. 기존 Day 14 검사 2개는 포획 연타 탈출 뒤 일반 피격을 검사하고 히트스톱 종료를 기다리도록 보완했다. 관련 기능·그래픽 전용 실행 **16/16**, 최종 그래픽 재실행 **1/1** 통과(모두 종료 코드 0)다.
- 최신 전체 Play Mode **136/136 통과**(기존 123 + 신규 기능 13, 전체 149, Explicit 13, 실패 0, 종료 코드 0), [결과 XML](../Logs/TrapConsumption/Full/playmode-results.xml) · [로그](../Logs/TrapConsumption/Full/playmode-tests.log). 실행 시각은 **2026-11-12 09:03:32~09:06:28 KST**다. 최종 [그래픽 캡처 18장](../Logs/TrapConsumption/Captures/20261112_000151/index.md)은 양 캐릭터 × 세 함정 × 저항/진행/완료이며 얼굴·자세 읽힘, 바닥·벽 앵커, 완료 후 얼굴·몸체 잔존/중복 없음을 이미지로 검수했다.
- 소스 16개 해시는 `Logs/TrapConsumption/validation-inputs.json`, 자산 21개 보존은 `asset-preservation.json`, 이번 공유 문서 6개 수정 직전 사본은 `Logs/TrapConsumption/BeforeFinalDocs/`에 있다. 초기 Day 13·14의 123/123과 기존 Day 11·12 확정 기록은 역사 증빙으로 보존한다. 새 Windows 빌드·수동 조작감 검수는 하지 않았다.

## 1-15. 2026-11-12 전용 함정 도트 모션 6시트

- 최신 사용자 요청은 기존 인물 도트에 점점 삼키는 전용 모션을 적용하는 것이다. 사진 **1=촉수의 팔다리·몸통 나선 감김**, **2=살덩이 벽 틈으로 옆방향 끌림**, 점액은 승인된 재질을 사용했다. 상세 [TRAP_DEDICATED_MOTION.md](TRAP_DEDICATED_MOTION.md). 위 1-14절의 절차적 레이어·136/136·18장 기록은 당시 증빙이다.
- 새 Resources `Assets/SaintKnightDay10/Resources/SaintKnightDay10/TrapMotion`에 세실리아·아델라 × 점액·촉수·벽 **6 PNG+6 JSON**을 추가했다. 조합당 삼켜짐 6·단계별 저항 12·탈출 3·완전 흡수 반복 2 = **23장**, 총 **138장**이다. 8열×3행의 마지막 셀은 비어 있다. 환경 셀 128×96/48PPU/피벗(.5,.26625), 벽 160×112/48PPU/피벗(.5,0). 캐릭터 원본 Pounced/Held/Resist/Escape 픽셀을 정수 이동/가림으로 합성했고 인물 색·길이를 재생성하지 않았다. AI 생성은 함정 단독 소재만 사용했다.
- `SKTrapAnimationLibrary`가 manifest를 엄격히 읽어 프레임을 공급하고 `SKTrapAnimationPlayback`은 저항 직전 잠김 깊이를 유지한 뒤 완만하게 되돌린다. `SKCharacterSpriteView`는 환경 포획/성공 탈출, `SKTrapEnvelopeView` 자식은 완전 흡수 후 덩어리, `SKFleshWall`은 벽 전용 합성을 재생한다. 시트가 있으면 이전 덮개를 끄며 없을 때만 이전 표시로 돌아간다. 새 공격·피격·R·전환·비활성 후 과거 탈출 프레임이 남지 않는다. 판정/피해/포획/4초 기한/재시도는 변경하지 않았다.
- 신규 규칙 2+실제 상태 4검사, 기존 표시 단정 3파일과 기존 Explicit 캡처 1개를 보완했다. 관련+그래픽 **22/22 통과**(종료 0), 전체 Play Mode **142/142 통과**(전체 155, Explicit 13, 실패 0, 종료 코드 0): [XML](../Logs/TrapDedicatedMotion/Full/playmode-results.xml) · [로그](../Logs/TrapDedicatedMotion/Full/playmode-tests.log). Explicit 수는 13을 유지한다.
- [실제 씬 138 PNG](../Logs/TrapDedicatedMotion/Captures/20261112_044531_553_0b3e2027/index.md)와 최종 v3 6시트를 직접 검수했다. 원본 인물·씬·기존 벽 아트/메타 관련 **42개**가 보존됐고 `Logs/TrapDedicatedMotion/asset-preservation.json`, `validation-inputs.json`, `applied-assets.json`, `build-report.json`에 기록했다. 소스 사본은 `Before/`·`BeforeVisualEvidence/`, 공유 문서 9개 사본은 `BeforeFinalDocs/`다. 생성 프롬프트·소재·빌더·최종 미리보기는 `Docs/Design/Characters/trap-dedicated-motion/`에 있다. 새 Windows 빌드·수동 조작감 검수는 하지 않았다. 다음 Day 15 정신 상태.
- 후속(2026-11-12, Claude): 점액·촉수 시트 4장의 칸 4·5·14~17을 교체했다. 마지막 두 단계는 소재의 실제 틈으로만 몸이 보인다. `applied-assets.json`의 이 4장 해시는 교체 전 값이다. 제작 스크립트는 사본 `Docs/Design/Characters/trap-downscale-claude/builder-clip/`(원본 + 조건 한 블록)이며, 원본 빌더로 다시 만들면 v3로 돌아간다. 전체 Play Mode **142/142 통과** [XML](../Logs/TrapClip/Full/playmode-results.xml), 캡처 138장은 교체 전 기준. 변경 전 `Logs/TrapClip/Before/`. 상세 [TRAP_DEDICATED_MOTION.md](TRAP_DEDICATED_MOTION.md) 끝 절.

## 1-16. 2026-11-12 Day 15 — HP 회복·무저항 손실·원인별 게임 오버·HP 구슬

- 사용자 결정: “정신 붕괴”는 HP가 깎이는 모습의 이름이며 **별도 정신 수치는 만들지 않는다.** 이전 절들의 “별도 정신 상태 유지”, “무저항 4초 흡수”, “Drain 간격” 서술은 이 절로 대체된다. 상세 [DAY15.md](DAY15.md).
- `SKHealth`: `regenPerSecond`(기본 0)·`regenDelaySeconds`(3). Day 9 두 씬의 플레이어만 5/3. 잡힘·HP 0 중 회복 없음. `LastCause`(마지막 가해자)와 `TryTakeDamage(..., cause)`/`Drain(amount, cause)`.
- `SKEnvironmentalCapture`·`SKFleshWall`: 마지막 연타로부터 0.35초가 지나면 `최대 HP / 4초`로 계속 Drain, HP 0 = 흡수. `AbsorptionProgress` = 잃은 HP 비율. 간격 피해(`TickDamage`, `grabTickDamage`)는 제거. 적 A·보스 잡기는 그대로.
- `SKGameOverScreen`(`Assets/SaintKnightDay15/Runtime`): `SKEnemyTrainingSession.Start`가 붙인다. HP 0이면 문구 없이 월드 카메라 앞 검은 스프라이트 `GameOver_Dim`(알파 0.5, 0.5초)로 전투 화면만 어둡게 한다(HUD 제외, 사용자 결정). `KeyFor(cause)`로 원인 이름(화면 표시 없음), `Resources/SaintKnightDay15/DeathCutscenes/<이름>` 스프라이트가 있으면 `session.HoldRetry`로 자동 재시도를 멈추고 R을 기다린다. 그림은 아직 없다. 보스방 바닥 촉수는 보스의 자식이라 `BOSS`다.
- 필요한 그림(사용자 지시): 보스에게 죽었을 때와 각 패턴·함정에 맞았을 때마다 ① 캐릭터 도트 ② 패턴이 플레이어에게 적용되는 도트 ③ 그에 맞는 컷씬. 현황 표는 [DAY15.md](DAY15.md) “필요한 그림”, 요청서는 [ART_REQUEST_DAY15.md](Design/ART_REQUEST_DAY15.md)(원인별 측면 HUD 이미지 방향성, 성적인 묘사는 사용자와 대화로 결정 — 비워 둠). 컷씬 틀은 지금 원인당 한 장이라 캐릭터·보스 패턴을 구분하려면 `KeyFor`를 넓혀야 한다.
- `SKDay01HUD`: 일러스트 왼쪽 위 `HP_ORB_FRAME`(캐릭터별 장식, 색 고정) 안에 `HP_ORB`(런타임 생성). 모두 임시 그림, [미리보기](Design/HUD/hp_orb_placeholder_preview.png). `OrbState` 0 파랑(단계 0)·1 노랑(1~2)·2 빨강(3)·3 꺼짐(4), 캐릭터별 문양. 표시 전용.
- 전체 Play Mode **146/146 통과**(전체 159, Explicit 13, 실패 0, 종료 코드 0) [XML](../Logs/Day15/Full/playmode-results.xml). 신규 `Day15HealthTests` 4개, 기존 검사 7개를 새 규칙에 맞춰 수정. Explicit 13개는 실행하지 않았다(`TrapConsumptionVisualTests`는 이전 규칙 기준일 수 있음). 변경 전 사본 `Logs/Day15/Before/`·`BeforeDocs/`. 컷씬 그림, 수동 검수, 빌드는 없다. 다음 Day 16.
- Day 16 선행(2026-11-12): 본작업(아델라 HUD 일러스트)은 그림 대기 — 그림이 오면 `make_portraits.py`로 변환해 씬 HUD의 `adelaStagePortraits`에 넣는다(코드 준비됨). Explicit 13개를 그래픽 배치로 실행해 모두 통과시켰다(`Logs/Day16/Explicit1`·`Explicit2`): `TrapConsumptionVisualTests`의 연타 단정을 Day 15 규칙(손실 정지, 회복 없음)에 맞추고 `PlaytestProbe.FightBots`에 `Timeout(600000)`. 함정 캡처 138장 재촬영 `Logs/TrapDedicatedMotion/Captures/20261112_080308_006_b3da66f8/`(전체 이미지 검수는 미실시). 전체 **146/146** [XML](../Logs/Day16/Full/playmode-results.xml). 빌드 `Builds/Win64_Day16/`(배치 기동 15초). Explicit 전체 실행 필터는 테스트 전체 이름을 `;`로 잇고 `-nographics`를 뺀다.

## 1-17. 2026-11-13 SpriteCook 시안 검토 및 점액 합성 v2

`Assets/SaintKnightSpriteCook/Drafts/`는 게임 미연결 시안이다. PNG 10장·JSON 2개와 48 PPU/Point/무압축 Texture 설정을 확인했다. `Design/Boss/pattern-dots/make_boss_spit_from_material.py`가 v1을 보존한 `staging-spritecook-v2/`에 두 128×96·23칸 시트를 생성하며, `--sync-drafts`로 두 PNG/JSON만 Drafts에 복사한다. 실제 내용 변경은 PNG 2장이다. 첫 방울 머리 접촉, 가는 곡선 줄기, 중간 단계 방울 끝, 후반 장비 가림을 수정했다. 원본 인물·소재·기존 GUID·Resources·C#·씬을 바꾸지 않았다. GIF 재생성도 스크립트에 포함했다.

합성 검사 `python Docs/Design/Boss/pattern-dots/check_spritecook_slime.py` 6/6, PNG/JSON 4개 재현 해시 일치. Unity 임포트 확인, 전체 Play Mode **146/146 통과**(전체 159, Explicit 13, 실패 0, 종료 코드 0). 결과 `Logs/SpriteCookReview/20261113/PlayMode/playmode-results.xml`. 증빙·변경 전 50파일은 `Logs/SpriteCookReview/20261113/`.

초기 검토 당시에는 SpriteCook 생성 도구 미노출로 잔액·생성을 확인하지 못했다. 같은 날 후속에서 전용 도구 연결이 복구됐으며, 현재 상태는 아래 1-18절을 따른다. `Design/Stage/spritecook/regeneration-plan.json`은 초기 7장 재생성 준비안에서 후속 결과·기각 상태를 반영한 진행표로 갱신했다. 새 호출이 가능하면 현재 모델/가격·잔액·기존 참조 에셋 ID부터 확인한다. 사용자 우선순위는 적 사망 아델라/세실리아 → 보스 양 캐릭터 → HP 장식 → 함정 반응 양 캐릭터, 이후 규격 통일·게임 연결. 자세한 검증 범위는 [검토 기록](SPRITECOOK_DRAFTS_REVIEW.md).

## 1-18. 2026-11-13 SpriteCook 연결 복구·세실리아 디자인 교정

이 절은 당시 이력이다. 남은5장·정규화·분할은 후속 1-19절에서 완료했다.

SpriteCook 전용 도구가 로드됐고 참조 2개를 픽셀 대조해 복구했다. 32크레딧으로 Nano Banana Pro·1K 적 쓰러짐 2장 생성(각16): 아델라는 raw v2만 보관, 세실리아 v2는 긴 머리·의상 변경으로 사용자가 기각했다. 현재 잔액0, 남은 SpriteCook 보스2·함정반응2·HP장식1은 미생성이다. 모델 `high` 옵션과 Pro의 고정 품질을 혼동하지 않는다.

사용자 “모습 제대로 해서 다시 해 변형하지 말고”에 따라 세실리아 v1 시트와 승인된 원본 단일 도트를 직접 참조해 내장 ImageGen 편집으로 v3를 만들었다. 원본32색·서 있는 키55px·128×96칸·4열2행·8프레임, 발높이92, 알파0/255로 정규화했다. `Assets/SaintKnightSpriteCook/Drafts/4_enemy_death_cecilia_v3.png`와 JSON에만 별도 저장(새 GUID·48 PPU/Point/무압축). 아직 일반 Texture이고 게임 연결은 하지 않았다.

재현 `python Docs/Design/Stage/spritecook/normalize_cecilia_identity.py --sync-drafts`. 규격6항목·원본 해시·PNG/JSON 재현 일치, 전체8프레임과 원본/v2/v3 밝은·어두운 배경 비교를 검수했다. Unity 임포트 확인·전체 Play Mode **146/146 통과**(전체159, Explicit13, 실패0, 종료0). 결과 `Logs/SpriteCookIdentityFix/20261113/PlayMode/playmode-results.xml`. [상세·디자인 보존 규칙](SPRITECOOK_CECILIA_IDENTITY_FIX.md), 증빙 `Logs/SpriteCookIdentityFix/20261113/`, 모델 요청·에셋 ID `Logs/SpriteCookHigh/20261113/jobs.json` 및 로컬 `spritecook-assets.json`. 기존 Resources·C#·씬·기존 GUID는 그대로다. 세실리아 v2는 후속 참조로 사용하지 않는다.


## 1-19. 2026-11-13 요청 도트 7장 완성·규격 통일·Sprite 분할

요청 7장 모두 `Assets/SaintKnightSpriteCook/Drafts/`에 정규화 PNG/JSON으로 준비했다. 각128×96칸, 세실리아55px·아델라62px, 원본32색(점액5색 추가), 48 PPU/Point/무압축. 검수된 세실리아v3 PNG/JSON은 바이트 보존, 아델라 적 쓰러짐v2는 Pro 원본 정규화, 남은 보스2·함정반응2·HP장식1은 내장 ImageGen으로 v1/승인 도트 직접 참조 편집했다. 추가 SpriteCook 지출0, 마지막 잔액0. 기각된 세실리아v2를 재사용하지 않는다.

시트 자체는 Texture이며 `Drafts/Sliced/<시트 이름>/00.png` 등 **59개(캐릭터56+UI3)**는 실제 Single Sprite다. 보스 들림4칸은 바닥보다12px 위, 걷기4칸 차이, HP 구슬/장식 중심 일치·알파 겹침0을 확인했다. 빌더 `python Docs/Design/Stage/spritecook/build_completed_drafts.py --sync-drafts`는 다른 내용의 기존 에셋을 덮어쓰지 않는다. 정규화/미리보기/JSON은 `Design/Stage/spritecook/completed-pack/`, 편집 프롬프트는 `../Logs/SpriteCookCompletion/20261113/edit-requests.json`.

재현84파일 해시 일치, 기존Assets35파일 해시 보존, 점액 합성6/6. 신규 `SpriteCookDraftImportTests`3개가 실제59Sprite/설정/피벗·들림 간격·UI 레이어를 검사하며 전체 Play Mode **149/149 통과**(전체162, Explicit13, 실패0, 종료0). [XML](../Logs/SpriteCookCompletion/20261113/PlayMode/playmode-results.xml), 변경 전44파일 `Logs/SpriteCookCompletion/20261113/Before/`. 기존 Resources·런타임·씬·기존GUID는 유지했다.

**다음은 게임 연결과 장면 검수다.** 보스/적 원인별 모션 선택, 함정 반응 교체, HP HUD 레이어 연결이 필요하다. 정규화·Sprite 임포트 완료를 실제 게임 표시 완료로 표현하지 않는다. 새 Windows 빌드·수동 플레이는 하지 않았다. [전체 파일표·미리보기](SPRITECOOK_DRAFTS_COMPLETED.md).


## 2. 구현 지도: 무엇을 바꾸려면 어디를 읽는가

| 목적 | 파일 | 핵심 함수/데이터 |
| --- | --- | --- |
| 입력·이동·점프·방향·전환 | [SKDay01Player.cs](../Assets/SaintKnightDay01/Runtime/SKDay01Player.cs) | `Update`, `UpdateFacing`, `SwitchCharacter`, `SelectedCharacter`, `Facing` |
| 플레이어 준비·타격·회복 | [SKPlayerCombat.cs](../Assets/SaintKnightDay02/Runtime/SKPlayerCombat.cs) | `AttackProfile`, `TryAttack`, `StartStrike`, `CancelAttack`, `ResetCombat`, `State`, `AttackFacing`, `LocksFacing` |
| 타격 범위·중복 피해 방지 | [SKHitbox.cs](../Assets/SaintKnightDay02/Runtime/SKHitbox.cs) | `ShowWindup`, `Begin`, `FixedUpdate`, `Cancel` |
| 피해를 받는 영역 | [SKHurtbox.cs](../Assets/SaintKnightDay02/Runtime/SKHurtbox.cs) | `Owner` → SKHealth |
| HP·무적·경직·넉백·손상 단계 | [SKHealth.cs](../Assets/SaintKnightDay02/Runtime/SKHealth.cs) | `TryTakeDamage`, `Damaged`, `Changed`, `ResetHealth`, `DamageStage`, `DamageStageFor` |
| 공통 타격 정지 | [SKHitStop.cs](../Assets/SaintKnightDay02/Runtime/SKHitStop.cs) | `Request`, `CancelActive`, `IsActive` |
| HUD | [SKDay01HUD.cs](../Assets/SaintKnightDay01/Runtime/SKDay01HUD.cs) | `BindHealth`, `SetCharacter`, `DamageStage`, `Refresh` |
| 적 A/B의 이동·예고·판정 | [SKEnemyAgent.cs](../Assets/SaintKnightDay04/Runtime/SKEnemyAgent.cs) | `State`, `FixedUpdate`, `ResetAgent`, `AttacksEnabled` |
| 최신 씬의 R·훈련 상태 | [SKEnemyTrainingSession.cs](../Assets/SaintKnightDay04/Runtime/SKEnemyTrainingSession.cs) | `ResetTraining` |
| 점액·촉수 포획·흡수 | [SKEnvironmentalCapture.cs](../Assets/SaintKnightDay10/Runtime/SKEnvironmentalCapture.cs) | `TryCapture`, `AbsorptionProgress`, `IsPlayerConsumed`, `ResetCapture`, `ForPlayer` |
| 환경 포획 후 완전 흡수 표시·시트 없을 때 이전 덮개 | [SKTrapEnvelopeView.cs](../Assets/SaintKnightDay06/Runtime/SKTrapEnvelopeView.cs) | 전용 Consumed 자식 렌더러 / 이전 표시 fallback |
| 전용 함정 시트·저항 깊이 표시 | [SKTrapAnimationLibrary.cs](../Assets/SaintKnightDay10/Runtime/SKTrapAnimationLibrary.cs) | `Load`, `Sample`, `Owns`, `SKTrapAnimationPlayback.Held` |
| Day 2 표적/초기화 | [SKTrainingTarget.cs](../Assets/SaintKnightDay02/Runtime/SKTrainingTarget.cs), [SKTrainingSession.cs](../Assets/SaintKnightDay02/Runtime/SKTrainingSession.cs) | 과거 회귀 씬 전용 |
| Day 5 밸런스 저장값 | [Day05_EnemyPatterns.unity](../Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity) | `cecilia` / `adela`, 적 A/B 직렬화 값 |
| Day 4/5 재생성 코드 | [SKEnemySceneBuilder.cs](../Assets/SaintKnightDay04/Editor/SKEnemySceneBuilder.cs) | `ConfigureReferenceCombat`는 Day 5에만 새 프로필 적용 |

Day 5 수치를 조정하면 씬과 생성 코드의 해당 값이 일치하도록 관리한다. **클래스 기본값만 수정해서 현재 씬도 바뀌었다고 보고하지 않는다.** 생성 메뉴 `Tools > SaintKnight > Day 04-05 > Create Enemy Training Scenes`는 두 씬을 덮어쓴다. 일반 수정은 필요한 컴포넌트/직렬화 값만 바꾼다.

## 3. 현재 동작의 정확한 의미

입력 흐름: `SKDay01Player.Update` → 방향 확정 → 점프 → 전환 → `TryAttack`.
공격 흐름: `TryAttack` → `pendingAttack`/`strikeAt`/`readyAt` → `ShowWindup` → `StartStrike` → `SKHitbox.Begin` → 활성 종료 → 남은 회복.
피해 흐름: Hitbox 접촉 → Hurtbox의 Owner → `SKHealth.TryTakeDamage` 승인 → HP·무적·경직·넉백 → Damaged/Changed → 적중 시만 히트스톱.

- `IsAttacking`은 실제 유효 판정만 뜻한다. 준비 단계까지 포함하려면 `LocksFacing` 또는 `IsWindingUp`도 봐야 한다.
- `State` 우선순위는 Disabled → Dead → Hurt → Windup → Attacking → Recovery → Ready.
- `cooldown`은 **입력부터 다음 입력까지의 최소 간격**이다. 활성 종료 뒤 그만큼 더 기다린다는 의미가 아니다.
- 준비와 활성의 합보다 쿨타임이 짧아도 활성 구간을 자르거나 새 공격을 겹치지 않는다. 늦게 실행된 준비 종료도 전체 활성 시간을 보장한다.
- 준비·타격 동안 방향은 고정되지만 몸체 이동은 가능하다. 방향과 이동속도를 같은 값으로 강제하지 않는다.
- 전환 시 `CancelAttack`은 `pendingAttack`과 Hitbox를 지우고 `cancelledFrame`을 기록한다. `readyAt`은 유지한다. `ResetCombat`에서만 준비/판정과 쿨타임을 함께 초기화한다.
- 플레이어 스크립트를 비활성화해도 공격이 취소된다. 애니메이션 쪽에서 뒤늦게 판정을 다시 켜면 안 된다.
- `SKHitbox.ShowWindup`은 기존 사각형을 흐리게 보여 주는 훈련 표현이다. 최종 무기 모션이 아니다.
- HP 100 공유는 임시 정책이다. 별도 인물 HP·방패 가드는 아직 구현되지 않았다. 별도 정신 상태는 만들지 않는다(1-16절).

이 계약은 이미 적용·검증됐다. 처음부터 상태 머신을 교체할 필요가 없다.

## 4. 현재 수치와 아직 구현하지 않은 것

플레이어 최신 값은 루트 CLAUDE.md 표 및 [REFERENCE_APPLY.md](REFERENCE_APPLY.md)가 기준이다. 다음은 관련 공통/적 값이다.

| 항목 | 현재 값 |
| --- | --- |
| 플레이어 속도 / 점프 초기속도 / 중력 배율 | 3.6 / 7 / 2.2 |
| 플레이어 무적 / 경직 | 0.55 / 0.16초 |
| 적 무적 / 경직 | 0.12 / 0.16초. SKHealth 코드 기본 무적값과 씬 값이 다름 |
| 성공 타격 히트스톱 | 기본 0.045초 |
| 적 A HP / 피해 / 예고 / 유효 / 회복 | 60 / 15 / 0.4 / 0.18 / 1.0 |
| 적 B HP / 피해 / 예고 / 유효 / 회복 | 80 / 25 / 0.85 / 0.25 / 1.5 |
| A 순찰 / 추적 / 공격 시작 거리 | 0.7 / 1.5 / 1.1 |
| B 순찰 / 추적 / 공격 시작 거리 | 0.45 / 0.95 / 1.5 |
| 카메라 정규화 뷰 | x=0.25, y=0.125, width=0.75, height=0.75 |
| 카메라 orthographicSize | 3.75. Day 8에서는 16:9 뷰를 HUD가 비워 둔 영역에 맞춘다. 화면 검수 미완료 |

인계 시 `Assets` 검색에서 `.anim`, `.controller`, `.glb`, `.fbx` 파일이 발견되지 않았다. 이는 외부 저장소/ZIP/Downloads에 자료가 없다는 뜻은 아니다.

현재 초상화: `Assets/SaintKnightDay01/Art/cecilia_approved_cropped.png`, `adela_approved_cropped.png`.
현재 전투 대역: 같은 폴더의 `cecilia_PLACEHOLDER.png`, `adela_PLACEHOLDER.png`.
Day 2~8 씬의 적 A/B는 `Assets/SaintKnightDay02/Art/training_dummy.png`를 공유한다. Day 9는 임시 실루엣(`Assets/SaintKnightDay09/Art/enemy_*_NOT_FINAL.png`)을 쓴다. 이 파일들을 최종 동작 시트로 오인하여 자동 분할하지 않는다.

미완료: 실제 도트 모션, 최종 몬스터 디자인, 3D HUD 임포트·리깅·렌더링, 정신 시스템, 정식 저장/부활, 보스 처치 후 클리어 흐름, 메뉴 완성, 배포 빌드. 기존 기능 테스트가 이 항목의 완료 증거는 아니다.

## 1-16. 요청 도트 게임 화면 연결 (2026-11-13)

표시 연결만 했다. 선택 규칙·칸 번호·파일·검사·남은 일은 [SPRITECOOK_APPLIED.md](SPRITECOOK_APPLIED.md). 시트 접근은 `SKAppliedSheets`, 보스 점액 선택은 `SKTrapAnimationLibrary.For`. 화면 캡처는 `SpriteCookAppliedTests`를 `-nographics` 없이 실행하면 `Logs/SpriteCookApply/Screens/`에 쓴다(배치 모드에서는 `WaitForEndOfFrame`이 오지 않으므로 쓰지 않는다).

## 5. 다음 요청별 바로 할 일

| 사용자가 요청한 것 | 바로 진행할 일 | 종료 조건 |
| --- | --- | --- |
| “이어 해줘”만 있고 디자인 없음 | 최신 씬에서 남아 있는 수동 조작감 검수를 수행할 수 있는지 확인. Day 6 도트 연결에는 준비 자료가 필요함을 구체적으로 안내 | 실제로 확인한 결과와 필요한 자료만 보고. 이미 끝난 원작 조사 재실행 금지 |
| Day 6 시작 + 디자인 자료 제공 | 아래 6절 순서대로 해당 자료 검사 후 연결 | 판정과 표시 일치, 기존 회귀 통과, 제공되지 않은 동작은 미완료 표기 |
| 밸런스 수정 | Day 5 씬 프로필 + ConfigureReferenceCombat + 관련 테스트를 변경 | 사거리/타수/회복의 요청된 변화 실증. 자동 검사에 맞추려고 의도와 무관한 값을 바꾸지 않음 |
| 버그 수정 | 구현 지도에서 관련 소스와 재현에 필요한 씬만 확인 | 재현·수정·검증, 나머지 기능 보존 |
| 카메라/스테이지/정신/HUD 요청 | 마스터의 해당 날짜 범위와 현재 파일만 대조 | 요청 범위의 산출물과 검증. Day 6 완료를 자동 가정하지 않음 |
| 원작 분석 근거 요청 | 아래 9절의 해당 보고서만 확인 | 확인 사실과 제안, 미측정 사항 구분 |

새 사용자 요청이 구체적이면 그 요청이 위 예시보다 우선한다. 새로운 기능 작업 지시가 없는 단순 문서 인계만으로 개발을 이어서 시작하지 않는다.

## 6. Day 6 구현용 작업 지시

**입력 자료:** 승인 디자인을 유지한 세실리아·아델라 Idle/Run/Attack/Hit. 몬스터 교체까지 요청받았다면 A/B의 Idle/이동/예고/Attack/Hit. 투명 PNG 또는 시트, 프레임 순서·속도·피벗·발 기준선·기본 방향·유효 타격 구간.

**작업 순서:**

1. 제공 파일을 열어 몸체 크기와 프레임 정렬을 확인한다. 48px 키는 프로젝트 출발점이며 캔버스 전체 크기가 아니다. 무기 여백 때문에 몸체를 키우지 않는다.
2. 새 동작 자료와 기존 참조를 구분한다. 원본 승인 그림·임시 모델을 덮어쓰지 말고 실제 작업 범위에 맞는 에셋 위치를 정한다.
3. 표시를 담당하는 얇은 어댑터에서 `SelectedCharacter`, `Facing`, `State`와 이동/접지 정보를 읽는 구성을 우선한다. 이동/접지 값이 필요하면 플레이어에 최소한의 읽기 API를 추가한다. Animator가 입력·HP·피해를 새로 소유하지 않는다.
4. 현재 `Windup`·`Attacking`·`Recovery`에 준비·타격·회수 프레임을 맞춘다. 시트 길이와 현재 시간이 맞지 않으면 차이를 명시하고 의도한 조정만 한다. 코드의 0.06/0.14초를 원작이나 최종 아트 사양이라고 우기지 않는다.
5. 입력/타이머와 Animation Event가 각각 `Begin`을 호출하는 이중 발동을 만들지 않는다. Event를 쓰도록 변경했다면 취소된 공격을 식별해 무효화하고 기존 회귀 계약을 보존한다.
6. 공격 방향 고정, 뒤로 이동, 공중, 전환, 피격, 사망, 비활성, R 초기화의 표시를 정의한다. Jump/Fall 신규 아트가 없으면 제공 프레임을 활용하는 규칙을 명시하고 임의 제작하지 않는다.
7. 인게임 모션과 전투 시간이 히트스톱에서 함께 멈추도록 한다. 애니메이션만 unscaled time으로 앞서 가지 않게 한다.
8. 기존 37개 회귀 검사와 변경에 필요한 검사를 실행한다. 화면에서는 무기 궤적·판정·발 기준선·좌우 반전·전환 중 잔여 모션을 확인한다.
9. 사용자에게 바뀐 동작과 실제 검증 범위를 보고하고 진행 문서를 갱신한다. 전 동작·손상·사망·3D HUD를 Day 6 필수로 늘리지 않는다.

**1차 연결 완료(2026-11-10):** `Assets/SaintKnightDay06/Runtime/SKCharacterSpriteView.cs`(표시 전용, 상태만 읽음), `Editor/SKDay06CharacterSprites.cs`(임포트 설정: 48 PPU·Point·무압축·발=콜라이더 바닥 피벗 (0.5, 0.266) + 메뉴 `Tools > SaintKnight > Day 06 > Attach Character Sprites To Player`로 Day 5·로비 플레이어에 컴포넌트만 추가), `Art/Characters/` 40장(원본 생성기 `Docs/Design/Characters/make_characters_v5.py` → `v5/`, 아트 수정 시 v5 PNG를 다시 복사). 상태 대응: Dead→쓰러짐 12프레임(10fps, scaled time) 후 마지막 유지 / Held·Hurt→hit / Windup→windup / Attacking→attack, Recovery는 같은 캐릭터가 실제로 친 직후 0.1초만 attack(전환 취소 후 잔여 공격 자세 없음) / 이동 또는 공중→run / 그 외 HP 단계 1~3→stage1~3, 0→idle. 검사 `Day06SpriteViewTests` 5개 + 캡처 1개(Explicit).
**판정 = 그림(2026-11-10 사용자 결정):** Day 5 판정 앞끝을 공격 그림의 궤적 끝에 맞춤 — 세실리아 0.72×0.8 / 0.66(0.3~1.02), 아델라 0.65×0.85 / 0.525(0.2~0.85). 씬 2개(Day 5·로비) + `ConfigureReferenceCombat`. `Day06SpriteViewTests.HitboxFarEdgeMatchesDrawnWeaponTip`가 공격 그림의 가장 오른쪽 픽셀과 판정 앞끝(±1.5px)을 비교하므로, 공격 그림을 바꾸면 판정도 함께 맞출 것. 사거리 차이 검사는 두 판정 끝의 중간 거리로 계산.
**측면 HUD(2026-11-10):** 3D 대신 2D 전신 일러스트. 같은 메뉴(`Attach Character Sprites To Player`)가 `SKDay01HUD.ceciliaStagePortraits`(0~4)를 채우고 초상 영역을 패널 (.02,.295)~(.98,.905)로 넓히며 `HUD_TITLE`·`NOTE`(3D 안내)를 숨긴다. 아델라 일러스트가 오면 `Art/Portraits/adela_portrait_stage0~4.png`로 넣고 `AttachPortraits`에 아델라 배열을 추가.
**후속:** 동작별 다중 프레임은 1-2절에서 적용했다. 수동 조작감·화면 검수는 남아 있다.

**합격 기준:** 보이는 타격과 실제 피해 일치, 준비 중 무피해, 공격당 대상별 1회 피해, 취소 후 늦은 판정 없음, 전환 쿨타임 유지, 방향·후퇴 규칙 보존, 히트스톱 복구, 기존 이동·점프·HP HUD 정상, 실제 제공된 아트만 사용.

## 7. 검사 실행 — 복사해서 사용할 PowerShell

같은 프로젝트가 Unity Editor에서 열려 있다면 그 편집기의 Test Runner를 사용하거나 저장 후 정상 종료한 다음 배치 검사를 실행한다. 사용자의 편집기를 강제 종료하지 않는다. Test Runner에서는 PlayMode의 `SaintKnight.PlayModeTests` 전체를 실행한다.

아래 명령은 기존 성공한 호출 인수를 사용하며 새 로그 폴더를 만든다. 문서 작성 때 이 명령 블록의 PowerShell 구문을 검사했지만, 문서만 변경했으므로 Unity 검사를 다시 실행하지 않았다.

```powershell
$projectRoot = 'C:\UnityProject\SaintAndKnight'
$unityExe = 'C:\Program Files\Unity\Hub\Editor\6000.0.81f1\Editor\Unity.exe'
if (-not (Test-Path -LiteralPath $unityExe)) { throw 'Required Unity editor is missing.' }
$runFolder = Join-Path $projectRoot ('Logs\ClaudeRuns\' + (Get-Date -Format 'yyyyMMdd_HHmmss'))
New-Item -ItemType Directory -Path $runFolder -ErrorAction Stop | Out-Null
$resultPath = Join-Path $runFolder 'playmode-results.xml'
$logPath = Join-Path $runFolder 'playmode-tests.log'
$runArgs = @(
    '-batchmode', '-nographics', '--burst-disable-compilation',
    '-projectPath', ('"{0}"' -f $projectRoot),
    '-runTests', '-testPlatform', 'PlayMode',
    '-testResults', ('"{0}"' -f $resultPath),
    '-logFile', ('"{0}"' -f $logPath)
)
$testProcess = Start-Process -FilePath $unityExe -ArgumentList $runArgs -WindowStyle Hidden -Wait -PassThru
$testProcess.ExitCode | Set-Content -LiteralPath (Join-Path $runFolder 'exit-code.txt')
if (-not (Test-Path -LiteralPath $resultPath)) { throw "No test results. Inspect $logPath" }
[xml]$testResults = Get-Content -LiteralPath $resultPath -Raw
$testResults.'test-run' | Select-Object result, total, passed, failed, skipped
if ($testProcess.ExitCode -ne 0 -or $testResults.'test-run'.result -ne 'Passed') {
    throw "Unity tests failed. Inspect $resultPath and $logPath"
}
```

`-quit`을 추가하지 않는다. 검사는 Test Runner가 종료하며, 실행 명령을 시작만 하고 도구 세션을 닫아 자식 프로세스가 사라지게 하지 않는다. 실제 완료까지 프로세스를 기다리고 XML과 종료 코드를 함께 확인한다.

직전 성공 증빙: [37/37 XML](../Logs/ReferenceApply/playmode-results.xml), [종료 코드 0](../Logs/ReferenceApply/test-exit-code.txt), [로그](../Logs/ReferenceApply/playmode-tests.log). 실행 시각은 **2026-11-07 23:40:57~23:41:54 KST**(XML의 UTC +9시간), 테스트 본 실행 약 57.7초. 편집기 시작·컴파일 시간은 별도다.

검사 구성:

| 파일 | 개수 | 보호하는 동작 |
| --- | --- | --- |
| [Day02CombatTests.cs](../Assets/Tests/PlayMode/Day02CombatTests.cs) | 7 | 기본 전투·이동·전환·HP 회귀 |
| [Day03CombatTests.cs](../Assets/Tests/PlayMode/Day03CombatTests.cs) | 9 | 상태·쿨타임·동시 입력·피격 취소·히트스톱·초기화 |
| [Day04EnemyTests.cs](../Assets/Tests/PlayMode/Day04EnemyTests.cs) | 12 | 순찰·탐지·추적·복귀·벽/낭떠러지·A/B 예고와 취소 |
| [ReferenceCombatTests.cs](../Assets/Tests/PlayMode/ReferenceCombatTests.cs) | 9 | 같은 프레임 방향, 준비 무피해, 후퇴 중 방향 유지, 취소, 사거리 차이, A 2타 처치 |

## 8. 알려진 실행 문제와 복구 범위

- 샌드박스 안에서 Unity 내부 BuildReportRestService 시작 중 충돌한 이력이 있다. 테스트 실패와 구분한다. [초기 충돌 로그](../Logs/ReferenceApply/sandbox-startup-crash.log)를 보존했다. 정상 권한에서 같은 프로젝트의 검사가 성공했다. 같은 문제가 재현되면 코드부터 바꾸지 말고 해당 도구의 권한 절차로 실행 환경을 해결한다.
- 라이선스·서비스 메시지가 로그에 있더라도 검사 XML과 종료 코드를 확인한다. 반대로 XML이 없으면 통과로 처리하지 않는다.
- 원본 Day 4/5 초기 순찰 검사는 고정 시간 가정 때문에 한 번 실패했고 이후 양끝 도달을 제한 시간 안에 확인하도록 개선됐다. 현재 `Day04EnemyTests`를 과거 고정 6초 검사로 되돌리지 않는다.
- 물리/입력 검사는 순서와 상태 복구가 중요하다. 현재 테스트의 키보드 장치 생성·제거, 입력 백그라운드 설정 복구, Time.timeScale 복구를 유지한다.
- 변경 전 사본: `Logs/ReferenceApply/Before/`. 일부 파일 사본이며 전체 프로젝트 복제본이 아니다. 사용자가 이후 수정한 파일에 일괄 복원하지 않는다.

## 9. 이미 끝난 분석에서 채택한 결론

- 짧은 구역에서 새로운 위험을 읽고, 실패 후 다시 학습하도록 구성한다.
- 피해량만 늘리기보다 거리·준비·회복·위치로 캐릭터 선택 이유를 만든다. 첫 기능 적용은 이미 완료됐다.
- 전투 영역은 즉시 위험, 측면 HUD는 누적 상태와 인물 표현을 담당한다.
- 공격·무적·경직·정신·외형 상태를 무조건 하나의 Animator 상태로 합치지 않는다.
- 다음 스테이지 구성 후보는 안전 진입 → A 단독 → B 단독 → 지형 변주 → A+B → 휴식 → 보스다. 아직 구현한 맵이 아니다.

필요할 때만 참조:

| 문서 | 목적과 주의 |
| --- | --- |
| [SINISISTAR_DEEP_ANALYSIS.md](Research/SINISISTAR_DEEP_ANALYSIS.md) | 종합 판단. 적용 전 수치 포함 |
| [SINISISTAR_LOCAL_ANALYSIS.md](Research/SINISISTAR_LOCAL_ANALYSIS.md) | 로컬 40개 타입의 선택적 CIL 분석. 모든 게임 코드 해독 아님 |
| [SINISISTAR_WEB_RESEARCH.md](Research/SINISISTAR_WEB_RESEARCH.md) | 공식·공략·한국 커뮤니티 출처, 판본 구분 |
| [SINISISTAR_RUNTIME_OBSERVATIONS.md](Research/SINISISTAR_RUNTIME_OBSERVATIONS.md) | 직접 실행 관찰의 실제 범위 |
| [SINISISTAR_PROJECT_MAPPING.md](Research/SINISISTAR_PROJECT_MAPPING.md) | 적용 전 프로젝트 분석. 현재 Day 5 값으로 사용 금지 |
| [DAY06_DESIGN_HANDOFF.md](DAY06_DESIGN_HANDOFF.md) | 디자인 전달 항목 |

원작은 1편 RJ247641이다. 로컬 readme는 v3.0.1p, 내부/설정 표시는 3.0.0. Steam Lite·2편을 섞지 않는다. YouTube 영상은 찾았지만 열기 실패로 시청 근거 없음. 원작 전 구간 직접 클리어·정확한 프레임 실측·음향 청취를 완료했다고 주장하지 않는다. 내부 Stamina는 확인한 사격 경로에서 화살 자원이며 자동 회복 지구력으로 단정하지 않는다.

## 10. 작업 종료 때 남길 것

1. 변경 목적과 실제 행동 차이, 수정 파일.
2. 실행한 검사·결과 파일·오류/미검증 사항.
3. 현재 상태와 다음 작업: [IMPLEMENTATION_STATUS.md](IMPLEMENTATION_STATUS.md), [project_status.json](../Planning/project_status.json), [CHANGELOG.md](../Planning/CHANGELOG.md), [DAILY_PROGRESS_LOG.csv](../Planning/DAILY_PROGRESS_LOG.csv).
4. 이 인계서와 루트 CLAUDE.md에서 달라진 상태/수치만 갱신한다. 과거 조사 기록을 최신 결과인 것처럼 덮어쓰지 않는다.

인계 기준 해시는 [HANDOFF_BASELINE.json](HANDOFF_BASELINE.json)에 있다. 문서·핵심 코드·씬·테스트·검사 결과를 선택해 기록했으며 전체 파일 감사를 의미하지 않는다. 후속 변경으로 해시가 다르면 그 파일과 관련 상태만 대조한다. 해시 불일치만으로 사용자의 변경을 되돌리거나 전체 조사를 다시 시작하지 않는다.

## 움직이는 사망 장면 틀 (2026-11-15)

`SKCharacterSpriteView.DeathPose`가 `Resources/SaintKnightSpriteCook/<캐릭터>_death_<원인>[1~4].png`를 반복 재생한다(그림 대기). 분담·이름표·남은 장면은 [요청서](Design/ART_REQUEST_DAY15.md)의 “HP 0 전투 도트” 절, 변경 내용은 [CHANGELOG](../Planning/CHANGELOG.md) 2026-11-15. 그림을 넣으면 `SpriteCookAppliedTests`의 사망 그림 이름 확인을 고친다.
