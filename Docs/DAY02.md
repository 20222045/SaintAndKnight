# Day 2 — 기본 전투 훈련

작업일: 2026-11-06 (Asia/Seoul). 기준 기획: [MASTER_PLAN.md](MASTER_PLAN.md).

## 실행

Unity 6000.0.81f1에서 `Assets/SaintKnightDay02/Scenes/Day02_CombatTraining.unity`를 열고 Play.
훈련 표적은 시작 위치의 오른쪽에 있다. 가까이 다가가 J로 공격한다.
표적은 플레이어가 가까이 있으면 주황색 영역으로 예고한 뒤 빨간 영역으로 반격한다.

| 키 | 동작 |
| --- | --- |
| A/D 또는 좌우 화살표 | 이동 |
| Space | 점프. 공격 중에도 가능 |
| Tab | 캐릭터 전환. 진행 중인 공격 취소, 공격 재사용 대기시간 유지 |
| J | 기본 공격. 누를 때마다 한 번, 누르고 있기 자동 공격 없음 |
| R | 훈련 초기화: 플레이어·표적 HP, 공격 상태, 플레이어 위치 복구 |
| Q / E | 기존 디버그 HP -25 / +25. 실제 HP와 HUD에 함께 반영 |

R은 이 훈련 씬의 테스트 도구이며 Day 20의 체크포인트·부활 시스템은 아니다.

## 구현

- 플레이어의 `SKHealth`가 실제 HP를 소유하고 기존 HUD는 변경 이벤트를 구독한다.
- Day 2 HP 바에 소스 이미지를 연결해 HP 숫자와 함께 막대 길이도 실제로 감소하도록 했다.
- Day 2의 체력은 두 캐릭터가 공유한다. 전환으로 회복되지 않는다.
- 세실리아·아델라의 공격 프로필은 각각 Inspector에서 조정할 수 있다.
  임시 값은 세실리아 20 피해 / 0.45초 간격, 아델라 25 피해 / 0.55초 간격.
  본격적인 공격 특성 차별화와 애니메이션은 이후 날짜 작업이다.
- 몸의 물리 충돌, 피격 Hurtbox, 공격 Hitbox를 별도 오브젝트/컴포넌트로 분리했다.
- 공격마다 피해 대상의 Health를 기록해 여러 물리 프레임·Hurtbox에서 중복 피해를 방지한다.
- 같은 팀과 자기 자신에게 피해를 주지 않는다. 공격 방향과 범위를 검사한다.
- 플레이어 피격: 0.16초 경직, 0.55초 무적, 색상 점멸, 반격에 의한 짧은 밀림.
- 피격 및 전환은 현재 공격을 취소한다. HP 0에서는 이동·점프·공격·전환을 차단한다.
- 훈련 표적: HP 150, 고정 위치, 0.5초 공격 예고, 공격당 15 피해, 1.5초 재공격 대기.
  이동·추적 AI는 추가하지 않았다.
- 기존 0~4단계 HUD 판정 유지. 정신 침식·정신 붕괴 시스템은 구현하지 않았다.
- Day 1 씬 파일을 보존하고 별도 Day 2 씬을 생성했다. Build Settings에 두 씬을 유지한다.
- 훈련 중 화면 밖으로 빠지지 않도록 양끝에 임시 벽을 배치했다. 횡스크롤 카메라는 미구현.

## 파일

- 수정: `Assets/SaintKnightDay01/Runtime/SKDay01Player.cs`, `SKDay01HUD.cs`.
- 추가: `Assets/SaintKnightDay02/Runtime/`의 Health, Hitbox, Hurtbox, PlayerCombat,
  TrainingTarget, TrainingSession 스크립트.
- 추가: `Assets/SaintKnightDay02/Editor/SKDay02SceneBuilder.cs`.
  메뉴 `Tools > SaintKnight > Day 02 > Create Combat Training Scene`으로 원래 Day 1 씬에서 재생성 가능.
  이 메뉴는 Day 2 씬을 재생성하므로 수동으로 조정한 Day 2 씬은 먼저 별도 저장한다.
