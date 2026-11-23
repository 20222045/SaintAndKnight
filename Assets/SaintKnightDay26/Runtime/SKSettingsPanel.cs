using System;
using System.Collections.Generic;
using SaintKnight.Day20;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Day26
{
    /// <summary>
    /// One-screen settings board. 2026-11-16 restyle (user: "too close to the reference, change the style"):
    /// a framed gothic reliquary panel in the middle of a darkened screen, Roman-numeral sections
    /// (I Sound / II Display left, III Controls right), ten-pip volume bars, ring toggles, corner-bracket
    /// buttons, and a crimson bar under the focused row. The leave / quit buttons sit at the panel's foot.
    /// Keyboard only: W/S (or arrows) move, A/D change sliders, Enter picks, Esc closes.
    /// </summary>
    public sealed class SKSettingsPanel : MonoBehaviour
    {
        private sealed class Item
        {
            public string Id;
            public Text Text;
            public Image Frame;
            public Image Bar;                 // focus highlight under the row
            public Func<string> Label;
            public Action Pick;
            public Action<int> Step;          // sliders
            public Func<bool> Selected;       // toggles
            public Func<float> Value;         // sliders 0..1
            public Image[] Pips;
            public Text Readout;
        }

        private readonly List<Item> items = new List<Item>();
        private Canvas canvas;
        private int cursor, openedFrame = -1;
        private bool capturing;
        private SKAction captureAction;
        public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;
        public bool Capturing => capturing;
        public int ClosedFrame { get; private set; } = -1;
        public string FocusedId => items.Count > 0 ? items[cursor].Id : "";
        public event Action Closed;

        /// <summary>(label, action) for the buttons at the panel's foot, e.g. Return to Sanctuary / Quit Game. The first is focused.</summary>
        public void Open(string title, params (string label, Action pick)[] buttons)
        {
            Build(title, buttons);
            canvas.gameObject.SetActive(true);
            cursor = Mathf.Max(0, items.FindIndex(it => it.Id == "top_0"));
            capturing = false;
            openedFrame = Time.frameCount;
            Render();
        }

        public void Close()
        {
            if (!IsOpen) return;
            capturing = false;
            canvas.gameObject.SetActive(false);
            ClosedFrame = Time.frameCount;   // the Esc that closed it must not reopen the pause board
            Closed?.Invoke();
        }

        public void Focus(string id) { int i = items.FindIndex(it => it.Id == id); if (i >= 0) { cursor = i; Render(); } }
        public void PickFocused() { items[cursor].Pick?.Invoke(); Render(); }
        public void StepFocused(int dir) { items[cursor].Step?.Invoke(dir); Render(); }

        private void Update()
        {
            if (!IsOpen || Time.frameCount == openedFrame) return;
#if ENABLE_INPUT_SYSTEM
            if (capturing)
            {
                var key = SKBindings.AnyPressed();
                if (key == Key.Escape) capturing = false;
                else if (key != Key.None) { SKBindings.Rebind(captureAction, key); capturing = false; }
                Render();
                return;
            }
#endif
            var item = items[cursor];
            if (SKKeys.Back) { Close(); return; }
            if (SKKeys.Up) cursor = (cursor - 1 + items.Count) % items.Count;
            else if (SKKeys.Down) cursor = (cursor + 1) % items.Count;
            else if (SKKeys.Left) { if (item.Step != null) item.Step(-1); else cursor = (cursor - 1 + items.Count) % items.Count; }
            else if (SKKeys.Right) { if (item.Step != null) item.Step(1); else cursor = (cursor + 1) % items.Count; }
            else if (SKKeys.Confirm && item.Pick != null) item.Pick();
            Render();
        }

        // ---------- layout (reference canvas 1280 x 720; panel 1040 x 600 in the middle, origin its top-left) ----------

        private const float PanelW = 1040f, PanelH = 604f;
        private const float LeftX = 56f, RightX = 560f;   // column starts inside the panel

        private void Build(string title, (string label, Action pick)[] buttons)
        {
            if (canvas != null) Destroy(canvas.gameObject);
            items.Clear();
            canvas = new GameObject("Settings_Canvas").AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            var root = canvas.transform;

            var shade = SKUi.Picture("Shade", root, SKUi.White, new Vector2(.5f, .5f), Vector2.zero, Vector2.zero);
            shade.rectTransform.anchorMin = Vector2.zero; shade.rectTransform.anchorMax = Vector2.one;
            shade.color = new Color(.01f, .005f, .015f, .80f);
            var ember = SKUi.Picture("Shade_Glow", root, SKUi.Glow, new Vector2(.5f, .5f), new Vector2(0f, -40f), new Vector2(1500f, 900f));
            ember.color = new Color(.35f, .02f, .05f, .35f);

            var panel = SKUi.Picture("Panel", root, SKUi.White, new Vector2(.5f, .5f), Vector2.zero, new Vector2(PanelW, PanelH));
            panel.color = new Color(.035f, .028f, .04f, .9f);
            var frame = SKUi.Picture("Panel_Frame", panel.transform, SKUi.Frame, new Vector2(.5f, .5f), Vector2.zero, new Vector2(PanelW + 8f, PanelH + 8f));
            frame.type = Image.Type.Sliced;
            var p = panel.transform;

            // header: spaced title between two crosses, a fading rule under it
            var head = SKUi.Label("Title", p, SKUi.Spaced(title), 28, new Vector2(.5f, 1f), new Vector2(0f, -44f), new Vector2(700f, 44f), TextAnchor.MiddleCenter);
            Mid(head.rectTransform);
            head.color = SKUi.Gold;
            float half = head.preferredWidth / 2f + 26f;
            Mid(Tint(SKUi.Picture("Title_Cross_L", p, SKUi.Cross, new Vector2(.5f, 1f), new Vector2(-half, -44f), new Vector2(11f, 17f)), SKUi.Blood * 1.6f).rectTransform);
            Mid(Tint(SKUi.Picture("Title_Cross_R", p, SKUi.Cross, new Vector2(.5f, 1f), new Vector2(half, -44f), new Vector2(11f, 17f)), SKUi.Blood * 1.6f).rectTransform);
            Mid(Tint(SKUi.Picture("Rule", p, SKUi.Fade, new Vector2(.5f, 1f), new Vector2(0f, -76f), new Vector2(PanelW - 120f, 1.5f)),
                new Color(SKUi.Gold.r, SKUi.Gold.g, SKUi.Gold.b, .7f)).rectTransform);
            // vertical rule between the columns (a horizontal fade sprite turned on its side)
            var divider = Tint(SKUi.Picture("Divider", p, SKUi.Fade, TL, new Vector2(RightX - 30f, -270f), new Vector2(380f, 1.5f)),
                new Color(SKUi.Gold.r, SKUi.Gold.g, SKUi.Gold.b, .35f));
            Mid(divider.rectTransform).localRotation = Quaternion.Euler(0f, 0f, 90f);

            // I. Sound
            Section(p, "I", "Sound", "소리", LeftX, -112f);
            Slider("bgm", p, "BGM", -160f, () => SKSave.Data.volume, d => SKSettings.Step(ref SKSave.Data.volume, d));
            Slider("voice", p, "Voice", -200f, () => SKSave.Data.voice, d => SKSettings.Step(ref SKSave.Data.voice, d));
            Slider("se", p, "Effects", -240f, () => SKSave.Data.se, d => SKSettings.Step(ref SKSave.Data.se, d));

            // II. Display
            Section(p, "II", "Display", "화면", LeftX, -296f);
            Toggle("full", p, "Full Screen", -344f, () => SKSave.Data.fullscreen, () => SKSettings.SetScreen(true, SKSave.Data.windowScale));
            Toggle("x1", p, "Window  1280 × 720", -384f, () => !SKSave.Data.fullscreen && SKSave.Data.windowScale == 1, () => SKSettings.SetScreen(false, 1));
            Toggle("x15", p, "Window  1920 × 1080", -424f, () => !SKSave.Data.fullscreen && SKSave.Data.windowScale == 2, () => SKSettings.SetScreen(false, 2));

            // III. Controls
            Section(p, "III", "Controls", "조작", RightX, -112f);
            var rows = new[] { (SKAction.Left, "Left"), (SKAction.Right, "Right"), (SKAction.Up, "Up"), (SKAction.Down, "Crouch"),
                               (SKAction.Jump, "Jump"), (SKAction.Attack, "Attack"), (SKAction.Switch, "Switch"), (SKAction.Retry, "Retry"), (SKAction.Pause, "Pause") };
            for (int i = 0; i < rows.Length; i++) KeyRow(p, rows[i].Item1, rows[i].Item2, -156f - i * 34f);
            Button("reset", p, () => "Reset Keys", SKBindings.ResetToDefaults, new Vector2(RightX + 330f, -112f), 120f, 28f, 15);

            // foot: leave / quit (first one focused on open)
            Tint(SKUi.Picture("Foot_Rule", p, SKUi.Fade, new Vector2(.5f, 0f), new Vector2(0f, 84f), new Vector2(PanelW - 120f, 1.5f)), new Color(SKUi.Gold.r, SKUi.Gold.g, SKUi.Gold.b, .5f));
            const float bw = 260f, gap = 30f;
            float total = buttons.Length * bw + (buttons.Length - 1) * gap;
            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                Button("top_" + i, p, () => b.label, b.pick, new Vector2(PanelW / 2f - total / 2f + i * (bw + gap), -(PanelH - 44f)), bw, 40f, 20);
            }
            var hint = SKUi.Label("Hint", p, "W / S  move     A / D  adjust     Enter  select     Esc  close", 14,
                new Vector2(.5f, 0f), new Vector2(0f, -34f), new Vector2(800f, 20f), TextAnchor.MiddleCenter);
            hint.color = SKUi.Dim;
            canvas.gameObject.SetActive(false);
        }

        private static Image Tint(Image image, Color c) { image.color = c; return image; }

        private static Vector2 TL => new Vector2(0f, 1f);

        private static RectTransform Left(RectTransform r) { r.pivot = new Vector2(0f, .5f); return r; }
        private static RectTransform Mid(RectTransform r) { r.pivot = new Vector2(.5f, .5f); return r; }

        private static void Section(Transform p, string numeral, string name, string korean, float x, float y)
        {
            var n = SKUi.Label("Sec_" + name + "_N", p, numeral, 26, TL, new Vector2(x, y), new Vector2(60f, 34f));
            Left(n.rectTransform);
            n.color = SKUi.Blood * 1.5f;
            var t = SKUi.Label("Sec_" + name, p, SKUi.Spaced(name), 20, TL, new Vector2(x + 44f, y), new Vector2(300f, 34f));
            Left(t.rectTransform);
            t.color = SKUi.Gold;
            var k = SKUi.Label("Sec_" + name + "_K", p, korean, 15, TL, new Vector2(x + 52f + t.preferredWidth, y + 1f), new Vector2(80f, 34f), TextAnchor.MiddleLeft, SKUi.Hangul);
            Left(k.rectTransform);
            k.color = SKUi.Dim;
            Left(Tint(SKUi.Picture("Sec_" + name + "_Rule", p, SKUi.White, TL, new Vector2(x, y - 20f), new Vector2(430f, 1f)),
                new Color(SKUi.Gold.r, SKUi.Gold.g, SKUi.Gold.b, .25f)).rectTransform);
        }

        private Image FocusBar(Transform p, string id, float x, float y, float w, float h)
        {
            var bar = SKUi.Picture(id + "_bar", p, SKUi.Fade, TL, new Vector2(x, y), new Vector2(w, h));
            Left(bar.rectTransform);
            bar.color = Color.clear;
            return bar;
        }

        private Item Button(string id, Transform p, Func<string> label, Action pick, Vector2 topLeft, float width, float height, int size)
        {
            var bar = FocusBar(p, id, topLeft.x - 20f, topLeft.y, width + 40f, height + 6f);
            var frame = SKUi.Picture(id, p, SKUi.Bracket, TL, topLeft, new Vector2(width, height));
            Left(frame.rectTransform);
            frame.type = Image.Type.Sliced;
            var text = SKUi.Label("Text", frame.transform, "", size, new Vector2(.5f, .5f), Vector2.zero, new Vector2(width, height), TextAnchor.MiddleCenter);
            var item = new Item { Id = id, Frame = frame, Bar = bar, Text = text, Label = label, Pick = pick };
            items.Add(item);
            return item;
        }

        private void Slider(string id, Transform p, string name, float y, Func<float> value, Action<int> step)
        {
            var bar = FocusBar(p, id, LeftX - 16f, y, 460f, 32f);
            var text = SKUi.Label(id + "_label", p, name, 20, TL, new Vector2(LeftX + 16f, y), new Vector2(120f, 30f));
            Left(text.rectTransform);
            var pips = new Image[10];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = SKUi.Picture(id + "_pip" + i, p, SKUi.White, TL, new Vector2(LeftX + 150f + i * 21f, y), new Vector2(14f, 10f));
                Left(pips[i].rectTransform);
            }
            var readout = SKUi.Label(id + "_value", p, "", 16, TL, new Vector2(LeftX + 370f, y), new Vector2(50f, 30f), TextAnchor.MiddleRight);
            Left(readout.rectTransform);
            items.Add(new Item { Id = id, Text = text, Bar = bar, Label = () => name, Step = step, Value = value, Pips = pips, Readout = readout });
        }

        private void Toggle(string id, Transform p, string name, float y, Func<bool> selected, Action pick)
        {
            var bar = FocusBar(p, id, LeftX - 16f, y, 460f, 32f);
            var mark = SKUi.Picture(id + "_mark", p, SKUi.Ring, TL, new Vector2(LeftX + 14f, y), new Vector2(22f, 22f));
            Left(mark.rectTransform);
            var text = SKUi.Label(id + "_label", p, name, 20, TL, new Vector2(LeftX + 50f, y), new Vector2(320f, 30f));
            Left(text.rectTransform);
            items.Add(new Item { Id = id, Text = text, Frame = mark, Bar = bar, Label = () => name, Pick = pick, Selected = selected });
        }

        private void KeyRow(Transform p, SKAction action, string name, float y)
        {
            var label = SKUi.Label("key_" + action + "_label", p, name, 18, TL, new Vector2(RightX + 16f, y), new Vector2(120f, 30f));
            Left(label.rectTransform);
#if ENABLE_INPUT_SYSTEM
            var item = Button("key_" + action, p,
                () => capturing && captureAction == action ? "press a key" : SKBindings.Name(SKBindings.Primary(action)),
                () => { capturing = true; captureAction = action; }, new Vector2(RightX + 150f, y), 150f, 28f, 16);
            item.Bar.rectTransform.anchoredPosition = new Vector2(RightX - 16f, y);
            item.Bar.rectTransform.sizeDelta = new Vector2(460f, 32f);
            item.Readout = label;
            var alt = SKUi.Label("key_" + action + "_alt", p, SKBindings.Name(SKBindings.Alternate(action)), 15, TL,
                new Vector2(RightX + 318f, y), new Vector2(140f, 28f));
            Left(alt.rectTransform);
            alt.color = SKUi.Dim;
#endif
        }

        private void Render()
        {
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                bool focus = i == cursor;
                var ink = focus ? SKUi.Focus : SKUi.Ink;
                if (it.Bar != null) it.Bar.color = focus ? new Color(SKUi.Blood.r, SKUi.Blood.g, SKUi.Blood.b, .55f) : Color.clear;
                if (it.Text != null)
                {
                    if (it.Frame != null && it.Frame.type == Image.Type.Sliced) it.Text.text = it.Label();
                    it.Text.color = ink;
                }
                if (it.Frame != null)
                {
                    if (it.Frame.type == Image.Type.Sliced) it.Frame.color = focus ? SKUi.Focus : new Color(.72f, .66f, .58f);
                    else it.Frame.color = focus ? SKUi.Focus : SKUi.Ink;
                    if (it.Selected != null) it.Frame.sprite = it.Selected() ? SKUi.RingOn : SKUi.Ring;
                }
                if (it.Pips != null)
                {
                    int lit = Mathf.RoundToInt(it.Value() * it.Pips.Length);
                    for (int k = 0; k < it.Pips.Length; k++)
                        it.Pips[k].color = k < lit ? (focus ? SKUi.Focus : SKUi.Gold) : new Color(.22f, .2f, .22f);
                    it.Readout.text = Mathf.RoundToInt(it.Value() * 100f).ToString();
                    it.Readout.color = focus ? SKUi.Focus : SKUi.Dim;
                }
                else if (it.Readout != null) it.Readout.color = focus ? SKUi.Focus : SKUi.Ink;   // key row name
            }
        }

        private void OnDestroy() { if (canvas != null) Destroy(canvas.gameObject); }
    }
}
