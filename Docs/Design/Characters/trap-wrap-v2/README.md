# 전신 함정 시안 — 게임 미적용

2026-11-12 생성한 시안 보관본이다. 이후 사용자가 **새 그림 대신 기존 게임 도트에 덮어 적용**하도록 정정했다.
이 폴더의 생성 인물 그림과 `packed-v2` 아틀라스는 Assets에 복사하지 않았다. 후속 에이전트가 자동 적용하지 않는다.

- `full_body_wrap_concept.png`: built-in image_gen으로 만든 점액/촉수 전신 감김 방향 시안. 프롬프트 `concept-prompt.txt`.
- `tentacle_struggle_sheet.png`: 같은 도구의 촉수 버둥거림 시안. 프롬프트 `tentacle-prompt.txt`.
- `Pack-WrapAtlas.ps1` / `WrapAtlasPacking.cs`: 시안의 기술적 리샘플링·아틀라스 패킹 실험. 출력은 Assets 밖의 staging 폴더로만 가능하다.

실제 채택한 구현은 `SKTrapEnvelopeView`다. 기존 캐릭터 아틀라스의 픽셀과 포즈를 읽고,
같은 48 PPU의 픽셀 레이어로 점액·촉수·살덩이의 감김과 차오름을 표시한다.
최신 동작과 검사 결과는 `Docs/TRAP_CONSUMPTION.md`를 따른다.
