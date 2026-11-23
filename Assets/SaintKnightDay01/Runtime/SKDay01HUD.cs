using UnityEngine;
using UnityEngine.UI;
using SaintKnight.Day02;

namespace SaintKnight.Day01
{
    /// <summary>
    /// Side status HUD. The portrait shows the user's 2D full-body illustration for the current
    /// damage stage when provided (2026-11-10: 2D HUD instead of a 3D model), else the approved bust crop.
    /// </summary>
    public sealed class SKDay01HUD : MonoBehaviour
    {
        [SerializeField] private Image portrait;
        [SerializeField] private Image hpFill;
        [SerializeField] private Text characterName;
        [SerializeField] private Text healthText;
        [SerializeField] private Text stageText;
        [SerializeField] private Sprite ceciliaPortrait;
        [SerializeField] private Sprite adelaPortrait;
        [Tooltip("Full-body illustration per appearance damage stage 0~4 (index = stage). Empty → base portrait.")]
        [SerializeField] private Sprite[] ceciliaStagePortraits = new Sprite[0];
        [SerializeField] private Sprite[] adelaStagePortraits = new Sprite[0];

        private float hp = 100f;
        private int selected = 0;
        private SKHealth health;
        public float DisplayedHp => hp;

        public void BindHealth(SKHealth source)
        {
            if (health != null) health.Changed -= SyncHealth;
            health = source;
            if (health != null) health.Changed += SyncHealth;
            SyncHealth();
        }

        private void SyncHealth()
        {
            if (health != null) hp = health.CurrentHp;
            Refresh();
        }

        private void OnDestroy()
        {
            if (health != null) health.Changed -= SyncHealth;
        }

        public void SetCharacter(int characterIndex)
        {
            selected = Mathf.Clamp(characterIndex, 0, 1);
            if (health == null) hp = 100f; // Keep the standalone Day 01 scaffold behavior.
            Refresh();
        }

        public void AdjustHp(float amount)
        {
            if (health != null) { health.AdjustForTraining(amount); return; }
            hp = Mathf.Clamp(hp + amount, 0f, 100f);
            Refresh();
        }

        private void Start() => SyncHealth();

        /// <summary>
        /// 2026-11-16 user decision: no name, HP number/bar, damage stage or control keys on screen.
        /// The illustration and the HP orb carry the state. Values keep updating (hidden, not removed).
        /// </summary>
        public void HideStatusText()
        {
            foreach (var g in new Graphic[] { characterName, healthText, stageText, hpFill })
                if (g != null) g.enabled = false;
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;
            foreach (var g in canvas.GetComponentsInChildren<Graphic>(true))
                if (System.Array.IndexOf(HiddenPanels, g.name) >= 0) g.enabled = false;
        }

        // Panels, bands and texts of the old side-panel layout (2026-11-16: only the illustration and the orb stay).
        private static readonly string[] HiddenPanels =
        {
            "HP_BACK", "CONTROLS", "BOTTOM_CONTROLS", "TOP_STATUS_BACKDROP", "HUD_Left_Panel", "HUD_TITLE",
            "HUD_Top_Line", "Enemy_Training_Status", "PROJECT", "NOTE", "DAMAGE"
        };

        /// <summary>Illustration height as a share of the screen; its top sits at <see cref="PortraitTop"/>.</summary>
        public const float PortraitHeight = 1.15f, PortraitTop = .72f;
        /// <summary>The illustration canvas has ~20% empty space on its left: shift it out of the screen by this share of its width.</summary>
        public const float PortraitLeftShift = .14f;
        /// <summary>Screen share (from the left, at 16:9) the follow camera keeps the player out of.</summary>
        public const float ClearOfPortrait = .32f;
        private bool overlay;

