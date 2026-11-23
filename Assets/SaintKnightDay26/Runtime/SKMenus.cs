using System;
using System.Collections.Generic;
using SaintKnight.Day02;
using SaintKnight.Day20;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Day26
{
    /// <summary>True while a menu owns the keyboard (player, lobby, R-reset and doors ignore input).</summary>
    public static class SKPause
    {
        public static bool IsPaused { get; set; }
    }

    /// <summary>Keyboard reads for menus (W/S or arrows, Enter/Space/J confirm, Esc/Backspace back).</summary>
    public static class SKKeys
    {
#if ENABLE_INPUT_SYSTEM
        private static bool P(Key a, Key b) { var k = Keyboard.current; return k != null && (k[a].wasPressedThisFrame || k[b].wasPressedThisFrame); }
        // Menus follow the key configuration too; Enter / Esc always work.
        public static bool Up => SKBindings.Pressed(SKAction.Up);
        public static bool Down => SKBindings.Pressed(SKAction.Down);
        public static bool Left => SKBindings.Pressed(SKAction.Left);
        public static bool Right => SKBindings.Pressed(SKAction.Right);
        public static bool Confirm => P(Key.Enter, Key.NumpadEnter) || SKBindings.Pressed(SKAction.Jump) || SKBindings.Pressed(SKAction.Attack);
        public static bool Back => P(Key.Escape, Key.Backspace) || SKBindings.Pressed(SKAction.Pause);
        public static bool Pause => SKBindings.Pressed(SKAction.Pause);
#else
        private static bool P(KeyCode a, KeyCode b) => Input.GetKeyDown(a) || Input.GetKeyDown(b);
        public static bool Up => P(KeyCode.W, KeyCode.UpArrow);
        public static bool Down => P(KeyCode.S, KeyCode.DownArrow);
        public static bool Left => P(KeyCode.A, KeyCode.LeftArrow);
        public static bool Right => P(KeyCode.D, KeyCode.RightArrow);
        public static bool Confirm => P(KeyCode.Return, KeyCode.Space) || P(KeyCode.J, KeyCode.KeypadEnter);
        public static bool Back => P(KeyCode.Escape, KeyCode.Backspace);
        public static bool Pause => P(KeyCode.Escape, KeyCode.P);
#endif
    }

    /// <summary>Applies saved settings once at boot.</summary>
    public static class SKSettings
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isEditor && !Application.isBatchMode) ApplyScreen();
        }

        // ponytail: BGM / VOICE / SE are saved only; sound is not built yet (Day 19), so nothing plays through them.
        // When audio arrives, route each to an AudioMixer group volume.
        public static void Step(ref float value, int dir)
        {
            value = Mathf.Clamp01(Mathf.Round((value + dir * .1f) * 10f) / 10f);
            SKSave.Write();
        }

        public static void StepVolume(int dir) => Step(ref SKSave.Data.volume, dir);

        /// <summary>Full screen, or a window at 1280x720 (scale 1) / 1920x1080 (scale 2).</summary>
        public static void SetScreen(bool fullscreen, int scale)
        {
            SKSave.Data.fullscreen = fullscreen;
            SKSave.Data.windowScale = scale == 2 ? 2 : 1;
            SKSave.Write();
            if (!Application.isEditor && !Application.isBatchMode) ApplyScreen();
        }

        public static void ToggleFullscreen() => SetScreen(!SKSave.Data.fullscreen, SKSave.Data.windowScale);

        private static void ApplyScreen()
        {
            var d = SKSave.Data;
            if (d.fullscreen)
                Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.FullScreenWindow);
            else if (d.windowScale == 2) Screen.SetResolution(1920, 1080, FullScreenMode.Windowed);
            else Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
        }
    }

    /// <summary>One line of a menu page.</summary>
    public sealed class SKMenuItem
    {
        public Func<string> Label;
        public Action Pick, Less, More;
        public Func<bool> Enabled = () => true;
        public SKMenuItem(Func<string> label, Action pick) { Label = label; Pick = pick; }
    }

    public sealed class SKMenuPage
    {
        public string Title;
        public Func<string> Body = () => "";
        public List<SKMenuItem> Items = new List<SKMenuItem>();
        public int Cursor;
        /// <summary>Background darkness for this page; below 0 = the view's ShadeAlpha.</summary>
        public float Shade = -1f;
        /// <summary>Full-screen picture behind the text (hot spring); null = none.</summary>
        public Texture Backdrop;
        /// <summary>How bright the backdrop is drawn (1 = as painted; character CGs read better near 1).</summary>
        public float BackdropBrightness = .62f;
        /// <summary>Reel-style talk scene (salon, after the user's reference reel): the picture fades into black at the
        /// bottom, the speaker's name sits on a gold bar over an underline, the line types out, dust drifts, lanterns flicker.</summary>
        public bool Reel;
        /// <summary>Reel pages: who is speaking (null = narration).</summary>
        public Func<string> Speaker = () => null;
        /// <summary>Body and list in the Korean OS font (dialogue pages).</summary>
        public bool Hangul;
    }

    /// <summary>
    /// Text menu drawn with uGUI over everything (keyboard only, no EventSystem needed).
    /// Pages stack: Back pops; popping the last page calls <see cref="Closed"/>.
    /// </summary>
    public class SKMenuView : MonoBehaviour
    {
        protected readonly Stack<SKMenuPage> pages = new Stack<SKMenuPage>();
        private Canvas canvas;
        private Image shade;
        private Text title, body, list;
        private RawImage backdrop;
        public RawImage Backdrop => backdrop;
        /// <summary>The plain text list; the title screen hides it and draws its own main menu.</summary>
        protected Text ListText => list;
        public RawImage Picture { get; private set; }
        public Text Caption { get; private set; }
        public event Action Closed;
        public bool IsOpen => pages.Count > 0;
        public SKMenuPage Top => pages.Count > 0 ? pages.Peek() : null;
        /// <summary>Opaque background (title screen) or a see-through shade (pause).</summary>
        public float ShadeAlpha = .72f;

        protected void Build()
        {
            if (canvas != null) return;
            canvas = new GameObject("Menu_Canvas").AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            shade = Ui<Image>("Shade", Vector2.zero, Vector2.one);
            shade.color = new Color(.02f, .015f, .03f, ShadeAlpha);
            backdrop = Ui<RawImage>("Backdrop", Vector2.zero, Vector2.one);
            backdrop.color = new Color(.62f, .6f, .6f);
            var fit = backdrop.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;
            backdrop.gameObject.SetActive(false);
            title = Label("Title", new Vector2(.1f, .78f), new Vector2(.9f, .92f), 40, TextAnchor.MiddleCenter, new Color(.86f, .7f, .45f));
            body = Label("Body", new Vector2(.1f, .58f), new Vector2(.9f, .78f), 18, TextAnchor.UpperCenter, new Color(.78f, .74f, .72f));
            list = Label("List", new Vector2(.1f, .08f), new Vector2(.9f, .58f), 24, TextAnchor.UpperCenter, Color.white);
            list.supportRichText = true;
            Picture = Ui<RawImage>("Picture", new Vector2(.05f, .1f), new Vector2(.95f, .92f));
            Picture.gameObject.SetActive(false);
            Caption = Label("Caption", new Vector2(.1f, .02f), new Vector2(.9f, .09f), 18, TextAnchor.MiddleCenter, new Color(.86f, .7f, .45f));
            Caption.gameObject.SetActive(false);
            canvas.gameObject.SetActive(false);
        }

        private T Ui<T>(string n, Vector2 min, Vector2 max) where T : Graphic
        {
            var g = new GameObject(n).AddComponent<T>();
            g.transform.SetParent(canvas.transform, false);
            var r = g.rectTransform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            return g;
        }

        private Text Label(string n, Vector2 min, Vector2 max, int size, TextAnchor anchor, Color color)
        {
            var t = Ui<Text>(n, min, max);
            t.font = SKUi.Serif;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public void Push(SKMenuPage page)
        {
            Build();
            pages.Push(page);
            canvas.gameObject.SetActive(true);
            ApplyShade();
            pushedFrame = Time.frameCount;   // the key that opened this page is not read again
            Skip(1);
        }
        private int pushedFrame = -1;

        public void Pop()
        {
            if (pages.Count > 0) pages.Pop();
            if (pages.Count == 0) { canvas.gameObject.SetActive(false); Closed?.Invoke(); }
            else ApplyShade();
        }

        public void CloseAll() { while (pages.Count > 0) Pop(); }

        private void ApplyShade()
        {
            float a = Top != null && Top.Shade >= 0f ? Top.Shade : ShadeAlpha;
            shade.color = new Color(.02f, .015f, .03f, a);
        }

        // keep the cursor off disabled lines
        private void Skip(int dir)
        {
            var p = Top;
            if (p == null || p.Items.Count == 0) return;
            for (int i = 0; i < p.Items.Count && !p.Items[p.Cursor].Enabled(); i++)
                p.Cursor = (p.Cursor + dir + p.Items.Count) % p.Items.Count;
        }

        /// <summary>Returns true while this view consumed input this frame.</summary>
        protected bool Drive()
        {
            var p = Top;
            if (p == null) return false;
            if (Time.frameCount == pushedFrame) { Render(); return true; }
            if (Picture.gameObject.activeSelf)
            {
                if (SKKeys.Back || SKKeys.Confirm) HidePicture();
                else if (SKKeys.Left || SKKeys.Right) PictureStep?.Invoke(SKKeys.Left ? -1 : 1);
                return true;
            }
            if (p.Items.Count > 0)
            {
                if (SKKeys.Up) { p.Cursor = (p.Cursor - 1 + p.Items.Count) % p.Items.Count; Skip(-1); }
                if (SKKeys.Down) { p.Cursor = (p.Cursor + 1) % p.Items.Count; Skip(1); }
                var item = p.Items[p.Cursor];
                if (item.Enabled())
                {
                    if (SKKeys.Left && item.Less != null) item.Less();
                    else if (SKKeys.Right && item.More != null) item.More();
                    else if (SKKeys.Confirm && item.Pick != null) { item.Pick(); return true; }
                }
            }
            if (SKKeys.Back) Pop();
            Render();
            return true;
        }

        public void Render()
        {
            var p = Top;
            if (p == null) return;
            title.text = p.Title;
            body.text = p.Reel ? Typed(p) : p.Body();
            backdrop.texture = p.Backdrop;
            float lit = p.BackdropBrightness;
            if (p.Reel)   // lantern flicker: two slow waves and a faint quick one
            {
                float t = Time.unscaledTime;
                lit *= 1f + .03f * Mathf.Sin(t * 1.7f) * Mathf.Sin(t * .63f + 1f) + .008f * Mathf.Sin(t * 11f);
            }
            backdrop.color = new Color(lit, lit * .97f, lit * .97f);
            backdrop.gameObject.SetActive(p.Backdrop != null);
            LayoutFor(p.Backdrop != null, p.Reel);
            if (p.Reel) SpeakerPlate(p.Speaker());
            body.font = list.font = p.Hangul ? SKUi.Hangul : SKUi.Serif;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < p.Items.Count; i++)
            {
                var it = p.Items[i];
                string text = it.Label();
                if (!it.Enabled()) sb.Append("<color=#5a5560>").Append(text).Append("</color>");
                else if (i == p.Cursor) sb.Append("<color=#e8c27a>>  ").Append(text).Append("  <</color>");
                else sb.Append(text);
                sb.Append('\n');
            }
            list.text = sb.ToString();
        }

        // Over a picture (hot spring): title top-left, dialogue in a dark band along the bottom, choices beside it,
        // so the faces stay clear. Otherwise the original centred layout.
        private Image band, fade, fadeSolid, nameBar, nameLine;
        private Text nameText;
        private void LayoutFor(bool scene, bool reel = false)
        {
            if (band == null)
            {
                band = Ui<Image>("Dialogue_Band", new Vector2(0f, 0f), new Vector2(1f, .27f));
                band.sprite = SKUi.White;
                band.color = new Color(.02f, .015f, .03f, .78f);
                band.transform.SetSiblingIndex(backdrop.transform.GetSiblingIndex() + 1);
                // reel style: the picture sinks into black instead of a box
                var ink = new Color(.012f, .008f, .01f, 1f);
                fadeSolid = Ui<Image>("Reel_FadeSolid", Vector2.zero, new Vector2(1f, .16f));
                fadeSolid.sprite = SKUi.White;
                fadeSolid.color = new Color(ink.r, ink.g, ink.b, .97f);
                fade = Ui<Image>("Reel_Fade", new Vector2(0f, .16f), new Vector2(1f, .4f));
                fade.sprite = SKUi.White;
                var g = fade.gameObject.AddComponent<SKGradient>();
                g.Top = new Color(ink.r, ink.g, ink.b, 0f);
                g.Bottom = new Color(ink.r, ink.g, ink.b, .97f);
                fade.transform.SetSiblingIndex(band.transform.GetSiblingIndex());
                fadeSolid.transform.SetSiblingIndex(band.transform.GetSiblingIndex());
                nameBar = Ui<Image>("Reel_NameBar", new Vector2(.05f, .205f), new Vector2(.053f, .245f));
                nameBar.sprite = SKUi.White;
                nameBar.color = new Color(.9f, .7f, .36f);
                nameLine = Ui<Image>("Reel_NameLine", new Vector2(.05f, .195f), new Vector2(.36f, .197f));
                nameLine.sprite = SKUi.White;
                nameLine.color = new Color(.9f, .72f, .45f, .55f);
                nameText = Label("Reel_Name", new Vector2(.062f, .2f), new Vector2(.5f, .25f), 22, TextAnchor.MiddleLeft, new Color(.96f, .93f, .88f));
                nameText.font = SKUi.Hangul;
            }
            band.gameObject.SetActive(scene && !reel);
            fade.gameObject.SetActive(scene && reel);
            fadeSolid.gameObject.SetActive(scene && reel);
            if (!(scene && reel)) nameBar.gameObject.SetActive(false);
            if (!(scene && reel)) { nameLine.gameObject.SetActive(false); nameText.gameObject.SetActive(false); }
            Anchor(title, scene ? new Vector2(.03f, .88f) : new Vector2(.1f, .78f), scene ? new Vector2(.5f, .98f) : new Vector2(.9f, .92f));
            title.alignment = scene ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            title.fontSize = scene ? 26 : 40;
            if (scene && reel)
            {
                Anchor(body, new Vector2(.062f, .02f), new Vector2(.66f, .18f));
                body.alignment = TextAnchor.UpperLeft;
                body.fontSize = 22;
                body.color = new Color(.97f, .95f, .92f);
                Anchor(list, new Vector2(.72f, .03f), new Vector2(.97f, .24f));
                list.alignment = TextAnchor.UpperLeft;
                list.fontSize = 19;
                return;
            }
            body.color = new Color(.78f, .74f, .72f);
            Anchor(body, scene ? new Vector2(.05f, .03f) : new Vector2(.1f, .58f), scene ? new Vector2(.68f, .24f) : new Vector2(.9f, .78f));
            body.alignment = scene ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            body.fontSize = scene ? 22 : 18;
            Anchor(list, scene ? new Vector2(.70f, .03f) : new Vector2(.1f, .08f), scene ? new Vector2(.97f, .24f) : new Vector2(.9f, .58f));
            list.alignment = scene ? TextAnchor.UpperLeft : TextAnchor.UpperCenter;
            list.fontSize = scene ? 20 : 24;
        }

        // reel pages: the line types out at a steady pace; a new line starts over
        public const float TypeCharsPerSecond = 28f;
        private string typedFull;
        private float typedSince;
        private string Typed(SKMenuPage p)
        {
            string full = p.Body() ?? "";
            if (full != typedFull) { typedFull = full; typedSince = Time.unscaledTime; }
            int n = Mathf.Min(full.Length, Mathf.FloorToInt((Time.unscaledTime - typedSince) * TypeCharsPerSecond));
            string shown = full.Substring(0, n);
            return p.Speaker() == null ? "<color=#b9b2a6>" + shown + "</color>" : shown;
        }

        private void SpeakerPlate(string who)
        {
            bool on = !string.IsNullOrEmpty(who);
            nameBar.gameObject.SetActive(on);
            nameLine.gameObject.SetActive(on);
            nameText.gameObject.SetActive(on);
            if (on) nameText.text = who;
        }

        private static void Anchor(Text t, Vector2 min, Vector2 max)
        {
            var r = t.rectTransform;
            r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
        }

        public Action<int> PictureStep;
        public void ShowPicture(Texture texture, string caption)
        {
            Picture.texture = texture;
            Picture.gameObject.SetActive(true);
            var fitter = Picture.GetComponent<AspectRatioFitter>() ?? Picture.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = texture != null && texture.height > 0 ? (float)texture.width / texture.height : 1f;
            Caption.text = caption + "    (A/D  NEXT    ESC  BACK)";
            Caption.gameObject.SetActive(true);
            list.gameObject.SetActive(false);
            body.gameObject.SetActive(false);
        }

        public void HidePicture()
        {
            Picture.gameObject.SetActive(false);
            Caption.gameObject.SetActive(false);
            list.gameObject.SetActive(true);
            body.gameObject.SetActive(true);
            PictureHidden?.Invoke();
        }
        public event Action PictureHidden;

        // ---------- shared pages ----------

        private SKSettingsPanel panel;
        /// <summary>The one-screen settings board (volume, screen size, key configuration).</summary>
        public SKSettingsPanel Panel => panel != null ? panel : panel = gameObject.AddComponent<SKSettingsPanel>();

        public SKMenuPage RecordsPage() => SKGallery.RecordsPage(this);
    }

    /// <summary>Esc pause menu in every scene with a player (added by the Day 20 director).</summary>
    public sealed class SKPauseMenu : SKMenuView
    {
        public const string TitleScene = "Title";
        private float previousScale = 1f;
        private SKHealth player;

        public void Bind(SKHealth who) => player = who;

        private void Update()
        {
            if (SaintKnight.Rest.SKEpilogue.Playing || SaintKnight.Rest.SKEpilogue.FinishedFrame == Time.frameCount) return;
            if (SaintKnight.Rest.SKShop.Current != null || SaintKnight.Rest.SKShop.ClosedFrame == Time.frameCount) return;   // a shop owns the keys
            if (IsOpen) { Drive(); return; }
            if (Panel.IsOpen || Panel.ClosedFrame == Time.frameCount || !SKKeys.Pause) return;
            Open();
        }

        private void BeginPause()
        {
            SKHitStop.CancelActive();
            previousScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            Time.timeScale = 0f;
            SKPause.IsPaused = true;
        }

        /// <summary>Lobby lectern (Day 28): records straight away; Esc returns to play.</summary>
        public void OpenRecords()
        {
            if (IsOpen) return;
            BeginPause();
            Push(RecordsPage());
            Render();
        }

        /// <summary>Lobby hot spring (2026-11-17): rest and talk; Esc / Leave returns to play.</summary>
        public void OpenHotSpring()
        {
            if (IsPausedOpen || !SaintKnight.Rest.SKHotSpring.Unlocked || SaintKnight.Rest.SKEpilogue.Playing) return;
            // first visit after the Madonna falls: the epilogue plays once, then the spring opens as usual
            if (SaintKnight.Rest.SKEpilogue.ShouldPlay) { SaintKnight.Rest.SKEpilogue.Play(transform, OpenHotSpring); return; }
            if (SaintKnight.Rest.SKShop.Current != null) return;
            SaintKnight.Rest.SKShop.Open(SaintKnight.Rest.SKHotSpring.Place(player));   // 2026-11-22: walked into like the shops
        }

        /// <summary>Esc: the settings board is the pause screen (layout after the user's reference). Esc again resumes.</summary>
        public void Open()
        {
            BeginPause();
            bool inLobby = SceneManager.GetActiveScene().name == SKTitleMenu.LobbyScene;
            Panel.Open("Pause Menu",
                inLobby ? ("Title Screen", (Action)(() => Leave(TitleScene))) : ("Return to Sanctuary", () => Leave(SKTitleMenu.LobbyScene)),
                ("Quit Game", SKTitleMenu.Quit));
        }

        /// <summary>True while the pause board or a records page is up.</summary>
        public bool IsPausedOpen => IsOpen || Panel.IsOpen;

        public void CloseEverything()
        {
            if (Panel.IsOpen) Panel.Close();
            CloseAll();
        }

        private void Awake()
        {
            Closed += Resume;
            Panel.Closed += Resume;
            SaintKnight.Lobby.SKLobby.RecordsOpened += OpenRecords;
            SaintKnight.Lobby.SKLobby.HotSpringOpened += OpenHotSpring;
        }

        private void Resume()
        {
            SKPause.IsPaused = false;
            Time.timeScale = previousScale;
        }

        private void Leave(string scene)
        {
            Closed -= Resume;
            Panel.Closed -= Resume;
            CloseEverything();
            SKPause.IsPaused = false;
            Time.timeScale = 1f;
            SaintKnight.Day09.SKSceneDoor.Go(player, scene);
        }

        private void OnDestroy()
        {
            SaintKnight.Lobby.SKLobby.RecordsOpened -= OpenRecords;
            SaintKnight.Lobby.SKLobby.HotSpringOpened -= OpenHotSpring;
            if (!IsPausedOpen) return;
            SKPause.IsPaused = false;
            Time.timeScale = 1f;
        }
    }
}
