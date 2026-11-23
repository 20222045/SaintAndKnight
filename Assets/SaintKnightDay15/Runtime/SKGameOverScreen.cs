using SaintKnight.Day02;
using SaintKnight.Day04;
using SaintKnight.Day09;
using SaintKnight.Day10;
using UnityEngine;

namespace SaintKnight.Day15
{
    /// <summary>
    /// Day 15 game over: when the player's HP reaches 0 the play field darkens a little and, since
    /// 2026-11-19 (user), a small "GAME OVER" fades in at the bottom-right corner; the side HUD stays lit. It also names what finished them, and shows that monster's cutscene
    /// if one exists at Resources/SaintKnightDay15/DeathCutscenes/&lt;KEY&gt;. With a cutscene on screen
    /// the room waits for R; without one the existing auto retry runs.
    /// Display only: HP, restraint and retry rules stay with their owners.
    /// </summary>
    public sealed class SKGameOverScreen : MonoBehaviour
    {
        public const string CutsceneFolder = "SaintKnightDay15/DeathCutscenes/";
        public const float DimTarget = .5f, DimSeconds = .5f;
        private SKHealth player;
        private SKEnemyTrainingSession session;
        private SpriteRenderer dim;

        public bool IsShowing { get; private set; }
        public string CauseKey { get; private set; } = "";
        public Sprite Cutscene { get; private set; }
        /// <summary>Opacity of the black layer over the play field: 0 alive, DimTarget once down.</summary>
        public float DimAlpha => dim != null ? dim.color.a : 0f;

        public void Bind(SKHealth who, SKEnemyTrainingSession owner)
        {
            player = who;
            session = owner;
        }

        /// <summary>MONK, EXECUTIONER, BOSS, SLIME, TENTACLE, FLESHWALL or UNKNOWN.</summary>
        public static string KeyFor(Object cause)
        {
            var c = cause as Component;
            if (c == null) return "UNKNOWN";
            if (c.GetComponentInParent<SKBossAgent>() != null) return "BOSS";
            if (c.GetComponentInParent<SKFleshWall>() != null) return "FLESHWALL";
            var capture = c.GetComponentInParent<SKEnvironmentalCapture>();
            if (capture != null) return capture.Kind.ToString().ToUpperInvariant();
            if (c.GetComponentInParent<SKFloorSpike>() != null) return "TENTACLE";
            var enemy = c.GetComponentInParent<SKEnemyAgent>();
            if (enemy == null) return "UNKNOWN";
            // ponytail: enemy kind = last word of its display name ("A1 MONK" -> MONK); add a kind field if names stop following this.
            string name = enemy.DisplayName.Trim();
            return name.Substring(name.LastIndexOf(' ') + 1).ToUpperInvariant();
        }

        private void Update()
        {
            bool dead = player != null && player.IsDead;
            if (dead != IsShowing)
            {
                IsShowing = dead;
                CauseKey = dead ? KeyFor(player.LastCause) : "";
                Cutscene = dead ? Resources.Load<Sprite>(CutsceneFolder + CauseKey) : null;
                var motion = player != null ? player.GetComponent<SaintKnight.Day06.SKCharacterSpriteView>() : null;
                if (session != null) session.HoldRetry = Cutscene != null || dead && motion != null && motion.HasNativeDeathMotion;
                if (!dead && dim != null) SetDim(0f);   // a retry lights the room at once
            }
            if (dead) SetDim(Mathf.MoveTowards(DimAlpha, DimTarget, DimTarget / DimSeconds * Time.deltaTime));
        }

        // A black sprite in front of the world camera: it darkens the play field but not the overlay HUD.
        private void SetDim(float alpha)
        {
            if (dim == null)
            {
                var camera = Camera.main;
                if (camera == null) return;
                dim = new GameObject("GameOver_Dim").AddComponent<SpriteRenderer>();
                dim.transform.SetParent(camera.transform, false);
                dim.transform.localPosition = new Vector3(0f, 0f, camera.nearClipPlane + .5f);
                dim.transform.localScale = new Vector3(400f, 400f, 1f);
                dim.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(.5f, .5f), 1f);
                var layers = SortingLayer.layers;
                dim.sortingLayerID = layers[layers.Length - 1].id;
                dim.sortingOrder = SaintKnight.SpriteCook.SKNativeDefeatLibrary.DefeatSortingOrder - 1;
            }
            dim.color = new Color(0f, 0f, 0f, alpha);
            dim.enabled = alpha > 0f;
        }

        private void OnDisable()
        {
            if (session != null) session.HoldRetry = false;
            if (dim != null) SetDim(0f);
        }

        private void OnGUI()
        {
            if (!IsShowing) return;
            if (Cutscene != null)
                GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Cutscene.texture, ScaleMode.ScaleToFit);
            // 2026-11-19 user: a small "GAME OVER" in the bottom-right corner once she is down.
            if (label == null)
                label = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.LowerRight, fontStyle = FontStyle.Bold };
            label.fontSize = Mathf.Max(12, Screen.height / 40);
            float pad = Screen.height * .03f;
            var area = new Rect(0f, 0f, Screen.width - pad, Screen.height - pad);
            label.normal.textColor = new Color(0f, 0f, 0f, .8f);
            GUI.Label(new Rect(area.x + 2f, area.y + 2f, area.width, area.height), GameOverText, label);
            label.normal.textColor = new Color(.85f, .78f, .7f, Mathf.Clamp01(DimAlpha / DimTarget));
            GUI.Label(area, GameOverText, label);
        }

        public const string GameOverText = "GAME OVER";
        private GUIStyle label;
    }
}
