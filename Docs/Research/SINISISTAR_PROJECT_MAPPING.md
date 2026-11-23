# 시니시스타 분석을 위한 현재 프로젝트 대조표

후속 변경: 이 문서는 적용 전 코드 스냅샷의 분석이다. 이후 승인된 수정과 현재 수치는 [즉시 적용 기록](../REFERENCE_APPLY.md)에 있다. 아래 줄 번호와 수치는 분석 당시 기준이다.

작성일: 2026-11-07 (Asia/Seoul). 대상: `C:/UnityProject/SaintAndKnight`.
이 문서는 **현재 프로젝트 소스·직렬화된 Day 5 씬·기존 테스트 기록의 읽기 전용 분석**이다. 시니시스타의 사실은 별도 원본 분석과 웹 조사에서 확인한다. 여기의 개선안은 우리 게임의 설계 제안이며 원작에서 그대로 가져온 수치가 아니다. Day 6 구현·새 아트 제작·기존 코드 변경은 하지 않았다.

## 1. 결론

현재 프로젝트에는 전투 판정과 상태 전이의 기초가 이미 있다. 가장 큰 다음 과제는 공격의 준비–명중–회수 동작과 실제 판정을 일치시키는 것이다. 원작의 분위기나 화면 배치만 따라가기보다, 플레이어가 어떤 위험을 미리 읽고 어떤 행동으로 해결할 수 있는지를 현재의 두 캐릭터 시스템에 연결해야 한다.

두 캐릭터의 현재 전투 차이는 예상보다 작다. 세실리아는 20 피해/0.45초, 아델라는 25 피해/0.55초지만 적 A(60 HP)는 둘 다 3타, 적 B(80 HP)는 둘 다 4타다. 장검·방패와 사슬이라는 무기 설정이 아직 서로 다른 판단을 요구하지 않는다. 이 차별화는 원래 일정의 Day 7에서 다룰 수 있고, Day 6은 이를 가능하게 하는 애니메이션·판정 연결의 기반으로 한정하는 것이 적절하다.

## 2. 확인된 구현과 증거

| 영역 | 실제 확인 내용 | 소스 근거 |
| --- | --- | --- |
| 이동·점프 | 수평 속도 직접 적용, 점프 초기속도 7, 중력 배율 2.2. 가속/감속·코요테 타임·점프 입력 버퍼 로직 없음 | [SKDay01Player.cs:15](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01Player.cs:15), [입력과 물리:44](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01Player.cs:44) |
| 공격 상태 | Disabled/Dead/Hurt/Attacking/Recovery/Ready 판정. CanAct는 공격/회복을 차단하지 않아 그중 이동·점프·전환 가능 | [SKPlayerCombat.cs:33](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKPlayerCombat.cs:33) |
| 공격 발동 | J의 누른 순간에 TryAttack, 선딜 없이 Begin 호출. 누르고 있기 자동 공격 없음 | [입력:128](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01Player.cs:128), [공격:65](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKPlayerCombat.cs:65) |
| 전환 | 단일 물리 몸체/체력을 유지하고 선택 인덱스와 표시 스프라이트 전환. 진행 공격 취소, readyAt 보존 | [전환:81](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01Player.cs:81), [취소:77](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKPlayerCombat.cs:77) |
| 판정 분리 | 몸체, Hurtbox, Hitbox 분리. 한 공격에서 Health별 중복 접촉 방지, 같은 팀 제외 | [SKHitbox.cs:48](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKHitbox.cs:48) |
| 피격 | 실제 피해 후 무적·경직·밀림, Damaged/Changed 이벤트. 사망은 HP≤0 | [SKHealth.cs:45](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKHealth.cs:45) |
| 적 AI | 지상 순찰/추적/복귀, 거리·높이·벽 검사, 공격 예고/발동/회복 | [SKEnemyAgent.cs:92](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Runtime/SKEnemyAgent.cs:92) |
| 적 공격 방향 | 예고 시작 시 방향 확정, 예고 동안 이동 정지, 경고 범위=판정 범위 | [예고:140](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Runtime/SKEnemyAgent.cs:140), [표시:203](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Runtime/SKEnemyAgent.cs:203) |
| 지형 경계 | 벽 앞/낭떠러지 앞 정지·순찰 반전. 점프·우회 탐색 없음 | [이동:183](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Runtime/SKEnemyAgent.cs:183) |
| 히트스톱 | 유효 피해 성공 시만 0.045초 기본 요청 | [SKHitbox.cs:10](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKHitbox.cs:10), [피해 호출:65](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay02/Runtime/SKHitbox.cs:65) |
| HUD | 실제 HP 이벤트 구독, 캐릭터명·정적 초상화·손상 단계 표시. 정신 시스템은 없음 | [SKDay01HUD.cs:26](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01HUD.cs:26), [단계:61](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01HUD.cs:61) |

