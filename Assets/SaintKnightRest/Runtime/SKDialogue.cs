using SaintKnight.Day01;
using SaintKnight.Day20;
using SaintKnight.Day26;
using UnityEngine;
using UnityEngine.UI;

namespace SaintKnight.Rest
{
    /// <summary>
    /// Over-the-head conversation (2026-11-19, laid out like the user's INARI screenshot): a wide dark box
    /// floats above the speaker, the name sits small just above its top-left corner, {keywords} type in red,
    /// and the speaker's bust stands on the right end of the box, overlapping it (Cecilia's stage-0
    /// illustration; Adela has none yet, so her lines show the box alone).
    /// Play is not paused: lines type and advance on their own, so walking around stays possible.
    /// </summary>
    public sealed class SKDialogue : MonoBehaviour
    {
        public const string Cecilia = "세실리아", Adela = "아델라";
        public const float CharsPerSecond = 28f, Pop = .25f;
        private const float TextWidth = 360f, Pad = 12f, BustSize = 132f, BustRoom = 116f, HeadHeight = 1.45f;
        private const string Keyword = "#d8434f";

        // Original writing for this project; 15+ wording. {word} = keyword shown in red.
        public static readonly string[][] LobbyTalk =
        {
            new[] { Adela, "여기까진 {살덩이}가 안 올라왔네. 성소는 아직 깨끗해." },
            new[] { Cecilia, "{머리 없는 성녀상}… 저분도 저처럼 바쳐졌던 거죠." },
            new[] { Adela, "…알고 있었어. 미안해." },
            new[] { Cecilia, "그럼 이번엔 같이 끊어요. {제단}도, {사슬}도." },
        };

        public static readonly string[][] ClearTalk =
        {
            new[] { Cecilia, "성모님이… 마지막에 ‘{다음 그릇}’이라고 했어요." },
            new[] { Adela, "근원이 하나가 아니란 소리지. 아래로 더 내려가야 해." },
            new[] { Cecilia, "{샘}이 따뜻해졌어요. 조금만 쉬었다 가요." },
        };

        public static SKDialogue Current { get; private set; }
        public int LineIndex { get; private set; }
        public bool IsTyping => typed < Plain(Line).Length;
        public string Speaker => lines[LineIndex][0];
        /// <summary>Visible text typed so far, without markup.</summary>
        public string Shown => Plain(Line).Substring(0, Visible);
        public Sprite Face => face != null && face.enabled ? face.sprite : null;

        private string[][] lines;
        private Transform anchor;
        private SKDay01HUD hud;
        private RectTransform canvasRect, box, faceRect;
        private Image face;
        private Text nameTag, text;
        private float typed, lineStarted, holdUntil = -1f;
        private System.Action finished;

        private string Line => lines[LineIndex][1];
        private int Visible => Mathf.Min(Plain(Line).Length, Mathf.FloorToInt(typed));

        public static string Plain(string s) => s.Replace("{", "").Replace("}", "");

        /// <summary>First <paramref name="count"/> visible characters as rich text, keywords coloured, tags always closed.</summary>
        public static string Rich(string s, int count)
        {
            var sb = new System.Text.StringBuilder();
            bool open = false;
            int n = 0;
            foreach (char c in s)
            {
                if (c == '{') { if (n < count) { sb.Append("<color=").Append(Keyword).Append('>'); open = true; } continue; }
                if (c == '}') { if (open) { sb.Append("</color>"); open = false; } continue; }
                if (n >= count) break;
                sb.Append(c);
                n++;
            }
            if (open) sb.Append("</color>");
            return sb.ToString();
        }

        /// <summary>Starts a conversation over <paramref name="speakerAnchor"/> (the player). Replaces any running one.</summary>
        public static SKDialogue Play(string[][] lines, Transform speakerAnchor, System.Action finished = null)
        {
            if (Current != null) Destroy(Current.gameObject);
            var d = new GameObject("Over_Head_Dialogue").AddComponent<SKDialogue>();
            d.lines = lines;
            d.anchor = speakerAnchor;
            d.finished = finished;
            d.hud = FindFirstObjectByType<SKDay01HUD>();
            d.Build();
            d.Show(0);
            Current = d;
            return d;
        }

        /// <summary>Sanctuary: the first arrival, then once after the first clear. Returns the talk or null.</summary>
        public static SKDialogue PlaySanctuary(Transform player)
        {
            var data = SKSave.Data;
            if (data.clears > 0 && !data.clearTalkSeen)
                return Play(ClearTalk, player, () => { SKSave.Data.clearTalkSeen = SKSave.Data.lobbyTalkSeen = true; SKSave.Write(); });
            if (!data.lobbyTalkSeen)
                return Play(LobbyTalk, player, () => { SKSave.Data.lobbyTalkSeen = true; SKSave.Write(); });
            return null;
        }

        private void Build()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;   // over the HUD, under menus and the epilogue
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = .5f;
            canvasRect = (RectTransform)transform;