- 추가: `Assets/SaintKnightDay02/Scenes/Day02_CombatTraining.unity` 및 임시 표적/판정 표시 PNG.
- 추가: 런타임·에디터·Play Mode 테스트 분리용 `.asmdef` 및 Unity `.meta` 파일.
- 추가: `Assets/Tests/PlayMode/Day02CombatTests.cs`.
- 수정: `ProjectSettings/EditorBuildSettings.asset` — Day 2를 첫 실행 씬으로 등록, Day 1 유지.

## 검증

Unity의 Play Mode 테스트로 실제 Keyboard 입력, 물리 판정, HUD 텍스트/바를 검증한다.
Test Runner에서 `Day02CombatTests`를 실행하거나 다음 명령을 사용한다.

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.0.81f1\Editor\Unity.exe' `
  -batchmode -nographics --burst-disable-compilation -projectPath C:\UnityProject\SaintAndKnight `
  -runTests -testPlatform PlayMode -testFilter Day02CombatTests `
  -testResults C:\UnityProject\SaintAndKnight\Logs\Day02\playmode-results.xml `
  -logFile C:\UnityProject\SaintAndKnight\Logs\Day02\playmode-tests.log
```

검증 결과는 아래 최종 기록 및 `Logs/Day02/`에 남긴다.

### 최종 검증 기록

- Unity 6000.0.81f1 씬 생성 성공, 실행 종료 코드 0.
- 최종 Play Mode: **7개 통과 / 실패 0 / 건너뜀 0**, 실행 종료 코드 0.
- C# 컴파일 오류(`error CS`) 및 컴파일 경고(`warning CS`) 없음.
- 검증 내용: A/D 이동, 공격 중 점프·전환, J 공격 입력, 캐릭터별 피해,
  방향·사거리, 공격당 1회 피해, 전환 시 재사용 대기시간 유지, 표적 반격·밀림,
  무적·경직·전투 불능, R 초기화, Q/E 디버그 HP, 원래 Day 1 씬 회귀.
- HUD: 실제 HP 85에 대해 텍스트 `HP 85 / 100`, fillAmount 0.85,
  실제 UI 메시의 가로 폭이 바 전체 폭의 85%인지 확인.
- 원래 Day 1 씬 SHA-256 (작업 전후 동일):
  `3815D18EA27F5D6C51AF98B6A5472F289479C94D6E6BAB1C73009328CBC40FC6`.

검증 과정에서 샌드박스의 Unity 소켓 초기화 충돌이 발생하여 정상 권한의 배치 실행으로
검증했다. 최초 검사에서는 화면 없는 환경의 입력 포커스와 물리 프레임 대기 부족으로
입력 테스트가 실패하여 **테스트 설정과 입력 유지 시간**을 수정했다.
UI 메시 검사의 오버로드 지정 문제도 수정하고 전체 테스트를 다시 통과했다.

최초 실행 중 Burst 네이티브 링커에서 Win32 IO 232 오류가 발생했다.
최종 자동 검증에서는 `--burst-disable-compilation`을 해당 실행에만 적용했다.
프로젝트의 Burst 설정은 변경하지 않았다. 일부 Unity 서비스 entitlement 조회 404 로그는
게임 C# 컴파일 오류와 별개로 남는다. 최종 결과는 위 조건의 배치 Play Mode 검증이며,
일반 에디터에서의 수동 조작감·렌더링 검수나 Windows 배포 빌드를 완료한 것은 아니다.

결과 원본: `Logs/Day02/playmode-results.xml`, `Logs/Day02/playmode-tests.log`.
씬 생성 기록: `Logs/Day02/scene-build.log`.

## 확인되지 않은 자료와 범위 밖 작업

프로젝트 파일 검색에서 `Cecilia_blockout_NOT_FINAL.glb`, `Adela_blockout_NOT_FINAL.glb`를
찾지 못했다. 외부 위치의 존재 여부·리깅 상태는 미확인이다. Unity GLB 임포트와 3D HUD 연결을
완료로 간주하지 않는다. 기존 원화·도트·디자인은 변경하지 않았다.

새 3D 모델, 전체 스테이지, 보스, 전투 애니메이션, 정신 붕괴 및 컷신은 이번 범위 밖이다.
