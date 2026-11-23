using System;
using System.Collections.Generic;
using SaintKnight.Day26;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Rest
{
    /// <summary>A thing to walk up to in a shop room: a host to talk to, an object to touch, or a way deeper in.</summary>
    public sealed class SKShopSpot
    {
        public string Label;          // "[W] <Label>" when looked at
        public Vector2 At;            // on the room picture, 0..1 from the top-left
        public string Host;           // who you talk to (null = an object)
        public string Picture;        // the host's default look: shown on approach and again when the talk ends
        public Func<int, string[][]> Talk;   // (times talked this visit) -> lines {speaker or null, text, optional picture}
        public SKShopRoom Room;       // set = walking there opens another view (the gambling table)
        public Func<string> Use;      // set = arriving does this (the hot spring soak) and returns its note
    }

    public sealed class SKShopRoom
    {
        public string Picture;
        public SKShopSpot[] Spots;
    }

    public sealed class SKShopDef
    {
        public string Title, Subtitle, Establishing;
        public SKShopRoom Main;
    }

    /// <summary>
    /// 2026-11-22 (user): walking into a shop shows the place once, then a first-person view of the room.
    /// A/D look around, the spot in front of you lights up; hold W to walk toward it step by step (the view
    /// closes in with a head bob), hold S to walk back. Reaching it: a host's talk (her default look stays when
    /// the talk ends), an object's close look, or another place (the gambling table). Esc steps back, then leaves.
    /// The game is paused the whole time. Replaces the salon's right-hand choice list.
    /// </summary>
    public sealed class SKShop : MonoBehaviour
    {
        public enum Mode { Establishing, Room, Approach, Talk, Idle }
        public const float EstablishSeconds = 2.4f, ApproachSeconds = .35f, RoomZoom = 1.3f, CloseZoom = 2.4f, CharsPerSecond = 40f;
        public const float WalkSeconds = 1.4f;   // from where you stand to the spot, W held
        private const float PanSpeed = .32f, FocusReach = .12f, Steps = 4f, StepInSeconds = .45f;

        /// <summary>0 = standing where you entered this place, 1 = at the spot you walk to.</summary>
        public float Walked { get; private set; }

        public static SKShop Current { get; private set; }
        public static int ClosedFrame { get; private set; } = -1;

        public Mode State { get; private set; }
        public SKShopDef Def { get; private set; }
        public SKShopRoom Room => rooms.Count > 0 ? rooms[rooms.Count - 1] : null;
        public int Focused { get; private set; } = -1;
        public float Pan { get; private set; } = .5f;
        public Texture Shown => picture.texture;
        public string Speaker => lines != null && line < lines.Length ? lines[line][0] : null;
        public string Line => lines != null && line < lines.Length ? lines[line][1] : null;
        public string Note { get; private set; }

        private readonly List<SKShopRoom> rooms = new List<SKShopRoom>();
        private readonly Dictionary<SKShopSpot, int> talked = new Dictionary<SKShopSpot, int>();
        private readonly HashSet<string> bonded = new HashSet<string>();
        private SKShopSpot spot;
        private string[][] lines;
        private int line;
        private float stateAt, typed, previousScale;
        private int openedFrame;
        private Action left;

        private RawImage picture;
        private CanvasGroup titleGroup, boxGroup;
        private Text title, subtitle, prompt, nameTag, body, more;
        private readonly List<Image> markers = new List<Image>();

        public static SKShop Open(SKShopDef def, Action left = null)
        {
            if (Current != null || def == null) return Current;
            var s = new GameObject("Shop_" + def.Title).AddComponent<SKShop>();
            s.Def = def;
            s.left = left;
            s.previousScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            SKPause.IsPaused = true;
            s.openedFrame = Time.frameCount;   // the key that opened the shop must not also skip its first picture
            s.Build();
            s.rooms.Add(def.Main);
            s.Enter(Mode.Establishing);
            Current = s;
            return s;
        }

        /// <summary>Shop art: the Git-ignored Private picture, else the public pixel CG of the same name.</summary>
        public static Texture2D Art(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            var tex = Resources.Load<Texture2D>(SKTavern.Folder + name) ?? SKHotSpring.LoadClearArt(name);
            if (tex != null) tex.filterMode = tex.width > 640 ? FilterMode.Bilinear : FilterMode.Point;
            return tex;
        }

        // ---------- input (public for tests) ----------

        /// <summary>Walks toward the spot in front (positive) or back (negative); arriving opens it.</summary>
        public void Walk(float delta)
        {
            if (State != Mode.Room || Focused < 0 && Walked <= 0f) return;
            Walked = Mathf.Clamp01(Walked + delta);
            if (Walked >= 1f) Arrive();
        }

        public void Look(float delta)
        {
            if (Walked > 0f) return;   // turn only while standing
            float half = .5f / RoomZoom;
            Pan = Mathf.Clamp(Pan + delta, half, 1f - half);
        }

        public void Confirm()
        {
            switch (State)
            {
                case Mode.Establishing: Enter(Mode.Room); break;
                case Mode.Room:   // Enter: walk the rest of the way at once
                    if (Focused < 0) return;
                    Walked = 1f;
                    Arrive();
                    break;
                case Mode.Talk:
                    if (typed < Line.Length) { typed = Line.Length; return; }
                    if (line + 1 < lines.Length) { ShowLine(line + 1); return; }
                    EndTalk();
                    break;
                case Mode.Idle: StartTalk(); break;
            }
        }

        public void Back()
        {
            if (State == Mode.Talk || State == Mode.Idle || State == Mode.Approach) { Enter(Mode.Room); Walked = .5f; return; }   // a step back
            if (State == Mode.Room && Walked > 0f) { Walked = 0f; return; }
            if (State == Mode.Room && rooms.Count > 1) { rooms.RemoveAt(rooms.Count - 1); Pan = .5f; Walked = 0f; Enter(Mode.Room); return; }
            Close();
        }

        public void Close()
        {
            if (Current == this) Current = null;
            ClosedFrame = Time.frameCount;
            SKPause.IsPaused = false;
            Time.timeScale = previousScale;
            Destroy(gameObject);
            left?.Invoke();
        }

        /// <summary>Where you are looking across the whole picture, 0 (left edge) .. 1 (right edge).</summary>
        public float Gaze
        {
            get { float half = .5f / RoomZoom; return Mathf.InverseLerp(half, 1f - half, Pan); }
        }

        /// <summary>Turns the view to look at the spot (tests).</summary>
        public void FaceSpot(int index)
        {
            Walked = 0f;
            float half = .5f / RoomZoom;
            Pan = Mathf.Lerp(half, 1f - half, Room.Spots[index].At.x);
            Focused = FindFocus();
        }

        // ---------- flow ----------

        private void Enter(Mode mode)
        {
            State = mode;
            stateAt = Time.unscaledTime;
            if (mode == Mode.Establishing) picture.texture = Art(Def.Establishing) ?? Art(Def.Main.Picture);
            if (mode == Mode.Room) { lines = null; Note = null; picture.texture = Art(Room.Picture); Focused = FindFocus(); }
        }

        private void Arrive()
        {
            spot = Room.Spots[Focused];
            if (spot.Use != null) { string note = spot.Use(); Walked = 0f; Enter(Mode.Room); Note = note; return; }
            if (spot.Room != null) { rooms.Add(spot.Room); Pan = .5f; Walked = 0f; Enter(Mode.Room); return; }   // a new place
            stateAt = Time.unscaledTime - ApproachSeconds;   // already there: no extra zoom
            State = Mode.Approach;
            StartTalk();
        }

        private void StartTalk()
        {
            talked.TryGetValue(spot, out int n);
            talked[spot] = n + 1;
            lines = spot.Talk(n);
            Note = null;
            if (spot.Host != null) picture.texture = Art(spot.Picture) ?? picture.texture;
            State = Mode.Talk;
            ShowLine(0);
        }

        private void ShowLine(int i)
        {
            line = i;
            typed = 0f;
            var l = lines[i];
            if (l.Length > 2 && Art(l[2]) is Texture2D t) picture.texture = t;
            SKBustMorph.BounceAll(l[0] != null && l[0] == spot.Host ? 1f : .4f);
        }

        private void EndTalk()
        {
            if (spot.Host == null) { Enter(Mode.Room); return; }   // an object: back to looking around
            State = Mode.Idle;
            stateAt = Time.unscaledTime;
            // 2026-11-20 user: when the talk ends her default look stays on screen
            if (spot.Host != null) picture.texture = Art(spot.Picture) ?? picture.texture;
            Note = null;
            if (SKTavern.IsHost(spot.Host) && bonded.Add(spot.Host))
            {
                int b = SKTavern.AddBond(spot.Host);
                Note = b == SKTavern.FriendTalks ? spot.Host + "와(과) 친해졌다. 다음엔 온천에 같이 가자고 한다."
                    : b < SKTavern.FriendTalks ? spot.Host + "와(과) 조금 가까워졌다. (" + b + "/" + SKTavern.FriendTalks + ")" : null;
            }
        }

        private int FindFocus()
        {
            if (Room == null) return -1;
            if (Walked > 0f && Focused >= 0) return Focused;   // the spot you walk to stays chosen
            // the gaze sweeps the whole picture while the view pans, so spots at the edges can be faced too
            float gaze = Gaze;
            int best = -1;
            float bestD = FocusReach;
            for (int i = 0; i < Room.Spots.Length; i++)
            {
                float d = Mathf.Abs(Room.Spots[i].At.x - gaze);
                if (d <= bestD) { best = i; bestD = d; }
            }
            return best;
        }

        // the part of the picture on screen
        private Rect View()
        {
            float now = Time.unscaledTime;
            switch (State)
            {
                case Mode.Establishing:
                    float z = Mathf.Lerp(1.08f, 1f, Mathf.Clamp01((now - stateAt) / EstablishSeconds));   // slow push out
                    return Around(new Vector2(.5f, .5f), z);
                case Mode.Room:
                    // stepping into a place: a short push in; walking: close in on the spot with a head bob
                    float stepIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - stateAt) / StepInSeconds));
                    float zoom = Mathf.Lerp(RoomZoom * .88f, RoomZoom, stepIn);
                    var centre = new Vector2(Pan, .5f);
                    if (Walked > 0f && Focused >= 0)
                    {
                        float w = Mathf.SmoothStep(0f, 1f, Walked);
                        centre = Vector2.Lerp(centre, Room.Spots[Focused].At, w);
                        zoom = Mathf.Lerp(zoom, CloseZoom, w);
                        centre.y += Mathf.Abs(Mathf.Sin(Walked * Steps * Mathf.PI)) * .012f;   // a bob per step
                    }
                    return Around(centre, zoom);
                default:
                    if (spot == null || spot.Host != null) return new Rect(0f, 0f, 1f, 1f);
                    return Around(spot.At, CloseZoom);   // an object: looking at it up close
            }
        }

        private static Rect Around(Vector2 topLeftCentre, float zoom)
        {
            float w = 1f / zoom;
            float x = Mathf.Clamp(topLeftCentre.x - w * .5f, 0f, 1f - w);
            float y = Mathf.Clamp(1f - topLeftCentre.y - w * .5f, 0f, 1f - w);
            return new Rect(x, y, w, w);
        }

        private void Update()
        {
            float now = Time.unscaledTime;
            if (SKEpilogue.Playing || SKEpilogue.FinishedFrame == Time.frameCount) return;   // the soak film owns the keys
            if (State == Mode.Establishing && now - stateAt >= EstablishSeconds) Enter(Mode.Room);
            if (State == Mode.Talk) typed += Time.unscaledDeltaTime * CharsPerSecond;

            if (State == Mode.Room)
            {
                bool keysOk = Time.frameCount != openedFrame;
                float dir = (SKBindings.Held(SKAction.Right) ? 1f : 0f) - (SKBindings.Held(SKAction.Left) ? 1f : 0f);
                if (dir != 0f) Look(dir * PanSpeed * Time.unscaledDeltaTime);
                Focused = FindFocus();
                float step = (keysOk && SKBindings.Held(SKAction.Up) ? 1f : 0f) - (keysOk && SKBindings.Held(SKAction.Down) ? 1f : 0f);
                if (step != 0f) Walk(step * Time.unscaledDeltaTime / WalkSeconds);
            }
            bool keys = Time.frameCount != openedFrame, walking = State == Mode.Room;
            // in a place W / S walk (held, above); Enter still walks straight there, Esc steps back
            bool forward = keys && (SKKeys.Confirm || !walking && SKKeys.Up), back = keys && (SKKeys.Back || !walking && SKKeys.Down);
            if (back) Back();
            else if (forward) Confirm();
            if (this != null && Current == this) Draw(now);
        }

        // ---------- drawing ----------

        private void Draw(float now)
        {
            var view = View();
            picture.uvRect = view;
            bool room = State == Mode.Room;
            titleGroup.alpha = State == Mode.Establishing ? Mathf.Clamp01((now - stateAt) / .5f) * Mathf.Clamp01((EstablishSeconds - (now - stateAt)) / .4f) : 0f;

            for (int i = 0; i < markers.Count; i++)
            {
                bool on = room && Room != null && i < Room.Spots.Length && (Walked <= 0f || i == Focused);
                markers[i].enabled = on;
                if (!on) continue;
                var at = Room.Spots[i].At;
                var r = markers[i].rectTransform;
                r.anchorMin = r.anchorMax = new Vector2((at.x - view.x) / view.width, (1f - at.y - view.y) / view.height);
                bool f = i == Focused;
                float pulse = .5f + .5f * Mathf.Sin(now * 3f + i);
                markers[i].sprite = f ? SKUi.DiamondOn : SKUi.Diamond;
                markers[i].color = f ? SKUi.Focus : new Color(1f, .9f, .7f, .35f + .3f * pulse);
                r.localScale = Vector3.one * (f ? 1.2f + .1f * pulse : .8f);
            }

#if ENABLE_INPUT_SYSTEM
            string key = SKBindings.Name(SKBindings.Primary(SKAction.Up));
#else
            string key = "W";
#endif
            prompt.text = room
                ? (Focused >= 0 ? "[" + key + "] 걸어간다  ▸  " + Room.Spots[Focused].Label + "        " : "")
                  + (Walked > 0f ? "[S] 뒤로 걷는다    " : "[A / D] 둘러보기    ") + "[Esc] " + (rooms.Count > 1 ? "물러난다" : "나간다")
                : State == Mode.Idle ? "[" + key + "]  다시 말을 건다        [Esc] 물러난다" : "";

            bool box = State == Mode.Talk || (State == Mode.Idle || room) && Note != null;
            boxGroup.alpha = box ? 1f : 0f;
            if (State == Mode.Talk)
            {
                nameTag.text = Speaker ?? "";
                body.text = Line.Substring(0, Mathf.Min(Line.Length, (int)typed));
                more.enabled = typed >= Line.Length && Mathf.Repeat(now, .8f) < .5f;
            }
            else if (Note != null)
            {
                nameTag.text = "";
                body.text = "<color=#e8c27a>" + Note + "</color>";
                more.enabled = false;
            }
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;   // over menus (100), under the epilogue (120)
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;

            Stretch(Black("Back", transform, Color.black));
            picture = new GameObject("Picture", typeof(RectTransform)).AddComponent<RawImage>();
            picture.transform.SetParent(transform, false);
            picture.raycastTarget = false;
            Stretch(picture.rectTransform);
            SKBustMorph.Attach(picture);
            SKDustMotes.Attach(picture);
            SKHotSpringScene.Attach(picture);   // steam and water sound on spring pictures only
            var vignette = new GameObject("Vignette", typeof(RectTransform)).AddComponent<RawImage>();
            vignette.transform.SetParent(transform, false);
            vignette.texture = SKUi.Vignette();
            vignette.color = new Color(1f, 1f, 1f, .55f);
            vignette.raycastTarget = false;
            Stretch(vignette.rectTransform);

            for (int i = 0; i < 8; i++)
            {
                var m = SKUi.Picture("Spot" + i, transform, SKUi.Diamond, new Vector2(.5f, .5f), Vector2.zero, new Vector2(22f, 22f));
                markers.Add(m);
            }

            // establishing title: place name large, a small line under it
            var tg = SKUi.Rect("Title", transform, new Vector2(.5f, .5f), new Vector2(0f, -40f), new Vector2(900f, 200f));
            titleGroup = tg.gameObject.AddComponent<CanvasGroup>();
            title = SKUi.Label("Name", tg, Def.Title, 54, new Vector2(.5f, .6f), Vector2.zero, new Vector2(900f, 70f), TextAnchor.MiddleCenter, SKUi.Hangul);
            title.color = new Color(1f, .9f, .74f);
            title.gameObject.AddComponent<Outline>().effectColor = new Color(0f, 0f, 0f, .8f);
            subtitle = SKUi.Label("Sub", tg, Def.Subtitle, 20, new Vector2(.5f, .3f), Vector2.zero, new Vector2(900f, 30f), TextAnchor.MiddleCenter, SKUi.Hangul);
            subtitle.color = new Color(.85f, .78f, .7f);

            prompt = SKUi.Label("Prompt", transform, "", 18, new Vector2(.5f, 0f), new Vector2(0f, 22f), new Vector2(1200f, 26f), TextAnchor.MiddleCenter, SKUi.Hangul);
            prompt.rectTransform.pivot = new Vector2(.5f, 0f);
            prompt.color = new Color(1f, .93f, .8f);
            prompt.gameObject.AddComponent<Outline>().effectColor = Color.black;

            // talk box at the bottom: dark glass, thin gold line, name above its top-left
            var b = SKUi.Rect("TalkBox", transform, new Vector2(.5f, 0f), new Vector2(0f, 54f), new Vector2(1040f, 136f), new Vector2(.5f, 0f));
            boxGroup = b.gameObject.AddComponent<CanvasGroup>();
            Stretch(Black("Line", b, new Color(.8f, .66f, .42f, .7f)), -1f);
            Stretch(Black("Glass", b, new Color(.04f, .03f, .04f, .9f)));
            nameTag = SKUi.Label("Name", b, "", 22, new Vector2(0f, 1f), new Vector2(18f, 4f), new Vector2(400f, 30f), TextAnchor.LowerLeft, SKUi.Hangul);
            nameTag.rectTransform.pivot = Vector2.zero;
            nameTag.color = new Color(.91f, .76f, .48f);
            nameTag.gameObject.AddComponent<Outline>().effectColor = Color.black;
            body = SKUi.Label("Body", b, "", 23, new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(984f, 100f), TextAnchor.UpperLeft, SKUi.Hangul);
            body.rectTransform.pivot = new Vector2(0f, 1f);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.supportRichText = true;
            body.color = new Color(.95f, .92f, .88f);
            more = SKUi.Label("More", b, "▼", 16, new Vector2(1f, 0f), new Vector2(-22f, 14f), new Vector2(20f, 20f), TextAnchor.MiddleCenter, SKUi.Hangul);
            more.color = new Color(.91f, .76f, .48f);
        }

        private static Image Black(string name, Transform parent, Color c)
        {
            var img = SKUi.Picture(name, parent, SKUi.White, Vector2.zero, Vector2.zero, Vector2.zero);
            img.color = c;
            return img;
        }

        private static RectTransform Stretch(Graphic g, float inset = 0f) => Stretch(g.rectTransform, inset);
        private static RectTransform Stretch(RectTransform r, float inset = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
            return r;
        }

        private void OnDestroy()
        {
            if (Current != this) return;
            Current = null;
            SKPause.IsPaused = false;
            Time.timeScale = previousScale;
        }
    }
}
