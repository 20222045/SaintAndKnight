# 갤러리 그림 넣는 곳 (28일차, 3D 일러스트)

갤러리는 플레이로 열린다. 원인별로 캐릭터가 처음 쓰러지면 그 칸이 열리고, 보스를 처치하면 보스 칸이 열린다. 열린 칸은 아래 자리에 그림이 있으면 보여 주고, 없으면 “NO ILLUSTRATION YET”을 띄운다.

- 자리: `Assets/SaintKnightGallery/Resources/SaintKnightGallery/Private/<id>`
- `Private/` 폴더는 `.gitignore` 대상이라 Git에 올라가지 않는다(저장소는 15세 이용가 수준).
- 형식은 두 가지 중 하나를 쓴다.
  - **렌더한 이미지**(PNG, 어떤 크기든 화면에 맞춰 줄임). Sprite나 Default 텍스처 둘 다 된다.
  - **3D 프리팹**(같은 이름의 `.prefab`). 갤러리 카메라가 천천히 돌려 보여 준다. 현재 렌더러는 URP 2D라 3D 조명이 제대로 먹지 않을 수 있다. 그때는 Unlit 재질을 쓰거나, 렌더한 이미지로 넣는다.
- 원본 작업 파일(블렌더 등)은 `Docs/Design/Gallery/Source/`에 둔다(이 폴더도 Git 제외).

| id | 열리는 조건 |
| --- | --- |
| `boss_rose_madonna` | 장미창의 성모 처치 |
| `death_cecilia_<원인>` / `death_adela_<원인>` | 그 원인으로 처음 쓰러짐. 원인 = `monk`, `executioner`, `slime`, `tentacle`, `fleshwall`, `boss` |

보는 법: 타이틀의 RECORDS → GALLERY, 로비 독서대(W), 일시정지(Esc) → RECORDS. 그림을 연 상태에서 A/D로 다음 칸, Esc로 닫는다.
