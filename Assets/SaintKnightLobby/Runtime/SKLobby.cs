using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SaintKnight.Day01;
using SaintKnight.Day02;
using SaintKnight.Day20;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Lobby
{
    // Village / Sanctuary appended 2026-11-19, BeastBar / Forge / Apothecary 2026-11-22 (Tavern = the bunny gambling hall): scenes store the index, keep the order.
    public enum SKLobbyAction { SwitchCharacter, Pray, Sortie, Training, Records, HotSpring, Tavern, Village, Sanctuary, BeastBar, Forge, Apothecary }

    [System.Serializable]
    public struct SKLobbySpot
    {
        public string label;
        public SKLobbyAction action;
        public float x;
        [Tooltip("Optional \"[W] 들어가기\" bubble that pops up over the door (village); none = no bubble.")]
        public Sprite hintArt;   // pivot at the tail tip
        public float hintY;
    }

    /// <summary>
    /// Sanctuary lobby. The background shows which character the sword/chain answers to,
    /// and W / Up interacts with the nearest spot along the walk line.
    /// </summary>
    public sealed class SKLobby : MonoBehaviour
    {
        [SerializeField] private SKDay01Player player;
        [SerializeField] private SpriteRenderer background;
        [SerializeField] private Sprite ceciliaBackground;
        [SerializeField] private Sprite adelaBackground;
        [Tooltip("Props in front of the walk line (sword, map table, bell), drawn over the player.")]
        [SerializeField] private SpriteRenderer frontProps;
        [SerializeField] private Sprite ceciliaFront;
        [SerializeField] private Sprite adelaFront;
        [SerializeField] private Text prompt;
        [SerializeField] private float reach = .65f;
        [SerializeField] private float messageSeconds = 2.5f;
        [SerializeField] private string trainingScene = "Day05_EnemyPatterns";
        [SerializeField] private SKLobbySpot[] spots = new SKLobbySpot[0];
        [Tooltip("Prompt shown away from every spot.")]
        [SerializeField] private string idleText = "SANCTUARY";

        private string message;
        private float messageUntil;
        // 2026-11-22: drawn on a screen canvas over the HUD (as world sprites the HUD illustration covered them)
        private Image[] bubbles = new Image[0];
        private Text[] fallbacks = new Text[0];
        private RectTransform hintLayer;   // when Up is bound to another key than the W drawn in the bubble
        private float[] poppedAt = new float[0];
        private static bool Shown(Component c) => c != null && c.gameObject.activeSelf;
        public string HintText(int i) => i < bubbles.Length && (Shown(bubbles[i]) || Shown(fallbacks[i]))
            ? "[" + InteractKey() + "]  들어가기" : null;

        public int NearestSpot { get; private set; } = -1;
        /// <summary>Day 28: the records / gallery panel listens here.</summary>
        public static event System.Action RecordsOpened;
        /// <summary>2026-11-17: the hot spring (rest, heal lasting injuries, talk) listens here.</summary>
        public static event System.Action HotSpringOpened;

        private void Start()
        {
            BuildSigns();
            if (player != null && !SaintKnight.Rest.SKEpilogue.TryPlayReturn(transform) &&
                gameObject.scene.name == SKRunDirector.LobbyScene)
                SaintKnight.Rest.SKDialogue.PlaySanctuary(player.transform);
            if (!SaintKnight.Rest.SKHotSpring.Unlocked) return;
            if (Camera.main != null && FindAnyObjectByType<AudioListener>() == null)
                Camera.main.gameObject.AddComponent<AudioListener>();
            foreach (var s in spots)
            {
                if (s.action == SKLobbyAction.HotSpring) SaintKnight.Rest.SKHotSpring.MarkEntrance(transform, s.x);
                if (s.action == SKLobbyAction.Tavern || s.action == SKLobbyAction.Village || s.action >= SKLobbyAction.BeastBar)
                    SaintKnight.Rest.SKTavern.MarkEntrance(transform, s.x);
            }
        }
        public string PromptText => prompt != null ? prompt.text : string.Empty;

        private void Update()
        {
            if (player == null || SaintKnight.Day26.SKPause.IsPaused) return;
            bool cecilia = player.SelectedCharacter == 0;
            if (background != null) background.sprite = cecilia ? ceciliaBackground : adelaBackground;
            if (frontProps != null) frontProps.sprite = cecilia ? ceciliaFront : adelaFront;
            NearestSpot = FindNearest(player.transform.position.x);
            UpdateSigns();
            if (NearestSpot >= 0 && WasInteractPressed()) Interact(NearestSpot);
            if (prompt != null)
                prompt.text = Time.time < messageUntil ? message
                    : NearestSpot >= 0 ? "W  " + spots[NearestSpot].label
                    : idleText;
        }

        private int FindNearest(float x)
        {
            int best = -1;
            float bestDistance = reach;
            for (int i = 0; i < spots.Length; i++)
            {
                if (Locked(spots[i].action)) continue;
                float d = Mathf.Abs(spots[i].x - x);
                if (d <= bestDistance) { best = i; bestDistance = d; }
            }
            return best;
        }

        public void Interact(int index)
        {
            if (SaintKnight.Day26.SKPause.IsPaused) return;
            var action = spots[index].action;
            if (Locked(action)) return;
            switch (action)
            {
                case SKLobbyAction.SwitchCharacter:
                    player.SwitchCharacter();
                    Say(player.SelectedCharacter == 0 ? "THE CHAIN ANSWERS - CECILIA" : "THE BLADE ANSWERS - ADELA");
                    break;
                case SKLobbyAction.Pray:
                    var health = player.GetComponent<SKHealth>();
                    if (health != null) health.ResetHealth();
                    SKSave.Data.character = player.SelectedCharacter;
                    SKSave.Write();
                    Say("PRAYER - HP RESTORED, PROGRESS SAVED");
                    break;
                case SKLobbyAction.Sortie:
                    // Day 25: continue from the last checkpoint; the door always starts a fresh sortie.
                    if (!SKSave.HasRun) { Say(SKSave.Data.clears > 0 ? "THE MADONNA HAS FALLEN - USE THE DOOR TO SORTIE AGAIN" : "NO CHECKPOINT YET - USE THE DOOR"); break; }
                    SKCarry.Set(player.SelectedCharacter, player.GetComponent<SKHealth>().CurrentHp, SKSave.Data.checkpoint);
                    SceneManager.LoadScene(SKSave.Data.scene);
                    break;
                case SKLobbyAction.Training:
                    SaintKnight.Day09.SKSceneDoor.Go(player.GetComponent<SKHealth>(), trainingScene);
                    break;
                case SKLobbyAction.Records:
                    RecordsOpened?.Invoke();
                    if (RecordsOpened == null) Say("RECORDS - " + SKSave.Data.deaths + " FALLS, " + SKSave.Data.clears + " CLEARS");
                    break;
                case SKLobbyAction.HotSpring:
                    HotSpringOpened?.Invoke();
                    if (HotSpringOpened == null) Say("THE WATER IS STILL WARM");
                    break;
                case SKLobbyAction.Tavern:   // 2026-11-22: first-person shops
                    SaintKnight.Rest.SKShop.Open(SaintKnight.Rest.SKTavern.Gamble);
                    break;
                case SKLobbyAction.BeastBar:
                    SaintKnight.Rest.SKShop.Open(SaintKnight.Rest.SKTavern.BeastBar);
                    break;
                case SKLobbyAction.Forge:
                    SaintKnight.Rest.SKShop.Open(SaintKnight.Rest.SKTavern.Forge);
                    break;
                case SKLobbyAction.Apothecary:
                    SaintKnight.Rest.SKShop.Open(SaintKnight.Rest.SKTavern.Apothecary);
                    break;
                case SKLobbyAction.Village:
                    SaintKnight.Day09.SKSceneDoor.Go(player.GetComponent<SKHealth>(), SKRunDirector.VillageScene);
                    break;
                case SKLobbyAction.Sanctuary:
                    SaintKnight.Day09.SKSceneDoor.Go(player.GetComponent<SKHealth>(), SKRunDirector.LobbyScene);
                    break;
            }
        }

        // the village and its places open after the first boss clear
        private static bool Locked(SKLobbyAction a) =>
            (a == SKLobbyAction.HotSpring || a == SKLobbyAction.Tavern || a == SKLobbyAction.Village || a >= SKLobbyAction.BeastBar) &&
            !SaintKnight.Rest.SKHotSpring.Unlocked;

        private void Say(string text)
        {
            message = text;
            messageUntil = Time.time + messageSeconds;
        }

        // 2026-11-19 user: "[W] 들어가기" over each village door once she stands at it (the name boards were removed
        // the same day at the user's request). A cream pixel bubble that pops up and bobs (make_village_signs.py).
        private void BuildSigns()
        {
            bubbles = new Image[spots.Length];
            fallbacks = new Text[spots.Length];
            poppedAt = new float[spots.Length];
            if (System.Array.TrueForAll(spots, s => s.hintArt == null)) return;
            var canvas = new GameObject("Village_Hints").AddComponent<Canvas>();
            canvas.transform.SetParent(transform, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;   // over the HUD (0), under the dialogue (90) and menus (100)
            var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);   // 640x360 art at 2x
            scaler.matchWidthOrHeight = .5f;
            hintLayer = (RectTransform)canvas.transform;
            for (int i = 0; i < spots.Length; i++)
            {
                if (spots[i].hintArt == null) continue;
                var c = new Vector2(.5f, .5f);
                var art = spots[i].hintArt.rect.size * 2f;
                bubbles[i] = SaintKnight.Day26.SKUi.Picture("SignHint_" + i, hintLayer, spots[i].hintArt, c, Vector2.zero, art);
                bubbles[i].rectTransform.pivot = new Vector2(.5f, 0f);   // tail tip
                fallbacks[i] = SaintKnight.Day26.SKUi.Label("SignHintText", hintLayer, "", 22, c, Vector2.zero, new Vector2(240f, 30f),
                    TextAnchor.LowerCenter, SaintKnight.Day26.SKUi.Hangul);
                fallbacks[i].rectTransform.pivot = new Vector2(.5f, 0f);
                fallbacks[i].color = new Color(1f, .95f, .84f);
                fallbacks[i].gameObject.AddComponent<Outline>().effectColor = Color.black;
                bubbles[i].gameObject.SetActive(false);
                fallbacks[i].gameObject.SetActive(false);
            }
        }

        private Vector2 OnScreen(Vector3 world)
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(hintLayer, cam.WorldToScreenPoint(world), null, out var local);
            return local;
        }

        private void UpdateSigns()
        {
            float now = Time.unscaledTime;
            string key = InteractKey();
            for (int i = 0; i < bubbles.Length; i++)
            {
                if (bubbles[i] == null) continue;
                bool near = i == NearestSpot && !Locked(spots[i].action), art = near && key == "W";
                if (art && !bubbles[i].gameObject.activeSelf) poppedAt[i] = now;
                bubbles[i].gameObject.SetActive(art);
                fallbacks[i].gameObject.SetActive(near && !art);
                fallbacks[i].text = "[" + key + "]  들어가기";
                float camX = Camera.main != null ? Camera.main.transform.position.x : 0f;   // the village camera scrolls (2026-11-22)
                var at = OnScreen(new Vector3(Mathf.Clamp(spots[i].x, camX - 5.9f, camX + 5.9f), spots[i].hintY, 0f));   // inside the screen
                fallbacks[i].rectTransform.anchoredPosition = at;
                if (!art) continue;
                float k = Mathf.Clamp01((now - poppedAt[i]) / .22f), b = k - 1f;
                float pop = 1f + 2.7f * b * b * b + 1.7f * b * b;                    // back-out: overshoots, then settles
                bubbles[i].rectTransform.localScale = new Vector3(pop, pop, 1f);
                float bob = Mathf.Round(Mathf.Sin(now * 3.2f) * 1.4f) * 2f;          // whole art pixels (2 canvas units each)
                bubbles[i].rectTransform.anchoredPosition = at + new Vector2(0f, bob);
            }
        }

        private static string InteractKey()
        {
#if ENABLE_INPUT_SYSTEM
            return SaintKnight.Day26.SKBindings.Name(SaintKnight.Day26.SKBindings.Primary(SaintKnight.Day26.SKAction.Up));
#else
            return "W";
#endif
        }

        private static bool WasInteractPressed()
        {
#if ENABLE_INPUT_SYSTEM
            return SaintKnight.Day26.SKBindings.Pressed(SaintKnight.Day26.SKAction.Up);
#else
            return Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow);
#endif
        }
    }
}
