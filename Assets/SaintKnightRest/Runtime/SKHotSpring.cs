using SaintKnight.Day02;
using SaintKnight.Day20;
using SaintKnight.Day26;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Rest
{
    /// <summary>
    /// Sanctuary hot spring (2026-11-17, after the user's reference: rest scenes between runs).
    /// A dialogue page over a pixel backdrop: soak = full HP + save,
    /// talk = the two knights' conversation, picked from how the run has gone. Kept at a 15+ level.
    /// </summary>
    public static class SKHotSpring
    {
        public const string BackdropPath = "SaintKnightRest/hot_spring_bg";
        private const string ClearArtPath = "SaintKnightRest/AfterClear/";

        /// <summary>
        /// 2026-11-19: an HD NovelAI version in the Git-ignored Private folder wins over the public pixel CG of the same name.
        /// HD pictures are smoothed; pixel ones stay on the pixel grid.
        /// </summary>
        internal static Texture2D LoadClearArt(string name)
        {
            var art = Resources.Load<Texture2D>(SKEpilogue.Folder + name) ?? Resources.Load<Texture2D>(ClearArtPath + name);
            if (art != null) art.filterMode = art.width > 640 ? FilterMode.Bilinear : FilterMode.Point;
            return art;
        }

        /// <summary>
        /// Story: the spring under the sanctuary went cold while the Rose Window Madonna was corrupted.
        /// Once she falls (stage 1 cleared) the water warms again and the steam shows in the lobby.
        /// </summary>
        public static bool Unlocked => SKSave.Data.clears > 0;

        private const string C = "세실리아", A = "아델라";

        // (speaker, line). Original writing for this project.
        public static readonly string[][] Intro =
        {
            new[] { A, "성소 아래에 이런 샘이 있을 줄은 몰랐네." },
            new[] { C, "옛 수녀님들이 순례자를 씻기던 곳이래요. 물이 아직 따뜻해요." },
            new[] { A, "그럼 잠깐만 쉬자. 갑옷 끈이 어깨를 파고들어서 죽겠어." },
            new[] { C, "…저도 사슬을 내려놓을게요. 오늘은 기도보다 이게 먼저예요." },
        };
        public static readonly string[][] Falls =
        {
            new[] { A, "회랑에서 몇 번이나 쓰러졌는지 세다가 그만뒀어." },
            new[] { C, "그래도 매번 다시 일어났잖아요. 성소가 우리를 돌려보내 주니까." },
            new[] { A, "언젠가 돌려보내 주지 않는 날이 오면?" },
            new[] { C, "…그 전에 끝내요. 둘이서." },
        };
        public static readonly string[][] Epilogue =
        {
            new[] { C, "장미창의 성모가… 정말 쓰러졌어요." },
            new[] { A, "살아서 돌아왔으면 그걸로 됐어. 오늘 밤은 아무 생각도 하지 말자." },
            new[] { C, "내일은 성소 종을 같이 울려요. 이긴 날엔 그렇게 하는 거래요." },
            new[] { A, "좋아. …물 식기 전에 어깨 좀 주물러 줄래?" },
            new[] { C, "후후. 오늘만이에요." },
        };
        public static readonly string[][] Chat =
        {
            new[] { A, "너는 왜 사슬을 골랐어? 성녀라면 지팡이가 어울릴 텐데." },
            new[] { C, "지팡이로는 붙잡을 수가 없잖아요. 사슬은 놓치지 않아요." },
            new[] { A, "…무섭게 말하네." },
            new[] { C, "아델라 씨를 놓치지 않겠다는 뜻이었어요." },
        };

        /// <summary>Which conversation fits the save right now.</summary>
        public static string[][] Pick()
        {
            var d = SKSave.Data;
            if (d.bossDefeated) return Epilogue;
            if (d.hotSpringVisits == 0) return Intro;
            if (d.deaths >= 3) return Falls;
            return d.hotSpringVisits % 2 == 0 ? Intro : Chat;
        }

        // a talk with each speaker's own bath picture on her lines
        private static string[][] WithPictures(string[][] talk)
        {
            var lines = new string[talk.Length][];
            for (int i = 0; i < talk.Length; i++)
                lines[i] = new[] { talk[i][0], talk[i][1], talk[i][0] == C ? "spring_cecilia" : "spring_adela" };
            return lines;
        }

        /// <summary>
        /// 2026-11-22 (user): the hot spring is walked into like the shops. The place picture once, then the bath
        /// in first person: walk up to Cecilia or Adela to talk (both in the water stay on screen when it ends),
        /// to the water to soak (full HP, the bath film), or look at the candles and the window.
        /// </summary>
        public static SKShopDef Place(SKHealth player) => new SKShopDef
        {
            Title = "성소 온천",
            Subtitle = "옛 수녀들이 순례자를 씻기던 샘",
            Establishing = "spring_entrance",
            Main = new SKShopRoom
            {
                Picture = LoadClearArt("spring_company") != null ? "spring_company" : "hot_spring_bg",
                Spots = new[]
                {
                    new SKShopSpot { Label = "촛불을 본다", At = new Vector2(.07f, .55f), Talk = n => new[]
                    {
                        new string[] { null, "탕 가장자리에 촛불이 수십 개. 녹은 촛농이 물가까지 흘러내렸다." },
                        new[] { C, "옛 수녀님들이 한 사람씩 켜 두고 간 거래요. 지금은 저희 차례고요." },
                    } },
                    new SKShopSpot { Label = "세실리아 곁으로", At = new Vector2(.27f, .4f), Host = C, Picture = "spring_company", Talk = n => WithPictures(Pick()) },
                    new SKShopSpot
                    {
                        Label = "탕에 몸을 담근다", At = new Vector2(.48f, .88f), Use = () =>
                        {
                            if (player != null) player.ResetHealth();
                            SKEpilogue.PlaySoak(SKSave.Data.hotSpringVisits == 0, null);
                            SKSave.Data.hotSpringVisits++;
                            SKSave.Write();
                            return "따뜻한 물이 상처를 감싼다. 몸이 가벼워졌다.";
                        },
                    },
                    new SKShopSpot { Label = "아델라 곁으로", At = new Vector2(.68f, .4f), Host = A, Picture = "spring_company",
                        Talk = n => WithPictures(SKSave.Data.deaths >= 3 ? Falls : Chat) },
                    new SKShopSpot { Label = "창 너머 성소", At = new Vector2(.93f, .3f), Talk = n => new[]
                    {
                        new string[] { null, "장미 문양 창 너머로 성소의 종탑이 보인다. 종은 조용하다." },
                        new[] { A, "저 종이 다시 울리면 그땐 이긴 날이야." },
                    } },
                },
            },
        };

        /// <summary>Rising steam over the lobby spot so the entrance reads without text.</summary>
        public static void MarkEntrance(Transform parent, float x)
        {
            var go = new GameObject("HotSpring_Steam");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, -2.7f, 0f);
            go.AddComponent<SKSteam>();
        }
    }

    /// <summary>
    /// Life on the hot spring backdrop (unscaled clock, the game is paused): the two knights in the water
    /// (Resources/SaintKnightRest/Private/hot_spring_bathers*.png, git-ignored; frames play back and forth),
    /// a slow breathing bob, drifting steam and a shimmer on the water. Without the picture only steam and water move.
    /// </summary>
    public sealed class SKHotSpringScene : MonoBehaviour
    {
        public const string BathersPrefix = "SaintKnightRest/Private/";
        // where the bathers stand on the 320x180 backdrop (pixels, origin top-left): water line along the pool
        private static readonly Rect BathersRect = new Rect(96f, 38f, 224f, 112f);
        private RawImage bathers;
        private Texture2D[] frames = new Texture2D[0];
        private readonly UnityEngine.UI.Image[] steam = new UnityEngine.UI.Image[7];
        private UnityEngine.UI.Image shimmer;
        private RawImage owner;
        private SKSpringAmbience water;

        public int FrameCount => frames.Length;
        public RawImage Bathers => bathers;

        public static void Attach(RawImage backdrop)
        {
            if (backdrop != null && backdrop.GetComponent<SKHotSpringScene>() == null)
                backdrop.gameObject.AddComponent<SKHotSpringScene>();
        }

        private void Awake()
        {
            owner = GetComponent<RawImage>();
            water = gameObject.AddComponent<SKSpringAmbience>();
            var list = new System.Collections.Generic.List<Texture2D>();
            if (SKHotSpring.LoadClearArt("spring_company") == null)
                foreach (var t in Resources.LoadAll<Texture2D>(BathersPrefix))
                    if (t.name.StartsWith("hot_spring_bathers")) { t.filterMode = FilterMode.Point; list.Add(t); }
            list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            frames = list.ToArray();
            bathers = Child<RawImage>("Bathers", BathersRect);
            bathers.texture = frames.Length > 0 ? frames[0] : null;
            bathers.enabled = frames.Length > 0;
            SKBustMorph.Attach(bathers);
            shimmer = Child<UnityEngine.UI.Image>("Water_Shimmer", new Rect(80f, 128f, 240f, 22f));
            shimmer.sprite = SaintKnight.Day26.SKUi.Fade;
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i] = Child<UnityEngine.UI.Image>("Steam" + i, new Rect(0f, 0f, 40f, 40f));
                steam[i].sprite = SaintKnight.Day26.SKUi.Glow;
            }
        }


        private T Child<T>(string n, Rect px) where T : Graphic
        {
            var g = new GameObject(n).AddComponent<T>();
            g.transform.SetParent(transform, false);
            g.raycastTarget = false;
            Place(g.rectTransform, px);
            return g;
        }

        // backdrop pixels (320x180, top-left origin) -> anchors on the backdrop rect
        private static void Place(RectTransform r, Rect px)
        {
            r.anchorMin = new Vector2(px.xMin / 320f, 1f - px.yMax / 180f);
            r.anchorMax = new Vector2(px.xMax / 320f, 1f - px.yMin / 180f);
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private void Update()
        {
            string picture = owner != null && owner.texture != null ? owner.texture.name : "";
            bool here = picture == "hot_spring_bg" || picture.StartsWith("spring_");
            water.Level = here ? 1f : 0f;
            float t = Time.unscaledTime;
            if (bathers != null)
            {
                bathers.enabled = picture == "hot_spring_bg" && frames.Length > 0;
                if (frames.Length > 1)
                {
                    int n = frames.Length, step = Mathf.FloorToInt(t * 3f) % (2 * n - 2);
                    bathers.texture = frames[step < n ? step : 2 * n - 2 - step];
                }
                float bob = Mathf.Round(Mathf.Sin(t * 1.3f) * 1f) / 180f;   // one backdrop pixel of breathing
                bathers.rectTransform.anchorMin = new Vector2(BathersRect.xMin / 320f, 1f - BathersRect.yMax / 180f + bob);
                bathers.rectTransform.anchorMax = new Vector2(BathersRect.xMax / 320f, 1f - BathersRect.yMin / 180f + bob);
            }
            shimmer.enabled = here;
            shimmer.color = new Color(1f, .86f, .62f, .10f + .06f * Mathf.Sin(t * 2.1f));
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i].enabled = here;
                float k = Mathf.Repeat(t * .12f + i / (float)steam.Length, 1f);
                float x = 110f + i * 30f + Mathf.Sin((k * 2f + i) * Mathf.PI) * 10f;
                float size = 30f + k * 50f;
                Place(steam[i].rectTransform, new Rect(x - size * .5f, 140f - k * 120f - size * .5f, size, size));
                steam[i].color = new Color(.95f, .93f, .9f, Mathf.Sin(k * Mathf.PI) * .16f);
            }
        }
    }

    internal sealed class SKSpringAmbience : MonoBehaviour
    {
        private AudioSource source;
        public float Level { get; set; }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.clip = Resources.Load<AudioClip>("SaintKnightRest/AfterClear/spring_water");
            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;
        }

        private void Update()
        {
            if (source.clip == null) return;
            float target = Level * Mathf.Clamp01(SKSave.Data.se) * .6f;
            source.volume = Mathf.MoveTowards(source.volume, target, Time.unscaledDeltaTime * .75f);
            if (source.volume > 0f && !source.isPlaying) source.Play();
            if (source.volume == 0f && source.isPlaying) source.Stop();
        }
    }

    /// <summary>Six soft round puffs drifting up and fading, on the unscaled clock.</summary>
    public sealed class SKSteam : MonoBehaviour
    {
        private SpriteRenderer[] puffs;

        private void Awake()
        {
            var sprite = SaintKnight.Day26.SKUi.Glow;   // soft round puff, 0.64 units across at scale 1
            puffs = new SpriteRenderer[6];
            for (int i = 0; i < puffs.Length; i++)
            {
                puffs[i] = new GameObject("Puff" + i).AddComponent<SpriteRenderer>();
                puffs[i].transform.SetParent(transform, false);
                puffs[i].sprite = sprite;
                puffs[i].sortingOrder = 5;
            }
        }

        private void Update()
        {
            for (int i = 0; i < puffs.Length; i++)
            {
                float t = Mathf.Repeat(Time.unscaledTime * .35f + i / (float)puffs.Length, 1f);
                float sway = Mathf.Sin((t * 3f + i) * Mathf.PI) * .12f;
                puffs[i].transform.localPosition = new Vector3(sway + (i % 3 - 1) * .2f, t * 2.2f, 0f);
                float size = (.35f + t * .9f) / .64f;   // 0.35 -> 1.25 units
                puffs[i].transform.localScale = new Vector3(size, size, 1f);
                puffs[i].color = new Color(.92f, .9f, .86f, Mathf.Sin(t * Mathf.PI) * .42f);
            }
        }
    }
}
