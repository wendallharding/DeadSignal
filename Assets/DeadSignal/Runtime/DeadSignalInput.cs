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
        bool IsRebinding { get; }
        string FireKeyboardBinding { get; }
        string InteractKeyboardBinding { get; }

        Vector2 ReadMovement();
        Vector3 ReadAimDirection(Camera camera, Transform player);
        bool PressedFire();
        bool PressedInteract();
        bool PressedRestart();
        bool PressedPause();
        bool PressedCameraImpulseToggle();
        bool PressedReducedFlashesToggle();
        bool PressedHighContrastToggle();
        bool PressedAudioToggle();
        void BeginFireKeyboardRebind();
        void BeginInteractKeyboardRebind();
        void CancelRebind();
    }

    /// <summary>
    /// Centralizes device polling and remembers the last device used for adaptive control prompts.
    /// </summary>
    internal sealed class DeadSignalInput : IDeadSignalInput, System.IDisposable
    {
        private const float GAMEPAD_STICK_DEADZONE = 0.18f;
        private const float MOUSE_DELTA_THRESHOLD = 0.5f;
        private const string FIRE_BINDING_KEY = "DeadSignal.Input.FireKeyboard";
        private const string INTERACT_BINDING_KEY = "DeadSignal.Input.InteractKeyboard";

        private readonly InputAction m_fireAction;
        private readonly InputAction m_interactAction;
        private InputAction m_rebindingAction;
        private int m_rebindingIndex;
        private string m_rebindingPreferenceKey;

        public InputPromptDevice ActivePromptDevice { get; private set; } = InputPromptDevice.KeyboardMouse;
        public bool IsRebinding => m_rebindingAction != null;
        public string FireKeyboardBinding => _keyboardBindingName(m_fireAction, 1);
        public string InteractKeyboardBinding => _keyboardBindingName(m_interactAction, 0);

        public DeadSignalInput()
        {
            m_fireAction = new InputAction("Fire", InputActionType.Button);
            m_fireAction.AddBinding("<Mouse>/leftButton");
            m_fireAction.AddBinding("<Keyboard>/space");
            m_fireAction.AddBinding("<Gamepad>/rightTrigger");
            m_fireAction.AddBinding("<Gamepad>/rightShoulder");
            m_interactAction = new InputAction("Interact", InputActionType.Button);
            m_interactAction.AddBinding("<Keyboard>/e");
            m_interactAction.AddBinding("<Gamepad>/buttonWest");
            _loadOverride(m_fireAction, 1, FIRE_BINDING_KEY);
            _loadOverride(m_interactAction, 0, INTERACT_BINDING_KEY);
            m_fireAction.Enable();
            m_interactAction.Enable();
        }

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
            if (m_fireAction.WasPressedThisFrame())
            {
                if (Gamepad.current != null &&
                    (Gamepad.current.rightTrigger.wasPressedThisFrame || Gamepad.current.rightShoulder.wasPressedThisFrame))
                {
                    _useGamepad();
                }
                else
                {
                    _useKeyboardMouse();
                }
                return true;
            }

            return false;
        }

        public bool PressedInteract()
        {
            if (!m_interactAction.WasPressedThisFrame())
            {
                return false;
            }

            if (Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame)
            {
                _useGamepad();
            }
            else
            {
                _useKeyboardMouse();
            }

            return true;
        }

        public void BeginFireKeyboardRebind() => _beginKeyboardRebind(m_fireAction, 1, FIRE_BINDING_KEY);

        public void BeginInteractKeyboardRebind() => _beginKeyboardRebind(m_interactAction, 0, INTERACT_BINDING_KEY);

        public void CancelRebind()
        {
            if (m_rebindingAction == null)
            {
                return;
            }

            m_rebindingAction.Enable();
            m_rebindingAction = null;
        }

        public void Dispose()
        {
            m_fireAction.Dispose();
            m_interactAction.Dispose();
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
            if (IsRebinding)
            {
                _captureKeyboardRebind();
                return false;
            }

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

        public bool PressedAudioToggle()
        {
            return _pressed(Keyboard.current?.mKey, Gamepad.current?.dpad.left);
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

        private void _beginKeyboardRebind(InputAction action, int bindingIndex, string preferenceKey)
        {
            CancelRebind();
            action.Disable();
            m_rebindingAction = action;
            m_rebindingIndex = bindingIndex;
            m_rebindingPreferenceKey = preferenceKey;
        }

        private void _captureKeyboardRebind()
        {
            foreach (var device in InputSystem.devices)
            {
                if (device is not Keyboard keyboard)
                {
                    continue;
                }

                foreach (var control in keyboard.allKeys)
                {
                    if (!control.wasPressedThisFrame)
                    {
                        continue;
                    }

                    if (control == keyboard.escapeKey)
                    {
                        CancelRebind();
                        return;
                    }

                    var path = $"<Keyboard>/{control.name}";
                    m_rebindingAction.ApplyBindingOverride(m_rebindingIndex, path);
                    PlayerPrefs.SetString(m_rebindingPreferenceKey, path);
                    PlayerPrefs.Save();
                    m_rebindingAction.Enable();
                    m_rebindingAction = null;
                    _useKeyboardMouse();
                    return;
                }
            }
        }

        private static void _loadOverride(InputAction action, int bindingIndex, string preferenceKey)
        {
            var path = PlayerPrefs.GetString(preferenceKey, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                action.ApplyBindingOverride(bindingIndex, path);
            }
        }

        private static string _keyboardBindingName(InputAction action, int bindingIndex)
        {
            return action.GetBindingDisplayString(bindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames)
                .ToUpperInvariant();
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
