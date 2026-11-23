using System;
using SaintKnight.Day20;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Day26
{
    public enum SKAction { Left, Right, Up, Down, Jump, Attack, Switch, Retry, Pause }

    /// <summary>
    /// Key configuration (2026-11-16). Each action has one rebindable key (saved) and one fixed
    /// alternate (arrows / P). Every gameplay and menu key read goes through here.
    /// </summary>
    public static class SKBindings
    {
        public static readonly SKAction[] Actions = (SKAction[])Enum.GetValues(typeof(SKAction));

#if ENABLE_INPUT_SYSTEM
        private static readonly Key[] Defaults = { Key.A, Key.D, Key.W, Key.S, Key.Space, Key.J, Key.Tab, Key.R, Key.Escape };
        private static readonly Key[] Fixed = { Key.LeftArrow, Key.RightArrow, Key.UpArrow, Key.DownArrow, Key.None, Key.None, Key.None, Key.None, Key.P };
        private static Key[] keys;

        private static Key[] Keys
        {
            get
            {
                if (keys != null) return keys;
                keys = (Key[])Defaults.Clone();
                foreach (var entry in SKSave.Data.keys ?? new string[0])
                {
                    var parts = entry.Split('=');
                    if (parts.Length == 2 && Enum.TryParse(parts[0], out SKAction a) && Enum.TryParse(parts[1], out Key k) && k != Key.None)
                        keys[(int)a] = k;
                }
                return keys;
            }
        }

        public static Key Primary(SKAction a) => Keys[(int)a];
        public static Key Alternate(SKAction a) => Fixed[(int)a];

        private static bool Check(Key k, bool held)
        {
            var kb = Keyboard.current;
            if (kb == null || k == Key.None) return false;
            var control = kb[k];
            return held ? control.isPressed : control.wasPressedThisFrame;
        }

        public static bool Pressed(SKAction a) => Check(Primary(a), false) || Check(Alternate(a), false);
        public static bool Held(SKAction a) => Check(Primary(a), true) || Check(Alternate(a), true);

        /// <summary>The key pressed this frame (for rebinding), or Key.None.</summary>
        public static Key AnyPressed()
        {
            var kb = Keyboard.current;
            if (kb == null) return Key.None;
            foreach (var control in kb.allKeys)
                if (control != null && control.wasPressedThisFrame) return control.keyCode;
            return Key.None;
        }

        /// <summary>Assigns <paramref name="key"/>; an action already using it takes this action's old key.</summary>
        public static void Rebind(SKAction a, Key key)
        {
            if (key == Key.None) return;
            var k = Keys;
            int other = Array.IndexOf(k, key);
            if (other >= 0 && other != (int)a) k[other] = k[(int)a];
            k[(int)a] = key;
            Store();
        }

        public static void ResetToDefaults()
        {
            keys = (Key[])Defaults.Clone();
            Store();
        }

        private static void Store()
        {
            var list = new string[Actions.Length];
            for (int i = 0; i < list.Length; i++) list[i] = Actions[i] + "=" + keys[i];
            SKSave.Data.keys = list;
            SKSave.Write();
        }

        public static string Name(Key k) => k == Key.None ? "-" : k switch
        {
            Key.LeftArrow => "LEFT ARROW", Key.RightArrow => "RIGHT ARROW", Key.UpArrow => "UP ARROW", Key.DownArrow => "DOWN ARROW",
            _ => k.ToString().ToUpperInvariant()
        };
#else
        public static bool Pressed(SKAction a) => false;
        public static bool Held(SKAction a) => false;
        public static void ResetToDefaults() { }
#endif

        /// <summary>Forget the cache (the save was replaced).</summary>
        public static void Reload()
        {
#if ENABLE_INPUT_SYSTEM
            keys = null;
#endif
        }
    }
}