이번 검색에서 `Assets` 안의 `.anim`, `.controller`, `.glb`, `.fbx` 파일은 발견되지 않았다. 검색 범위 밖 외부 모델의 존재나 리깅 상태는 이 결과로 부정할 수 없다. 프로젝트의 승인 크롭 이미지와 임시 스프라이트 PNG는 존재하지만 최종 동작 프레임 세트는 아니다.

## 3. 실제 Day 5 수치

코드 기본값뿐 아니라 [Day05_EnemyPatterns.unity](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity)를 확인했다. 특히 적 무적시간은 `SKHealth` 기본값 0.55초가 아니라 씬에서 0.12초로 지정되어 있다.

| 항목 | 세실리아 | 아델라 | 근거 |
| --- | --- | --- | --- |
| HP | 100 공유 | 100 공유 | [씬:1512](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:1512) |
| 이동속도 | 3.6 units/s | 3.6 units/s | [씬:1351](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:1351) |
| 피해 | 20 | 25 | [씬:1529](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:1529) |
| 판정 크기 | 0.95×0.8 | 1.1×0.85 | 같은 프로필 |
| 중심 오프셋 | 0.72 | 0.8 | 같은 프로필 |
| 활성 시간 | 0.18초 | 0.20초 | 같은 프로필 |
| 공격 시작 간 최소 간격 | 0.45초 | 0.55초 | 같은 프로필 |
| 활성 종료 후 남는 회복 시간 | 약 0.27초 | 약 0.35초 | `cooldown-duration`의 정적 계산, 히트스톱/물리 틱 지연 제외 |
| 플레이어 무적/경직 | 0.55초/0.16초 | 동일 | [씬:1513](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:1513) |

| 항목 | 적 A | 적 B |
| --- | --- | --- |
| HP | 60 | 80 |
| 순찰/추적 속도 | 0.7/1.5 | 0.45/0.95 |
| 순찰 반경 | 1.2 | 0.65 |
| 탐지/추적 해제 거리 | 3/4.5 | 3/4.5 |
| 활동 반경/높이 허용 | 4/1.2 | 4/1.2 |
| 공격 시작 거리 | 1.1 | 1.5 |
| 공격 예고 | 0.4초 | 0.85초 |
| 판정 크기/중심 오프셋 | 1×0.85 / 0.75 | 1.6×0.85 / 1 |
| 활성/회복 | 0.18/1.0초 | 0.25/1.5초 |
| 피해/수평 밀림 속도 | 15/2 | 25/3 |
| 피격 무적/경직 | 0.12/0.16초 | 0.12/0.16초 |

근거: [A 프로필:1811](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:1811), [A 체력:1718](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:1718), [B 프로필:708](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:708), [B 체력:755](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:755).

### 수치가 만드는 행동의 해석

