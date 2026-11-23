# SpriteCook 시안 점검 및 점액 합성 수정

**최신 후속:** 요청7장 제작·규격 통일·59Sprite 분할/임포트를 완료했다. Play Mode149/149 통과. 게임 연결은 남아 있다. [최신 완료 기록](SPRITECOOK_DRAFTS_COMPLETED.md)을 따른다. 아래는 이전 단계의 이력이다.

작업일: 2026-11-13 (Asia/Seoul).

**같은 날 후속:** SpriteCook 전용 도구가 연결돼 Pro·1K 2장을 생성했다(총32, 잔액0). 세실리아 v2는 사용자 디자인 불일치 지적으로 기각하고 원본 직접 참조 편집 v3를 별도 정규화했다. 최신 상태·남은 작업은 [디자인 교정 기록](SPRITECOOK_CECILIA_IDENTITY_FIX.md)을 따른다. 아래 내용은 그 이전 점액/초기 연결 점검 기록이다.

## 확인한 내용

- `Assets/SaintKnightSpriteCook/Drafts/`의 PNG 10장과 JSON 2개가 존재한다. 재생성 대상 7장은 `Docs/Design/Stage/spritecook/`의 원본과 동일한 파일이었다.
- PNG 10장은 정상 해독되며 투명도가 0/255인 도트 이미지다. 기존 PNG `.meta`는 48 PPU·Point·무압축이고 일반 Texture(`textureType=0`, `spriteMode=0`)다. 아직 개별 Sprite나 게임 표시로 연결된 상태가 아니다.
- 생성 도구의 요청 크기는 최종 PNG 크기를 보장하지 않는다. 예를 들어 세실리아 보스 시트는 390×234, 적 사망 시트는 세실리아 421×272·아델라 636×358이다. 현재 파일을 128×96 등분으로 그대로 연결할 수 없다.
- 제공받은 모델·품질·프롬프트·과거 크레딧 사용량은 PNG만으로 입증할 수 없다. 로컬에 기존 업로드/생성 에셋 ID 기록이 없어 원격 생성 이력은 미확인이다.

## 실제 수정

`Design/Boss/pattern-dots/make_boss_spit_from_material.py`의 기존 소재 합성을 수정했다. 새 인물이나 소재를 생성하지 않았다.

1. 위쪽 한 줄을 반복 복사하던 처리를 제거하고, 같은 소재의 올리브·장미색으로 폭이 변하는 가는 줄기를 연결했다.
2. 2~4단계 소재 아랫단을 기존 픽셀로 늘여 울퉁불퉁한 방울 끝으로 만들었다.
3. 첫 방울의 실제 끝을 머리에 닿게 정렬했다.
4. 후반에는 본체 덩어리의 안쪽에서만 인물이 보이도록 마스킹했다. 떨어진 소재 조각 때문에 망토·방패·손이 밖으로 나타나는 경우도 제거했다.

세실리아·아델라 각각 128×96칸 23프레임, 8열×3행 PNG와 기존 JSON 계약을 유지했다. 저항·완료 프레임은 바닥에 고정된 미세한 기울기/높이 변화로 구별한다. 캐릭터의 원본 픽셀은 다시 그리거나 확대하지 않았다.

산출물은 `Design/Boss/pattern-dots/staging-spritecook-v2/`이며, 수정된 두 합성 PNG만 기존 Drafts 경로에 복사했다. JSON 내용과 기존 PNG GUID는 그대로다. v1 원본 디렉터리, 7장 재생성 대상, 기존 Resources 아트·C#·씬은 변경하지 않았다. 미리보기 GIF 2개는 v2 폴더에만 있다.

재생성:

```powershell
python Docs/Design/Boss/pattern-dots/make_boss_spit_from_material.py --sync-drafts
python Docs/Design/Boss/pattern-dots/check_spritecook_slime.py
```

## 검사

- 합성 관련 6/6 통과: 23칸 배치·유일성·투명도, 첫 방울 머리 접촉, 줄기 연속성/폭/변화, 잘리지 않은 방울 끝, 후반 장비 가림, 노출된 원본 인물 픽셀 보존.
- 두 번 생성한 PNG/JSON 4개와 Drafts 사본의 SHA-256이 일치한다. 기존 23칸 JSON도 동일하다.
- 양 캐릭터 전체 46프레임 비교 그림과 확대 컨택트 시트를 검수했다. 실제 게임 화면 검수는 하지 않았다.
- Unity 배치에서 Drafts PNG 10장·JSON 2개 임포트를 확인했다. 전체 Play Mode **146/146 통과**(전체 159, Explicit 13, 실패 0, 종료 코드 0). [결과 XML](../Logs/SpriteCookReview/20261113/PlayMode/playmode-results.xml). C# 컴파일 오류는 없었다.

증빙: `Logs/SpriteCookReview/20261113/`의 `draft-audit.json`, `compositor-checks.log`, `reproducibility.json`, `before-after.png`, `PlayMode/`. 변경 전 파일 50개는 `Before/`, 해시는 `before-hashes.json`에 있다.

## 재생성 미완료 및 다음 작업

SpriteCook OAuth 로그인 성공과 인증된 서버의 도구 목록 조회는 확인했다. 그러나 현재 채팅에 실행 도구가 로드되지 않았고, 정식 app-server를 통한 호출도 현재 채팅이 사용 중이라는 보호 오류로 거부됐다. 브라우저 대체 경로는 창 바인딩 오류가 재조회 후에도 반복돼 사용하지 못했다. 인증 정보 접근·별도 에이전트 실행·보호 우회는 하지 않았다. 현재 잔액·현재 모델 가격 조회, 업로드, 새 이미지 생성은 수행하지 못했다.

`Design/Stage/spritecook/regeneration-plan.json`에 7장 분량의 영어 프롬프트와 설정을 준비했다. **이 파일은 제출 이력이 아니다.** 사용자 우선순위대로 적 사망 아델라·세실리아 → 보스 양 캐릭터 → HP 장식 → 함정 반응 양 캐릭터 순이다. GPT Image 2의 high 품질을 계획했지만 실제 모델/가격/도구 스키마는 호출 가능한 연결에서 확인해야 한다. 참조 업로드는 기존 에셋 ID를 먼저 복구하고, 없을 때만 선택된 원본을 업로드한다.

후속은 7장 재생성 → 인물 키 55/62px·128×96칸 정규화 → Sprite 분할 및 게임 상태 연결이다. 현재 수정은 Drafts 점액 합성까지이며, 게임 연결 완료로 처리하지 않는다.
