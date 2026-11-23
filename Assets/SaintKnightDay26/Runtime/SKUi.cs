using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Day26
{
    /// <summary>
    /// Shared look for menus (2026-11-16): serif OS font, thin light outlines, elongated-hexagon buttons,
    /// diamond toggles. Code-drawn so no third-party art or fonts ship with the game.
    /// </summary>
    public static class SKUi
    {
        public static readonly Color Ink = new Color(.86f, .84f, .80f);
        public static readonly Color Dim = new Color(.45f, .43f, .42f);
        public static readonly Color Focus = new Color(1f, .82f, .52f);
        public static readonly Color Accent = new Color(.86f, .32f, .12f);
        public const string Version = "0.30";

        private static Font serif, hangul;
        /// <summary>Windows serif (Palatino Linotype first); falls back to Unity's built-in font.</summary>
        public static Font Serif => serif != null ? serif : serif = OsFont(new[] { "Palatino Linotype", "Book Antiqua", "Georgia", "Times New Roman" });
        /// <summary>Korean serif for the title subtitle.</summary>
        public static Font Hangul => hangul != null ? hangul : hangul = OsFont(new[] { "Batang", "HYSinMyeongJo-Medium", "Malgun Gothic" });

        private static Font OsFont(string[] names)
        {
            try
            {
                var installed = Font.GetOSInstalledFontNames();
                foreach (var n in names)
                    if (System.Array.IndexOf(installed, n) >= 0) return Font.CreateDynamicFontFromOSFont(n, 32);
            }
            catch (System.Exception) { }
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = r.anchorMax = anchor;
            r.pivot = pivot ?? anchor;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static Text Label(string name, Transform parent, string text, int size, Vector2 anchor, Vector2 pos, Vector2 box,
            TextAnchor align = TextAnchor.MiddleLeft, Font font = null)
        {
            var t = Rect(name, parent, anchor, pos, box).gameObject.AddComponent<Text>();
            t.font = font ?? Serif;
            t.fontSize = size;
            t.alignment = align;
            t.color = Ink;
            t.text = text;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static Image Picture(string name, Transform parent, Sprite sprite, Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var img = Rect(name, parent, anchor, pos, size).gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.raycastTarget = false;
            return img;
        }

        // ---------- code-drawn sprites ----------

        private static Sprite hex, diamond, diamondOn, white;

        /// <summary>Elongated hexagon outline (left/right points), 9-sliced horizontally. Draw at 40 px height.</summary>
        public static Sprite Hex => hex != null ? hex : hex = MakeHex();
        public static Sprite Diamond => diamond != null ? diamond : diamond = MakeDiamond(false);
        public static Sprite DiamondOn => diamondOn != null ? diamondOn : diamondOn = MakeDiamond(true);
        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var t = new Texture2D(1, 1) { filterMode = FilterMode.Point };
                t.SetPixel(0, 0, Color.white);
                t.Apply();
                return white = Sprite.Create(t, new UnityEngine.Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 100f);
            }
        }

        private static Sprite MakeHex()
        {
            const int w = 64, h = 40, tip = 18;
            var pts = new[] { new Vector2(.5f, h / 2f), new Vector2(tip, .5f), new Vector2(w - tip, .5f), new Vector2(w - .5f, h / 2f),
                              new Vector2(w - tip, h - .5f), new Vector2(tip, h - .5f) };
            var tex = Shape(w, h, pts, new Color(.05f, .045f, .05f, .55f), 1.6f);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(tip + 2, 0, tip + 2, 0));
        }

        private static Sprite MakeDiamond(bool on)
        {
            const int s = 32;
            var pts = new[] { new Vector2(s / 2f, 1f), new Vector2(s - 1f, s / 2f), new Vector2(s / 2f, s - 1f), new Vector2(1f, s / 2f) };
            var tex = Shape(s, s, pts, new Color(0f, 0f, 0f, .4f), 1.6f);
            if (on)
                for (int y = 0; y < s; y++)
                    for (int x = 0; x < s; x++)
                        if (Mathf.Abs(x + .5f - s / 2f) + Mathf.Abs(y + .5f - s / 2f) < s * .2f) tex.SetPixel(x, y, Accent);
            tex.Apply();
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }

        // Filled convex polygon with a light outline (anti-aliased by distance to the edges).
        private static Texture2D Shape(int w, int h, Vector2[] pts, Color fill, float line)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var p = new Vector2(x + .5f, y + .5f);
                    float d = float.MaxValue;
                    bool inside = true;
                    for (int i = 0; i < pts.Length; i++)
                    {
                        Vector2 a = pts[i], b = pts[(i + 1) % pts.Length];
                        d = Mathf.Min(d, SegmentDistance(p, a, b));
                        if ((b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x) < 0f) inside = false;
                    }
                    float edge = Mathf.Clamp01(line - d + .5f);
                    var c = inside ? fill : Color.clear;
                    c = Color.Lerp(c, Color.white, edge);
                    if (!inside && edge <= 0f) c = Color.clear;
                    tex.SetPixel(x, y, c);
                }
            tex.Apply();
            return tex;
        }

        private static float SegmentDistance(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return Vector2.Distance(p, a + ab * t);
        }

        // ---------- gothic set (2026-11-16 restyle: own look, not the reference's hexagons and diamonds) ----------

        public static readonly Color Gold = new Color(.80f, .66f, .42f);
        public static readonly Color Blood = new Color(.62f, .07f, .09f);

        private static Sprite frame, bracket, ring, ringOn, cross, glow, fade;

        /// <summary>Panel border: double gold rule with a small diamond in each corner. 9-sliced, border 16.</summary>
        public static Sprite Frame => frame != null ? frame : frame = MakeFrame();
        /// <summary>Button: dark fill, faint edge, bright corner brackets. White (tinted by Image.color), border 10.</summary>
        public static Sprite Bracket => bracket != null ? bracket : bracket = MakeBracket();
        public static Sprite Ring => ring != null ? ring : ring = MakeRing(false);
        public static Sprite RingOn => ringOn != null ? ringOn : ringOn = MakeRing(true);
        /// <summary>Small latin cross, white.</summary>
        public static Sprite Cross => cross != null ? cross : cross = MakeCross();
        /// <summary>Soft radial glow, white.</summary>
        public static Sprite Glow => glow != null ? glow : glow = MakeGlow();
        /// <summary>Horizontal bar fading out at both ends, white (focus bars, rules).</summary>
        public static Sprite Fade => fade != null ? fade : fade = MakeFade();

        private static Texture2D Tex(int w, int h, FilterMode mode = FilterMode.Point) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = mode, wrapMode = TextureWrapMode.Clamp };

        private static Sprite MakeFrame()
        {
            const int s = 48, b = 16;
            var t = Tex(s, s);
            var inner = new Color(Gold.r, Gold.g, Gold.b, .55f);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int ex = Mathf.Min(x, s - 1 - x), ey = Mathf.Min(y, s - 1 - y), e = Mathf.Min(ex, ey);
                    var c = Color.clear;
                    if (e == 1) c = Gold;
                    else if (e == 5) c = inner;
                    int d = Mathf.Abs(ex - 3) + Mathf.Abs(ey - 3);
                    if (d <= 3) c = d <= 1 ? Blood : Gold;
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        private static Sprite MakeBracket()
        {
            const int s = 32, b = 10, arm = 8;
            var t = Tex(s, s);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    int ex = Mathf.Min(x, s - 1 - x), ey = Mathf.Min(y, s - 1 - y);
                    var c = new Color(0f, 0f, 0f, .5f);
                    if (ex <= 1 || ey <= 1)
                        c = (ex <= 1 && ey < arm) || (ey <= 1 && ex < arm) ? Color.white : new Color(1f, 1f, 1f, .22f);
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(b, b, b, b));
        }

        private static Sprite MakeRing(bool on)
        {
            const int s = 32;
            var t = Tex(s, s, FilterMode.Bilinear);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(s / 2f, s / 2f));
                    float a = Mathf.Clamp01(1.4f - Mathf.Abs(d - 12f));
                    var c = new Color(1f, 1f, 1f, a);
                    if (on && d < 7f) c = Color.Lerp(Accent, new Color(1f, .55f, .35f), Mathf.Clamp01(1f - d / 7f)) * new Color(1, 1, 1, Mathf.Clamp01(7f - d));
                    t.SetPixel(x, y, c);
                }
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }

        private static Sprite MakeCross()
        {
            const int w = 11, h = 17;
            var t = Tex(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, y, (x >= 4 && x <= 6) || (y >= 10 && y <= 12) ? Color.white : Color.clear);
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
        }

        private static Sprite brokenLink;
        /// <summary>Broken chain link, white pixels (story motif: the chain that bound the saint). 11x17.</summary>
        public static Sprite BrokenLink => brokenLink != null ? brokenLink : brokenLink = MakePattern(new[]
        {
            // a closed link, then the next link snapped in two with sparks in the gap
            "...#####...",
            "..##...##..",
            "..#.....#..",
            "..#.....#..",
            "..#.....#..",
            "..##...##..",
            "...#####...",
            "....#.#....",
            "...#...#...",
            "..#.....#..",
            "...........",
            "#.........#",
            "...........",
            "..#.....#..",
            "..##...##..",
            "...#####...",
            "...........",
        });

        private static Sprite MakePattern(string[] rows)
        {
            int w = rows[0].Length, h = rows.Length;
            var t = Tex(w, h);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    t.SetPixel(x, h - 1 - y, rows[y][x] == '#' ? Color.white : Color.clear);
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
        }

        private static Sprite MakeGlow()
        {
            const int s = 64;
            var t = Tex(s, s, FilterMode.Bilinear);
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(s / 2f, s / 2f)) / (s / 2f);
                    float a = Mathf.Clamp01(1f - d);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f);
        }

        private static Sprite MakeFade()
        {
            const int w = 64, h = 4;
            var t = Tex(w, h, FilterMode.Bilinear);
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.Clamp01(Mathf.Sin((x + .5f) / w * Mathf.PI) * 1.6f);
                for (int y = 0; y < h; y++) t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, w, h), new Vector2(.5f, .5f), 100f);
        }

        /// <summary>"PAUSE" → "P A U S E" (legacy Text has no letter spacing).</summary>
        public static string Spaced(string s, string gap = " ") => string.Join(gap, s.ToUpperInvariant().ToCharArray());

        /// <summary>Full-screen darkening: edges and bottom fade to black (title backdrop).</summary>
        public static Texture2D Vignette(int w = 160, int h = 90)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x + .5f) / w * 2f - 1f, dy = (y + .5f) / h * 2f - 1f;
                    float edge = Mathf.Clamp01((dx * dx * .8f + dy * dy) - .25f);
                    float bottom = Mathf.Clamp01(.45f - (y + .5f) / h) * 1.8f;
                    tex.SetPixel(x, y, new Color(0f, 0f, 0f, Mathf.Clamp01(edge * .95f + bottom)));
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Transparent film grain specks (animated by moving uvRect).</summary>
        public static Texture2D GrainOverlay(int w = 640, int h = 360, int seed = 11)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var rng = new System.Random(seed);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    double n = rng.NextDouble();
                    tex.SetPixel(x, y, n > .985 ? new Color(1f, .96f, .9f, (float)(n - .985) * 40f) : n < .05 ? new Color(0f, 0f, 0f, .5f) : Color.clear);
                }
            tex.Apply();
            return tex;
        }

        /// <summary>Dark grainy background with a vignette (title screen).</summary>
        public static Texture2D Grain(int w = 320, int h = 180, int seed = 7)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false) { filterMode = FilterMode.Point };
            var rng = new System.Random(seed);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float dx = (x - w / 2f) / (w / 2f), dy = (y - h / 2f) / (h / 2f);
                    float vignette = Mathf.Clamp01(1f - .55f * (dx * dx + dy * dy));
                    float n = (float)rng.NextDouble();
                    float v = .035f + .03f * n * n + (n > .985f ? .07f : 0f);
                    v *= vignette;
                    tex.SetPixel(x, y, new Color(v, v * .97f, v * .95f));
                }
            tex.Apply();
            return tex;
        }
    }
}
