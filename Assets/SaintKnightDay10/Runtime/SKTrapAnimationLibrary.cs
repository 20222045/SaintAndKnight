using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaintKnight.Day10
{
    public enum SKTrapMotionKind { Slime, Tentacle, Wall, BossSlime }
    public enum SKTrapMotionPhase { Swallow, Resist, Escape, Consumed }

    /// <summary>Display-only native trap composites. No input, damage, capture or escape decisions.</summary>
    public sealed class SKTrapAnimationLibrary
    {
        public const float ResistSeconds = .36f, EscapeSeconds = .30f, ConsumedFrameSeconds = .18f;
        private const int Columns = 8, Rows = 3, UsedFrames = 23;
        private static readonly Dictionary<string, SKTrapAnimationLibrary> Cache =
            new Dictionary<string, SKTrapAnimationLibrary>();

        [Serializable]
        private sealed class Manifest
        {
            public int frameWidth = 0, frameHeight = 0, columns = 0, rows = 0, usedFrames = 0;
            public float ppu = 0f, pivotX = 0f, pivotY = 0f;
        }

        private readonly Texture2D atlas;
        private readonly Manifest data;
        private readonly string character;
        private readonly SKTrapMotionKind kind;
        private readonly Sprite[] sprites = new Sprite[UsedFrames];

        private SKTrapAnimationLibrary(Texture2D atlas, Manifest data, string character, SKTrapMotionKind kind)
        { this.atlas = atlas; this.data = data; this.character = character; this.kind = kind; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => Cache.Clear();

        public static SKTrapAnimationLibrary Load(int characterIndex, SKTrapMotionKind kind)
        {
            if (characterIndex < 0 || characterIndex > 1) return null;
            string character = characterIndex == 0 ? "cecilia" : "adela";
            string key = character + "_" + kind.ToString().ToLowerInvariant();
            if (Cache.TryGetValue(key, out var library)) return library;
            string path = "SaintKnightDay10/TrapMotion/" + key;
            var atlas = Resources.Load<Texture2D>(path);
            var manifest = Resources.Load<TextAsset>(path);
            if (atlas == null || manifest == null) return null; // Keep the existing presentation as fallback.
            Manifest data;
            try { data = JsonUtility.FromJson<Manifest>(manifest.text); }
            catch (ArgumentException) { return null; }
            int width = kind == SKTrapMotionKind.Wall ? 160 : 128;
            int height = kind == SKTrapMotionKind.Wall ? 112 : 96;
            float pivotY = kind == SKTrapMotionKind.Wall ? 0f : (3f + .47f * 48f) / 96f;
            if (data == null || data.frameWidth != width || data.frameHeight != height ||
                data.columns != Columns || data.rows != Rows || data.usedFrames != UsedFrames ||
                !Mathf.Approximately(data.ppu, 48f) || !Mathf.Approximately(data.pivotX, .5f) ||
                !Mathf.Approximately(data.pivotY, pivotY) ||
                atlas.width != width * Columns || atlas.height != height * Rows) return null;
            atlas.filterMode = FilterMode.Point;
            atlas.wrapMode = TextureWrapMode.Clamp;
            library = new SKTrapAnimationLibrary(atlas, data, character, kind);
            Cache.Add(key, library);
            return library;
        }

        /// <summary>
        /// Sheet for a live capture. A timed (boss-spat) pool uses the rose slime poured from above;
        /// without that sheet it falls back to the corridor slime.
        /// </summary>
        public static SKTrapAnimationLibrary For(int characterIndex, SKEnvironmentalCapture capture)
        {
            if (capture == null) return null;
            if (capture.Kind != SKEnvironmentalCaptureKind.Slime) return Load(characterIndex, SKTrapMotionKind.Tentacle);
            if (capture.TryGetComponent<SKSlimePool>(out var pool) && !pool.Permanent)
            {
                var boss = Load(characterIndex, SKTrapMotionKind.BossSlime);
                if (boss != null) return boss;
            }
            return Load(characterIndex, SKTrapMotionKind.Slime);
        }

        public static int StageFor(float progress) => Mathf.Clamp(Mathf.FloorToInt(Mathf.Clamp01(progress) * 6f), 0, 5);
        public bool Owns(Sprite sprite) => sprite != null && sprite.texture == atlas;

        public Sprite Sample(SKTrapMotionPhase phase, float progress, float seconds)
        {
            int stage = StageFor(progress), frame, cell;
            seconds = Mathf.Max(0f, seconds);
            switch (phase)
            {
                case SKTrapMotionPhase.Resist:
                    frame = Mathf.FloorToInt(seconds / .09f) % 2;
                    cell = 6 + stage * 2 + frame;
                    break;
                case SKTrapMotionPhase.Escape:
                    frame = Mathf.Min(2, Mathf.FloorToInt(seconds / .10f));
                    cell = 18 + frame;
                    break;
                case SKTrapMotionPhase.Consumed:
                    frame = Mathf.FloorToInt(seconds / ConsumedFrameSeconds) % 2;
                    cell = 21 + frame;
                    break;
                default:
                    frame = stage;
                    cell = stage;
                    break;
            }
            if (sprites[cell] == null)
            {
                var rect = new Rect(cell % Columns * data.frameWidth,
                    atlas.height - (cell / Columns + 1) * data.frameHeight, data.frameWidth, data.frameHeight);
                sprites[cell] = Sprite.Create(atlas, rect, new Vector2(data.pivotX, data.pivotY), data.ppu,
                    0, SpriteMeshType.FullRect);
                string trap = kind.ToString().ToLowerInvariant();
                string name = character + "_" + trap + "_" + phase.ToString().ToLowerInvariant();
                if (phase == SKTrapMotionPhase.Resist) name += "_stage" + stage;
                sprites[cell].name = name + "_f" + frame.ToString("D2");
            }
            return sprites[cell];
        }
    }

    /// <summary>Visual depth eases back after resistance; the authoritative four-second deadline is untouched.</summary>
    public sealed class SKTrapAnimationPlayback
    {
        private bool initialized;
        private float visualProgress, lastUpdate, lastResistance = float.NegativeInfinity, resistedDepth;
        public float VisualProgress => visualProgress;

        public void Reset()
        { initialized = false; visualProgress = 0f; lastResistance = float.NegativeInfinity; }

        public Sprite Held(SKTrapAnimationLibrary library, float progress, float struggleAt, float now)
        {
            if (library == null) return null;
            if (!initialized)
            { initialized = true; visualProgress = progress; lastUpdate = now; }
            if (struggleAt > lastResistance)
            { resistedDepth = visualProgress; lastResistance = struggleAt; }
            float age = now - struggleAt;
            bool resisting = age >= 0f && age < SKTrapAnimationLibrary.ResistSeconds;
            if (!resisting)
                visualProgress = Mathf.MoveTowards(visualProgress, progress, Mathf.Max(0f, now - lastUpdate) * .75f);
            lastUpdate = now;
            return library.Sample(resisting ? SKTrapMotionPhase.Resist : SKTrapMotionPhase.Swallow,
                resisting ? resistedDepth : visualProgress, resisting ? age : now);
        }
    }
}
