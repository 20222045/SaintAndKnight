using System.Collections.Generic;
using SaintKnight.Day01;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Rest
{
    /// <summary>
    /// Expression close-up (2026-11-17, after the reference's fear-stage close-ups): when a hit pushes her
    /// into a worse damage stage, the face of the new stage illustration (2D dot art) pops up large in a
    /// gold frame for a moment, so the expression change reads. Unscaled time; play does not stop.
    /// Only illustrations with a known face spot are used (Cecilia's five stages for now).
    /// </summary>
    public sealed class SKExpressionCutIn : MonoBehaviour
    {
        public const float Show = 1.5f, FadeIn = .12f, FadeOut = .35f;
        private const float FaceSize = 210f;   // square crop, illustration pixels

        // face centre on each 784x1168 illustration (pixels from the top-left)
        private static readonly Dictionary<string, Vector2> Faces = new Dictionary<string, Vector2>
        {
            { "cecilia_portrait_stage0", new Vector2(377f, 156f) },
            { "cecilia_portrait_stage1", new Vector2(396f, 131f) },
            { "cecilia_portrait_stage2", new Vector2(346f, 106f) },
            { "cecilia_portrait_stage3", new Vector2(359f, 156f) },
            { "cecilia_portrait_stage4", new Vector2(334f, 131f) },
            // full-size NovelAI collapse stages (832x1216, scaled to the 784-wide table), 2026-11-19
            { "cecilia_stage0", new Vector2(393f, 152f) }, { "cecilia_stage1", new Vector2(387f, 152f) },
            { "cecilia_stage2", new Vector2(358f, 283f) }, { "cecilia_stage3", new Vector2(347f, 283f) },
            { "cecilia_stage4", new Vector2(296f, 152f) },
            { "adela_stage0", new Vector2(405f, 131f) }, { "adela_stage1", new Vector2(395f, 136f) },
            { "adela_stage2", new Vector2(317f, 273f) }, { "adela_stage3", new Vector2(381f, 325f) },
            { "adela_stage4", new Vector2(453f, 162f) },
        };

        private SKDay01HUD hud;
        private int lastStage = -1;
        private bool pending;
        private float shownAt = float.NegativeInfinity;
        private RectTransform root;
        private Image face, ring, back;
        private static readonly Dictionary<(Sprite, float, float), Sprite> crops = new Dictionary<(Sprite, float, float), Sprite>();

        /// <summary>
        /// Square crop of an illustration with a known face spot, else null. Default = face close-up;
        /// a bigger <paramref name="size"/> moved <paramref name="down"/> gives the dialogue bust.
        /// </summary>
        public static Sprite CropFace(Sprite art, float size = FaceSize, float down = 0f)
        {
            if (art == null || !Faces.TryGetValue(art.name, out var centre)) return null;
            if (crops.TryGetValue((art, size, down), out var crop) && crop != null) return crop;
            centre.y += down;
            var t = art.texture;
            float s = t.width / 784f;   // the table is in 784-wide illustration pixels
            var r = new Rect((centre.x - size * .5f) * s, t.height - (centre.y + size * .5f) * s, size * s, size * s);
            r.x = Mathf.Clamp(r.x, 0f, t.width - r.width);
            r.y = Mathf.Clamp(r.y, 0f, t.height - r.height);
            crop = Sprite.Create(t, r, new Vector2(.5f, .5f), 100f);
            crop.name = art.name + "_face";
            crops[(art, size, down)] = crop;
            return crop;
        }

        public bool IsShowing => Time.unscaledTime - shownAt < Show;
        public Sprite Face => face != null ? face.sprite : null;

        public static SKExpressionCutIn Attach(SKDay01HUD hud)
        {
            if (hud == null || hud.Portrait == null) return null;
            if (!hud.TryGetComponent<SKExpressionCutIn>(out var c)) c = hud.gameObject.AddComponent<SKExpressionCutIn>();
            c.hud = hud;
            return c;
        }

        private void LateUpdate()
        {
            if (hud == null) return;
            int stage = hud.DamageStage;
            if (lastStage >= 0 && stage > lastStage) pending = true;   // worse: show once the HUD swapped the art
            lastStage = stage;
            if (pending && TryShow()) pending = false;
            Animate();
        }

        private bool TryShow()
        {
            var crop = CropFace(hud.Portrait.sprite);
            if (crop == null) return true;   // nothing to show
            if (root == null) Build();
            face.sprite = crop;
            shownAt = Time.unscaledTime;
            root.SetAsLastSibling();
            return true;
        }

        private void Build()
        {
            var canvas = hud.Portrait.canvas != null ? hud.Portrait.canvas.rootCanvas.transform : hud.transform;
            root = new GameObject("EXPRESSION_CLOSEUP", typeof(RectTransform)).GetComponent<RectTransform>();
            root.SetParent(canvas, false);
            // upper middle-left, clear of the corner illustration and of the HP orb
            root.anchorMin = root.anchorMax = new Vector2(.36f, .68f);
            root.sizeDelta = new Vector2(240f, 240f);
            back = Child("Shade", SaintKnight.Day26.SKUi.White, 1.06f);
            face = Child("Face", null, 1f);
            ring = Child("Frame", SaintKnight.Day26.SKUi.Frame, 1.1f);
            ring.type = Image.Type.Sliced;
            root.gameObject.SetActive(false);
        }

        private Image Child(string n, Sprite s, float scale)
        {
            var img = new GameObject(n).AddComponent<Image>();
            img.transform.SetParent(root, false);
            img.sprite = s;
            img.raycastTarget = false;
            var r = img.rectTransform;
            r.anchorMin = r.anchorMax = new Vector2(.5f, .5f);
            r.sizeDelta = root.sizeDelta * scale;
            return img;
        }

        private void Animate()
        {
            if (root == null) return;
            float t = Time.unscaledTime - shownAt;
            bool on = t < Show;
            root.gameObject.SetActive(on);
            if (!on) return;
            float a = Mathf.Min(Mathf.Clamp01(t / FadeIn), Mathf.Clamp01((Show - t) / FadeOut));
            root.localScale = Vector3.one * Mathf.Lerp(1.12f, 1f, Mathf.Clamp01(t / .25f));
            face.color = new Color(1f, 1f, 1f, a);
            back.color = new Color(0f, 0f, 0f, .85f * a);
            ring.color = new Color(1f, 1f, 1f, a);
        }
    }
}