- 단순 지속 DPS는 20/0.45≈44.4와 25/0.55≈45.5로 약 2.3% 차이다. 이는 모든 공격이 명중하고 최소 간격으로 입력된다는 가정의 이론값이며 실제 측정 DPS가 아니다.
- A는 두 캐릭터 모두 3타, B는 둘 다 4타다. 최초 명중 이후 필요한 최소 공격 간격의 합은 세실리아가 오히려 더 짧다. 아델라의 약간 긴 사거리가 실제 상황에서 이득을 만드는지는 별도 플레이 검증이 필요하다.
- 플레이어 속도는 A 추적의 2.4배, B 추적의 약 3.79배다. 빈 평지에서는 후퇴 공간이 충분할 때 접근을 쉽게 끊을 수 있다는 설계상 추론이다. 지형·화면 끝·다중 적이 더해진 실제 난이도와 같다고 단정하지 않는다.
- 적 예고는 피격으로 취소되고 취소 후 회복에 들어간다. 연속 공격으로 억제하기 쉬운 상대라는 가설을 세울 수 있다. 모든 적에게 같은 취소 규칙을 적용하면 관찰보다 선공 연타가 유리해질 가능성이 있으므로 후속 적 역할 설계에서 검증한다.
- 현재 플레이어 공격 밀림 인수는 0이다. 사슬의 제어력이나 장검의 충격감은 아직 수치만으로 구현되지 않았다.

## 4. 시각·화면 연결에 중요한 실제 조건

현재 카메라는 정규화 좌표 x=0.25, y=0.125, width=0.75, height=0.75이며 orthographicSize=3.75다. 게임 화면은 전체 화면 면적의 56.25%, 왼쪽 HUD는 너비 25%를 차지한다. 이 값은 원작 실측치가 아니라 **현재 프로젝트의 구현값**이다. 근거: [Day05 씬:2898](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay04/Scenes/Day05_EnemyPatterns.unity:2898), [화면 생성:38](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Editor/SKDay01SceneBuilder.cs:38).

생성기에서 임시 인게임 스프라이트는 48 PPU, 초상화는 100 PPU, Point 필터/무압축/밉맵 끔으로 설정한다. 인게임 키 48px라는 기획과 PPU 48은 다른 개념이다. 무기 여백 때문에 96px 캔버스가 필요해도 몸체 키를 두 배로 만들 이유는 없다. 근거: [스프라이트 로딩:24](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Editor/SKDay01SceneBuilder.cs:24), [임포트:122](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Editor/SKDay01SceneBuilder.cs:122).

1920×1080 출력·현 카메라·48 PPU를 가정하면 게임 뷰 높이 810px/7.5 units=108 화면 px/unit이고 원본 1px는 2.25 화면 px가 된다. 따라서 Point 필터만으로 정수 배율을 보장하지는 않는다. 이는 계산상의 점검 항목이며 이번에 실화면을 캡처해 떨림을 확인한 것은 아니다. Day 8에서 가상 해상도와 Pixel Perfect/정수 배율 정책을 함께 결정할 가치가 있다.

## 5. Day 6의 핵심 연결 계약 — 제안

**사용자 승인과 디자인 준비 전에는 착수하지 않는다.** 기존 [디자인 인계 문서](C:/UnityProject/SaintAndKnight/Docs/DAY06_DESIGN_HANDOFF.md:3)는 실제 프레임·기준선·피벗·공격 프레임 확인을 요구한다. 최신 사용자 요청은 분석 후 Day 6 시작 전에 먼저 묻는 것이다.

