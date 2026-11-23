using UnityEngine;

namespace SaintKnight.Lobby
{
    /// <summary>
    /// 2026-11-19 (user): the village sky moves through the day on its own, and the chimneys smoke.
    /// The town art has its sky cut out (Docs/Design/Village/make_village_bg.py); this draws a banded pixel
    /// gradient, stars and a sun / moon behind it, tints the town by the light, keeps the lit windows bright at
    /// night, and plays the pixelfx chimney smoke sheet (Docs/Design/Village/pixelfx/) over each chimney.
    /// </summary>
    public sealed class SKVillageSky : MonoBehaviour
    {
        private const float Ppu = 48f;

        [SerializeField] private SpriteRenderer town;
        [SerializeField] private SpriteRenderer lights;
        [SerializeField] private Texture2D smokeSheet;
        [Tooltip("Width of the street art in pixels (640 one screen, 1260 since 2026-11-22).")]
        [SerializeField, Min(640f)] private float artWidth = 640f;
        [Tooltip("Chimney tops in pixels of the street art.")]
        [SerializeField] private Vector2[] chimneys = new Vector2[0];
        [Tooltip("Seconds for a whole day.")]
        [SerializeField, Min(10f)] private float dayLength = 480f;
        [Tooltip("Phase the clock starts at (0 midnight, .5 noon); .72 = the painted dusk.")]
        [SerializeField, Range(0f, 1f)] private float startPhase = .72f;

        /// <summary>Set to pin the time of day (tests, captures); negative = the running clock.</summary>
        public static float PhaseOverride = -1f;
        public float Phase => PhaseOverride >= 0f ? PhaseOverride : Mathf.Repeat(startPhase + Time.unscaledTime / dayLength, 1f);
        public Color SkyTop { get; private set; }
        public Color TownTint { get; private set; }
        public int SmokeCount => smoke.Length;

        // phase, sky top, horizon, town tint, stars, window lights
        private struct Key { public float t; public Color top, horizon, tint; public float stars, glow; }
        private static Key K(float t, string top, string hor, string tint, float stars, float glow)
        {
            ColorUtility.TryParseHtmlString(top, out var a);
            ColorUtility.TryParseHtmlString(hor, out var b);
            ColorUtility.TryParseHtmlString(tint, out var c);
            return new Key { t = t, top = a, horizon = b, tint = c, stars = stars, glow = glow };
        }
        // 2026-11-22 user: a darker village that fits the game — overcast grey days, a blood-red dusk, black nights
        private static readonly Key[] Keys =
        {
            K(0f,   "#05050d", "#171222", "#423e52", 1f,   1f),    // midnight
            K(.20f, "#0c0a1a", "#33263a", "#575062", .7f,  .95f),  // before dawn
            K(.26f, "#2a2740", "#94594a", "#8c7e80", .1f,  .6f),   // sunrise, smoky
            K(.34f, "#4a5670", "#959ba6", "#aaa6a8", 0f,   .2f),   // morning, overcast
            K(.50f, "#55637c", "#a3a8ae", "#bbb7b4", 0f,   .1f),   // noon, still grey
            K(.64f, "#4b5469", "#a8907e", "#b2a8a0", 0f,   .2f),   // afternoon
            K(.72f, "#28182e", "#8e3c34", "#9a8088", .15f, .55f),  // dusk, blood-red horizon
            K(.80f, "#100c1e", "#3a1e30", "#615670", .6f,  .9f),   // evening
            K(1f,   "#05050d", "#171222", "#423e52", 1f,   1f),
        };

        private SpriteRenderer sky, stars, sun, moon;
        private Texture2D skyTex;
        private SpriteRenderer[] smoke = new SpriteRenderer[0];
        private Sprite[] smokeFrames = new Sprite[0];
        private readonly Color[] band = new Color[90];

        private Vector3 World(float px, float py, float z) => new Vector3((px - artWidth / 2f) / Ppu, (180f - py) / Ppu, z);

        private void Awake()
        {
            skyTex = new Texture2D(1, band.Length, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            sky = Layer("Village_Sky", Sprite.Create(skyTex, new Rect(0, 0, 1, band.Length), new Vector2(.5f, .5f), 1f), -13, 2f);
            sky.transform.localScale = new Vector3(artWidth / Ppu, 360f / Ppu / band.Length, 1f);
            stars = Layer("Village_Stars", Sprite.Create(StarField((int)artWidth), new Rect(0, 0, (int)artWidth, 220), new Vector2(.5f, 1f), Ppu), -12, 1.9f);
            stars.transform.position = World(artWidth / 2f, 0f, 1.9f);
            sun = Layer("Village_Sun", Sprite.Create(Disc(new Color(1f, .92f, .7f), 7, false), new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), Ppu), -12, 1.8f);
            moon = Layer("Village_Moon", Sprite.Create(Disc(new Color(.93f, .9f, .8f), 6, true), new Rect(0, 0, 16, 16), new Vector2(.5f, .5f), Ppu), -12, 1.8f);
            BuildSmoke();
        }

