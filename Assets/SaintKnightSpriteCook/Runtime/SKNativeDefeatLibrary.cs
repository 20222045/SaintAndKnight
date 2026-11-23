using System.Collections.Generic;
using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day10;
using UnityEngine;

namespace SaintKnight.SpriteCook
{
    /// <summary>Native game-art composites. Display only; owns no combat or retry clock.</summary>
    public sealed class SKNativeDefeatLibrary
    {
        public const int Width = 128, Height = 96, FrameCount = 18, LoopStart = 8;
        public const int CommonBossCount = 4, GrabDefeatFrames = 32;
        public const float FramesPerSecond = 8f;
        public const int DefeatSortingOrder = short.MaxValue - 7;
        public const string Folder = "SaintKnightSpriteCook/DefeatMotion/";
        private static readonly Dictionary<string, SKNativeDefeatLibrary> Cache = new Dictionary<string, SKNativeDefeatLibrary>();
        private Sprite[] frames;
        public int Length => frames.Length;
        public string Key { get; private set; }
        public Texture2D Atlas { get; private set; }
        public int FrameWidth { get; private set; }
        public int FrameHeight { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => Cache.Clear();

        public static bool IsBossPattern(string key) =>
            key == "boss_spikes" || key == "boss_grab" || key == "boss_spit";

        public static float BossPatternSeconds(string key) =>
            (key == "boss_grab" ? GrabDefeatFrames : FrameCount) / FramesPerSecond;

        public static SKNativeDefeatLibrary Load(int character, string key, bool defeat = true)
        {
            if (defeat && IsBossPattern(key))
            {
                var fatal = LoadSheet(character, key, "_defeat", key == "boss_grab" ? GrabDefeatFrames : FrameCount);
                if (fatal != null) return fatal;
            }
            return LoadHeld(character, key);
        }

        // The live struggle keeps its original HP-driven art, even when the death has a new choreography.
        public static SKNativeDefeatLibrary LoadHeld(int character, string key) => LoadSheet(character, key, "", FrameCount);

        private static SKNativeDefeatLibrary LoadSheet(int character, string key, string suffix, int count)
        {
            if (character < 0 || character > 1 || string.IsNullOrEmpty(key)) return null;
            string name = (character == 1 ? "adela_" : "cecilia_") + key + suffix;
            if (Cache.TryGetValue(name, out var found)) return found;
            var atlas = Resources.Load<Texture2D>(Folder + name);
            int width = key == "fleshwall" ? 160 : Width, height = key == "fleshwall" ? 112 : Height;
            if (atlas == null || atlas.width != width * count || atlas.height != height) return null;
            var library = new SKNativeDefeatLibrary { Key = key, Atlas = atlas, FrameWidth = width, FrameHeight = height, frames = new Sprite[count] };
            atlas.filterMode = FilterMode.Point; atlas.wrapMode = TextureWrapMode.Clamp;
            for (int i = 0; i < count; i++)
            {
                var sprite = Sprite.Create(atlas, new Rect(i * width, 0, width, height),
                    new Vector2(.5f, key == "fleshwall" ? 0f : (3f + .47f * 48f) / height), 48f, 0, SpriteMeshType.FullRect);
                sprite.name = name.Replace("_" + key, "_native_" + key) + "_f" + i.ToString("D2");
                library.frames[i] = sprite;
            }
            Cache.Add(name, library); return library;
        }

        public Sprite Sample(float seconds, int firstFrame = 0)
        {
            int step = Mathf.Max(0, Mathf.FloorToInt(seconds * FramesPerSecond)) + Mathf.Clamp(firstFrame, 0, Length - 1);
            if (step >= Length) step = LoopStart + (step - LoopStart) % (Length - LoopStart);
            return frames[step];
        }

        /// <summary>Alive captures progress with actual HP loss; resistance never drives damage.</summary>
        public Sprite Held(float lostHpFraction, float seconds, bool resisting)
        {
            int stage = Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(lostHpFraction) * LoopStart), 0, LoopStart - 1);
            if (Key == "boss_grab") stage = Mathf.Max(stage, Mathf.Clamp((int)(seconds * 8f), 0, 4));
            // Keep the direction/depth stable. A press adds only one existing neighbour pose.
            if (resisting && Mathf.FloorToInt(seconds * 10f) % 2 == 0) stage = Mathf.Max(0, stage - 1);
            return frames[stage];
        }

        /// <summary>Call at the accepted lethal hit, before the boss cancels its attack.</summary>
        public static string KeyFor(Object cause)
        {
            var c = cause as Component;
            if (c == null) return null;
            var boss = c.GetComponentInParent<SKBossAgent>();
            if (boss != null)
            {
                if (c.GetComponentInParent<SKFloorSpike>() != null) return "boss_spikes";
                if (c.GetComponentInParent<SKSlimePool>() != null) return "boss_spit";
                switch (boss.Attack)
                {
                    case SKBossAttack.Sweep: return null; // User: keep the sweep unchanged.
                    case SKBossAttack.Spikes: return "boss_spikes";
                    case SKBossAttack.Grab: return "boss_grab";
                    case SKBossAttack.Spit: return "boss_spit";
                    default: return "boss";
                }
            }
            if (c.GetComponentInParent<SKFleshWall>() != null) return "fleshwall";
            var capture = c.GetComponentInParent<SKEnvironmentalCapture>();
            if (capture != null)
            {
                if (capture.Kind == SKEnvironmentalCaptureKind.Tentacle) return "tentacle";
                var pool = capture.GetComponent<SKSlimePool>();
                return pool != null && !pool.Permanent ? "boss_spit" : null; // User: keep ordinary slime.
            }
            if (c.GetComponentInParent<SKFloorSpike>() != null) return "tentacle";
            var enemy = c.GetComponentInParent<SKEnemyAgent>();
            if (enemy == null) return null;
            string name = enemy.DisplayName.Trim();
            if (name.EndsWith("MONK", System.StringComparison.OrdinalIgnoreCase)) return "monk";
            if (name.EndsWith("EXECUTIONER", System.StringComparison.OrdinalIgnoreCase)) return "executioner";
            return null;
        }

        public static bool IsTrap(string key) => key == "tentacle" || key == "fleshwall" ||
            key == "boss_spikes" || key == "boss_grab" || key == "boss_spit";
    }

    /// <summary>Only the new defeat actor stays legible above the world's dim layer. Restores the prior order.</summary>
    public sealed class SKNativeDefeatSorting
    {
        private int layer, order;
        private Material material;
        private bool raised;
        public void Apply(SpriteRenderer renderer, bool active)
        {
            if (renderer == null || active == raised) return;
            if (active)
            {
                layer = renderer.sortingLayerID; order = renderer.sortingOrder;
                material = renderer.sharedMaterial;
                var presentation = Resources.Load<Material>(SKNativeDefeatLibrary.Folder + "DefeatPresentation");
                if (presentation != null) renderer.sharedMaterial = presentation;
                var layers = SortingLayer.layers;
                renderer.sortingLayerID = layers[layers.Length - 1].id;
                renderer.sortingOrder = SKNativeDefeatLibrary.DefeatSortingOrder;
            }
            else
            {
                renderer.sortingLayerID = layer; renderer.sortingOrder = order;
                renderer.sharedMaterial = material; material = null;
            }
            raised = active;
        }
    }
}