1. **게임 로직과 표시의 기준을 하나로 정한다.** 현재는 공격 타이머가 권위자다. 연결 시 Animator는 전투 상태를 표현하고, 판정 시작/끝은 명시적 공격 타임라인 한 곳에서 관리하는 방식을 추천한다. Animation Event와 기존 타이머가 각각 별도 공격을 시작하면 중복 발동·취소 후 잔여 이벤트 문제가 생길 수 있다.
2. **선딜을 추가할지는 별도 결정한다.** 현재 발동은 즉시다. 원화에서 무기를 크게 들어 올리는 준비 프레임을 넣고 판정만 즉시 유지하면 보이기 전에 맞는 모순이 생긴다. 즉시 판정에 맞는 짧은 출발 동작을 쓰거나, 합의한 선딜을 추가하며 판정도 같이 늦춰야 한다. 후자는 단순 아트 연결을 넘어 조작감 변경이다.
3. **이동·방향 정책을 명시한다.** 현재 공격 중 이동과 방향 전환은 가능하지만 Hitbox 오프셋은 공격 시작 방향으로 고정된다. 반대쪽으로 돌아섰을 때 몸의 표시와 판정이 갈라질 수 있다. 공격 방향을 잠그거나, 몸 방향과 공격 동작을 분리하거나, 허용된 구간에서만 재지정하는 세 선택 중 하나를 정한다. 지금 바로 변경하지 않는다.
4. **취소 우선순위를 보존한다.** 사망/피격/비활성/전환 후 옛 애니메이션 이벤트가 공격을 되살리지 않게 공격 인스턴스 또는 현재 상태를 검증한다. 기존 `cancelledFrame`, `readyAt`, 중복 타격 방지를 유지한다.
5. **히트스톱과 재생 시간을 맞춘다.** 현재 물리·전투 시간은 함께 멈춘다. 인게임 Animator도 같은 시간축을 써야 적중 순간에 무기만 먼저 움직이지 않는다. 별도 HUD 애니메이션은 이후 다른 정책을 선택할 수 있다.
6. **아트 파일의 기준을 고정한다.** 캐릭터별 몸체 높이, 발바닥 피벗, 좌우 방향, 무기 여백, 동작별 루프 여부/재생속도, 유효 타격 프레임을 표로 받는다. 승인 디자인을 자동 재해석하거나 원본 게임 이미지를 가져오지 않는다.

Day 6 최소 범위는 기존 문서대로 Idle/Run/Attack/Hit 연결이다. 점프가 이미 존재하므로 공중에서 어떤 표시를 사용할지는 정의해야 하지만, 별도 Jump/Fall 신규 아트 제작을 승인 없이 필수 범위로 확대하지 않는다. 사망·전 손상 단계·정신 연출·3D HUD까지 한 번에 요구하지 않는다.

### 승인 후 검수할 상황

| 상황 | 합격 기준 |
| --- | --- |
| 제자리/이동 중 공격 | 보이는 무기 궤적과 실제 피해 구간이 설명 가능하게 일치 |
| 공격 중 반대 방향 입력 | 합의한 방향 잠금 또는 재지정 규칙대로 표시와 판정 일치 |
| 공격 중 전환·피격 | 공격 표시/판정 즉시 취소, 쿨타임 보존, 잔여 이벤트 무효 |
| 히트스톱 | 몸·무기·타이머가 함께 멈추고 자연스럽게 재개 |
| 반복 입력·다중 Hurtbox | 한 공격/한 대상당 피해 1회 유지 |
| R 초기화·HP 0 | 잘못 남은 공격·Animator 상태·timeScale 없음 |
| A/B 예고 | 경고·동작·실제 방향 및 범위 일치, 피해로 취소된 예고 재발동 없음 |
| 화면 비율 변경 | 발 기준선/픽셀 표시/HUD와 전투 시야 유지 |

## 6. Day 7 이후 적용 제안

| 원래 일정 | 적용 주제 | 제안과 결정 기준 |
| --- | --- | --- |
| Day 7 | 캐릭터 선택 이유 | 세실리아는 사슬의 거리 관리/제어, 아델라는 장검의 근접 안정성/방패 정체성을 검토. 기본 공격 1개씩으로 다른 판단을 만드는 것을 우선하고 미승인 방어·패링 시스템을 자동 추가하지 않음 |
| Day 8 | 화면과 카메라 | HUD 폭을 포함한 유효 전투 시야로 적 반응 거리를 설계. 화면 밖 선공 방지, 진행 방향 선행 시야, 픽셀 배율 검수 |
| Day 9~10 | 첫 구간 | 안전한 단독 A → 후퇴 공간이 있는 B → 공간 제약 → A/B 조합 순으로 학습. 새 적 수보다 이미 배운 행동의 재사용을 먼저 검증 |
| Day 12 | 전환 | 공유 HP 유지 여부, 공격 취소의 용도, 전환 자체가 전략적 선택인지 검증. HP/쿨타임 정책 변경은 별도 합의 |
| Day 13~17 | 상태/HUD | 체력 손상 0~4와 정신 상태를 별개 데이터로 유지. 두 캐릭터별 상태와 3D 표시 매핑을 정한 뒤 연결 |
| Day 18~19 | 피드백 | 현재 0.045초 히트스톱을 기준으로 적중음·피격색·짧은 카메라 반응의 역할 분리. 모든 타격을 같은 강도로 강조하지 않음 |
| Day 20~21 | 재시도/저장 | 훈련용 R과 실제 체크포인트를 분리. 실패 후 다시 같은 상황을 학습하기까지 걸리는 시간을 측정 |
| Day 22~25 | 보스/통합 | 초반에 배운 거리·예고·전환 판단을 조합. 새 규칙과 보스 체력 증가만으로 난이도를 만들지 않음 |
| Day 26~30 | 제품 검수 | 디버그 Q/E/R 노출, 일시정지와 히트스톱 소유권, 키 설정·해상도·실패 후 복귀 흐름 확인 |

