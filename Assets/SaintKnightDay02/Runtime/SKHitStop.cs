using UnityEngine;
using UnityEngine.SceneManagement;

namespace SaintKnight.Day02
{
    /// <summary>Scene-local hit pause. Combat timers and physics pause; UI/reset still update.</summary>
    public sealed class SKHitStop : MonoBehaviour
    {
        private static SKHitStop instance;
        private bool holdingTime;
        private float resumeAt;
        private float previousTimeScale;
        public static bool IsActive => instance != null && instance.holdingTime;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => instance = null;

        public static void Request(float seconds, Scene scene)
        {
            if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            if (!IsActive && Time.timeScale <= 0f) return; // Do not take ownership of an existing pause.
            if (instance == null)
            {
                var go = new GameObject("Combat_HitStop");
                SceneManager.MoveGameObjectToScene(go, scene);
                instance = go.AddComponent<SKHitStop>();
            }
            if (!instance.holdingTime)
            {
                instance.previousTimeScale = Time.timeScale;
                instance.holdingTime = true;
                instance.resumeAt = Time.unscaledTime;
                Time.timeScale = 0f;
            }
            // Concurrent hits extend to the longest remaining pause, never add durations.
            instance.resumeAt = Mathf.Max(instance.resumeAt, Time.unscaledTime + Mathf.Min(seconds, .1f));
        }

        private void Update()
        {
            if (holdingTime && (Time.unscaledTime >= resumeAt || Time.timeScale != 0f)) Release();
        }

        public static void CancelActive()
        {
            if (instance != null) instance.Release();
        }

        private void Release()
        {
            if (!holdingTime) return;
            // Respect another system that has already changed the speed.
            if (Time.timeScale == 0f) Time.timeScale = previousTimeScale;
            holdingTime = false;
        }

        private void OnDisable() => Release();
        private void OnDestroy()
        {
            Release();
            if (instance == this) instance = null;
        }
    }
}
