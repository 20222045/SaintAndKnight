using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace SaintKnight.Art
{
    /// <summary>
    /// Per-stage colour grade (2026-11-19, art direction from the user's INARI reference): each area leans
    /// into one hue family, lights glow softly and the frame edges sink into shadow. Added at scene load as a
    /// global URP volume built in code, so no scene file changes. Scenes not listed are left untouched.
    /// </summary>
    public static class SKAtmosphere
    {
        public struct Mood
        {
            public Color filter, shadows, highlights;
            public float exposure, contrast, saturation, bloom, vignette;
        }

        // ponytail: hand-tuned per scene; move to a ScriptableObject if artists start tuning many stages.
        public static readonly System.Collections.Generic.Dictionary<string, Mood> Moods = new System.Collections.Generic.Dictionary<string, Mood>
        {
            // sanctuary: candle amber, the last warm place
            { "Lobby_Sanctuary", new Mood { filter = new Color(1f, .94f, .86f), shadows = new Color(.62f, .48f, .56f), highlights = new Color(1f, .8f, .55f),
                exposure = .1f, contrast = 8f, saturation = -5f, bloom = .5f, vignette = .25f } },
            // collapsed cathedral: rust red and ember orange
            { "Day09_Stage1", new Mood { filter = new Color(1f, .9f, .84f), shadows = new Color(.78f, .4f, .36f), highlights = new Color(1f, .72f, .48f),
                exposure = .3f, contrast = 6f, saturation = -5f, bloom = .6f, vignette = .28f } },
            // Madonna's chapel: deep crimson under the rose window
            { "Day09_BossChapel", new Mood { filter = new Color(1f, .86f, .86f), shadows = new Color(.8f, .32f, .38f), highlights = new Color(1f, .62f, .52f),
                exposure = .2f, contrast = 8f, saturation = -5f, bloom = .7f, vignette = .3f } },
        };

        public static Volume Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            SceneManager.sceneLoaded -= OnLoaded;
            SceneManager.sceneLoaded += OnLoaded;
            OnLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private static void OnLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || !Moods.TryGetValue(scene.name, out var mood)) return;
            Apply(mood);
        }

        public static Volume Apply(Mood m)
        {
            if (Active != null) Object.Destroy(Active.gameObject);
            var cam = Camera.main;
            if (cam != null) cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(m.exposure);
            color.contrast.Override(m.contrast);
            color.saturation.Override(m.saturation);
            color.colorFilter.Override(m.filter);
            var split = profile.Add<SplitToning>(true);
            split.shadows.Override(m.shadows);
            split.highlights.Override(m.highlights);
            split.balance.Override(-10f);
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.Override(.85f);
            bloom.intensity.Override(m.bloom);
            bloom.scatter.Override(.6f);
            bloom.tint.Override(m.highlights);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(m.vignette);
            vig.smoothness.Override(.45f);
            vig.color.Override(Color.black);

            var go = new GameObject("SK_Atmosphere");
            Active = go.AddComponent<Volume>();
            Active.isGlobal = true;
            Active.priority = 10f;
            Active.sharedProfile = profile;
            return Active;
        }
    }
}