## 7. 추후 검토할 위험과 미확정 사항

- **정신 붕괴와 HP 0 문구의 혼동:** 현재 HUD는 손상 단계 4(HP 0)에 `COLLAPSE`를 붙인다. 마스터 기획은 정신 붕괴를 체력과 별도 시스템으로 요구한다. 지금은 정신 시스템이 있다는 뜻이 아니며 후일 표현·상태를 분리해야 한다. [HUD:80](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01HUD.cs:80).
- **점프 접지 검사 후보:** 현재 접지 검사는 자신의 collider를 제외한 모든 비트리거 콜라이더를 인정한다. 적 몸체를 지형으로 오인하는지 추후 지형 레이어/자기 계층 제외를 확인할 필요가 있다. 이번 조사에서 재현한 버그로 단정하지 않는다. [접지:97](C:/UnityProject/SaintAndKnight/Assets/SaintKnightDay01/Runtime/SKDay01Player.cs:97).
- **실루엣 차별화:** A/B는 같은 훈련 표적 그림이다. 숫자와 예고 시간이 다르더라도 외형만으로 적 종류를 읽는 검증은 아직 할 수 없다.
- **입력 지원:** 현재 키보드 직접 조회 중심이다. 게임패드·키 재설정·입력 버퍼는 구현된 것으로 간주하지 않는다.
- **설계 권위:** `SKDay01HUD` 주석은 3D 연결을 Day 19+라 적지만 최신 마스터 일정은 Day 16~17이다. 일정 판단은 최신 마스터를 우선한다. 이번 보고서에서는 주석을 수정하지 않았다.
- **화면 평가 한계:** 이번 소스 대조는 수동 플레이, 픽셀 떨림, 실제 애니메이션 타이밍, 3D HUD 성능을 검증한 결과가 아니다.

## 8. 기존 검증과 이번 작업의 범위

[기존 Day 5 테스트 결과](C:/UnityProject/SaintAndKnight/Logs/Day05/playmode-results.xml:2)는 28/28 통과, 실패·건너뜀 0을 기록한다. 실행 시각은 2026-11-06 02:46:11~02:46:57 KST다. 이번 분석에서 테스트를 새로 실행하지 않았다. 문서가 추가되었다는 이유로 Unity를 실행하거나 씬을 재생성하지 않았다.

현재 코드는 전투 기초의 회귀 검증을 갖췄고, 아직 완성된 스테이지·최종 조작감·출시 빌드로 검증된 것은 아니다. 원작 분석 결과를 채택해도 기존 테스트 통과만으로 원작과 같은 긴장감이나 시각 품질을 얻었다고 판단할 수 없다. 승인 후에는 기능 회귀와 별도로 공격 가독성, 선택 이유, 재시도 흐름을 실제 화면에서 검수해야 한다.

분석 결론의 출처 구분: 위 구현/수치는 로컬 코드·씬 직접 확인, DPS/면적/배율은 명시된 입력에 대한 계산, 후속 단계는 설계 제안이다. 시니시스타의 내부 구현을 위 내용으로 추정하지 않는다.