            box = SKUi.Rect("Box", transform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(200f, 40f), new Vector2(.5f, 0f));
            Fill("Border", box, new Color(.55f, .5f, .5f, .45f), -1f);
            Fill("Back", box, new Color(.03f, .025f, .03f, .9f), 0f);
            // name: small, just above the box's top-left corner, outside it
            nameTag = SKUi.Label("Name", box, "", 14, new Vector2(0f, 1f), new Vector2(2f, 3f), new Vector2(200f, 18f), TextAnchor.LowerLeft, SKUi.Hangul);
            nameTag.rectTransform.pivot = Vector2.zero;
            nameTag.fontSize = 15;
            nameTag.color = Color.white;
            var shade = nameTag.gameObject.AddComponent<Outline>();
            shade.effectColor = new Color(0f, 0f, 0f, .9f);
            shade.effectDistance = new Vector2(1f, -1f);
            text = SKUi.Label("Line", box, "", 17, new Vector2(0f, .5f), new Vector2(Pad, 0f), new Vector2(TextWidth, 30f), TextAnchor.MiddleLeft, SKUi.Hangul);
            text.rectTransform.pivot = new Vector2(0f, .5f);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.supportRichText = true;

            // bust on the box's right end, drawn over it
            face = SKUi.Picture("Bust", transform, null, new Vector2(.5f, .5f), Vector2.zero, new Vector2(BustSize, BustSize));
            face.preserveAspect = true;
            faceRect = face.rectTransform;
            faceRect.pivot = new Vector2(.5f, 0f);
        }

        private static void Fill(string name, RectTransform parent, Color c, float inset)
        {
            var img = SKUi.Picture(name, parent, SKUi.White, Vector2.zero, Vector2.zero, Vector2.zero);
            var r = img.rectTransform;
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
            img.color = c;
        }

        private void Show(int index)
        {
            LineIndex = index;
            typed = 0f;
            holdUntil = -1f;
            lineStarted = Time.unscaledTime;
            bool cecilia = Speaker == Cecilia;
            nameTag.text = Speaker;

            var portrait = hud != null ? SKExpressionCutIn.CropFace(hud.CalmPortrait(cecilia ? 0 : 1), 330f, 70f) : null;
            face.sprite = portrait;
            face.enabled = portrait != null;

            // size the box to the full line so it does not grow while typing
            text.text = Rich(Line, int.MaxValue);
            text.rectTransform.sizeDelta = new Vector2(TextWidth, 30f);
            float h = Mathf.Max(text.preferredHeight, 22f);
            text.rectTransform.sizeDelta = new Vector2(TextWidth, h);
            box.sizeDelta = new Vector2(TextWidth + Pad * 2f + (face.enabled ? BustRoom : 0f), h + Pad * 2f);
            text.text = "";
        }

        private void LateUpdate()
        {
            if (lines == null) return;
            if (!SKPause.IsPaused)
            {
                typed += Time.unscaledDeltaTime * CharsPerSecond;
                if (!IsTyping && holdUntil < 0f) holdUntil = Time.unscaledTime + 1.4f + Plain(Line).Length * .04f;
                if (holdUntil >= 0f && Time.unscaledTime >= holdUntil) { Next(); if (lines == null) return; }
            }
            text.text = Rich(Line, Visible);
            Place();
        }

        /// <summary>Finish typing the line, or go to the next one.</summary>
        public void Next()
        {
            if (IsTyping) { typed = Plain(Line).Length; return; }
            if (LineIndex + 1 < lines.Length) { Show(LineIndex + 1); return; }
            lines = null;
            if (Current == this) Current = null;
            finished?.Invoke();
            Destroy(gameObject);
        }

        // above the speaker's head, kept on screen
        private void Place()
        {
            var cam = Camera.main;
            if (cam == null || anchor == null) return;
            // the 128x96 cell is taller than the figure: stand the box on the feet line plus the figure's height
            float top = anchor.position.y + 1.4f;
            foreach (var r in anchor.GetComponentsInChildren<SpriteRenderer>())
                if (r.enabled && r.sprite != null) { top = r.bounds.min.y + HeadHeight; break; }
            var screen = cam.WorldToScreenPoint(new Vector3(anchor.position.x, top, anchor.position.z));
            var canvas = GetComponent<Canvas>();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen,
                canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera, out var local);
            var half = canvasRect.rect.size * .5f;
            float bw = box.sizeDelta.x * .5f;
            local.x = Mathf.Clamp(local.x, -half.x + bw + 8f, half.x - bw - 8f);
            local.y = Mathf.Min(local.y, half.y - Mathf.Max(box.sizeDelta.y + 24f, face.enabled ? BustSize + 8f : 0f));
            box.anchoredPosition = local;

            float t = Mathf.Clamp01((Time.unscaledTime - lineStarted) / Pop);
            box.localScale = Vector3.one * Mathf.Lerp(.94f, 1f, t);
            float pop = t < 1f ? Mathf.Lerp(.85f, 1f, t) : 1f;   // the bust springs in as the speaker changes
            faceRect.anchoredPosition = new Vector2(local.x + bw - BustRoom * .5f - 4f, local.y + Mathf.Lerp(-10f, 0f, t));
            faceRect.localScale = new Vector3(pop, pop, 1f);
        }

        private void OnDestroy()
        {
            if (Current == this) Current = null;
        }
    }
}
