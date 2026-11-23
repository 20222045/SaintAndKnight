using System.IO;
using UnityEngine;

namespace SaintKnight.Day20
{
    /// <summary>One save slot (Day 21). Written as JSON next to the player's other Unity data.</summary>
    [System.Serializable]
    public sealed class SKSaveData
    {
        public int version = 1;
        public int character;              // 0 Cecilia, 1 Adela
        public string scene = "";          // checkpoint scene; empty = no run started
        public string checkpoint = "";     // checkpoint object name in that scene
        public bool bossDefeated;
        public int clears;
        public float playSeconds;
        public int deaths;
        public string[] deathKeys = new string[0];   // "<character>_<CAUSE>" seen at least once (records / gallery)
        public string[] unlocked = new string[0];    // gallery entry ids
        public int hotSpringVisits;
        public bool hotSpringEpilogueSeen;   // the first-visit epilogue after the first boss clear
        public bool returnRevealPending;    // the first boss clear happened, but its sanctuary scene is unseen
        public int tavernVisits;             // the village tavern (first visit = welcome scene)
        public string[] salonBonds = new string[0];   // one "<host>" per salon visit her talk was heard to the end (2026-11-19)
        public int springGuestsJoined;               // bit per salon host who already walked into the hot spring
        public bool lobbyTalkSeen, clearTalkSeen;   // over-the-head conversations in the sanctuary (2026-11-19)
        // settings (Day 26)
        public float volume = 1f;          // BGM
        public float voice = 1f, se = 1f;
        public bool fullscreen = true;
        public int windowScale = 1;        // windowed: 1 = 1280x720, 2 = 1920x1080
        public string[] keys = new string[0];   // "Action=Key" overrides (SKBindings)
    }

    /// <summary>
    /// Save file and the hand-off between scenes. Display/flow only; HP and combat stay with their owners.
    /// </summary>
    public static class SKSave
    {
        private static SKSaveData data;

        /// <summary>Batch runs (tests, builds) write a separate file so they never touch the player's save.</summary>
        public static string FilePath => Path.Combine(Application.persistentDataPath,
            Application.isBatchMode ? "save_batch.json" : "save.json");

        public static SKSaveData Data => data ??= Read();
        public static bool HasRun => !string.IsNullOrEmpty(Data.scene);

        private static SKSaveData Read()
        {
            try
            {
                if (File.Exists(FilePath))
                    return JsonUtility.FromJson<SKSaveData>(File.ReadAllText(FilePath)) ?? new SKSaveData();
            }
            catch (System.Exception e) { Debug.LogWarning("Save file unreadable, starting fresh: " + e.Message); }
            return new SKSaveData();
        }

        public static void Write()
        {
            try
            {
                string tmp = FilePath + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(Data, true));
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(tmp, FilePath);
            }
            catch (System.Exception e) { Debug.LogWarning("Save failed: " + e.Message); }
        }

        /// <summary>New game: keeps settings and records, forgets the run position.</summary>
        public static void StartNewRun()
        {
            Data.scene = Data.checkpoint = "";
            Data.bossDefeated = false;
            Write();
        }

        /// <summary>Forget everything (tests, "delete save").</summary>
        public static void ResetAll(bool keepSettings = true)
        {
            var old = Data;
            data = new SKSaveData();
            if (keepSettings)
            {
                data.volume = old.volume; data.voice = old.voice; data.se = old.se;
                data.fullscreen = old.fullscreen; data.windowScale = old.windowScale; data.keys = old.keys;
            }
            Write();
            SaintKnight.Day26.SKBindings.Reload();
        }

        public static void Unlock(string id)
        {
            if (string.IsNullOrEmpty(id) || System.Array.IndexOf(Data.unlocked, id) >= 0) return;
            Data.unlocked = Append(Data.unlocked, id);
        }

        public static bool IsUnlocked(string id) => System.Array.IndexOf(Data.unlocked, id) >= 0;

        public static void RecordDeath(int character, string cause)
        {
            Data.deaths++;
            string key = (character == 0 ? "cecilia_" : "adela_") + cause;
            if (System.Array.IndexOf(Data.deathKeys, key) < 0) Data.deathKeys = Append(Data.deathKeys, key);
            Unlock("death_" + key.ToLowerInvariant());
            Write();
        }

        private static string[] Append(string[] list, string item)
        {
            var next = new string[list.Length + 1];
            list.CopyTo(next, 0);
            next[list.Length] = item;
            return next;
        }
    }

    /// <summary>
    /// State handed from one scene to the next (character and HP). Only set by a door / menu right
    /// before it loads a scene, and consumed by the next scene's director, so scenes loaded any other
    /// way (tests, editor) start exactly as they did before Day 20.
    /// </summary>
    public static class SKCarry
    {
        public static bool Pending { get; private set; }
        public static int Character { get; private set; }
        public static float Hp { get; private set; } = -1f;   // < 0 = full
        public static string Checkpoint { get; private set; } = "";

        public static void Set(int character, float hp, string checkpoint = "")
        {
            Pending = true;
            Character = character;
            Hp = hp;
            Checkpoint = checkpoint ?? "";
        }

        public static void Clear()
        {
            Pending = false;
            Hp = -1f;
            Checkpoint = "";
        }
    }
}
