using System.Collections.Generic;
using SaintKnight.Day20;
using SaintKnight.Day26;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Rest
{
    /// <summary>
    /// Plays the first sanctuary return and first spring visit with the same full-screen pixel CG player.
    /// The return advances on its own. Confirm advances either story and Back skips it.
    /// </summary>
    public sealed class SKEpilogue : MonoBehaviour
    {
        public const string Folder = "SaintKnightRest/Private/";
        private const string C = "세실리아", A = "아델라";
        private const float Fade = .6f, CharsPerSecond = 32f, StillSeconds = 3.2f;   // a wordless cut stays this long

        public struct Cut
        {
            public string picture;      // Resources name under AfterClear; null = black
            public string fallback;     // older private picture, "@backdrop", or null (black)
            public Vector2 rippleAt;    // screen position of the spring's live water rings; zero disables
            public bool bathers;        // lay the animated bathers over the picture
            public float steam;         // 0..1 steam drifting across
            public string[][] lines;    // (speaker or null for narration, text)
            public bool optional;       // 2026-11-22: dropped when its Private picture is missing (a clean checkout)
        }

        // 2026-11-22: NovelAI story cuts in the HUD art's style (Private; without them the pixel cuts as before):
        // the Madonna's last words -> walking out -> that night on the stairs -> the warm spring
        private static readonly Cut[] ReturnCuts =
        {
            new Cut { picture = "return_fall", optional = true, steam = 0f, lines = new[]
            {
                new string[] { null, "무너지는 장미창 아래에서, 성모가 마지막으로 입을 열었다." },
                new string[] { null, "“다음… 그릇은… 이미…”" },
            } },
            new Cut { picture = "return_threshold", steam = .35f, lines = new[]
            {
                new[] { C, "아델라 씨, 성녀상 밑에서 물소리가 나요." },
                new[] { A, "성모가 수로를 움켜쥐고 있었나 봐. 손을 놓으니 샘이 다시 흐르네." },
            } },
            new Cut { picture = "spring_hall", fallback = "@backdrop", steam = .75f, lines = new[]
            {
                new[] { A, "사냥 종도 멎었어. 옆 계단에 누가 등불을 걸어 놨다." },
                new[] { C, "어디로 갈지는 우리 둘이 정해요. 오늘 밤만큼은요." },
            } },
            new Cut { picture = "return_night", optional = true, steam = 0f, lines = new[]
            {
                new[] { A, "팔 내밀어. 사슬에 쓸린 자리가 다 벗겨졌잖아." },
                new[] { C, "…아델라 씨 손, 따뜻해요." },
                new[] { A, "성모가 마지막에 한 말은 잊어. 그릇이든 뭐든, 넌 내가 지켜." },
            } },
        };

        // 2026-11-19 user: the first-visit epilogue shows pictures only, no dialogue (lines removed; black narration cuts dropped).
        public static readonly Cut[] Cuts =
        {
            Still("spring_steps", .4f, new Vector2(.34f, .15f), "ep_feet"),
            Still("spring_arrival", .6f, default, "ep_towels"),
            Still("spring_company", .8f, default, "@backdrop", true),
            Still("spring_vow", .8f, default, "ep_back"),
            Still("spring_dawn", 0f, default, "ep_bed"),
        };


        // 2026-11-19 user: an entering-the-spring scene, then the salon's beastkin join one by one, then the bunny girl.
        // Same day: "no dialogue, just the pictures" — each cut holds, then fades on by itself (Enter = sooner, Esc = skip).
        // Private HD pictures only (Git-ignored); without them soaking stays a plain menu action.
        private static Cut Still(string picture, float steam, Vector2 ripple = default, string fallback = null, bool bathers = false) =>
            new Cut { picture = picture, fallback = fallback, bathers = bathers, steam = steam, rippleAt = ripple, lines = new[] { new string[] { null, "" } } };

        // Salon hosts join only once they are friends (SKTavern bonds); bunny girl last. Before that the two are alone.
        public static readonly (string host, string picture)[] Guests =
            { ("그레타", "spring_greta"), ("소렐", "spring_sorel"), ("릴리", "spring_lili"), ("루네", "spring_rune") };

        /// <summary>
        /// Which pictures this soak shows: undress, (first soak: towels + feet), the two in the water, then each host who
        /// became a friend since the last soak walks in one by one; once all four are friends, the six together.
        /// </summary>
        public static List<string> SoakPictures(bool firstTime, bool[] friends, int joinedMask, out int newMask)
        {
            var list = new List<string> { "spring_enter" };
            if (firstTime) { list.Add("spring_arrival"); list.Add("spring_steps"); }
            newMask = joinedMask;
            bool allFriends = true;
            for (int i = 0; i < Guests.Length; i++) allFriends &= friends[i];
            if (joinedMask != (1 << Guests.Length) - 1) list.Add("spring_company");
            for (int i = 0; i < Guests.Length; i++)
                if (friends[i] && (joinedMask & (1 << i)) == 0) { list.Add(Guests[i].picture); newMask |= 1 << i; }
            if (allFriends) list.Add("spring_all");
            return list;
        }

        /// <summary>Plays the soak scene over the hot spring menu; false when the private pictures are missing.</summary>
        public static bool PlaySoak(bool firstTime, System.Action finished)
        {
            if (Playing || SKHotSpring.LoadClearArt("spring_enter") == null) return false;
            var friends = new bool[Guests.Length];
            for (int i = 0; i < Guests.Length; i++) friends[i] = SKTavern.IsFriend(Guests[i].host);
            var pictures = SoakPictures(firstTime, friends, SKSave.Data.springGuestsJoined, out int mask);
            SKSave.Data.springGuestsJoined = mask;
            SKSave.Write();
            var cuts = new Cut[pictures.Count];
            for (int i = 0; i < cuts.Length; i++)
                cuts[i] = pictures[i] == "spring_steps" ? Still(pictures[i], .4f, new Vector2(.34f, .15f)) : Still(pictures[i], .75f);
            Begin(null, cuts, true, finished);
            return true;
        }

        private static SKEpilogue active;
        public static bool Playing => active != null;
        public static int FinishedFrame { get; private set; } = -1;
        public static bool ShouldPlay => !SKSave.Data.hotSpringEpilogueSeen;

        public int CutIndex { get; private set; }
        public int LineIndex { get; private set; }
        public bool IsTyping => typed < Current.Length;
        public string Shown => Current.Substring(0, Mathf.Min(Current.Length, Mathf.FloorToInt(typed)));
        private Canvas canvas;
        private SKSpringAmbience water;
        private RawImage picture, bathersLayer;
        private Image black, band;
        private Text speaker, line, narration;
        private readonly Image[] steam = new Image[6];
        private readonly Image[] rings = new Image[3];
        private Texture2D[] bathers = new Texture2D[0];
        private float cutStarted, typed;
        private float fadeOutUntil = -1f;
        private float readUntil = -1f;
        private float previousScale = 1f;
        private bool wasPaused;   // opened over a paused menu (soak): give the pause back, not play
        private bool automatic;
        private Cut[] sequence;
        private System.Action finished;

        private string Current => sequence[CutIndex].lines[LineIndex][1];

        public static bool TryPlayReturn(Transform host)
        {
            if (!SKHotSpring.Unlocked || !SKSave.Data.returnRevealPending) return false;
            var cuts = System.Array.FindAll(ReturnCuts, c => !c.optional || SKHotSpring.LoadClearArt(c.picture) != null);
            Begin(host, cuts, true, () =>
            {
                SKSave.Data.returnRevealPending = false;
                SKSave.Data.clearTalkSeen = SKSave.Data.lobbyTalkSeen = true;
                SKSave.Write();
            });
            return true;
        }

        public static SKEpilogue Play(Transform host, System.Action finished)
        {
            return Begin(host, Cuts, true, () =>
            {
                SKSave.Data.hotSpringEpilogueSeen = true;
                SKSave.Write();
                finished?.Invoke();
            });
        }

        private static SKEpilogue Begin(Transform host, Cut[] cuts, bool automatic, System.Action finished)
        {
            if (Playing) return active;
            var go = new GameObject(cuts == ReturnCuts ? "Sanctuary_First_Return" : automatic ? "Hot_Spring_Soak" : "Hot_Spring_Epilogue");
            go.SetActive(false);
            go.transform.SetParent(host, false);
            var e = go.AddComponent<SKEpilogue>();
            e.sequence = cuts;
            e.automatic = automatic;
            e.finished = finished;
            go.SetActive(true);
            return e;
        }

        private void Awake()
        {
            active = this;
            wasPaused = SKPause.IsPaused;
            SKPause.IsPaused = true;
            previousScale = Time.timeScale > 0f || wasPaused ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            water = gameObject.AddComponent<SKSpringAmbience>();
            Build();
            if (!automatic && SKHotSpring.LoadClearArt("spring_company") == null)
            {
                var frames = new List<Texture2D>();
                foreach (var t in Resources.LoadAll<Texture2D>(Folder))
                    if (t.name.StartsWith("hot_spring_bathers")) { t.filterMode = FilterMode.Point; frames.Add(t); }
                frames.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                bathers = frames.ToArray();
            }
            ShowCut(0);
        }

        private void Build()
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            var bgFill = Ui<Image>("Black_Back", Vector2.zero, Vector2.one);
            bgFill.color = Color.black;
            picture = Ui<RawImage>("Picture", Vector2.zero, Vector2.one);
            var fit = picture.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            bathersLayer = new GameObject("Bathers").AddComponent<RawImage>();
            bathersLayer.transform.SetParent(picture.transform, false);
            bathersLayer.raycastTarget = false;
            // same spot as the hot spring page: 224x112 at (96,38) on the 320x180 backdrop
            var br = bathersLayer.rectTransform;
            br.anchorMin = new Vector2(96f / 320f, 1f - 150f / 180f);
            br.anchorMax = new Vector2(320f / 320f, 1f - 38f / 180f);
            br.offsetMin = br.offsetMax = Vector2.zero;
            SKBustMorph.Attach(picture);
            SKBustMorph.Attach(bathersLayer);
            for (int i = 0; i < rings.Length; i++)
            {
                rings[i] = Ui<Image>("Ripple" + i, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
                rings[i].sprite = SKUi.Ring;
                rings[i].enabled = false;
            }
            for (int i = 0; i < steam.Length; i++)
            {
                steam[i] = Ui<Image>("Steam" + i, Vector2.zero, Vector2.zero);
                steam[i].sprite = SKUi.Glow;
            }
            band = Ui<Image>("Band", new Vector2(0f, 0f), new Vector2(1f, .25f));
            band.sprite = SKUi.Fade;
            band.color = new Color(0f, 0f, 0f, .8f);
            speaker = Label("Speaker", new Vector2(.08f, .17f), new Vector2(.6f, .23f), 22, TextAnchor.LowerLeft, new Color(.91f, .76f, .48f));
            line = Label("Line", new Vector2(.08f, .03f), new Vector2(.92f, .165f), 24, TextAnchor.UpperLeft, new Color(.93f, .91f, .88f));
            narration = Label("Narration", new Vector2(.1f, .42f), new Vector2(.9f, .58f), 26, TextAnchor.MiddleCenter, new Color(.93f, .91f, .88f));
            black = Ui<Image>("Fade", Vector2.zero, Vector2.one);
            black.color = Color.black;
        }

        private T Ui<T>(string n, Vector2 min, Vector2 max) where T : Graphic
        {
            var g = new GameObject(n).AddComponent<T>();
            g.transform.SetParent(transform, false);
            g.raycastTarget = false;
            var r = g.rectTransform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            return g;
        }

        private Text Label(string n, Vector2 min, Vector2 max, int size, TextAnchor a, Color c)
        {
            var t = Ui<Text>(n, min, max);
            t.font = SKUi.Hangul;
            t.fontSize = size;
            t.alignment = a;
            t.color = c;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            return t;
        }

        private void ShowCut(int index)
        {
            CutIndex = index;
            LineIndex = 0;
            typed = 0f;
            readUntil = -1f;
            cutStarted = Time.unscaledTime;
            var cut = sequence[index];
            Texture tex = cut.picture == null ? null : SKHotSpring.LoadClearArt(cut.picture);
            bool hasArt = tex != null;
            if (!hasArt && cut.fallback != null)
                tex = Load(cut.fallback) ?? (cut.picture == "spring_dawn" ? null : Load("@backdrop"));
            picture.texture = tex;
            picture.enabled = tex != null;
            foreach (var ring in rings)
                ring.rectTransform.anchorMin = ring.rectTransform.anchorMax = cut.rippleAt;
            rippling = hasArt && cut.rippleAt != Vector2.zero;
            bathersLayer.enabled = !hasArt && cut.bathers && bathers.Length > 0;
        }

        private bool rippling;

        private static Texture Load(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var tex = Resources.Load<Texture2D>(name == "@backdrop" ? SKHotSpring.BackdropPath : Folder + name);
            if (tex != null) tex.filterMode = tex.width > 640 ? FilterMode.Bilinear : FilterMode.Point;
            return tex;
        }

        /// <summary>Picture actually shown in the current cut (null = black).</summary>
        public Texture ShownPicture => picture != null && picture.enabled ? picture.texture : null;

        // Rings follow the expanding ripple in the spring steps painting.
        private void Ripple(float t)
        {
            for (int i = 0; i < rings.Length; i++)
            {
                float k = Mathf.Repeat(t / 4.2f + i / (float)rings.Length, 1f);
                rings[i].enabled = rippling;
                float width = 24f + k * 250f;
                rings[i].rectTransform.sizeDelta = new Vector2(width, width * .27f);
                rings[i].color = new Color(1f, .9f, .72f, (1f - k) * .22f);
            }
        }

        private void Update()
        {
            float t = Time.unscaledTime;
            Ripple(t);
            float since = t - cutStarted;
            var cut = sequence[CutIndex];
            if (SKKeys.Back) { Finish(); return; }
            // fade in at the start of a cut, out when leaving it
            float a = fadeOutUntil > 0f ? 1f - Mathf.Clamp01((fadeOutUntil - t) / Fade) : 1f - Mathf.Clamp01(since / Fade);
            black.color = new Color(0f, 0f, 0f, a);
            water.Level = cut.steam * (1f - a);
            if (fadeOutUntil > 0f && t >= fadeOutUntil)
            {
                fadeOutUntil = -1f;
                if (CutIndex + 1 >= sequence.Length) { Finish(); return; }
                ShowCut(CutIndex + 1);
                return;
            }
            // slow push-in, like a camera easing toward the subjects
            picture.rectTransform.localScale = Vector3.one * (1f + Mathf.Min(since, 12f) * .004f);
            if (bathersLayer.enabled && bathers.Length > 1)
            {
                int n = bathers.Length, step = Mathf.FloorToInt(t * 3f) % (2 * n - 2);
                bathersLayer.texture = bathers[step < n ? step : 2 * n - 2 - step];
            }
            else if (bathersLayer.enabled) bathersLayer.texture = bathers[0];
            DriftSteam(t, cut.steam);

            if (since > Fade * .5f) typed = Mathf.Min(Current.Length, typed + Time.unscaledDeltaTime * CharsPerSecond);
            string who = cut.lines[LineIndex][0];
            bool narrate = who == null;
            band.enabled = speaker.enabled = line.enabled = !narrate;
            narration.enabled = narrate;
            // over a picture the narration rises to the top so faces and rings stay clear; on black it stays centred
            bool high = picture.enabled;
            narration.rectTransform.anchorMin = new Vector2(.1f, high ? .78f : .42f);
            narration.rectTransform.anchorMax = new Vector2(.9f, high ? .94f : .58f);
            speaker.text = who ?? "";
            if (narrate) narration.text = Shown; else line.text = Shown;

            if (fadeOutUntil > 0f) return;
            if (SKKeys.Confirm) { Advance(); return; }
            if (automatic && !IsTyping)
            {
                if (readUntil < 0f) readUntil = t + (Current.Length == 0 ? StillSeconds : 2.25f);
                else if (t >= readUntil) Advance();
            }
        }

        /// <summary>Finish the typing, else the next line, else fade to the next cut.</summary>
        public void Advance()
        {
            if (fadeOutUntil > 0f) return;
            if (IsTyping) { typed = Current.Length; return; }
            if (LineIndex + 1 < sequence[CutIndex].lines.Length)
            {
                LineIndex++;
                typed = 0f;
                readUntil = -1f;
                SKBustMorph.BounceAll(.7f);
                return;
            }
            fadeOutUntil = Time.unscaledTime + Fade;
        }

        // wide soft puffs crossing the frame from left to right, the way steam drifts past a camera
        private void DriftSteam(float t, float amount)
        {
            for (int i = 0; i < steam.Length; i++)
            {
                float k = Mathf.Repeat(t * .05f + i / (float)steam.Length, 1f);
                var r = steam[i].rectTransform;
                float y = .25f + .5f * Mathf.Repeat(i * .37f, 1f) + Mathf.Sin(t * .4f + i) * .03f;
                r.anchorMin = r.anchorMax = new Vector2(-.2f + k * 1.4f, y);
                r.sizeDelta = new Vector2(520f, 300f) * (.8f + .4f * Mathf.Repeat(i * .61f, 1f));
                steam[i].color = new Color(.95f, .93f, .9f, amount * .13f * Mathf.Sin(k * Mathf.PI));
            }
        }

        public void Finish()
        {
            if (active != this) return;
            FinishedFrame = Time.frameCount;
            Resume();
            canvas.enabled = false;
            Destroy(gameObject);
            finished?.Invoke();
        }

        private void Resume()
        {
            active = null;
            SKPause.IsPaused = wasPaused;
            Time.timeScale = previousScale;
        }

        private void OnDestroy()
        {
            if (active == this) Resume();
        }
    }
}
