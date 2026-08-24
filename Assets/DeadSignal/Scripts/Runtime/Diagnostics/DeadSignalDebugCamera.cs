using DeadSignal.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadSignal.Diagnostics
{
    /// <summary>Development camera override for inspecting rooms independently of the player-follow rig.</summary>
    public sealed class DeadSignalDebugCamera : MonoBehaviour
    {
        private const float MOVE_SPEED = 10f;

        private PlayerFollowCamera m_followCamera;
        private Vector3 m_restLocalPosition;
        private Quaternion m_restLocalRotation;

        public bool IsFree { get; private set; }

        public void Configure(PlayerFollowCamera followCamera)
        {
            m_followCamera = followCamera;
            m_restLocalPosition = transform.localPosition;
            m_restLocalRotation = transform.localRotation;
            enabled = false;
        }

        public void SetFree(bool free)
        {
            IsFree = free;
            enabled = free;
            if (m_followCamera != null)
            {
                m_followCamera.enabled = !free;
            }

            if (!free)
            {
                transform.localPosition = m_restLocalPosition;
                transform.localRotation = m_restLocalRotation;
            }
        }

        public void ShowOverview()
        {
            SetFree(true);
            transform.position = new Vector3(0f, 31f, -5f);
            transform.rotation = Quaternion.Euler(82f, 0f, 0f);
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            if (!IsFree || keyboard == null)
            {
                return;
            }

            var input = new Vector3(
                (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f),
                (keyboard.eKey.isPressed ? 1f : 0f) - (keyboard.qKey.isPressed ? 1f : 0f),
                (keyboard.wKey.isPressed ? 1f : 0f) - (keyboard.sKey.isPressed ? 1f : 0f));
            transform.position += transform.TransformDirection(Vector3.ClampMagnitude(input, 1f)) *
                                  (MOVE_SPEED * Time.unscaledDeltaTime);
        }
    }
}