        /// <summary>
        /// 2026-11-16 user layout: play field fills the screen; the full-body illustration stands over its
        /// lower-left corner, cut off by the screen bottom; the HP orb sits alone in the top-left corner.
        /// </summary>
        public void ApplyOverlayLayout()
        {
            HideStatusText();
            overlay = true;
            if (portrait == null) return;
            var r = portrait.rectTransform;
            var root = GetComponentInParent<Canvas>();
            if (root != null)
            {
                r.SetParent(root.transform, false);   // out of the old side panel: anchors are the whole screen now
                r.SetAsFirstSibling();
            }
            r.anchorMin = new Vector2(0f, PortraitTop - PortraitHeight);
            r.anchorMax = new Vector2(0f, PortraitTop);
            r.pivot = Vector2.zero;
            r.anchoredPosition = Vector2.zero;
            r.sizeDelta = Vector2.zero;
            portrait.preserveAspect = true;
            var fit = portrait.GetComponent<AspectRatioFitter>() ?? portrait.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
            fit.aspectRatio = 784f / 1168f;   // the illustrations' canvas (ART_REQUEST_DAY15)
            AddShadowLayer(root != null ? root.transform : r.parent);
            // 2026-11-20 user: the illustration stays still (SKPortraitSway bounce no longer attached).
            PlaceOrb();
            PlacePortrait();
        }

        /// <summary>
        /// Stages 0~1 (standing) are cut off by the screen bottom; from stage 2 the kneeling/lying pictures fill
        /// their canvas, so the whole picture is shown with its bottom on the screen edge (2026-11-20 user).
        /// </summary>
        public bool PortraitShowsWhole => DamageStage >= 2;

        private void PlacePortrait()
        {
            if (!overlay || portrait == null) return;
            var r = portrait.rectTransform;
            float height = PortraitShowsWhole ? PortraitTop : PortraitHeight;
            r.anchorMin = new Vector2(0f, PortraitTop - height);
            r.anchorMax = new Vector2(0f, PortraitTop);
            var fit = portrait.GetComponent<AspectRatioFitter>();
            var root = GetComponentInParent<Canvas>();
            var scaler = root != null ? root.GetComponent<CanvasScaler>() : null;
            float refHeight = scaler != null ? scaler.referenceResolution.y : 720f;
            r.anchoredPosition = new Vector2(-PortraitLeftShift * height * refHeight * (fit != null ? fit.aspectRatio : 784f / 1168f), 0f);
        }

        /// <summary>
        /// 2026-11-16, after the reference play screen: the field darkens toward its edges and the left side
        /// falls into shadow behind the illustration, so she stands out of the dark rather than over the stage.
        /// </summary>
        private void AddShadowLayer(Transform root)
        {
            if (root.Find("HUD_Shadow") != null) return;
            var go = new GameObject("HUD_Shadow", typeof(RectTransform), typeof(RawImage));
            var rect = (RectTransform)go.transform;
            rect.SetParent(root, false);
            rect.SetAsFirstSibling();   // under the illustration and the orb
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var image = go.GetComponent<RawImage>();
            image.raycastTarget = false;
            image.texture = ShadowTexture();
        }

