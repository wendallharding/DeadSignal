using UnityEngine;
using UnityEngine.InputSystem;

namespace DeadSignal
{
    /// <summary>
    /// Centralizes device polling so gameplay orchestration does not depend on binding details.
    /// </summary>
    internal static class DeadSignalInput
    {
        private const float GAMEPAD_STICK_DEADZONE = 0.18f;

        public static Vector2 ReadMovement()
        {
            var keyboard = Keyboard.current;
            var keyboardMovement = Vector2.zero;
            if (keyboard != null)
            {
                keyboardMovement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                                     (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                keyboardMovement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                                     (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
            }

            var gamepadMovement = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            if (gamepadMovement.sqrMagnitude < GAMEPAD_STICK_DEADZONE * GAMEPAD_STICK_DEADZONE)
            {
                gamepadMovement = Vector2.zero;
            }

            return Vector2.ClampMagnitude(keyboardMovement + gamepadMovement, 1f);
        }

        public static Vector3 ReadAimDirection(Camera camera, Transform player)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude >= GAMEPAD_STICK_DEADZONE * GAMEPAD_STICK_DEADZONE)
                {
                    return new Vector3(stick.x, 0f, stick.y).normalized;
                }
            }

            var mouse = Mouse.current;
            if (mouse == null || camera == null)
            {
                return player != null ? player.forward : Vector3.forward;
            }

            var ray = camera.ScreenPointToRay(mouse.position.ReadValue());
            var deck = new Plane(Vector3.up, Vector3.zero);
            if (deck.Raycast(ray, out float distance))
            {
                var direction = ray.GetPoint(distance) - player.position;
                direction.y = 0f;
                return direction.normalized;
            }

            return player.forward;
        }

        public static bool PressedFire()
        {
            return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                   (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && (Gamepad.current.rightTrigger.wasPressedThisFrame ||
                                                Gamepad.current.rightShoulder.wasPressedThisFrame));
        }

        public static bool PressedInteract()
        {
            return (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame);
        }

        public static bool PressedRestart()
        {
            var keyboard = Keyboard.current;
            return (keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame)) ||
                   (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        }

        public static bool PressedPause()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
        }

        public static bool PressedCameraImpulseToggle()
        {
            return (Keyboard.current != null && Keyboard.current.cKey.wasPressedThisFrame) ||
                   (Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame);
        }
    }
}
