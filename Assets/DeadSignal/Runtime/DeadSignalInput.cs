using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace DeadSignal
{
    public enum InputPromptDevice
    {
        KeyboardMouse,
        Gamepad
    }

    internal interface IDeadSignalInput
    {
        InputPromptDevice ActivePromptDevice { get; }

        Vector2 ReadMovement();
        Vector3 ReadAimDirection(Camera camera, Transform player);
        bool PressedFire();
        bool PressedInteract();
        bool PressedRestart();
        bool PressedPause();
        bool PressedCameraImpulseToggle();
        bool PressedReducedFlashesToggle();
        bool PressedHighContrastToggle();
    }

    /// <summary>
    /// Centralizes device polling and remembers the last device used for adaptive control prompts.
    /// </summary>
    internal sealed class DeadSignalInput : IDeadSignalInput
    {
        private const float GAMEPAD_STICK_DEADZONE = 0.18f;
        private const float MOUSE_DELTA_THRESHOLD = 0.5f;

        public InputPromptDevice ActivePromptDevice { get; private set; } = InputPromptDevice.KeyboardMouse;

        public Vector2 ReadMovement()
        {
            var keyboard = Keyboard.current;
            var keyboardMovement = Vector2.zero;
            if (keyboard != null)
            {
                keyboardMovement.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f) -
                                     (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
                keyboardMovement.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f) -
                                     (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
                if (keyboardMovement.sqrMagnitude > 0f)
                {
                    _useKeyboardMouse();
                }
            }

            var gamepadMovement = Gamepad.current != null ? Gamepad.current.leftStick.ReadValue() : Vector2.zero;
            if (gamepadMovement.sqrMagnitude < GAMEPAD_STICK_DEADZONE * GAMEPAD_STICK_DEADZONE)
            {
                gamepadMovement = Vector2.zero;
            }
            else
            {
                _useGamepad();
            }

            return Vector2.ClampMagnitude(keyboardMovement + gamepadMovement, 1f);
        }

        public Vector3 ReadAimDirection(Camera camera, Transform player)
        {
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                var stick = gamepad.rightStick.ReadValue();
                if (stick.sqrMagnitude >= GAMEPAD_STICK_DEADZONE * GAMEPAD_STICK_DEADZONE)
                {
                    _useGamepad();
                    return new Vector3(stick.x, 0f, stick.y).normalized;
                }
            }

            var mouse = Mouse.current;
            if (mouse == null || camera == null)
            {
                return player != null ? player.forward : Vector3.forward;
            }

            if (mouse.delta.ReadValue().sqrMagnitude >= MOUSE_DELTA_THRESHOLD * MOUSE_DELTA_THRESHOLD)
            {
                _useKeyboardMouse();
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

        public bool PressedFire()
        {
            if ((Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) ||
                (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                _useKeyboardMouse();
                return true;
            }

            if (Gamepad.current != null && (Gamepad.current.rightTrigger.wasPressedThisFrame ||
                                            Gamepad.current.rightShoulder.wasPressedThisFrame))
            {
                _useGamepad();
                return true;
            }

            return false;
        }

        public bool PressedInteract()
        {
            return _pressed(Keyboard.current?.eKey, Gamepad.current?.buttonWest);
        }

        public bool PressedRestart()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.rKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame))
            {
                _useKeyboardMouse();
                return true;
            }

            return _pressed(null, Gamepad.current?.buttonSouth);
        }

        public bool PressedPause()
        {
            return _pressed(Keyboard.current?.escapeKey, Gamepad.current?.startButton);
        }

        public bool PressedCameraImpulseToggle()
        {
            return _pressed(Keyboard.current?.cKey, Gamepad.current?.buttonNorth);
        }

        public bool PressedReducedFlashesToggle()
        {
            return _pressed(Keyboard.current?.fKey, Gamepad.current?.dpad.down);
        }

        public bool PressedHighContrastToggle()
        {
            return _pressed(Keyboard.current?.hKey, Gamepad.current?.dpad.up);
        }

        private bool _pressed(ButtonControl keyboardButton, ButtonControl gamepadButton)
        {
            if (keyboardButton != null && keyboardButton.wasPressedThisFrame)
            {
                _useKeyboardMouse();
                return true;
            }

            if (gamepadButton != null && gamepadButton.wasPressedThisFrame)
            {
                _useGamepad();
                return true;
            }

            return false;
        }

        private void _useKeyboardMouse()
        {
            ActivePromptDevice = InputPromptDevice.KeyboardMouse;
        }

        private void _useGamepad()
        {
            ActivePromptDevice = InputPromptDevice.Gamepad;
        }
    }
}
