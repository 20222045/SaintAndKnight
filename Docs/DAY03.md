# Day 3 — 공격·피격 상태 안정화

작업일: 2026-11-06 (Asia/Seoul). 기획 기준: `MASTER_PLAN.md`의 Day 3 및
마스터 ZIP 로드맵의 기본 상태 머신·히트스톱·무적 시간 회귀 검증.

## 실행

기존 `Assets/SaintKnightDay02/Scenes/Day02_CombatTraining.unity`를 열고 Play한다.
Day 3는 같은 훈련 씬의 전투 로직을 개선하며 별도 씬 생성이나 재생성이 필요 없다.
A/D 이동, Space 점프, Tab 전환, J 공격, Q/E 디버그 HP, R 초기화를 유지한다.
히트스톱 중에는 이동·점프·전환·공격·Q/E를 받지 않고 R 초기화는 허용한다.
정지 중 누른 공격은 예약되지 않으며 해제 후 다시 눌러야 한다.

## 상태와 입력 정책

- 플레이어: Ready → Attacking → Recovery → Ready.
- 표적: Ready → Windup → Attacking → Recovery → Ready.
- 양쪽 공통 우선순위: 컴포넌트 비활성 > Dead > Hurt > 공격 진행 단계 > Recovery > Ready.
  HP·Hitbox·시간 정보를 단일 기준으로 조회하므로 별도 상태 플래그가 뒤늦게 갱신되지 않는다.
- 피해를 받으면 공격/예고를 즉시 취소한다. 사망 상태에서는 이동·점프·공격·전환을 차단한다.
  사망 전에 읽은 이동 입력도 다음 물리 프레임에서 수평 속도를 0으로 만든다.
- 공격/회복 중 이동·점프·전환은 유지한다. 전환은 진행 중 공격을 취소하며 쿨타임은 유지한다.
- Tab+J 동시 입력은 전환만 수행한다. 불발 공격으로 쿨타임이 소모되지 않는다.
  입력 처리를 플레이어 Update 한 곳으로 모아 컴포넌트 실행 순서 의존성을 없앴다.
- 쿨타임을 공격 지속시간보다 짧게 설정해도 활성 공격을 재시작할 수 없다.
- 플레이어 경직 0.16초, 무적 0.55초를 유지한다. 실제 피해만 무적/경직을 갱신한다.
- 두 캐릭터는 기존처럼 HP를 공유한다. Q/E는 개발용이므로 E로 전투 불능에서 회복할 수 있다.

## 히트스톱

- 실제 Hitbox 피해 적용에 성공했을 때만 기본 0.045초 동안 멈춘다.
  헛공격·무적에 막힌 타격·Q 디버그 피해는 히트스톱을 발생시키지 않는다.
- `SKHitbox.hitStopSeconds`는 Inspector에서 0~0.1초로 조절 가능하다. 0은 끄기다.
- 전투 시간과 2D 물리는 `Time.timeScale = 0`으로 함께 정지한다. UI/입력 Update는 유지한다.
  복구는 unscaled time으로 계산하여 정지 중에도 만료된다.
- 공격 지속시간·쿨타임·경직·무적·표적 예고 시간은 정지 중 소모되지 않는다.
- 중첩 요청은 남은 정지 시간의 최댓값을 사용하며 시간을 누적하지 않는다.
- 정지 전 속도를 기억한다. R 초기화, 씬 언로드, 컨트롤러 비활성/파괴 시 복구한다.
  이미 timeScale=0인 외부 일시정지는 소유하지 않는다.
- 이후 Day 26 일시정지 메뉴를 구현할 때는 히트스톱 중 별도로 발생한 메뉴 정지도
  구분하도록 공용 시간 제어에 통합해야 한다. 현재 프로젝트에는 메뉴 일시정지가 없다.

## 변경 파일