        private SpriteRenderer Layer(string name, Sprite sprite, int order, float z)
        {
            var r = new GameObject(name).AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, false);
            r.transform.position = new Vector3(0f, 0f, z);
            r.sprite = sprite;
            r.sortingOrder = order;
            return r;
        }

        private static Texture2D StarField(int width)
        {
            var tex = new Texture2D(width, 220, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var clear = new Color[width * 220];
            tex.SetPixels(clear);
            var rng = new System.Random(51001);
            for (int i = 0; i < 170 * width / 640; i++)
            {
                int x = rng.Next(width), y = rng.Next(220);
                float a = .35f + (float)rng.NextDouble() * .65f * (y / 220f);   // fainter toward the horizon (bottom row = 0)
                tex.SetPixel(x, y, new Color(1f, .97f, .92f, a));
            }
            tex.Apply();
            return tex;
        }

        private static Texture2D Disc(Color c, int radius, bool crescent)
        {
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    float dx = x - 7.5f, dy = y - 7.5f;
                    bool inside = dx * dx + dy * dy <= radius * radius;
                    if (crescent && (dx - 3f) * (dx - 3f) + (dy - 1.5f) * (dy - 1.5f) <= radius * radius) inside = false;
                    tex.SetPixel(x, y, inside ? c : Color.clear);
                }
            tex.Apply();
            return tex;
        }

        private void BuildSmoke()
        {
            if (smokeSheet == null || chimneys.Length == 0) return;
            const int fw = 48, fh = 72, cols = 8, count = 36;   // pixelfx sheet: chimney-smoke.json
            smokeFrames = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                int c = i % cols, row = i / cols;
                smokeFrames[i] = Sprite.Create(smokeSheet, new Rect(c * fw, smokeSheet.height - (row + 1) * fh, fw, fh),
                    new Vector2(16f / fw, 1f - 68f / fh), Ppu);
            }
            smoke = new SpriteRenderer[chimneys.Length];
            for (int i = 0; i < chimneys.Length; i++)
            {
                smoke[i] = Layer("Chimney_Smoke" + i, smokeFrames[0], -8, .5f);
                smoke[i].transform.position = World(chimneys[i].x, chimneys[i].y, .5f);
            }
        }

        private void Update()
        {
            float phase = Phase;
            int k = 1;
            while (k < Keys.Length - 1 && Keys[k].t < phase) k++;
            var a = Keys[k - 1]; var b = Keys[k];
            float u = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a.t, b.t, phase));
            SkyTop = Color.Lerp(a.top, b.top, u);
            var horizon = Color.Lerp(a.horizon, b.horizon, u);
            TownTint = Color.Lerp(a.tint, b.tint, u);
            for (int i = 0; i < band.Length; i++)
            {
                float h = 1f - i / (band.Length - 1f);                         // 0 at the top row (texture row 89)
                band[i] = Color.Lerp(SkyTop, horizon, Mathf.Clamp01(h / .62f));
            }
            skyTex.SetPixels(band);
            skyTex.Apply();
            stars.color = new Color(1f, 1f, 1f, Mathf.Lerp(a.stars, b.stars, u));
            if (town != null) town.color = TownTint;
            if (lights != null) lights.color = new Color(1f, 1f, 1f, Mathf.Lerp(a.glow, b.glow, u));

            // the sun crosses 0.23 .. 0.77, the moon the other half; low = near the horizon line
            float sunT = Mathf.InverseLerp(.23f, .77f, phase), moonT = Mathf.InverseLerp(.73f, 1.27f, phase < .5f ? phase + 1f : phase);
            Place(sun, sunT);
            Place(moon, moonT);

            int frame = (int)(Time.unscaledTime * 12f);
            for (int i = 0; i < smoke.Length; i++)
            {
                smoke[i].sprite = smokeFrames[(frame + i * 7) % smokeFrames.Length];
                smoke[i].color = new Color(TownTint.r, TownTint.g, TownTint.b, .9f);
            }
        }

        private void Place(SpriteRenderer body, float t)
        {
            bool up = t > 0f && t < 1f;
            body.enabled = up;
            if (!up) return;
            float px = Mathf.Lerp(40f, artWidth - 40f, t), py = 150f - Mathf.Sin(t * Mathf.PI) * 125f;
            body.transform.position = World(px, py, body.transform.position.z);
        }
    }
}
