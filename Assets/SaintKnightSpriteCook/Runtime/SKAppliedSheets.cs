using System.Collections.Generic;
using UnityEngine;

namespace SaintKnight.SpriteCook
{
    /// <summary>
    /// Display-only access to the applied 128x96 sheets in Resources/SaintKnightSpriteCook
    /// (boss pattern, trap reaction, enemy death, HP orb parts). A missing sheet returns null so
    /// every caller keeps its previous picture as the fallback.
    /// </summary>
    public static class SKAppliedSheets
    {
        private const int CellWidth = 128, CellHeight = 96;
        private const float PixelsPerUnit = 48f;
        private static readonly Vector2 CharacterPivot = new Vector2(.5f, (3f + .47f * PixelsPerUnit) / CellHeight);
        private static readonly Dictionary<string, Sprite[]> Cache = new Dictionary<string, Sprite[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ClearCache() => Cache.Clear();

        /// <summary>kind: boss_pattern (4x3), trap_reaction (3x3) or enemy_death (4x2).</summary>
        public static Sprite Character(int character, string kind, int cell)
            => Cell((character == 1 ? "adela_" : "cecilia_") + kind, cell, CharacterPivot);

        /// <summary>
        /// Scene sheet where every cell is used (death_monk, death_boss2 ...): plays through once,
        /// then keeps alternating its last two cells. Null when the sheet is missing.
        /// </summary>
        public static Sprite CharacterScene(int character, string kind, int step)
        {
            string sheet = (character == 1 ? "adela_" : "cecilia_") + kind;
            if (Cell(sheet, 0, CharacterPivot) == null) return null;
            var sprites = Cache[sheet];
            int n = sprites.Length;
            return sprites[step < n || n < 2 ? Mathf.Min(step, n - 1) : n - 2 + (step - n) % 2];
        }

        /// <summary>hp_orb_ornaments: 0 glass, 1 Cecilia ornament, 2 Adela ornament.</summary>
        public static Sprite Ui(string sheet, int cell) => Cell(sheet, cell, new Vector2(.5f, .5f));

        private static Sprite Cell(string sheet, int cell, Vector2 pivot)
        {
            if (!Cache.TryGetValue(sheet, out var sprites))
            {
                var atlas = Resources.Load<Texture2D>("SaintKnightSpriteCook/" + sheet);
                bool valid = atlas != null && atlas.width % CellWidth == 0 && atlas.height % CellHeight == 0;
                sprites = valid ? new Sprite[atlas.width / CellWidth * (atlas.height / CellHeight)] : null;
                if (valid)
                {
                    atlas.filterMode = FilterMode.Point;
                    int columns = atlas.width / CellWidth;
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        var rect = new Rect(i % columns * CellWidth, atlas.height - (i / columns + 1) * CellHeight,
                            CellWidth, CellHeight);
                        sprites[i] = Sprite.Create(atlas, rect, pivot, PixelsPerUnit, 0, SpriteMeshType.FullRect);
                        sprites[i].name = sheet + "_f" + i.ToString("D2");
                    }
                }
                Cache.Add(sheet, sprites);
            }
            return sprites != null && cell >= 0 && cell < sprites.Length ? sprites[cell] : null;
        }
    }
}
