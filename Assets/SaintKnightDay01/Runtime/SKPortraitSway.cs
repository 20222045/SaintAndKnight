using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Day01
{
    /// <summary>
    /// Idle bounce of the HUD illustration, measured from the user's reference (2026-11-19, MOZO Games
    /// "Flower Survivors" reels DfrSWkvhSp_ / Df53jWhBVF7, frame-by-frame at 15 fps): the head stays put and
    /// the body below it is pulled up and let go — a vertical squash pivoted at the head, about 3% of the
    /// picture's height at the screen bottom, every ~0.53 s (rise 0.13 s, hold, settle 0.25 s, short rest).
    /// Linear in height, so only the quad's four corners move. Unscaled time: keeps going through hit-stop.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class SKPortraitSway : BaseMeshEffect
    {
        /// <summary>Head height as a share of the picture (pivot of the squash).</summary>
        public const float HeadShare = .86f;
        /// <summary>Peak squash: share of the picture height a point one picture-height below the head rises.</summary>
        public const float Squash = .03f;   // reels: 11 px over 310 px head-to-bottom (3.5%), 5 px over 240 px (2%)
        public const float PeriodSeconds = .53f;

        private float phase;

        public static SKPortraitSway Attach(Graphic target)
        {
            if (target == null) return null;
            var s = target.GetComponent<SKPortraitSway>();
            return s != null ? s : target.gameObject.AddComponent<SKPortraitSway>();
        }

        /// <summary>0..1 pulse over one period (reference: rise 25%, hold to 40%, settle by 85%).</summary>
        public static float Pulse(float p)
        {
            p -= Mathf.Floor(p);
            if (p < .25f) return Mathf.SmoothStep(0f, 1f, p / .25f);
            if (p < .4f) return 1f;
            if (p < .85f) return Mathf.SmoothStep(1f, 0f, (p - .4f) / .45f);
            return 0f;
        }

        /// <summary>Upward offset as a share of the picture height for a point at height share v (0 = bottom).</summary>
        public float LiftAt(float v) => Squash * Pulse(phase) * (HeadShare - v);

        private void Update() => Step(Mathf.Min(Time.unscaledDeltaTime, 1f / 30f));

        public void Step(float dt)
        {
            phase += dt / PeriodSeconds;
            if (phase > 1000f) phase -= 1000f;
            if (graphic != null) graphic.SetVerticesDirty();
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount != 4) return;   // only the plain quad (Image Simple / RawImage)
            float yMin = float.MaxValue, yMax = float.MinValue;
            var v = new UIVertex();
            for (int i = 0; i < 4; i++) { vh.PopulateUIVertex(ref v, i); yMin = Mathf.Min(yMin, v.position.y); yMax = Mathf.Max(yMax, v.position.y); }
            float h = yMax - yMin;
            if (h <= 0f) return;
            for (int i = 0; i < 4; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                v.position.y += LiftAt((v.position.y - yMin) / h) * h;
                vh.SetUIVertex(v, i);
            }
        }
    }
}
