using System;
using System.Collections.Generic;
using UnityEngine;

namespace SaintKnight.Day06
{
    public enum SKSpriteMotion
    {
        Idle, Run, Windup, Attack, FollowThrough, Hurt, Jump, Pounced, Held, Resist, Escape, Crouch,
        // Day 14 trap reactions (display only).
        SpikeHurt, SlimeWalk, SlimeIdle
    }

    /// <summary>Reads the native sprite atlas and timed display frames without driving gameplay.</summary>
    internal sealed class SKCharacterMotionLibrary
    {
        private const string ResourceFolder = "SaintKnightDay06/";
        private const int Columns = 12;
        private const int CellWidth = 128;
        private const int CellHeight = 96;
        private const float PixelsPerUnit = 48f;
        // The 3px transparent margin and the player's 0.47-unit collider half-height.
        private static readonly Vector2 Pivot = new Vector2(.5f, (3f + .47f * PixelsPerUnit) / CellHeight);
        private static readonly Dictionary<string, SKCharacterMotionLibrary> Cache =
            new Dictionary<string, SKCharacterMotionLibrary>();

        [Serializable]
        private sealed class Frame
        {
            public int cell = 0;
            public int ms = 0;
        }

        [Serializable]
        private sealed class ClipData
        {
            public string motion = null;
            public Frame[] frames = null;
        }

        [Serializable]
        private sealed class Manifest
        {
            public ClipData[] clips = null;
        }

        private sealed class Clip
        {
            internal readonly Frame[] Frames;
            internal readonly Sprite[] Sprites;
            internal readonly int Milliseconds;
            internal readonly string SpritePrefix;

            internal Clip(string name, SKSpriteMotion motion, Frame[] frames)
            {
                Frames = frames;
                Sprites = new Sprite[frames.Length];
                foreach (var frame in frames) Milliseconds += frame.ms;
                SpritePrefix = name + "_" + motion.ToString().ToLowerInvariant() + "_f";
            }
        }

        private readonly Texture2D atlas;
        private readonly Dictionary<SKSpriteMotion, Clip> clips;

        private SKCharacterMotionLibrary(Texture2D atlas, Dictionary<SKSpriteMotion, Clip> clips)
        {
            this.atlas = atlas;
            this.clips = clips;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => Cache.Clear();

        internal static SKCharacterMotionLibrary Load(string name)
        {
            if (Cache.TryGetValue(name, out var library)) return library;
            string path = ResourceFolder + name + "_motion";
            var texture = Resources.Load<Texture2D>(path);
            var manifest = Resources.Load<TextAsset>(path + "_frames");
            if (texture == null || manifest == null) return null;

            var data = JsonUtility.FromJson<Manifest>(manifest.text);
            if (texture.width != Columns * CellWidth || texture.height % CellHeight != 0 || data?.clips == null)
                return null;

            int cellCount = Columns * texture.height / CellHeight;
            var clips = new Dictionary<SKSpriteMotion, Clip>();
            foreach (var entry in data.clips)
            {
                if (entry == null || !Enum.TryParse(entry.motion, out SKSpriteMotion motion) ||
                    clips.ContainsKey(motion) || entry.frames == null || entry.frames.Length == 0)
                    return null;
                foreach (var frame in entry.frames)
                    if (frame == null || frame.cell < 0 || frame.cell >= cellCount || frame.ms <= 0)
                        return null;
                clips.Add(motion, new Clip(name, motion, entry.frames));
            }
            if (clips.Count != Enum.GetValues(typeof(SKSpriteMotion)).Length) return null;
            library = new SKCharacterMotionLibrary(texture, clips);
            Cache.Add(name, library);
            return library;
        }

        internal float Duration(SKSpriteMotion motion) => clips.TryGetValue(motion, out var clip)
            ? clip.Milliseconds * .001f : 0f;

        internal Sprite Sample(SKSpriteMotion motion, float seconds, bool loop = false)
        {
            if (!clips.TryGetValue(motion, out var clip)) return null;
            float elapsed = Mathf.Max(0f, seconds * 1000f);
            if (loop) elapsed = Mathf.Repeat(elapsed, clip.Milliseconds);
            int frame = 0;
            while (frame < clip.Frames.Length - 1 && elapsed >= clip.Frames[frame].ms)
            {
                elapsed -= clip.Frames[frame].ms;
                frame++;
            }

            if (clip.Sprites[frame] == null)
            {
                int cell = clip.Frames[frame].cell;
                var rect = new Rect(cell % Columns * CellWidth,
                    atlas.height - (cell / Columns + 1) * CellHeight, CellWidth, CellHeight);
                var sprite = Sprite.Create(atlas, rect, Pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                sprite.name = clip.SpritePrefix + frame.ToString("D2");
                clip.Sprites[frame] = sprite;
            }
            return clip.Sprites[frame];
        }
    }
}
