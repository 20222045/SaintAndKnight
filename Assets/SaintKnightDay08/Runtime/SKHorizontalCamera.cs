using UnityEngine;

namespace SaintKnight.Day08
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class SKHorizontalCamera : MonoBehaviour
    {
        private const float ViewAspect = 16f / 9f;

        [SerializeField] private Transform target;
        [Tooltip("Left and right limits of the visible level in world units.")]
        [SerializeField] private Vector2 horizontalBounds;

        private Camera gameplayCamera;
        private Rect gameplaySlot;
        private Vector2Int screenSize;
        private float restY;

        private void Awake()
        {
            gameplayCamera = GetComponent<Camera>();
            gameplaySlot = gameplayCamera.rect;
            restY = transform.position.y;
        }

        /// <summary>Share of the view's width (from the left) kept clear for the HUD illustration.</summary>
        public float LeftInset { get; set; }

        /// <summary>2026-11-16: the play field fills the screen (HUD floats over it). Refits at once.</summary>
        public void SetSlot(Rect slot)
        {
            if (gameplayCamera == null) Awake();
            gameplaySlot = slot;
            screenSize = Vector2Int.zero;
            FitViewport();
        }

        private void OnEnable()
        {
            screenSize = Vector2Int.zero;
            LateUpdate();
        }

        private void OnDisable()
        {
            gameplayCamera.rect = gameplaySlot;
            gameplayCamera.ResetAspect();
        }

        private void LateUpdate()
        {
            FitViewport();
            if (target == null) return;

            float halfWidth = gameplayCamera.orthographicSize * gameplayCamera.aspect;
            // LeftInset: share of the view on the left that the HUD illustration covers. The level's left end
            // stops at that line, so the player is never walked in under the illustration.
            float min = horizontalBounds.x + halfWidth - LeftInset * 2f * halfWidth;
            float max = horizontalBounds.y - halfWidth;
            var position = transform.position;
            // Keep the full view inside the level. If the level is narrower than the view, center it.
            position.x = min <= max ? Mathf.Clamp(target.position.x, min, max) : (min + max) * .5f;
            position.y = restY + SaintKnight.Day18.SKScreenShake.Offset;   // Day 18 hit shake
            transform.position = position;
        }

        private void FitViewport()
        {
            var size = new Vector2Int(Screen.width, Screen.height);
            if (size == screenSize || size.x <= 0 || size.y <= 0) return;
            screenSize = size;

            // Fit the 16:9 world view into its reserved slot, leaving the screen-space HUD in place.
            float slotAspect = size.x * gameplaySlot.width / (size.y * gameplaySlot.height);
            float width = gameplaySlot.width * Mathf.Min(1f, ViewAspect / slotAspect);
            float height = gameplaySlot.height * Mathf.Min(1f, slotAspect / ViewAspect);
            gameplayCamera.rect = new Rect(gameplaySlot.center - new Vector2(width, height) * .5f,
                new Vector2(width, height));
            gameplayCamera.aspect = ViewAspect;
        }
    }
}
