using SaintKnight.Day20;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SaintKnight.Day26
{
    /// <summary>
    /// Title screen, the first scene in the build. 2026-11-16 rebuild (user: "make it look sharp"):
    /// the boss chapel (the game's own art) slowly drifting in the dark, its rose window bleeding a crimson
    /// glow, embers rising, film grain; Cecilia's illustration on the left; a gold "SAINT ✠ KNIGHT" logo
    /// over the rose window; then "press any key" and a framed menu under the logo.
    /// 2026-11-19 (story A, Docs/Design/STORY.md): prologue on black, a chain-snap flash, then a 640x360 pixel
    /// altar scene at exactly 2x (the weeping Madonna under the rose window, the broken chain, both heroines);
    /// logo above the window, menu in a column right of the Madonna, broken-link marks.
    /// Without the scene's art references it still works on the dark grain alone.
    /// </summary>
    public sealed class SKTitleMenu : SKMenuView
    {
        public const string LobbyScene = "Lobby_Sanctuary";
        public const string Logo = "Saint and Knight", Subtitle = "성 녀 와   성 기 사";
        /// <summary>Story prologue (Docs/Design/STORY.md), one line at a time under the logo before "press any key".</summary>
        public static readonly string[] Prologue =
        {
            "장미창의 성모는 기적을 내렸다.",
            "그 값은, 언제나 한 명의 성녀였다.",
            "그리고 그날 밤, 성녀는 제단에서 도망쳤다.",
        };
        // Intro (2026-11-19, story A "the night of the ritual"): prologue on black → the chain snaps (flash)
        // → the pixel altar scene, logo, heroine → press any key. A key during the intro jumps to its end.
        private const float PrologueAt = .6f, PrologueStep = 1.1f, Reveal = 4.8f;
        private const float LogoIn = Reveal + .6f, HeroIn = Reveal + .9f, PromptAt = Reveal + 2f, IntroEnd = PromptAt + .6f;
        private Text[] prologue;
        private Image flash;
        private Image[] tears;

        /// <summary>Pixel scene: 640x360, shown at exactly 2x (Docs/Design/Title/make_title_scene.py).</summary>
        public const string ScenePath = "SaintKnightDay26/title_scene";
        // native pixel positions in that scene (keep in sync with the script)
        private static readonly Vector2Int RoseAt = new Vector2Int(415, 143);
        private static readonly Vector2Int[] TearAt = { new Vector2Int(413, 236), new Vector2Int(418, 236) };
        /// <summary>A native scene pixel (top-left origin) → this 1280x720 canvas (centre origin), pixel centre.</summary>
        private static Vector2 Native(float x, float y) => new Vector2(x * 2f + 1f - 640f, 360f - y * 2f - 1f);
        // 2026-11-22 (user's key-art reference): title large in the top-left, menu in the bottom-left, the heroines in the middle
        private static readonly Vector2 MenuAt = new Vector2(-440f, -170f);
        /// <summary>A full-screen NovelAI key art (Git-ignored); when present it replaces the pixel altar scene and the heroines.</summary>
        public const string KeyArtPath = "SaintKnightRest/Private/title_keyart";
        private const string HeroPath = "SaintKnightRest/Private/HUD/";
        public bool HasKeyArt { get; private set; }

        [Tooltip("Backdrop (the boss chapel).")]
        [SerializeField] private Sprite backdrop;
        [Tooltip("Heroine illustration on the left (784x1168 portrait canvas).")]
        [SerializeField] private Sprite heroine;

        private Text prompt;
        private CanvasGroup logoGroup, menuGroup, heroGroup;
        private Image black, backdropImage, roseGlow;
        private RawImage grain;
        private RectTransform logoRect, heroRect, cursorBar;
        private Text[] rows;
        private Image[] marks;
        private SKMenuPage main;
        private float shownAt = -1f;
        private Ember[] embers;
        public bool WaitingForKey { get; private set; } = true;

        private struct Ember { public RectTransform R; public Image I; public float X, Speed, Drift, Phase, Life; }


        private void Start()
        {
            startedAt = Time.unscaledTime;
            ShadeAlpha = .92f;   // records / gallery pages cover the title
            SKPause.IsPaused = false;
            Time.timeScale = 1f;
            BuildBackdrop();
            main = new SKMenuPage { Title = "", Body = () => "", Shade = 0f };   // drawn by this screen, not the text list
            bool played = SKSave.HasRun || SKSave.Data.deaths > 0 || SKSave.Data.clears > 0;
            main.Items.Add(new SKMenuItem(() => SKSave.HasRun ? "Continue  -  " + Pretty(SKSave.Data.scene) : "Continue", Continue)
                { Enabled = () => played });
            main.Items.Add(new SKMenuItem(() => "New Game", NewGame));
            main.Items.Add(new SKMenuItem(() => "Records", () => Push(RecordsPage())));
            main.Items.Add(new SKMenuItem(() => "Settings", () => Panel.Open("Settings", ("Back", (System.Action)Panel.Close))));
            main.Items.Add(new SKMenuItem(() => "Quit", Quit));
            BuildMenuRows();
        }

        /// <summary>Skips "press any key" (tests, returning players).</summary>
        public void ShowMenu()
        {
            if (!WaitingForKey) return;
            WaitingForKey = false;
            if (Age < IntroEnd) startedAt = Time.unscaledTime - IntroEnd;   // skip the rest of the intro
            shownAt = Age;
            prompt.gameObject.SetActive(false);
            Push(main);
            Closed += () => Push(main);   // Esc on the main list keeps the title up
            Render();
            DrawMenu();
        }

        private float startedAt;
        /// <summary>Seconds since this title screen opened (the intro runs again when coming back from a game).</summary>
        public float Age => Time.unscaledTime - startedAt;

        private void Update()
        {
            float t = Age;
            Animate(t);
            if (WaitingForKey)
            {
                prompt.color = new Color(SKUi.Ink.r, SKUi.Ink.g, SKUi.Ink.b, Mathf.Clamp01((t - PromptAt) / .6f) * (.45f + .4f * Mathf.Sin(t * 2.6f)));
                if (t > .6f && AnyKey()) ShowMenu();
                return;
            }
            if (!Panel.IsOpen) Drive();
            DrawMenu();
        }

        private static bool AnyKey()
        {
#if ENABLE_INPUT_SYSTEM
            var k = UnityEngine.InputSystem.Keyboard.current;
            return k != null && k.anyKey.wasPressedThisFrame;
#else
            return Input.anyKeyDown;
#endif
        }

        // ---------- motion ----------

        private void Animate(float t)
        {
            // black holds through the prologue, then lifts as the chain snaps
            black.color = new Color(0f, 0f, 0f, 1f - Mathf.SmoothStep(0f, 1f, (t - Reveal) / 1.2f));
            float snap = t < Reveal ? 0f : Mathf.Exp(-(t - Reveal) * 6f);
            flash.color = new Color(1f, .82f, .78f, .6f * snap);
            for (int i = 0; i < prologue.Length; i++)
            {
                float a = Mathf.SmoothStep(0f, 1f, (t - PrologueAt - i * PrologueStep) / .9f) * (1f - Mathf.SmoothStep(0f, 1f, (t - Reveal + .5f) / .5f));
                prologue[i].color = new Color(.82f, .76f, .70f, a);
            }

            roseGlow.color = new Color(.75f, .05f, .08f, .2f + .08f * Mathf.Sin(t * 1.3f) + .04f * Mathf.Sin(t * 3.7f));
            // blood tears: one scene pixel sliding down from each eye, a native pixel at a time
            for (int i = 0; i < tears.Length; i++)
            {
                float p = Mathf.Repeat(t * .45f + i * .43f, 1f);
                int drop = Mathf.FloorToInt(p * 16f);
                tears[i].rectTransform.anchoredPosition = Native(TearAt[i].x, TearAt[i].y + drop);
                tears[i].color = new Color(.62f, .05f, .07f, p < .85f ? 1f : (1f - p) / .15f);
            }

            float logoIn = Mathf.SmoothStep(0f, 1f, (t - LogoIn) / 1.4f);
            logoGroup.alpha = logoIn;
            float ls = Mathf.Lerp(1.08f, 1f, logoIn);
            logoRect.localScale = new Vector3(ls, ls, 1f);
            if (heroGroup != null)
            {
                heroGroup.alpha = Mathf.SmoothStep(0f, 1f, (t - HeroIn) / 1.8f);
                heroRect.anchoredPosition = new Vector2(heroRect.anchoredPosition.x, 3f * Mathf.Sin(t * .8f));
            }
            menuGroup.alpha = WaitingForKey ? 0f : Mathf.Clamp01((t - shownAt) / .35f);
            grain.uvRect = new Rect(Random.value, Random.value, 1.2f, 1.2f);

            for (int i = 0; i < embers.Length; i++)
            {
                var e = embers[i];
                e.Life += Time.unscaledDeltaTime * e.Speed;
                if (e.Life > 1f) { Respawn(ref e); }
                e.X += Mathf.Sin(t * 1.3f + e.Phase) * e.Drift * Time.unscaledDeltaTime;
                // snapped to the 2x scene pixel grid so embers stay pixels, not smears
                e.R.anchoredPosition = new Vector2(Mathf.Round(e.X / 2f) * 2f, Mathf.Round(Mathf.Lerp(-380f, 420f, e.Life) / 2f) * 2f);
                float a = Mathf.Sin(e.Life * Mathf.PI) * (.55f + .45f * Mathf.Sin(t * 6f + e.Phase));
                e.I.color = new Color(e.I.color.r, e.I.color.g, e.I.color.b, a);
                embers[i] = e;
            }
        }

        private static void Respawn(ref Ember e)
        {
            e.Life = 0f;
            e.Speed = Random.Range(.05f, .12f);
            e.Drift = Random.Range(10f, 40f);
            e.Phase = Random.Range(0f, 6.3f);
            e.X = Random.Range(-640f, 640f);
            e.R.anchoredPosition = new Vector2(e.X, -380f);
            float s = Random.value < .8f ? 2f : 4f;
            e.R.sizeDelta = new Vector2(s, s);
            e.I.color = Random.value < .6f ? new Color(1f, .45f, .15f, 0f) : new Color(.85f, .12f, .1f, 0f);
        }

        // ---------- build ----------

        private Transform root;

        private void BuildBackdrop()
        {
            var canvas = new GameObject("Title_Backdrop").AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            root = canvas.transform;
            Vector2 mid = new Vector2(.5f, .5f);

            FullScreen("Grain_Base", SKUi.Grain());

            // the pixel altar scene at exactly 2x, still (no zoom: pixels stay square); older chapel art as fallback
            var keyArt = Resources.Load<Texture2D>(KeyArtPath);
            HasKeyArt = keyArt != null;
            var scene = HasKeyArt ? null : Resources.Load<Sprite>(ScenePath);
            if (HasKeyArt)
            {
                keyArt.filterMode = FilterMode.Bilinear;
                var art = new GameObject("Key_Art", typeof(RectTransform)).AddComponent<RawImage>();
                art.transform.SetParent(root, false);
                art.texture = keyArt;
                art.raycastTarget = false;
                Fill(art.rectTransform);
                var fit = art.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = keyArt.width / (float)keyArt.height;
            }
            else if (scene != null || backdrop != null)
            {
                backdropImage = SKUi.Picture("Altar_Scene", root, scene != null ? scene : backdrop, mid, Vector2.zero, new Vector2(1280f, 720f));
                backdropImage.color = scene != null ? new Color(.8f, .76f, .82f) : new Color(.5f, .45f, .52f);
            }
            roseGlow = SKUi.Picture("Rose_Glow", root, SKUi.Glow, mid, Native(RoseAt.x, RoseAt.y), new Vector2(480f, 480f));
            tears = new Image[TearAt.Length];
            for (int i = 0; i < tears.Length; i++)
                tears[i] = SKUi.Picture("Blood_Tear_" + i, root, SKUi.White, mid, Native(TearAt[i].x, TearAt[i].y), new Vector2(2f, 2f));
            if (HasKeyArt) { roseGlow.enabled = false; foreach (var tear in tears) tear.enabled = false; }   // tied to the pixel scene
            else BuildHeroes(mid);

            FullScreen("Vignette", SKUi.Vignette());

            embers = new Ember[46];
            for (int i = 0; i < embers.Length; i++)
            {
                var img = SKUi.Picture("Ember_" + i, root, SKUi.White, mid, Vector2.zero, new Vector2(3f, 3f));
                var e = new Ember { R = img.rectTransform, I = img };
                Respawn(ref e);
                e.Life = Random.value;
                embers[i] = e;
            }

            if (heroine != null && !HasKeyArt && heroGroup == null)
            {
                // face near the top of the screen, knees and below sink off the bottom
                const float h = 980f, w = h * 784f / 1168f;
                heroRect = SKUi.Rect("Heroine", root, new Vector2(0f, 1f), new Vector2(-w * .1f, 0f), new Vector2(w, h), new Vector2(0f, 1f));
                heroGroup = heroRect.gameObject.AddComponent<CanvasGroup>();
                heroGroup.alpha = 0f;
                var art = heroRect.gameObject.AddComponent<Image>();
                art.sprite = heroine;
                art.preserveAspect = true;
                art.raycastTarget = false;
                art.color = new Color(.74f, .7f, .78f);
                // her lower body sinks into the dark
                var foot = SKUi.Picture("Heroine_Fade", root, BottomFade(), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(520f, 300f));
                foot.color = Color.black;
            }

            BuildLogo(mid);

            // the menu column bottom-left, darkened behind
            var shade = SKUi.Picture("Menu_Shade", root, SKUi.Glow, mid, MenuAt, new Vector2(620f, 460f));
            shade.color = new Color(0f, 0f, 0f, .8f);
            prompt = SKUi.Label("Press_Any_Key", root, SKUi.Spaced("Press any key"), 18, mid, MenuAt,
                new Vector2(600f, 40f), TextAnchor.MiddleCenter);

            // frame lines and footer
            foreach (var y in new[] { 1f, 0f })
            {
                var line = SKUi.Picture("Frame_Line", root, SKUi.Fade, new Vector2(.5f, y), new Vector2(0f, y > 0f ? -26f : 44f), new Vector2(1180f, 1f));
                line.color = new Color(SKUi.Gold.r, SKUi.Gold.g, SKUi.Gold.b, .35f);
            }
            SKUi.Label("Copyright", root, "© 2026   S A I N T   A N D   K N I G H T", 13, new Vector2(.5f, 0f), new Vector2(0f, 26f),
                new Vector2(600f, 24f), TextAnchor.MiddleCenter).color = SKUi.Dim;
            SKUi.Label("Version", root, "ver " + SKUi.Version, 13, new Vector2(1f, 0f), new Vector2(-50f, 26f), new Vector2(120f, 24f),
                TextAnchor.MiddleRight).color = SKUi.Dim;

            grain = FullScreen("Grain_Overlay", SKUi.GrainOverlay());
            grain.color = new Color(1f, 1f, 1f, .12f);

            flash = SKUi.Picture("Chain_Snap_Flash", root, SKUi.White, mid, Vector2.zero, Vector2.zero);
            Fill(flash.rectTransform);
            flash.color = Color.clear;
            black = SKUi.Picture("Fade_In", root, SKUi.White, mid, Vector2.zero, Vector2.zero);
            Fill(black.rectTransform);
            black.color = Color.black;

            // prologue over the black
            prologue = new Text[Prologue.Length];
            for (int i = 0; i < prologue.Length; i++)
            {
                prologue[i] = SKUi.Label("Prologue_" + i, root, Prologue[i], 22, mid, new Vector2(0f, 40f - i * 40f),
                    new Vector2(900f, 40f), TextAnchor.MiddleCenter, SKUi.Hangul);
                prologue[i].color = Color.clear;
            }
        }

        // the two NovelAI heroines in the middle of the altar scene (Git-ignored art; without it the old left illustration)
        private void BuildHeroes(Vector2 mid)
        {
            var cecilia = Resources.Load<Texture2D>(HeroPath + "cecilia_stage0");
            var adela = Resources.Load<Texture2D>(HeroPath + "adela_stage0");
            if (cecilia == null) return;
            heroRect = SKUi.Rect("Heroes", root, mid, Vector2.zero, new Vector2(1280f, 720f));
            heroGroup = heroRect.gameObject.AddComponent<CanvasGroup>();
            heroGroup.alpha = 0f;
            if (adela != null) Hero("Hero_Adela", adela, new Vector2(330f, -40f), 820f, new Color(.62f, .56f, .62f));
            Hero("Hero_Cecilia", cecilia, new Vector2(60f, -10f), 900f, new Color(.86f, .82f, .88f));
            var foot = SKUi.Picture("Heroes_Fade", root, BottomFade(), new Vector2(.5f, 0f), Vector2.zero, new Vector2(1280f, 300f));
            foot.rectTransform.pivot = new Vector2(.5f, 0f);
            foot.color = Color.black;
        }

        private void Hero(string name, Texture2D tex, Vector2 at, float height, Color tint)
        {
            tex.filterMode = FilterMode.Bilinear;
            var img = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            img.transform.SetParent(heroRect, false);
            img.texture = tex;
            img.color = tint;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.anchoredPosition = at;
            r.sizeDelta = new Vector2(height * tex.width / tex.height, height);
        }

        private void BuildLogo(Vector2 mid)
        {
            // top-left, like the reference key art: a large title, a thin rule and a small line under it
            var topLeft = new Vector2(0f, 1f);
            logoRect = SKUi.Rect("Logo", root, topLeft, new Vector2(64f, -44f), new Vector2(700f, 220f), topLeft);
            logoGroup = logoRect.gameObject.AddComponent<CanvasGroup>();
            logoGroup.alpha = 0f;

            var halo = SKUi.Picture("Logo_Halo", logoRect, SKUi.Glow, topLeft, new Vector2(260f, -80f), new Vector2(760f, 260f));
            halo.color = new Color(0f, 0f, 0f, .65f);   // keeps the letters off the art

            var title = SKUi.Label("Logo_Title", logoRect, "성녀와 기사", 96, topLeft, Vector2.zero, new Vector2(700f, 120f),
                TextAnchor.UpperLeft, SKUi.Hangul);
            title.rectTransform.pivot = topLeft;
            title.fontStyle = FontStyle.Bold;
            Gild(title);
            var link = SKUi.Picture("Logo_Broken_Link", logoRect, SKUi.BrokenLink, topLeft, new Vector2(560f, -28f), new Vector2(22f, 34f));
            link.color = new Color(.95f, .2f, .18f);
            var linkGlow = SKUi.Picture("Logo_Link_Glow", logoRect, SKUi.Glow, topLeft, new Vector2(560f, -28f), new Vector2(80f, 80f));
            linkGlow.color = new Color(.8f, .05f, .05f, .5f);
            linkGlow.transform.SetSiblingIndex(link.transform.GetSiblingIndex());

            var rule = SKUi.Picture("Logo_Rule", logoRect, SKUi.Fade, topLeft, new Vector2(250f, -132f), new Vector2(500f, 1.5f));
            rule.color = SKUi.Gold;
            var sub = SKUi.Label("Logo_Subtitle", logoRect, SKUi.Spaced("Saint and Knight", " ") + "   ·   " + Subtitle, 17, topLeft,
                new Vector2(4f, -146f), new Vector2(700f, 30f), TextAnchor.UpperLeft, SKUi.Hangul);
            sub.rectTransform.pivot = topLeft;
            sub.color = new Color(.78f, .72f, .66f);
        }

        private static void Gild(Text t)
        {
            t.color = Color.white;
            var g = t.gameObject.AddComponent<SKGradient>();
            g.Top = new Color(1f, .97f, .9f);
            g.Bottom = new Color(.66f, .5f, .32f);
            var outline = t.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.06f, .02f, .02f, .95f);
            outline.effectDistance = new Vector2(2f, -2f);
            var shadow = t.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(.45f, .02f, .04f, .75f);
            shadow.effectDistance = new Vector2(0f, -6f);
        }

        private void BuildMenuRows()
        {
            var mid = new Vector2(.5f, .5f);
            var box = SKUi.Rect("Title_Menu", root, mid, MenuAt, new Vector2(300f, 260f));
            box.SetSiblingIndex(prompt.transform.GetSiblingIndex());
            menuGroup = box.gameObject.AddComponent<CanvasGroup>();
            menuGroup.alpha = 0f;
            var bar = SKUi.Picture("Menu_Cursor", box, SKUi.Fade, mid, Vector2.zero, new Vector2(300f, 38f));
            bar.color = new Color(SKUi.Blood.r, SKUi.Blood.g, SKUi.Blood.b, .6f);
            cursorBar = bar.rectTransform;
            rows = new Text[main.Items.Count];
            marks = new Image[main.Items.Count * 2];
            for (int i = 0; i < rows.Length; i++)
            {
                float y = 92f - i * 46f;
                rows[i] = SKUi.Label("Menu_" + i, box, "", 24, mid, new Vector2(0f, y), new Vector2(400f, 40f), TextAnchor.MiddleCenter);
                for (int s = 0; s < 2; s++)
                {
                    var m = SKUi.Picture("Menu_Mark_" + i + "_" + s, box, SKUi.BrokenLink, mid, new Vector2(0f, y), new Vector2(11f, 17f));
                    marks[i * 2 + s] = m;
                }
            }
        }

        private void DrawMenu()
        {
            if (rows == null) return;
            bool own = Top == main;
            if (ListText != null) ListText.enabled = !own;
            menuGroup.gameObject.SetActive(own || Top == null);
            if (!own) return;
            for (int i = 0; i < rows.Length; i++)
            {
                var it = main.Items[i];
                bool on = i == main.Cursor, enabled = it.Enabled();
                string label = it.Label();
                rows[i].text = label.Length <= 10 ? SKUi.Spaced(label) : label.ToUpperInvariant();
                rows[i].fontSize = on ? 24 : 21;
                rows[i].color = !enabled ? new Color(.32f, .3f, .32f) : on ? SKUi.Focus : SKUi.Ink;
                float half = rows[i].preferredWidth / 2f + 22f;
                var y = rows[i].rectTransform.anchoredPosition.y;
                for (int s = 0; s < 2; s++)
                {
                    var m = marks[i * 2 + s];
                    m.enabled = on;
                    m.color = SKUi.Blood * 1.7f;
                    m.rectTransform.anchoredPosition = new Vector2(s == 0 ? -half : half, y);
                }
                if (on)
                    cursorBar.anchoredPosition = Vector2.Lerp(cursorBar.anchoredPosition, new Vector2(0f, y), Mathf.Clamp01(Time.unscaledDeltaTime * 18f));
            }
        }

        private static void Fill(RectTransform r)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }

        private RawImage FullScreen(string name, Texture texture)
        {
            var image = new GameObject(name, typeof(RectTransform)).AddComponent<RawImage>();
            image.transform.SetParent(root, false);
            Fill(image.rectTransform);
            image.texture = texture;
            image.raycastTarget = false;
            return image;
        }

        private static Sprite bottomFade;
        private static Sprite BottomFade()
        {
            if (bottomFade != null) return bottomFade;
            var t = new Texture2D(4, 32, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 4; x++) t.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - y / 31f)));
            t.Apply();
            return bottomFade = Sprite.Create(t, new Rect(0, 0, 4, 32), new Vector2(.5f, .5f), 100f);
        }

        private static string Pretty(string scene) => scene == "Day09_BossChapel" ? "Boss Chapel" : scene == "Day09_Stage1" ? "Ruined Cathedral" : scene;

        public static void Continue()
        {
            if (SKSave.HasRun)
            {
                SKCarry.Set(SKSave.Data.character, -1f, SKSave.Data.checkpoint);
                SceneManager.LoadScene(SKSave.Data.scene);
            }
            else
            {
                SKCarry.Set(SKSave.Data.character, -1f);
                SceneManager.LoadScene(LobbyScene);
            }
        }

        public static void NewGame()
        {
            SKSave.StartNewRun();
            SKCarry.Set(0, -1f);
            SceneManager.LoadScene(LobbyScene);
        }

        public static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
