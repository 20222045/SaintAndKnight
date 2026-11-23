using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace SaintKnight.Day02
{
    public sealed class SKTrainingSession : MonoBehaviour
    {
        [SerializeField] private SKHealth player;
        [SerializeField] private SKHealth target;
        [SerializeField] private Text status;
        private Vector3 startPosition;

        private void Start() => startPosition = player.transform.position;

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            bool reset = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
#else
            bool reset = Input.GetKeyDown(KeyCode.R);
#endif
            if (reset) ResetTraining();
            if (status != null)
                status.text = "DAY 02  |  TRAINING TARGET  " + Mathf.CeilToInt(target.CurrentHp) +
                    " / " + Mathf.CeilToInt(target.MaximumHp) + "\n" +
                    (player.IsDead ? "PLAYER DOWN - R TO RESET" : target.GetComponent<SKTrainingTarget>().Status);
        }

        public void ResetTraining()
        {
            SKHitStop.CancelActive();
            player.ResetHealth();
            target.ResetHealth();
            player.GetComponent<SKPlayerCombat>().ResetCombat();
            target.GetComponent<SKTrainingTarget>().ResetCombat();
            player.transform.position = startPosition;
            player.GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;
        }
    }
}
