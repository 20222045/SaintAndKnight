using SaintKnight.Day20;
using UnityEngine;

namespace SaintKnight.Day26
{
    /// <summary>
    /// Day 28 records and gallery. Entries unlock from play (first fall to each cause per character,
    /// the boss clear). Each entry shows Resources/SaintKnightGallery/Private/&lt;id&gt; when it exists:
    /// a Texture2D / Sprite (rendered 3D illustration) or a prefab (3D model, shown on a turntable).
    /// The Private folder is git-ignored (repository stays 15+); without art an unlocked entry says so.
    /// </summary>
    public static class SKGallery
    {
        public const string Folder = "SaintKnightGallery/Private/";
        public static readonly string[] Causes = { "MONK", "EXECUTIONER", "SLIME", "TENTACLE", "FLESHWALL", "BOSS" };

        public struct Entry { public string id, title; }

        public static Entry[] Entries
        {
            get
            {
                var list = new System.Collections.Generic.List<Entry> { new Entry { id = "boss_rose_madonna", title = "THE ROSE WINDOW MADONNA FALLS" } };
                foreach (var who in new[] { "cecilia", "adela" })
                    foreach (var c in Causes)
                        list.Add(new Entry { id = "death_" + who + "_" + c.ToLowerInvariant(), title = who.ToUpperInvariant() + "  -  " + c });
                return list.ToArray();
            }
        }

        public static string RecordsText()
        {
            var d = SKSave.Data;
            int t = Mathf.FloorToInt(d.playSeconds);
            int seen = 0;
            foreach (var e in Entries) if (SKSave.IsUnlocked(e.id)) seen++;
            return "PLAY TIME  " + t / 3600 + ":" + (t / 60 % 60).ToString("00") + ":" + (t % 60).ToString("00") +
                   "      FALLS  " + d.deaths + "      CLEARS  " + d.clears +
                   "\nGALLERY  " + seen + " / " + Entries.Length;
        }

        public static SKMenuPage RecordsPage(SKMenuView view)
        {
            var page = new SKMenuPage { Title = "RECORDS", Body = RecordsText };
            page.Items.Add(new SKMenuItem(() => "GALLERY", () => view.Push(GalleryPage(view))));
            page.Items.Add(new SKMenuItem(() => "BACK", view.Pop));
            return page;
        }

        public static SKMenuPage GalleryPage(SKMenuView view)
        {
            var entries = Entries;
            var page = new SKMenuPage { Title = "GALLERY", Body = () => "ENTER  VIEW      ESC  BACK" };
            for (int i = 0; i < entries.Length; i++)
            {
                int index = i;
                page.Items.Add(new SKMenuItem(
                    () => SKSave.IsUnlocked(entries[index].id) ? entries[index].title : "? ? ?",
                    () => Show(view, entries, index)) { Enabled = () => SKSave.IsUnlocked(entries[index].id) });
            }
            page.Items.Add(new SKMenuItem(() => "BACK", view.Pop));
            return page;
        }

        private static SKTurntable turntable;

        private static void Show(SKMenuView view, Entry[] entries, int index)
        {
            Clear();
            var e = entries[index];
            var art = Resources.Load<Object>(Folder + e.id);
            Texture tex = art as Texture2D;
            if (art is Sprite s) tex = s.texture;
            if (art is GameObject prefab) tex = (turntable = SKTurntable.Create(prefab)).Target;
            view.ShowPicture(tex, tex != null ? e.title : e.title + "  -  NO ILLUSTRATION YET");
            view.PictureHidden -= Clear;
            view.PictureHidden += Clear;
            view.PictureStep = dir =>
            {
                for (int k = 1; k <= entries.Length; k++)
                {
                    int next = (index + dir * k + entries.Length * k) % entries.Length;
                    if (SKSave.IsUnlocked(entries[next].id)) { Show(view, entries, next); return; }
                }
            };
        }

        private static void Clear()
        {
            if (turntable != null) Object.Destroy(turntable.gameObject);
            turntable = null;
        }
    }

    /// <summary>Shows a 3D gallery model far below the level into a RenderTexture, slowly turning.</summary>
    public sealed class SKTurntable : MonoBehaviour
    {
        public RenderTexture Target { get; private set; }
        private Transform model;

        public static SKTurntable Create(GameObject prefab)
        {
            var root = new GameObject("Gallery_Turntable").AddComponent<SKTurntable>();
            root.transform.position = new Vector3(0f, -1000f, 0f);
            root.model = Instantiate(prefab, root.transform).transform;
            root.model.localPosition = Vector3.zero;
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (var r in root.model.GetComponentsInChildren<Renderer>()) bounds.Encapsulate(r.bounds);
            root.Target = new RenderTexture(1280, 720, 24);
            var cam = new GameObject("Gallery_Camera").AddComponent<Camera>();
            cam.transform.SetParent(root.transform, false);
            float size = Mathf.Max(bounds.extents.magnitude, .5f);
            cam.transform.position = bounds.center + new Vector3(0f, size * .1f, -size * 2.6f);
            cam.transform.LookAt(bounds.center);
            cam.fieldOfView = 40f;
            cam.nearClipPlane = .05f;
            cam.farClipPlane = size * 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(.04f, .03f, .05f);
            cam.targetTexture = root.Target;
            var light = new GameObject("Gallery_Light").AddComponent<Light>();
            light.transform.SetParent(root.transform, false);
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);
            return root;
        }

        private void Update() => model.Rotate(0f, 20f * Time.unscaledDeltaTime, 0f, Space.World);

        private void OnDestroy() { if (Target != null) Target.Release(); }
    }
}