        private static Texture2D shadowTexture;
        private static Texture2D ShadowTexture()
        {
            if (shadowTexture != null) return shadowTexture;
            const int w = 160, h = 90;
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float u = (x + .5f) / w, v = (y + .5f) / h;
                    float dx = u * 2f - 1f, dy = v * 2f - 1f;
                    float edge = Mathf.Clamp01(dx * dx * .55f + dy * dy * .75f - .3f) * .75f;
                    float left = Mathf.Clamp01(1f - u / .36f);
                    left = left * left * .7f;
                    t.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01(edge + left)));
                }
            t.Apply();
            return shadowTexture = t;
        }

        private void PlaceOrb()
        {
            if (!overlay || orbFrame == null) return;
            var canvas = GetComponentInParent<Canvas>();
            var r = orbFrame.rectTransform;
            r.SetParent(canvas != null ? canvas.transform : portrait.transform.parent, false);
            r.anchorMin = r.anchorMax = r.pivot = new Vector2(0f, 1f);
            r.anchoredPosition = new Vector2(8f, -4f);
            r.localScale = Vector3.one * 1.5f;
            r.SetAsLastSibling();
        }

        // Day 17: while something holds her, the side HUD can show that hold (art: <char>_hud_<cause>_held).
        private string heldKey = "";
        private SKGrabState grab;
        public string HeldKey => heldKey;

        private void LateUpdate()
        {
            // 2026-11-20 user: the illustration no longer fades while she walks behind it.
            if (health == null) return;
            if (grab == null) grab = health.GetComponent<SKGrabState>();
            string key = grab != null && grab.IsHeld && !health.IsDead
                ? SaintKnight.Day15.SKGameOverScreen.KeyFor(grab.Holder) : "";
            if (key == heldKey) return;
            heldKey = key;
            Refresh();
        }

        public int DamageStage => health != null ? health.DamageStage : StageFor(hp);

        /// <summary>Compatibility for the standalone Day 01 HUD; SKHealth owns the shared rule.</summary>
        public static int StageFor(float percent) => SKHealth.DamageStageFor(percent, 100f);

        // Day 15 health orb: one per character (own emblem), blue = fine, yellow = hurt, red = one step from down.
        private static readonly Color[] OrbColors =
        {
            new Color(.35f, .75f, 1f), new Color(1f, .85f, .25f), new Color(1f, .25f, .2f), new Color(.28f, .28f, .32f)
        };
        private readonly Sprite[] orbSprites = new Sprite[2], frameSprites = new Sprite[2];
        private Image orb, orbFrame;

        /// <summary>0 blue (stage 0), 1 yellow (stages 1~2), 2 red (stage 3), 3 out (HP 0).</summary>
        public int OrbState => DamageStage == 0 ? 0 : DamageStage <= 2 ? 1 : DamageStage == 3 ? 2 : 3;
        public Color OrbColor => OrbColors[OrbState];
        public Image Orb => orb;
        /// <summary>Untinted ornament around the orb: Cecilia's halo and chain, Adela's sword.</summary>
        public Image OrbFrame => orbFrame;
        /// <summary>The side illustration (damage-stage art); read by the expression close-up.</summary>
        public Image Portrait => portrait;
        /// <summary>Undamaged (stage 0) illustration of a character, or null when none is drawn yet (dialogue portraits).</summary>
        public Sprite CalmPortrait(int character)
        {
            var stages = character == 0 ? ceciliaStagePortraits : adelaStagePortraits;
            return stages != null && stages.Length > 0 ? stages[0] : null;
        }

        private void RefreshOrb()
        {
            if (portrait == null || !Application.isPlaying) return;
            // Drawn parts (128x96 cells, orb centre (64,49), glass 40 px): ornament, tinted fill, glass on top.
            var ornament = SaintKnight.SpriteCook.SKAppliedSheets.Ui("hp_orb_ornaments", 1 + selected);
            if (orb == null)
            {
                bool drawn = ornament != null;
                // Fallback: 80x32 px code-drawn ornament at 2x with the 24 px orb sitting in its ring.
                orbFrame = NewOrbImage("HP_ORB_FRAME", portrait.transform, new Vector2(6f, -6f),
                    drawn ? new Vector2(128f, 96f) : new Vector2(160f, 64f));
                orb = NewOrbImage("HP_ORB", orbFrame.transform, drawn ? new Vector2(45f, -30f) : new Vector2(8f, -8f),
                    drawn ? new Vector2(38f, 38f) : new Vector2(48f, 48f));
                if (drawn)
                    NewOrbImage("HP_ORB_GLASS", orbFrame.transform, Vector2.zero, new Vector2(128f, 96f)).sprite =
                        SaintKnight.SpriteCook.SKAppliedSheets.Ui("hp_orb_ornaments", 0);
                PlaceOrb();
            }
            if (orbSprites[selected] == null)
            {
                orbSprites[selected] = MakeOrb(selected);
                frameSprites[selected] = MakeOrbFrame(selected);
            }
            orbFrame.sprite = ornament != null ? ornament : frameSprites[selected];
            orb.sprite = orbSprites[selected];
            orb.color = OrbColor;
        }

        private static Image NewOrbImage(string name, Transform parent, Vector2 topLeft, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = topLeft;
            rect.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            return image;
        }

        // Placeholder ornament (2026-11-12, to be replaced by drawn art). Orb centre (16,16) in an 80x32 cell.
        // Cecilia: silver halo with rays, her chain running right to a small cross.
        // Adela: studded gold pommel ring, then grip, crossguard and blade of her long sword.
        private static Sprite MakeOrbFrame(int character)
        {
            const int w = 80, h = 32;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var silver = new Color(.84f, .86f, .93f);
            var shade = new Color(.42f, .45f, .55f);
            var gold = new Color(.86f, .68f, .3f);
            var steel = new Color(.72f, .75f, .82f);
            var dark = new Color(.2f, .2f, .26f);
            var leather = new Color(.36f, .22f, .15f);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = x - 15.5f, dy = y - 15.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy);
                    Color c = Color.clear;
                    if (character == 0)
                    {
                        if (d >= 12.5f && d < 14.5f) c = silver;                                   // halo
                        else if (d >= 14.5f && d < 16f && (ax < 1f || ay < 1f || Mathf.Abs(ax - ay) < 1f)) c = shade; // rays
                        else if (x >= 31 && x < 71)
                        {
                            int k = (x - 31) % 8;                                                  // chain: ring, bar, ring...
                            if (k < 5) { if ((ay < 2.6f) && (k == 0 || k == 4 || ay > 1.6f) && !((k == 0 || k == 4) && ay > 1.6f)) c = silver; }
                            else if (ay < .6f) c = shade;
                        }
                        else if (x >= 71 && x < 78 && ((x == 74 && y >= 10 && y <= 22) || (y == 18 && x >= 72 && x <= 76))) c = silver; // cross
                    }
                    else
                    {
                        if (d >= 12.5f && d < 15.5f) c = (ax < 1.5f || ay < 1.5f || Mathf.Abs(ax - ay) < 1.5f) && d >= 13.2f && d < 14.8f ? dark : gold; // pommel
                        else if (x >= 31 && x < 37 && ay < 2.6f) c = (x % 2 == 0) ? leather : dark;  // grip
                        else if (x >= 37 && x < 40 && ay < 8.6f) c = gold;                           // crossguard
                        else if (x >= 40 && x < 74 && ay < 2.6f) c = ay < .6f ? shade : steel;       // blade and fuller
                        else if (x >= 74 && x < 77 && ay < 2.6f - (x - 73)) c = steel;               // tip
                    }
                    texture.SetPixel(x, y, c);
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), h);
            sprite.name = character == 0 ? "hp_orb_frame_cecilia" : "hp_orb_frame_adela";
            return sprite;
        }

        // Grey pixel orb tinted by Image.color. Cecilia (saint): cross and a thin rim. Adela (knight): shield and a studded rim.
        private static Sprite MakeOrb(int character)
        {
            const int size = 24;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - 11.5f, dy = y - 11.5f, d = Mathf.Sqrt(dx * dx + dy * dy);
                    float ax = Mathf.Abs(dx), ay = Mathf.Abs(dy), lum;
                    if (d > 11.5f) { texture.SetPixel(x, y, Color.clear); continue; }
                    if (d > (character == 0 ? 10f : 9f))
                        lum = character == 1 && (ax < 1.5f || ay < 1.5f) ? .75f : .32f;
                    else
                    {
                        bool emblem = character == 0
                            ? (ax < 1.5f && ay < 6.5f) || (Mathf.Abs(dy - 2f) < 1.5f && ax < 4.5f)
                            : dy < 5.5f && dy > -6.5f && ax < (dy >= -1f ? 5f : 5f + (dy + 1f));
                        lum = emblem ? .3f : Mathf.Lerp(.65f, 1f, Mathf.Clamp01((dy - dx) / 22f + .5f));
                    }
                    texture.SetPixel(x, y, new Color(lum, lum, lum, 1f));
                }
            texture.Apply();
            var sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(.5f, .5f), size);
            sprite.name = character == 0 ? "hp_orb_cecilia" : "hp_orb_adela";
            return sprite;
        }

        /// <summary>Resources path of the HP 0 illustration, e.g. SaintKnightDay15/DefeatPortraits/cecilia_hud_monk.</summary>
        public static string DefeatPortraitPath(int character, string causeKey) =>
            "SaintKnightDay15/DefeatPortraits/" + (character == 1 ? "adela" : "cecilia") + "_hud_" + causeKey.ToLowerInvariant();

        private static readonly System.Collections.Generic.Dictionary<string, Sprite> DefeatPortraits =
            new System.Collections.Generic.Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearDefeatPortraits() => DefeatPortraits.Clear();

        /// <summary>
        /// Resources path of the full-size collapse-stage illustration (NovelAI, 2026-11-19, user: "use the pictures as
        /// drawn"), e.g. SaintKnightRest/Private/HUD/cecilia_stage2. Git-ignored (18+); missing → the serialized portraits.
        /// </summary>
        public static string StagePortraitPath(int character, int stage) =>
            "SaintKnightRest/Private/HUD/" + (character == 1 ? "adela" : "cecilia") + "_stage" + stage;

        /// <summary>Full-size collapse-stage illustration when present, else null.</summary>
        public static Sprite StagePortrait(int character, int stage) => LoadPortrait(StagePortraitPath(character, stage));

        private static Sprite DefeatPortrait(int character, string causeKey) => LoadPortrait(DefeatPortraitPath(character, causeKey));

        // Works whether the PNG was imported as a Sprite or as a plain texture. Missing art → null.
        private static Sprite LoadPortrait(string path)
        {
            if (DefeatPortraits.TryGetValue(path, out var sprite)) return sprite;
            sprite = Resources.Load<Sprite>(path);
            var texture = sprite == null ? Resources.Load<Texture2D>(path) : null;
            if (texture != null)
            {
                sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(.5f, .5f), 100f);
                sprite.name = texture.name;
            }
            DefeatPortraits.Add(path, sprite);
            return sprite;
        }

        private void Refresh()
        {
            RefreshOrb();
            if (portrait != null)
            {
                var stages = selected == 0 ? ceciliaStagePortraits : adelaStagePortraits;
                int stage = DamageStage;
                portrait.sprite = StagePortrait(selected, stage) ?? (stages != null && stage < stages.Length && stages[stage] != null
                    ? stages[stage] : selected == 0 ? ceciliaPortrait : adelaPortrait);
                // HP 0: the illustration for what finished her, when one has been drawn; else stage 4 above.
                var defeat = stage == 4 && health != null
                    ? DefeatPortrait(selected, SaintKnight.Day15.SKGameOverScreen.KeyFor(health.LastCause)) : null;
                if (defeat != null) portrait.sprite = defeat;
                var held = stage < 4 && heldKey.Length > 0 ? DefeatPortrait(selected, heldKey + "_held") : null;
                if (held != null) portrait.sprite = held;
                // the frame follows the picture's own proportions (old pixel art 784x1168, new illustrations 832x1216)
                var fit = portrait.GetComponent<AspectRatioFitter>();
                if (fit != null && portrait.sprite != null) fit.aspectRatio = portrait.sprite.rect.width / portrait.sprite.rect.height;
                PlacePortrait();
            }
            if (characterName != null) characterName.text = selected == 0 ? "CECILIA NOEL" : "ADELA CROW";
            float maximum = health != null ? health.MaximumHp : 100f;
            if (healthText != null) healthText.text = "HP " + Mathf.RoundToInt(hp) + " / " + Mathf.RoundToInt(maximum);
            if (stageText != null) stageText.text = "DAMAGE STAGE " + DamageStage + " / 4" +
                (DamageStage == 4 ? "  |  INCAPACITATED" : "");
            if (hpFill != null) hpFill.fillAmount = hp / maximum;
        }
    }
}
