# Day 9 몬스터 도트 v1

무너진 대성당의 묘지와 회랑에 배치할 오염된 수도사(A), 처형인(B)의 도트 키포즈. 첨부에서 낮은 채도, 거친 명암, 비대칭 실루엣을 참고했다. 두 캐릭터는 기본 이미지 생성 도구로 새로 만들고, 원래 픽셀을 최근접 샘플링해 게임 크기에 맞췄다.

| 파일 | 왼쪽에서 오른쪽 |
| --- | --- |
| `enemy_a_monk.png` | 대기 → 잡기 예고 → 잡기 |
| `enemy_b_executioner.png` | 대기 → 도끼 들기 → 상단 가로 휘두르기 |
| `monster_preview.png` | 3배 확대 비교. 왼쪽 첫 열은 크기 비교용 세실리아 |

- 각 PNG는 **576×112 RGBA**, 셀은 **192×112**, 셀당 1포즈. 캐릭터는 오른쪽을 본다.
- **48 PPU / Point** 기준. 발 기준선은 위에서 109px(아래에서 3px), 가로 기준은 셀 왼쪽에서 96px. Unity에서 발 중심 피벗을 쓰면 정규화 좌표는 `(0.5, 0.026786)`.
- 대기 몸체 높이는 수도사 약 54px, 처형인 약 62px. 3포즈는 비주얼 검토용이다. 이동·피격·회복, 수도사의 일반 할퀴기, 인게임 타격 판정 정렬은 후속 범위다.

<details>
<summary>이미지 생성 프롬프트</summary>

### 오염된 수도사

Use case stylized-concept. An original clean game-art animation key-pose sheet for one enemy, a corrupted monk in a grotesque medieval cathedral horror side-scroller, native low-resolution hand-placed pixel-art look. Three sprites only, one horizontal row of exactly three evenly spaced square cells without visible dividers. Face right in side-on 3/4 projection in all three poses. Consistent scale and common flat foot line. First sprite idles with his silhouette curled inward, two sickly long wax-gray hands folded close to the chest, knuckles out. Second sprite is the telegraph, both long arms unfold away from his body with visibly separate bony hands and the hood lifts. Third sprite is a forward lunging two-handed grab with those hands extended strongly to the right at chest height, rear foot planted. One distinct design: a bent adult human silhouette in frayed soot-brown hooded cassock, a crooked drooping cowl, black hollow face with a small pale nose bridge but no luminous eyes, a single small burnt-ochre wax seal on the chest, stretched desaturated bone-gray knuckles, resin-black thin seams on the sleeves, uneven ragged cloth at the calves. Harsh readable silhouette, slightly asymmetric proportions, cursed and uncanny, largely earth tones and narrow silver-gray rim lights. Dark, low-chroma, late-1990s detailed horror pixel sprites. Genuine hard square stepped edges and coherent small pixel clusters, no gradient glow and no anti-aliased illustration. Render as if the body itself is roughly 54 logical pixels tall, then nearest-neighbor enlarged. Leave clear transparent gutters between every pose. All figures full body and separate. Real transparent background. No other character, no text, no panels, no background, no floor, no shadows under feet, no decorative effects. Fictional fully clothed monster, no nudity, no blood.

### 처형인

Use case stylized-concept. An original clean game-art animation key-pose sheet for one enemy, a cathedral executioner in a grotesque medieval horror side-scroller, native low-resolution hand-placed pixel-art look. Three sprites only, one horizontal row of exactly three evenly spaced square cells without visible dividers. Face right in side-on 3/4 projection in all poses. Consistent scale and common flat foot line. First pose idles with a wide planted stance and a single heavy broad-headed medieval execution axe lowered and dragged behind his left rear foot, visibly held by one hand; second pose is a legible attack telegraph with both hands taking the same haft and lifting the axe head high behind the shoulder; third pose is a high sweeping attack with the same axe thrust far to the right at head or upper chest height, far above the ankles, so a crouching character would fit below it. No slash arcs. One distinct design: huge sloping shoulders, squashed head under a heavy dark pointed sack hood, tiny crooked slit with one dull gray highlight, squared wide heavy apron made of charcoal-brown cracked hide, one armored forearm with a crude dull bronze cuff, the other in a torn sleeve, weathered pitted cleaver-shaped axe head with worn cold steel edge. Slightly asymmetrical posture, stooped top-heavy weight, ominous silhouette and distressed clusters. Late-1990s detailed horror pixel sprite, muted dark warm-gray and charcoal palette, subdued highlights, genuine hard square stepped edges and coherent tiny pixel clusters, no gradient glow and no anti-aliased illustration. Render as if the body itself is roughly 60 logical pixels tall, then nearest-neighbor enlarged. Clear transparent gutters between every pose. All figures full body and separate. Real transparent background. No other character, no text, no panels, no scenery, no floor, no shadows under feet, no decorative effects. Fictional fully clothed monster, no nudity, no blood.

</details>
