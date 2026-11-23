using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Rest
{
    /// <summary>
    /// Bust morph for full-screen CGs (2026-11-19, user request): soft breathing plus a springy bounce in
    /// the bust regions of whatever picture the RawImage shows. Regions are listed per picture name (pixels,
    /// origin top-left) in <see cref="Regions"/>; two entries in a row are one character's left and right.
    /// Pictures without an entry draw normally. Runs on unscaled time (menus pause the game).
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public sealed class SKBustMorph : MonoBehaviour
    {
        /// <summary>Picture name (or name prefix, for animation frames) → (centre x, centre y, radius x, radius y) in pixels.</summary>
        public static readonly Dictionary<string, Vector4[]> Regions = new Dictionary<string, Vector4[]>
        {
            ["ep_towels"] = new[] { new Vector4(104, 57, 5, 5), new Vector4(112, 57, 5, 5), new Vector4(146, 54, 7, 6), new Vector4(156, 54, 7, 6) },
            ["hot_spring_bathers"] = new[] { new Vector4(184, 86, 10, 7), new Vector4(203, 86, 10, 7) },
            // 1216x684 NovelAI illustrations, not pixelated (make_salon_novelai.py hd, 2026-11-19). Each ellipse is the
            // whole breast outline (refitted on a 50 px grid): everything inside it moves as one piece. Every salon_ name is
            // listed: the prefix fallback would otherwise give salon_rune_lose1 the regions of salon_rune.
            ["salon_rune"] = new[] { new Vector4(520, 520, 90, 115), new Vector4(690, 520, 95, 115) },
            ["salon_rune_lose1"] = new[] { new Vector4(500, 485, 115, 115), new Vector4(730, 485, 120, 115) },
            ["salon_rune_lose2"] = new[] { new Vector4(512, 515, 95, 120), new Vector4(690, 515, 95, 120) },
            ["salon_rune_lose3"] = new[] { new Vector4(520, 470, 85, 100), new Vector4(680, 470, 85, 100) },
            ["salon_rune_serve"] = new[] { new Vector4(470, 535, 80, 95), new Vector4(620, 535, 85, 95) },
            // Greta redrawn 2026-11-19 as a bright golden-retriever girl (user: the grey wolf looked old)
            ["salon_greta"] = new[] { new Vector4(420, 570, 130, 130), new Vector4(670, 570, 130, 130) },
            ["salon_greta_lie"] = new[] { new Vector4(240, 600, 120, 95), new Vector4(460, 600, 105, 95) },
            ["salon_greta_mix1"] = new[] { new Vector4(550, 545, 115, 110), new Vector4(770, 545, 115, 110) },
            ["salon_greta_mix2"] = new[] { new Vector4(452, 585, 150, 110), new Vector4(752, 585, 150, 110) },
            ["salon_greta_serve"] = new[] { new Vector4(525, 570, 130, 130), new Vector4(780, 570, 130, 130) },
            // 릴리, rabbit beastkin (added 2026-11-19); her main CG has a dutch tilt, so the right side sits higher
            ["salon_lili"] = new[] { new Vector4(425, 430, 125, 130), new Vector4(633, 387, 113, 120) },
            ["salon_lili_lean"] = new[] { new Vector4(464, 600, 170, 120), new Vector4(784, 600, 147, 120) },
            ["salon_sorel"] = new[] { new Vector4(505, 430, 80, 95), new Vector4(650, 430, 80, 95) },   // sorel_hot1
            ["salon_sorel_wine"] = new[] { new Vector4(450, 600, 145, 100), new Vector4(735, 600, 145, 100) },
            ["salon_sorel_serve1"] = new[] { new Vector4(503, 445, 57, 75), new Vector4(617, 445, 62, 75) },
            ["salon_sorel_serve2"] = new[] { new Vector4(510, 585, 115, 105), new Vector4(740, 585, 120, 105) },
        };

        // breath after the reference reel (De04zvrggQb: the chest rises and falls about every 1.7–2 s, clearly visible)
        public const float Stiffness = 70f, Damping = 4.5f, BreathPixels = 1.5f, BreathSeconds = 2.2f, MaxShare = .4f;
        /// <summary>Off switch (user, 2026-11-19: "if it can't move the whole breast naturally, better not to move").</summary>
        public static bool Enabled = false;   // user, 2026-11-19 later: "let's just not morph" — off; set true to bring it back
        private static readonly List<SKBustMorph> live = new List<SKBustMorph>();

        private RawImage raw;
        private Material mat;
        private Texture shown;
        private Vector4[] regions;
        private float scale = 1f;   // motion is tuned for ~180 px tall pixel CGs; full-size illustrations move in proportion
        private readonly Vector4[] uvRegions = new Vector4[4], uvOffsets = new Vector4[4];
        private readonly Vector2[] pos = new Vector2[4], vel = new Vector2[4];

        /// <summary>Current offset of each region in pixels (for checks).</summary>
        public Vector2 Offset(int i) => pos[i];
        public bool Active => regions != null;

        public static SKBustMorph Attach(RawImage target)
        {
            if (target == null) return null;
            var m = target.GetComponent<SKBustMorph>();
            return m != null ? m : target.gameObject.AddComponent<SKBustMorph>();
        }

        /// <summary>Kick every visible bust (a line is spoken, a laugh, a step). 1 = a normal bounce.</summary>
        public static void BounceAll(float strength = 1f)
        {
            foreach (var m in live) m.Bounce(strength);
        }

        public void Bounce(float strength = 1f)
        {
            if (regions == null) return;
            for (int i = 0; i < regions.Length && i < 4; i++)
            {
                // the pair moves together, the second side a touch weaker so they do not look glued
                float k = (i % 2 == 0 ? 1f : .85f) * Random.Range(.9f, 1.1f);
                vel[i] += new Vector2((i % 2 == 0 ? -1f : 1f) * 3f, 26f) * strength * k * Mathf.Sqrt(regions[i].w / 6f / scale) * scale;
            }
        }

        private void Awake() => raw = GetComponent<RawImage>();
        private void OnEnable() => live.Add(this);
        private void OnDisable()
        {
            live.Remove(this);
            if (raw != null && raw.material == mat) raw.material = null;
            shown = null;
        }
        private void OnDestroy() { if (mat != null) Destroy(mat); }

        private static Vector4[] Find(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (Regions.TryGetValue(name, out var r)) return r;
            foreach (var kv in Regions) if (name.StartsWith(kv.Key)) return kv.Value;
            return null;
        }

        private void LateUpdate()
        {
            var tex = raw.texture;
            var found = tex == shown ? regions : tex != null ? Find(tex.name) : null;
            shown = tex;
            if (found != regions || (found != null && raw.material != mat))   // a new picture (animation frames share one set and keep moving)
            {
                regions = found;
                scale = tex != null ? Mathf.Max(1f, tex.height / 180f) : 1f;
                for (int i = 0; i < 4; i++) { pos[i] = vel[i] = Vector2.zero; uvRegions[i] = Vector4.zero; }
                if (regions == null || !Enabled) { regions = null; if (!Enabled) shown = null; if (raw.material == mat) raw.material = null; return; }   // off: look again next frame
                if (mat == null)
                {
                    var sh = Resources.Load<Shader>("SaintKnightRest/SKBustMorph");
                    if (sh == null) { regions = null; return; }
                    mat = new Material(sh) { name = "SKBustMorph (instance)" };
                }
                for (int i = 0; i < regions.Length && i < 4; i++)
                {
                    var r = regions[i];
                    uvRegions[i] = new Vector4(r.x / tex.width, 1f - r.y / tex.height, r.z / tex.width, r.w / tex.height);
                }
                mat.SetVectorArray("_Region", uvRegions);
                raw.material = mat;
                Bounce(.6f);   // a picture appearing settles in with a small bounce
            }
            if (regions == null || tex == null) return;

            Step(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f), Time.unscaledTime);
            mat.SetVectorArray("_Offset", uvOffsets);
        }

        /// <summary>Squash/stretch per region (x, y relative scale) for checks: rising fast squashes, falling stretches.</summary>
        public Vector2 Squash(int i) => new Vector2(uvOffsets[i].z, uvOffsets[i].w);

        // 2026-11-19 user: "morphing is the most important — make it natural". Besides breathing and the spring:
        // each side has its own spring constants so the pair drifts out of step after a bounce, a slow sideways
        // sway keeps it from ever standing still, and the speed squashes/stretches the region (volume kept).
        // The shader holds the top edge (the chest) and lets the lower half swing.
        public void Step(float dt, float t)
        {
            var tex = raw.texture;
            if (regions == null || tex == null) return;
            for (int i = 0; i < regions.Length && i < 4; i++)
            {
                bool second = i % 2 == 1;
                float stiff = Stiffness * (second ? .9f : 1f), damp = Damping * (second ? .92f : 1f);
                // chest rises with the breath; the spring chases it and overshoots after a bounce
                float breath = BreathPixels * scale * (.5f + .5f * Mathf.Sin(t * 2f * Mathf.PI / BreathSeconds + (i / 2) * 1.3f + (second ? .25f : 0f)));
                float sway = .35f * BreathPixels * scale * Mathf.Sin(t * .83f + (i / 2) * 2.1f + (second ? .5f : 0f));
                var target = new Vector2(sway, breath);
                vel[i] += (stiff * (target - pos[i]) - damp * vel[i]) * dt;
                pos[i] += vel[i] * dt;
                float cap = MaxShare * Mathf.Min(regions[i].z, regions[i].w);
                pos[i] = Vector2.ClampMagnitude(pos[i], cap);
                float sy = Mathf.Clamp(-.04f * vel[i].y / Mathf.Max(regions[i].w, 1f), -MaxSquash, MaxSquash);
                uvOffsets[i] = new Vector4(pos[i].x / tex.width, pos[i].y / tex.height, -.5f * sy, sy);
            }
        }

        public const float MaxSquash = .06f;   // the whole breast squashes, so keep it subtle
    }
}
