using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day04;
using UnityEngine;

namespace SaintKnight.Day20
{
    /// <summary>
    /// Day 20 checkpoint: a votive candle stand. Walking past it lights it, makes it the place
    /// R / auto-retry returns to, and saves the run (Day 21). It does not heal (Day 15 regen does that).
    /// </summary>
    public sealed class SKCheckpoint : MonoBehaviour
    {
        [SerializeField, Min(.1f)] private float reach = .6f;
        private SKHealth player;
        private SKEnemyTrainingSession session;
        private SpriteRenderer stand, flame;
        private TextMesh label;
        private float labelUntil;

        public bool IsLit { get; private set; }
        public static System.Action<SKCheckpoint> Lit;

        private void Start()
        {
            if (stand != null) return;   // already built by LightQuietly
            session = FindAnyObjectByType<SKEnemyTrainingSession>();
            if (session != null) player = session.Player;
            if (player == null)
            {
                var p = FindAnyObjectByType<SKDay01Player>();
                if (p != null) player = p.GetComponent<SKHealth>();
            }
            stand = Pixel("Stand", Stand(), new Vector2(.5f, 0f), 6);
            flame = Pixel("Flame", Flame(), new Vector2(.5f, 0f), 7);
            flame.transform.localPosition = new Vector3(0f, 21f / 48f, 0f);
            label = new GameObject("Label").AddComponent<TextMesh>();
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 1.05f, 0f);
            label.characterSize = .035f;
            label.fontSize = 48;
            label.anchor = TextAnchor.MiddleCenter;
            label.color = new Color(.9f, .78f, .55f);
            var serif = SerifFont();   // 2026-11-16: same serif as the menus, not the default sans
            if (serif != null) { label.font = serif; label.GetComponent<MeshRenderer>().sharedMaterial = serif.material; }
            label.GetComponent<MeshRenderer>().sortingOrder = 32;
            SetLit(false);
        }

        private static Font serifFont;
        private static Font SerifFont()
        {
            if (serifFont != null) return serifFont;
            try
            {
                var installed = Font.GetOSInstalledFontNames();
                foreach (var n in new[] { "Palatino Linotype", "Book Antiqua", "Georgia" })
                    if (System.Array.IndexOf(installed, n) >= 0) return serifFont = Font.CreateDynamicFontFromOSFont(n, 48);
            }
            catch (System.Exception) { }
            return null;
        }

        /// <summary>Lights this stand without walking to it (scene carry-over / continue).</summary>
        public void LightQuietly() { if (stand == null) Start(); SetLit(true); }

        private void Update()
        {
            if (label != null) label.gameObject.SetActive(Time.time < labelUntil);
            if (IsLit && flame != null)
                flame.transform.localScale = new Vector3(1f, 1f + .12f * Mathf.Sin(Time.time * 9f), 1f);
            if (IsLit || player == null || player.IsDead) return;
            var p = player.transform.position;
            var c = transform.position;
            if (Mathf.Abs(p.x - c.x) > reach || Mathf.Abs(p.y - c.y - .5f) > 1.2f) return;
            SetLit(true);
            label.text = "- C H E C K P O I N T -";
            labelUntil = Time.time + 2f;
            var who = player.GetComponent<SKDay01Player>();
            var save = SKSave.Data;
            save.scene = gameObject.scene.name;
            save.checkpoint = name;
            save.character = who != null ? who.SelectedCharacter : save.character;
            SKSave.Write();
            Lit?.Invoke(this);
        }

        private void SetLit(bool lit)
        {
            IsLit = lit;
            if (lit && session != null) session.SetSpawn(RespawnPoint());
            if (flame != null) flame.enabled = lit;
            if (stand != null) stand.color = lit ? Color.white : new Color(.55f, .55f, .6f);
        }

        /// <summary>Player centre standing at this stand (the player's pivot is its body centre).</summary>
        public Vector3 RespawnPoint()
        {
            float half = player != null && player.TryGetComponent<BoxCollider2D>(out var box)
                ? box.size.y * .5f - box.offset.y : .55f;
            return new Vector3(transform.position.x, transform.position.y + half + .01f, 0f);
        }

        private SpriteRenderer Pixel(string n, Texture2D tex, Vector2 pivot, int order)
        {
            var r = new GameObject(n).AddComponent<SpriteRenderer>();
            r.transform.SetParent(transform, false);
            r.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 48f);
            r.sortingOrder = order;
            return r;
        }

        // ponytail: code-drawn placeholder pixels; swap for art in Resources when it exists.
        private static Texture2D Stand()
        {
            var t = Blank(11, 22);
            var iron = new Color32(52, 46, 50, 255);
            var lit = new Color32(96, 86, 84, 255);
            for (int x = 2; x <= 8; x++) { t.SetPixel(x, 0, iron); t.SetPixel(x, 1, iron); }   // foot
            for (int y = 2; y <= 17; y++) { t.SetPixel(5, y, iron); t.SetPixel(4, y, y % 4 == 0 ? lit : iron); }
            for (int x = 1; x <= 9; x++) t.SetPixel(x, 18, iron);                                 // dish
            for (int x = 3; x <= 7; x++) for (int y = 19; y <= 21; y++) t.SetPixel(x, y, new Color32(214, 204, 186, 255)); // candle
            t.Apply();
            return t;
        }

        private static Texture2D Flame()
        {
            var t = Blank(5, 7);
            var outer = new Color32(255, 150, 52, 255);
            var core = new Color32(255, 236, 170, 255);
            int[] widths = { 2, 2, 2, 1, 1, 0, 0 };
            for (int y = 0; y < 7; y++)
                for (int x = 2 - widths[y]; x <= 2 + widths[y]; x++)
                    t.SetPixel(x, y, Mathf.Abs(x - 2) < widths[y] && y < 4 ? core : outer);
            t.Apply();
            return t;
        }

        private static Texture2D Blank(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            t.SetPixels32(new Color32[w * h]);
            return t;
        }
    }
}