- 추가: `Assets/SaintKnightDay02/Runtime/SKCombatState.cs`, `SKHitStop.cs`와 `.meta`.
- 수정: 같은 Runtime 폴더의 `SKPlayerCombat.cs`, `SKHitbox.cs`, `SKHealth.cs`,
  `SKTrainingTarget.cs`, `SKTrainingSession.cs`.
- 수정: `Assets/SaintKnightDay01/Runtime/SKDay01Player.cs`.
- 추가: `Assets/Tests/PlayMode/Day03CombatTests.cs`와 `.meta`.
- 진행 기록: 이 문서, `IMPLEMENTATION_STATUS.md`, `Planning/`의 상태·변경 기록.

변경 전 백업: `Logs/Day03/Backup_20261106_015554/`.
원본 마스터 ZIP과 캐릭터 이미지·모델을 추가하거나 수정하지 않았다.
씬 파일과 Build Settings도 재생성하지 않았다.

## 검증

기존 Day 2 테스트 7개와 Day 3 테스트 9개를 함께 실행한다.
Day 3 검증은 공격/회복 상태, 짧은 쿨타임의 중복 타격 방지, Tab+J,
피격/사망/비활성 취소, 실제 타격의 물리·시간 정지, 헛공격/무적 접촉,
R·씬 전환의 시간 복구, 정지 중 입력 미예약, 표적 예고 중단을 포함한다.

결과 기록 위치: `Logs/Day03/playmode-results.xml`, `Logs/Day03/playmode-tests.log`.
최종 결과: **16개 통과 / 실패 0 / 건너뜀 0**, Unity 종료 코드 0.
실행 시각: 2026-11-06 02:04:34~02:04:59 KST. C# 컴파일 오류·경고 없음.
기존 Day 2 테스트 7개도 수정 없이 통과했다.

첫 실행은 Unity 초기화 단계에서 실패했고, 재시도 과정에서 이전 배치 프로세스의
프로젝트 잠금과 충돌했다. 해당 프로세스들의 종료 확인 후 정상 권한으로 실행했다.
최초 테스트 실행의 15/16 통과 후, 초기화가 플레이어를 안전한 시작 위치로 되돌린다는 점을
누락한 테스트를 수정했다. 다시 표적 가까이 배치한 최종 실행에서 16/16을 확인했다.
실패 기록도 `Logs/Day03/`에 보존했다.

Day 2와 동일하게 이번 배치 실행에만 `--burst-disable-compilation`을 사용했다.
Unity 서비스 라이선스 entitlement 404 로그는 남아 있지만 테스트는 정상 종료했다.
프로젝트의 Burst 설정은 변경하지 않았다.

기존 아트/씬 파일 11개는 작업 전후 SHA-256이 동일하며 Build Settings도 동일하다.
증빙 목록: `Logs/Day03/assets-before.csv`.

실행 명령(다른 Unity 인스턴스가 이 프로젝트를 열고 있지 않은 상태):

```powershell
$unityProcess = Start-Process -FilePath 'C:\Program Files\Unity\Hub\Editor\6000.0.81f1\Editor\Unity.exe' `
  -ArgumentList @('-batchmode','-nographics','--burst-disable-compilation',
    '-projectPath','C:\UnityProject\SaintAndKnight','-runTests','-testPlatform','PlayMode',
    '-testResults','C:\UnityProject\SaintAndKnight\Logs\Day03\playmode-results.xml',
    '-logFile','C:\UnityProject\SaintAndKnight\Logs\Day03\playmode-tests.log') `
  -WindowStyle Hidden -Wait -PassThru
$unityProcess.ExitCode
```

수동 조작감·렌더링 검수와 Windows 배포 빌드는 이번 자동 검증에 포함하지 않는다.
캐릭터 디자인은 보류한다. 이후 디자인 제작·적용이 필요해지면 작업 전에 사용자에게 알린다.
다음 개발 범위는 Day 4 일반 몬스터 A의 이동·추적 AI다.
