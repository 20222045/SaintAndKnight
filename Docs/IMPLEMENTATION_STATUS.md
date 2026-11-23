# 현재 프로젝트 구현 상태

Claude 및 다른 도구의 작업 진입점: 루트 [CLAUDE.md](../CLAUDE.md), [구현 인계서](CLAUDE_HANDOFF.md).

기준일: 2026-11-16 (Asia/Seoul). 대상: `C:/UnityProject/SaintAndKnight`.
마스터 ZIP은 이전 상태의 자료다. 아래는 실제 로컬 소스·씬·테스트를 기준으로 한 기록이다.

| 범위 | 상태 | 증빙 |
| --- | --- | --- |
| 마을 거리 좌우 확장 (2026-11-22) | 구현·자동 검증·캡처 확인 | 기존 마을 그림 양옆만 NovelAI 인페인트로 늘려 640→1260px(옛 그림은 가운데 그대로), 마을 카메라가 플레이어를 따라감. Play Mode 199통과/실패0(전체225/Explicit26) `Logs/VillageWide20261122/Full2/`, 캡처 `Logs/Village/Screens/`. 직접 플레이 미실시 |
| 촉수 잡기 2D 중간 자세 | 적용·빌드 | 두 캐릭터의 촉수 잡기 첫 1초에 2D 자세 네 장씩을 넣어 뒷무릎 착지와 이마에 손이 닿는 순간을 나눴다. 프롬프트 원문과 그림을 담은 PDF·비공개 ZIP도 제작했다. Play Mode 198개 통과·26개 제외·실패 0개, 보스방 그래픽 17/17. Windows 빌드·15초 기동 확인. [결과](Design/DeathScenes-20261117/BossReference/CLAUDE_2D_HANDOFF.md) |
| 마을·시간대 하늘·굴뚝 연기 (2026-11-19) | 구현·자동 검증·캡처 확인 | 보스 클리어 출구 → 새 씬 `Village`. 온천·은토끼 살롱을 로비에서 마을로 옮김(로비 옆문이 마을로). NovelAI 1장을 도트화한 상점가 배경, `SKVillageSky` 하루 8분 주기(하늘·별·해달·건물 색·창문 불빛), 굴뚝 6곳 pixelfx 연기. Play Mode 198통과/실패0(전체224/Explicit26) `Logs/Village/Full3/`. 수동 조작 검수·빌드 미실시 |
| 보스 캐릭터 디자인 실적용 (2026-11-19) | 재생·촬영·빌드 | 세실리아와 아델라의 동작별 캐릭터 원화를 일곱 보스 사망 연출에 연결했다. 두 캐릭터 합계 14시트·280프레임이고, 고정 자세 168프레임에서 양팔과 촉수 접점 오차는 0px였다. Play Mode 191개 통과·23개 제외·실패 0개, 별도 그래픽 16/16. Windows 빌드와 15초 기동을 확인했다. [원화·GIF](Design/DeathScenes-20261117/BossReference/CharacterPoses/README.md) |
| 보스 사망 사진 7장 (2026-11-19) | 적용·촬영·검사·빌드 완료 | 바닥 둥지·이마 손 잡기·세로 점액과 공통 마무리 4종을 두 캐릭터에 적용했다. 14시트·280칸. 잡기는 4초이며 실제 보스까지 촉수가 이어진다. Play Mode 185개 통과(실패 0), 관련 그래픽 16/16, Windows 빌드·15초 기동 확인. [결과](Design/DeathScenes-20261117/BossReference/RESULT.md) |
| 대성당 회랑 3D 배경 실험 (2026-11-19) | 분리 씬에서 검증·촬영 | 아치 한 칸·기둥 두 개를 640×360 실시간 렌더링. 원본 2D와 전환, 세실리아 걷기 비교 32쌍. Play Mode 191 통과, 실패 0, 전체 213, Explicit 22. 그래픽 1/1. [실험 기록과 비교](Design/Stage/CORRIDOR_3D_PREVIEW.md) |
| INARI식 대화·색감 (2026-11-19) | 구현·자동 검증·캡처 확인 | 머리 위 대화 상자 `SKDialogue`(이름 왼쪽 위, 붉은 키워드, 오른쪽 흉상, 자동 진행; 성소 첫 도착·첫 클리어 대사), 씬별 URP 색감 `SKAtmosphere`. Play Mode 191통과/실패0(전체212/Explicit21) `Logs/Story/InariFull2/`, 캡처 `Logs/Story/InariCaptures/`. 아델라 흉상 없음, 수동 검수·빌드 미실시 |
| 스토리 A안·도트 시작 화면 (2026-11-19) | 구현·자동 검증·캡처 확인 | 교단 제물 의식 스토리 [STORY.md](Design/STORY.md). 시작 화면: 검은 화면 프롤로그 → 사슬 섬광 → 640×360 제단 도트 장면 2배(성모 피눈물·끊어진 사슬·두 주인공, `Design/Title/make_title_scene.py`), 로고 장미창 위·메뉴 오른쪽·끊어진 사슬 표식. Play Mode 186통과/실패0(전체206/Explicit20) `Logs/Story/PixelFull/`, 캡처 4장 확인. 아델라 일러스트 없음, 수동 검수·빌드·소리 미실시 |
| 남은 일반 적·회랑4종 (2026-11-18) | 두 캐릭터 구현·촬영·검사·빌드 완료 | 남은 수도사·처형인·회랑 바닥·살덩이 벽을 두 캐릭터 모두 새 원본 도트 동작으로 마무리했다. 양팔 붙듦/옆으로 접힘/수직 삼키기/수평 벽 흡수를 구분했다. 보스와 일반 점액 PNG25개는 해시 불변. Play Mode 185통과/실패0(전체204/Explicit19), 관련 그래픽 16/16. 실제 촬영·Windows 빌드·15초 기동 확인. [결과](Design/DeathScenes-20261117/StageRevision/RESULT_20261118.md). 수동 조작감 미검수 |
| 보스 공통 사망 운반 (2026-11-18) | 구현·검사·실제 촬영·빌드 완료 | 보스 공통 무릎 자세를 실제 보스 앞으로 들어 옮기는 연출을 연결했다. 원본 전신 도트·복장 유지, 넓은 촉수 날개를 짧은 팔/무릎 감김과 실제 보스에서 이어지는 세 가닥으로 교체. 두 캐릭터×좌우4사례 도착 확인. Play Mode 182통과/실패0(전체201/Explicit19), 관련 그래픽 13/13. Windows 빌드·15초 기동 확인. [결과](Design/DeathScenes-20261117/BossRevision/CARRY_RESULT_20261118.md). 수동 조작감 미검수 |
| 원본 전신·공통 무릎 사진 맞춤 (2026-11-18) | 구현·접점/원본 의상 검사·실제 촬영·빌드 완료 | 사용자 최신 사진에 맞춰 보스 공통6키의 마지막 자세를 무릎·이마 손·앞으로 굽힌 팔·뒤에서 붙듦으로 통일했다. 생성 포즈를 제외하고 원본 전신·팔·손·장비를 사용한다.26시트/468칸, 반복160프레임의 윗팔·손목 접점 검사. Play Mode 180/180(전체198/Explicit18/실패0), 그래픽10/10. 실제32사례 캡처·Windows 빌드·15초 기동 확인. [최신 결과](Design/DeathScenes-20261117/BossRevision/CONTACT_RESULT_20261118.md). 수동 조작감 미검수 |
| 보스 후속 수정 (2026-11-17) | 26시트/468칸·공통6종·연결·자동 검사·빌드 완료 | 26시트/468칸(두 캐릭터×13종), 보스3패턴+공통6종. 원본 PNG·머리·팔레트 보존, AI 보조 신규6자세·곡선 촉수. Play Mode 178/178(전체196/Explicit18/실패0), 그래픽8/8. 실제26사망+6포획 캡처96장/동작576장, Windows 빌드·15초 기동 확인. 수동 조작감 미검수. [결과](Design/DeathScenes-20261117/BossRevision/RESULT.md) |
| 최초 원본 도트 포획·사망 모션 (2026-11-17) | 24시트/432칸 연결·자동 검사·실제 게임 캡처·Windows 빌드 완료 | 두 캐릭터/12종, 원본 픽셀 복사·소재 합성. 보스3패턴+공통5종, 점액·휩쓸기 유지, 새 사망 R 대기. Play Mode180/180(전체197/Explicit17), 그래픽8/8 `Logs/NativeDefeat20261117/`, 빌드 `Builds/Win64_NativeDefeat/`. [적용·한계·GIF·프롬프트](Design/DeathScenes-20261117/DESIGN.md). 수동 조작감 미검수 |
| 클리어 후 휴식 공간 (2026-11-17) | 구현·자동 검증 완료; 2026-11-19 술집→은토끼 살롱(8/8 `Logs/RestCG/Run5/`), 살롱 CG 4장 대기; 수동 검수·빌드 미실시 | 1스테이지 클리어 후 성소 온천(`SKHotSpring`)·술집(`SKTavern`), 첫 방문 에필로그 8컷(`SKEpilogue`), 세실리아 단계 일러스트 2D 도트(2배·128색), 표정 클로즈업(`SKExpressionCutIn`). 부위 룰렛은 사용자 결정으로 삭제. Play Mode 178/178(전체196/Explicit18/실패0/종료0) `Logs/RestInjury/Full4/`, 캡처 `Logs/RestInjury/Screens/`. [분석·적용](Design/REFERENCE_UNREASONX.md) |
| Day 17~30 그림·소리 제외분 (2026-11-16) | 구현·자동 검증·빌드 완료; 소리(Day 19)·그림·수동 검수 미실시 | 체크포인트·저장·씬 간 HP/캐릭터 유지, 보스 클리어→성소 귀환, 타이틀·일시정지·설정, 기록·갤러리(3D 일러 자리), 피격 이펙트, 잡힘 HUD 자리. 화면의 HP·이름·데미지·조작키 글자와 적·보스 HP 막대 제거(사용자 결정). 같은 날 화면 다듬기(전투 화면 그늘·Esc 새 스타일·시작 화면 새 디자인) 후 Play Mode 165/165(전체179/Explicit14/실패0/종료0) `Logs/UiRestyle/full/`, PlaytestProbe 4/4, 빌드 `Builds/Win64_Day30/`(배치 기동 15초). [Day 17~30 기록](DAY17_30.md) |
| 움직이는 사망 장면 (2026-11-15) | 틀 연결 + 수도사 장면 두 캐릭터 적용·자동 검증; 나머지 장면·화면 캡처·수동 검수 미실시 | `<캐릭터>_death_<원인>[1~4].png`를 넣으면 반복 재생. 수도사는 수위 없는 판 적용. Play Mode 151/151(전체164/Explicit13/실패0/종료0) `Logs/DeathScenes/Full4/`. [요청서](Design/ART_REQUEST_DAY15.md) |
| 요청 도트 게임 화면 연결 (2026-11-13) | 연결·자동 검증 완료; 수동 검수·빌드 미실시 | 함정 반응·적/보스 사망·보스 잡기/쓰러짐·보스 점액 시트·HP 구슬. 표시만 변경. Play Mode 151/151(전체164/Explicit13/실패0/종료0), 게임 화면 28장. [연결 기록](SPRITECOOK_APPLIED.md) |
| SpriteCook 요청 도트7장 (2026-11-13) | 제작·규격 통일·59Sprite 분할 완료; 게임 연결은 위 행 | 128×96칸·세실리아55px/아델라62px·48 PPU/Point/무압축. 캐릭터56/UI3, 원본 보존·재현84파일 일치·실제Sprite59 임포트 검증. 신규3개 포함 Play Mode149/149(전체162/Explicit13/실패0/종료0). [완료 기록](SPRITECOOK_DRAFTS_COMPLETED.md) |
| Day 1 이동·점프·전환·HP HUD | 구현 및 자동 회귀 검증 완료 | 기존 씬 유지, Day 2 테스트의 Day 1 회귀 항목 통과 |
| Day 2 기본 전투·표적·피격·HUD 연동 | 구현 및 자동 검증 완료 | `DAY02.md`, 기존 테스트 7개 재통과 |
| Day 3 상태 안정화·히트스톱 | 구현 및 자동 검증 완료 | `DAY03.md`, 새 테스트 9개 통과 |
| Day 4 적 이동·탐지·추적·복귀 | 구현 및 자동 검증 완료 | 전용 Day 4 씬, Day 4/5 통합 테스트 통과 |
| Day 5 두 번째 적 접근·공격 | 구현 및 자동 검증 완료 | A/B 공격 예고·범위·피해 차별화, 전용 Day 5 씬 |
| 레퍼런스 분석 즉시 적용 | 구현 및 자동 검증 완료 | 공격 준비/방향/취소, Day 5 캐릭터별 사거리·위력, HP 0 문구. `REFERENCE_APPLY.md` |
| 미결 결정 처리 (2026-11-10) | 구현 및 자동 검증, 수동 미검수 | 적 반격(피격 후 다음 준비 아머)·공격 중 점프 불가·S/↓ 웅크리기·B 높은 공격. Day 5·로비. `REFERENCE_UPGRADE_20261108.md` 7절 |
| Day 7 캐릭터별 공격 특성 (2026-11-10) | 구현 및 자동 검증, 수동 미검수 | 사용자 선택 “아머 파괴 + 사슬 견제”. 아델라 장검 `breaksArmor`: B 큰 공격·A 잡기 준비의 고유 아머를 끊음(맞은 뒤 반격 아머는 유지). 세실리아 사슬 `interruptDelay` 0.2초: 적의 공격 준비를 끊으면 다음 공격이 0.2초 늦음(그 사이 타격은 대기를 바꾸지 않음). Day 5·Day 8·로비 씬. `CombatDecisionTests` +3 |
| 2차 분석 업그레이드 (2026-11-08) | 구현 및 자동 검증 완료 | 가변 점프·공중 이동·피격 방향·적 A 잡기/연타 탈출·자동 재시도. Day 5 씬만. `REFERENCE_UPGRADE_20261108.md` |
| Day 6 캐릭터 다중 프레임 (2026-11-10) | 로비·Day 5에 연결, 수동 조작감 미검수 | `SKCharacterSpriteView`와 `SKCharacterMotionLibrary`. 세실리아 68셀·아델라 78셀, 기존 크기·팔레트·피벗. 대기·달리기·점프·준비/타격/회수·피격·잡기 성공/버티기/연타 저항/성공 탈출을 실제 게임 상태로 재생. 모든 유효 타격의 앞끝을 기존 판정에 맞춤. HP 손상 단계별 대기·쓰러짐 12장·2D HUD 유지. `Day06SpriteViewTests`가 로비·Day 5와 실제 적 A 잡기를 검사한다. |
| 캐릭터 도트 시안 v1~v5 (2026-11-08~10) | v5를 게임에 연결 | `Docs/Design/Characters/make_characters_v*.py`. v1 직접 그림(폐기) → v2 시트 작은 도트 → v3 큰 콘셉트 픽셀화 → v4 크기 15%·보정 → v5 동작 프레임 통합 → 사용자 선택으로 v4 느낌(필터·얼굴 손질 없음) 확정. v6 손 배치 거친 도트는 불채택 |
| Day 9 첫 스테이지 기본 지형 (2026-11-10) | 구현 및 자동 검증, 수동 조작감 미검수 | `Day09_Stage1`(Day 8 기반, 생성 `Tools > SaintKnight > Day 09 > Create Stage 1 Scene`). 무너진 대성당 4화면(묘지 → 회랑 → 계단·낮은 천장 → 예배당 앞), 배경 = `Docs/Design/Stage/pixel-redraw-v1` 도트, 끝 문에 닿으면(Day 11 자동 입장) → 보스 예배당 `Day09_BossChapel`(한 화면, 보스 `SKBossAgent` 장미창의 성모 — 사용자 첨부 기반 전신 도트 v4. 대기·붉은 2페이즈·처치 포즈가 기존 보스 상태를 따라 표시됨), 적 A1 → B1 → A2(위층) → A3+B2 학습 순서. 그림·충돌은 `Docs/Design/Stage/make_day09_art.py`의 `stage1_layout.json` 하나로 맞춤. 적은 사용자 도트(`Docs/Design/Enemies/Day09` → `SKEnemySpriteView`, 수도사 잡힘은 세실리아/아델라 합성 그림). `Day09StageTests` 9개 + 캡처 1개(Explicit) |
| Day 10 함정과 환경 피해 (2026-11-11) | 정식 도트 연결 및 자동 검증, 수동 조작감 미검수 | `Day09_Stage1` 회랑과 `Day09_BossChapel`이 `SKFloorSpike`(예고 2·솟음 3·들어감 2장, 피해 15)와 `SKSlimePool`(반복 3장, 걷기 ×0.5)을 공유한다. 예배당 앞 x1945px의 `SKFleshWall` 초기 아트는 대기3·뻗음1·캐릭터별 삼켜짐2/버팀2/탈출1이며 현재 포획 표시는 아래 전용 시트 후속을 따른다. `Day10TrapTests` 9개. 초기 Day14 반응 그림과 당시123/123은 [DAY14.md](DAY14.md)에 보존하며, 양 캐릭터의 포획·삼켜짐 전용 시트는 [최신 기록](TRAP_DEDICATED_MOTION.md)을 따른다. HUD 상태 변형은 Day17 예정. [도트 요청서](Design/Enemies/SPRITE_REQUEST_DAY09.md#4-함정-day-10) |
| Day 11 문·상호작용 (2026-11-11) | 구현 및 자동 검증·캡처 확인, 수동 조작감 미검수 | `Day09_Stage1` x2500 문은 플레이어가 닿으면(좌우 0.6유닛) 키 입력 없이 보스 예배당으로 넘어간다(`SKSceneDoor`, “W  ENTER” 문구 삭제). 사용자 결정: 레버·잠금은 만들었다가 뺐다 — 길목의 적이 관문이다. 전투 불능·잡힘 중에는 넘어가지 않는다. |
| Day 12 캐릭터 능력 및 전환 검증 (2026-11-11) | 자동 통합 검증 완료, 수동 조작감 미검수 | `Day12AbilityTests` 3개 + `Day12SwitchTests` 7개. 최신 스테이지의 전환 후 아머 파괴/사슬 지연/반격 아머 유지, 스테이지·보스방의 공격 취소·쿨타임·공유 HP·HUD·행동 불가 상태, 웅크림·공중·점액·살덩이 벽/R 복구. 보스방 공격 프로필 비교 추가. 런타임·씬·아트 변경 없음. [상세](DAY12.md) |
| Day 13 외형 손상 0~4단계 판정 (2026-11-11~12) | 공용 체력 판정 및 자동 통합 검증 완료, 수동 미검수 | `SKHealth.DamageStage` / `DamageStageFor`를 HUD·캐릭터 표시에서 공유. 최대 HP 비율·정확한 경계·피해/Drain·회복/Reset·양 캐릭터·최신 두 씬·실제 살덩이 벽을 신규 8개로 검증. 기존 아트·씬·전투 수치 유지. 기존 보스 점프 검사는 실제 상승·타격 순간 발높이·착지를 관측하도록 보완 후 통과. [상세](DAY13.md) |
| Day 14 초기 함정 반응 도트 연결 (2026-11-12) | 초기 임시 합성 그림 연결 및 자동 검증 | `SpikeHurt`·`SlimeWalk`·`SlimeIdle` 초기 연결과 `Day14TrapReactionTests` 2개 포함 당시 통합 전체 123/123을 보존한다. 이후 포획 표현은 아래 추가 요청 구현을 따른다. [기록](DAY14.md) · [도트 안내](Design/Characters/TRAP_REACTION_GUIDE.md). 다음 Day 15 정신 상태. |
| 함정 포획·전신 흡수 추가 요청 (2026-11-12) | 기존 도트 기반 구현·자동 기능 검증·그래픽 캡처 검수 완료 | `SKEnvironmentalCapture`·`SKTrapEnvelopeView`: 점액 연속 접지 1.25초/촉수 승인 15 피해 직후 포획, 마지막 저항 후 4초 무저항 흡수, 연타 탈출·R/자동 재시도·비활성/회복 복구. 벽은 기존 드레인을 유지하고 내던지기를 무저항 흡수로 교체. 기존 아틀라스·씬·GUID 관련 21개 보존, 생성 AI 인물 시안 미적용. 신규 기능 13개 포함 전체 136/136, Explicit 그래픽 1/1·최종 18장 이미지 검수. [상세](TRAP_CONSUMPTION.md) · [캡처 목록](../Logs/TrapConsumption/Captures/20261112_000151/index.md). 수동 조작감·새 빌드 미실시. |
| 전용 함정 도트 모션 후속 (2026-11-12) | 6시트·138프레임 연결·자동 검증·실제 씬 이미지 검수 완료 | 기존 인물 픽셀과 새 함정 소재를 합성. 사진1 촉수 나선 감김·사진2 벽 쪽 수평 끌림·승인된 점액 침수. 조합당 삼켜짐6/저항12/탈출3/완료2, 표시만 교체. 관련·그래픽22/22, 전체142/142(전체155·Explicit13·종료0), 실제138장 검수, 원본42개 보존. [상세](TRAP_DEDICATED_MOTION.md) · [캡처](../Logs/TrapDedicatedMotion/Captures/20261112_044531_553_0b3e2027/index.md). 수동 조작감·새 빌드 미실시. |
| Day 15 HP 회복·무저항 손실·게임 오버·HP 구슬 (2026-11-12) | 구현 및 자동 검증 완료, 컷씬 그림·수동 검수 없음 | 사용자 결정으로 별도 정신 상태 폐기(“붕괴” = HP 손실의 표현). 플레이어 HP는 마지막 피해 3초 뒤 초당 5 회복. 함정은 무저항 동안 초당 25 손실, HP 0 = 흡수(이전 “무저항 4초 즉시 흡수” 대체). `SKGameOverScreen` 원인별 이름·컷씬 자리(그림 대기). HUD 캐릭터별 HP 구슬 파랑·노랑·빨강(임시 도트). 신규 4개 포함 전체 146/146. [상세](DAY15.md) |
| Day 8 횡스크롤 카메라와 화면 경계 | 전용 플레이 씬에 구현 및 자동 검증 | `Day08_Scrolling`, `SKHorizontalCamera`. 로비 문 진입, 수평 추적, 양끝 화면 제한과 물리 벽, 점프 시 높이 유지, R 초기화 후 시작 뷰 복귀. 16:9 월드 뷰와 기존 2D HUD. |
| 로비(거점) v6 Unity 씬 (2026-11-08) | 구현 및 자동 검증 완료, 수동 조작 검수 미실시 | `SaintKnightLobby/`, 시작 씬. 칼=캐릭터 전환·배경 빛 전환, 제단=HP 회복(저장 미구현), 문=Day 9(2026-11-10, 이전 Day 8), 지도 탁자·독서대=안내만. 칼·지도 탁자·종은 플레이어 앞 레이어. `LobbyTests` 6개. 캐릭터는 2026-11-10부터 Day 6 도트(`SKCharacterSpriteView`) |
| 로비(거점) 도트 시안 v1~v6 (2026-11-08) | 콘셉트 이미지 | `Design/Lobby/LOBBY_CONCEPT.md`. 최신 v6(v5 + CC0 외부 에셋 GothicVania Church Pack 일부, 출처 `Design/Lobby/external/gothicvania_church/SOURCE.md`). v3: 다크 고딕 3/4 원근 실내 + 캐릭터 전환 칼(세실리아/아델라 두 상태). 캐릭터·UI 없음, 사용자 검토 대기 |
| 측면 HUD 2D 일러스트 (2026-11-10) | 세실리아 연결·자동 검증, 아델라 일러스트 대기 | 사용자 제작 0~4단계 전신 일러스트를 흰 배경 제거(`Design/Characters/make_portraits.py`) → `SaintKnightDay06/Art/Portraits`. `SKDay01HUD`가 HP 단계로 교체(`ceciliaStagePortraits`/`adelaStagePortraits`, 비면 기존 흉상). 이미지 영역 확대, 3D 안내 문구 2개 숨김. Day 5·로비. 검사 `SideHudShowsCeciliaIllustrationForTheDamageStage`, 캡처 `Logs/Day06Hud_stage*.png` |
| 3D GLB 임포트·HUD 연결 | 사용자 결정으로 보류(2D HUD 사용) | 모델 위치 확인(2026-11-10): `Downloads/SaintKnight_Day01_Unity6_Starter/SaintKnight_Day01_Starter/Existing_3D_Models/{Cecilia,Adela}_blockout_NOT_FINAL.glb`. trimesh로 만든 도형 조합 블록아웃(각 약 30조각·2~3천 정점), **뼈대·애니메이션·표정·재질·텍스처 없음**. Unity에 glTF 임포터 패키지 없음. 미리보기 `Design/Characters/glb_blockout_preview.png` |
| 최신 그래픽 배치 캡처 이미지 검수 | 전용 모션 실제 138장 검수 완료 | 양 캐릭터 × 세 함정 × 삼켜짐6/저항12/탈출3/완료2. [캡처](../Logs/TrapDedicatedMotion/Captures/20261112_044531_553_0b3e2027/index.md) · [상세](TRAP_DEDICATED_MOTION.md). 수동 조작감과 구분. |
| 이전 그래픽 배치 캡처 이미지 검수 | 당시 18장 검수 완료 | 양 캐릭터 × 점액/촉수/살덩이 벽 × 저항/진행/완료. 얼굴·자세, 바닥/벽 앵커, 완전 흡수 뒤 잔존·중복 없음을 확인. [캡처 목록](../Logs/TrapConsumption/Captures/20261112_000151/index.md). 수동 조작감 검수와 구분. |
| 수동 조작감 검수 | 이번 작업에서 미실시 | 자동 기능 검증·그래픽 배치 이미지 검수와 구분 |
| Windows 배포 빌드 (2026-11-12) | Day 15까지 빌드 성공, 배치 기동 15초. 창 모드 수동 검수 미실시 | [실행 파일](../Builds/Win64_Day16/SaintAndKnight.exe) · [빌드 로그](../Logs/Day16/Win64/build.log) |
| 이전 Windows 배포 빌드 (2026-11-11) | Day 11까지 빌드 성공, 배치 기동 15초. 창 모드 수동 검수 미실시 | [실행 파일](../Builds/Win64_Day11/SaintAndKnight.exe) · [빌드 로그](../Logs/Day11/Win64/build.log). 이전 `Builds/Win64_Day10/`은 Day 10 버전. |
| 이전 Windows 배포 빌드 (2026-11-10) | Day 8 빌드 성공. 창 모드 수동 검수 미실시 | [실행 파일](../Builds/Win64_Day08/SaintAndKnight.exe) · [빌드 로그](../Logs/Day08/Win64/build.log) |
| 이전 Windows 배포 빌드 (2026-11-10) | 다중 프레임 빌드 성공, 그래픽 없는 배치 기동 15초 확인. 수동 플레이는 미검수 | [실행 파일](../Builds/Win64_Day06Motion/SaintAndKnight.exe) · [빌드·기동 로그](../Logs/Day06MotionApply/Win64_20261110_120715/). 이전 실행 파일은 `Builds/Win64/`에 보존. |

같은 날 후속으로 점액·촉수 시트 4장의 후반 6칸씩을 교체했다(마지막 두 단계는 소재 틈으로만 몸이 보임, [상세](TRAP_DEDICATED_MOTION.md)). 교체 후 전체 Play Mode **142/142 통과** [XML](../Logs/TrapClip/Full/playmode-results.xml), 그래픽 캡처는 다시 찍지 않았다.

최신 검증(2026-11-12, Day 16 선행)은 전체 Play Mode **146/146 통과**(전체 159, Explicit 13, 실패 0, 종료 코드 0)이다. [결과 XML](../Logs/Day16/Full/playmode-results.xml). Explicit 13개도 그래픽 배치로 모두 통과했다([11개](../Logs/Day16/Explicit1/playmode-results.xml) + [수정 후 2개](../Logs/Day16/Explicit2/playmode-results.xml)): 함정 캡처 검사의 단정을 Day 15 규칙에 맞추고 `FightBots` 제한 시간을 늘렸다. 함정 캡처 138장을 [다시 찍었으나](../Logs/TrapDedicatedMotion/Captures/20261112_080308_006_b3da66f8/index.md) 전체 이미지 검수는 하지 않았다. 새 빌드 `Builds/Win64_Day16/`. 아델라 HUD 일러스트와 수동 조작감 검수는 남아 있다.

이전 검증(2026-11-12, Day 15)은 전체 Play Mode **146/146 통과**(전체 159, Explicit 13, 실패 0, 종료 코드 0)이다. [결과 XML](../Logs/Day15/Full/playmode-results.xml) · [상세](DAY15.md). Explicit 13개와 새 빌드·수동 조작감 검수는 하지 않았다. 다음 Day 16.

이전 검증(2026-11-12, 전용 함정 시트 연결)은 전체 Play Mode **142/142 통과**(전체 155, Explicit 13, 실패 0, 종료 코드 0)이다. 기존 136 + 신규 6개가 통과했고 관련·그래픽 22/22(종료 0) 및 실제 씬 138장 이미지 검수도 완료했다. [전체 XML](../Logs/TrapDedicatedMotion/Full/playmode-results.xml) · [관련·그래픽 XML](../Logs/TrapDedicatedMotion/TargetedVisual/playmode-results.xml) · [상세](TRAP_DEDICATED_MOTION.md). 원본 자산 42개·기존 GUID 보존을 확인했다. 다음 Day 15 정신 상태, 새 빌드·수동 조작감 검수는 남아 있다.

이전 전체 검증(2026-11-12, 함정 포획·전신 흡수 추가 요청 포함)은 Play Mode **136/136 통과**(전체 149, Explicit 13), 실패 0, 종료 코드 0이다. 기존 123개 + 살덩이 벽 6개 + 환경 포획 7개가 통과했다. [결과 XML](../Logs/TrapConsumption/Full/playmode-results.xml) · [로그](../Logs/TrapConsumption/Full/playmode-tests.log) · [종료 코드](../Logs/TrapConsumption/Full/exit-code.txt). 기능·그래픽 전용 실행 16/16과 최종 그래픽 1/1도 통과했고 최종 18장 이미지를 검수했다. 관련 소스 16개 해시는 `Logs/TrapConsumption/validation-inputs.json`, 원본 아트·씬·GUID 관련 21개 보존 증빙은 `asset-preservation.json`에 있다. 수동 조작감 검수와 새 Windows 빌드는 하지 않았다. 다음 로드맵은 Day 15 정신 상태다.

이전 통합 검증(2026-11-12, Day 13·14 초기 변경 포함)은 Play Mode **123/123 통과**(전체 135, Explicit 12), 실패 0, 종료 코드 0이다. 기존 113개 + Day 13 신규 8개 + Day 14 신규 2개가 모두 통과했으며 보완한 기존 보스 점프 검사도 통과했다. [결과 XML](../Logs/Day14/Full/playmode-results.xml). 이때는 새 캡처·수동 조작감·렌더링 검수·빌드를 하지 않았다.

이전 검증(2026-11-11 Day 12 캐릭터 능력·전환)은 Play Mode **113/113** 통과(전체 125, Explicit 12), 실패 0, 종료 코드 0이다. 기존 103개와 신규 통합 검사 10개가 모두 통과했다. [결과 XML](../Logs/Day12/Full/playmode-results.xml). 이후 2026-11-12 사용자 확인 재검증도 전체 113/113·Day 12 전용 10/10으로 통과했다(`Logs/ClaudeRuns/20261111_234543_day12_verify_full`, `20261111_235532_day12_verify_only`).

이전 검증(2026-11-11 Day 11 문 자동 입장)은 Play Mode **103/103** 통과(전체 115, Explicit 12), 실패 0, 종료 코드 0이다. 문 검사는 문 40px 앞에서는 그대로이고 문에 닿으면 넘어가는지 본다. [결과 XML](../Logs/ClaudeRuns/20261111_230942_day11_autodoor_full/playmode-results.xml) · 캡처 `Logs/Day09Capture_7~8.png`(`Logs/ClaudeRuns/20261111_231431_day11_autodoor_capture`).

이전 검증(2026-11-11 정식 함정 도트)은 Play Mode **103/103** 통과(전체 114, Explicit 11), 실패 0, 종료 코드 0이다. 함정 테스트 9개가 두 씬의 프레임 전환과 잡힘·탈출·초기화 시 그림 전환을 확인했다. [결과 XML](../Logs/Day10Art/Full/playmode-results.xml).

이전 검증(2026-11-11 살덩이 벽): 전체 Play Mode 2회 연속 **102/102**(전체 113, Explicit 11), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261111_004842_fleshwall_full2/`.

이전 검증(2026-11-10 불안정 검사 정리): 전체 Play Mode 2회 연속 **99/99**(전체 110, Explicit 11), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_234327_flakefix_6/`. 원인: 연타 반복이 탈출 뒤에도 누름, 실제 시간 기준 대기, Day 2 이동 비교 기준, 보스 껍질 닫힘 시점(게임 코드). CHANGELOG 참조.

이전 검증(2026-11-10 Day 10 함정): 전체 Play Mode 2회 각각 **98/99**(전체 110, Explicit 11), 실패는 매번 다른 Day 5 잡기 타이밍 검사 1개이고 단독 재실행 통과(Day 9/10 씬보다 먼저 실행되는 검사). `Logs/ClaudeRuns/20261110_225920_day10_full2/`. 새 `Day10TrapTests` 5개와 `Day09BossTests` 7개 모두 통과.

이전 최신 검증(2026-11-10 첫 보스 약화·축소): Play Mode **94/94 통과**(전체 105, Explicit 11), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_223954_boss_130/`. 보스 HP 360·껍질 ×0.3·키 130px. 난이도 수동 미검수.

이전 최신 검증(2026-11-10 첫 보스 구현): Play Mode **93/93 통과**(전체 104, Explicit 11), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_204708_day09_bossfight/`. `Day09BossTests` 7개. 보스 그림은 임시, 난이도·조작감 수동 미검수.

이전 검증(2026-11-10 Day 9 배경 교체·보스 예배당): Play Mode **86/86 통과**(전체 96, Explicit 10), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_200446_day09_boss_final/`. 씬 이동 시 HP·선택 캐릭터는 초기화된다(Day 20~21 저장 전까지).

이전 검증(2026-11-10 Day 9 배경 v2·잡힘 탈출/내던지기 그림): Play Mode **85/85 통과**(전체 95, Explicit 10), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_165316_day09_bgv2/`. 그래픽 캡처 `Logs/Day09Capture_0~6.png`.

이전 검증(2026-11-10 Day 9 몬스터 도트): Play Mode **84/84 통과**(전체 94, Explicit 10), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_162326_day09_enemyart/`. 그래픽 캡처 `Logs/Day09Capture_0~6.png`.

이전 검증(2026-11-10 Day 9 지형): Play Mode **81/81 통과**(전체 90, Explicit 9), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_160139_day09/`. 기존 77개 + `Day09StageTests` 4개(로비 문·공격 프로필 검사는 Day 9 기준으로 갱신). 이 실행 뒤 Explicit 캡처 `CaptureEachSection`을 추가해 그래픽 배치로 따로 통과(`Logs/Day09Capture_0~3.png`). Day 9 Windows 빌드는 하지 않았다.

이전 검증(2026-11-10 사슬 견제 수정 후): Play Mode **77/77 통과**(전체 86, Explicit 9), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_145735_chainfix/`. `Builds/Win64_Day08` 재빌드 성공 `Logs/Day07ChainFix/Win64_20261110_150029/`. Day 7 특성 표시(아머 파괴·견제 이펙트/소리)는 사용자 결정으로 Day 18에서 한다.

이전 검증(2026-11-10 Day 7·8 대조 후): Play Mode **76/76 통과**(전체 85, Explicit 9), 실패 0, 종료 코드 0. `Logs/ClaudeRuns/20261110_143306_day07_day08/`. Day 8 씬에 Day 7 값이 들어간 뒤 처음 끝까지 돈 검사이며, Day 5·Day 8·로비의 공격 프로필이 같은지 보는 `EveryPlayableSceneUsesTheDay5AttackProfiles`를 추가했다.

이전 검증(2026-11-10 Day 8): Play Mode **75/75 통과**, 실패 0, Explicit 9, 종료 코드 0. [결과 XML](../Logs/ClaudeRuns/20261110_140233_day07/playmode-results.xml). 기존 70개에 Day 7 전투 특성 3개와 Day 8 카메라 2개를 포함한다. 로비 문 진입도 기존 회귀 검사에서 통과했다.

이전 검증(2026-11-10 미결 결정 처리 후): Play Mode **70/70 통과**(+`CombatDecisionTests` 3), 실패 0, Explicit 9, 종료 코드 0. `Logs/ClaudeRuns/20261110_134818_decisions2/`. 반격·공격 중 점프 불가·웅크리기 — `REFERENCE_UPGRADE_20261108.md` 7절. 미뤄 둔 것(적 종류가 늘면 재검토): 발 묶기·Tab 탈출·타이밍 무작위.

이전 검증(2026-11-10 세실리아 달리기 시간 조정 후): Play Mode **67/67 통과**, 실패 0, Explicit 9 건너뜀(녹화 검사 추가), 종료 코드 0. `Logs/ClaudeRuns/20261110_132548_footslide/`. 세실리아 Run 12프레임 500→412ms(그림 그대로)로 한 걸음 약 5px 앞 미끄러짐 제거, 아델라는 오차 범위라 유지. 셀 13 발끝 정지는 그림 문제로 남음. `Builds/Win64_Day06Motion` 재빌드. 사본 `Logs/Day06FootSlide/Before/`.

이전 검증(2026-11-10 다중 프레임 적용 후): Play Mode **67/67 통과**, 실패 0, Explicit 8 건너뜀, 종료 코드 0. [XML](../Logs/Day06MotionApply/PlayMode_20261110_120423/playmode-results.xml) · [로그](../Logs/Day06MotionApply/PlayMode_20261110_120423/playmode-tests.log). 로비·Day 5에서 달리기·점프 프레임 진행, 적 A의 잡기 성공·버티기·연타 저항·탈출·초기화, 두 캐릭터 유효 타격 총 6장의 판정 앞끝 일치를 확인했다. 씬 및 기존 `.meta` 해시 대조 통과. 사본 `Logs/Day06MotionApply/Before/`.

이전 검증(2026-11-10 2D HUD 후): Play Mode **65/65 통과**(기존 56 + `Day06SpriteViewTests` 9), Explicit 8 건너뜀(측정 4 + 캡처 4), `Logs/ClaudeRuns/20261110_023638/`. 그 전 Day 6 마무리 64/64 `20261110_015952/`. 검수 시트 `Logs/Day06Review.png`. 그 전 판정=그림: 62/62 `20261110_012420/`. 그 전 Day 6 연결: **61/61 통과**(기존 56 + 5), Explicit 6개 건너뜀(측정 4 + 캡처 2), 종료 코드 0. `Logs/ClaudeRuns/20261110_011549/`(v4 느낌으로 아트 교체 후), 첫 연결 `20261110_002123/`. 그래픽 캡처 `Logs/ClaudeRuns/20261110_002507_capture/`, `Logs/Day06Capture_*.png`, `Logs/LobbyPlayCapture_*.png`. 변경 전 사본 `Logs/Day06Apply/Before/`.
이전 검증(2026-11-08 로비 추가 후): Unity 6000.0.81f1, Play Mode **56/56 통과**(기존 50 + `LobbyTests` 6), Explicit 5개 건너뜀(측정 4 + 로비 캡처 1), 종료 코드 0, 컴파일 경고 없음. `Logs/ClaudeRuns/20261108_023502/`. 이전 50/50 검증(로비 전)은 아래 기록 그대로.
이전 검증(2026-11-08): Unity 6000.0.81f1, Play Mode **50/50 통과**(기존 37 + 새 13), 측정용 `PlaytestProbe` 4개는 Explicit로 건너뜀, 종료 코드 0.
C# 컴파일 오류·경고 없음. `Logs/ClaudeRuns/20261108_005555/` 참조. 자동 플레이테스트 튜닝 내역은 `REFERENCE_UPGRADE_20261108.md` 6절. 이전 37/37 증빙은 `Logs/ReferenceApply/`.
배치 검증에서만 Burst 컴파일을 비활성화했다. Unity 라이선스·서비스 관련 로그는 남는다.
두 캐릭터의 HP 공유와 Q/E 개발용 체력 조절은 임시 정책이다.

로비에서 문으로 들어가면 `Assets/SaintKnightDay09/Scenes/Day09_Stage1.unity`가 열린다(2026-11-10부터, Day 8은 카메라 비교용으로 유지). Day 5는 전투 비교·회귀 검사 씬으로 유지한다.
Day 4의 단일 적 씬과 기존 Day 1/2 씬도 유지한다. 적 A/B는 같은 훈련 표적 이미지를 사용한다.
Day 6: 사용자 제공 캐릭터 시트에서 만든 기존 도트를 바탕으로 다중 프레임을 적용했다. 잡기 연속 동작은 실제 잡기·연타·탈출 결과에 따라 재생하며 기존 물리를 따른다.
분석 후 사용자가 즉시 가능한 적용을 승인하여 전투 기반과 Day 5 프로필을 개선했다. 
