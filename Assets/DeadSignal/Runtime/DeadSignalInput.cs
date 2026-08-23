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
        string RebindStatusMessage { get; }
        string FireKeyboardBinding { get; }
        string InteractKeyboardBinding { get; }
        string MoveUpKeyboardBinding { get; }
        string MoveDownKeyboardBinding { get; }
        string MoveLeftKeyboardBinding { get; }
        string MoveRightKeyboardBinding { get; }

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
        bool PressedGuidanceToggle();
        bool PressedDifficultyToggle();
        void BeginFireKeyboardRebind();
        void BeginInteractKeyboardRebind();
        void BeginMoveUpKeyboardRebind();
        void BeginMoveDownKeyboardRebind();
        void BeginMoveLeftKeyboardRebind();
        void BeginMoveRightKeyboardRebind();
        void ResetKeyboardBindings();
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
        private const string MOVE_UP_BINDING_KEY = "DeadSignal.Input.MoveUpKeyboard";
        private const string MOVE_DOWN_BINDING_KEY = "DeadSignal.Input.MoveDownKeyboard";
        private const string MOVE_LEFT_BINDING_KEY = "DeadSignal.Input.MoveLeftKeyboard";
        private const string MOVE_RIGHT_BINDING_KEY = "DeadSignal.Input.MoveRightKeyboard";

        private readonly InputAction m_fireAction;
        private readonly InputAction m_interactAction;
        private readonly InputAction m_moveUpAction;
        private readonly InputAction m_moveDownAction;
        private readonly InputAction m_moveLeftAction;
        private readonly InputAction m_moveRightAction;
        private InputAction m_rebindingAction;
        private int m_rebindingIndex;
        private string m_rebindingPreferenceKey;

        public InputPromptDevice ActivePromptDevice { get; private set; } = InputPromptDevice.KeyboardMouse;
        public bool IsRebinding => m_rebindingAction != null;
        public string RebindStatusMessage { get; private set; } = string.Empty;
        public string FireKeyboardBinding => _keyboardBindingName(m_fireAction, 1);
        public string InteractKeyboardBinding => _keyboardBindingName(m_interactAction, 0);
        public string MoveUpKeyboardBinding => _keyboardBindingName(m_moveUpAction, 0);
        public string MoveDownKeyboardBinding => _keyboardBindingName(m_moveDownAction, 0);
        public string MoveLeftKeyboardBinding => _keyboardBindingName(m_moveLeftAction, 0);
        public string MoveRightKeyboardBinding => _keyboardBindingName(m_moveRightAction, 0);

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
            m_moveUpAction = _createMovementAction("Move Up", "<Keyboard>/w", "<Keyboard>/upArrow");
            m_moveDownAction = _createMovementAction("Move Down", "<Keyboard>/s", "<Keyboard>/downArrow");
            m_moveLeftAction = _createMovementAction("Move Left", "<Keyboard>/a", "<Keyboard>/leftArrow");
            m_moveRightAction = _createMovementAction("Move Right", "<Keyboard>/d", "<Keyboard>/rightArrow");
            _loadOverride(m_fireAction, 1, FIRE_BINDING_KEY);
            _loadOverride(m_interactAction, 0, INTERACT_BINDING_KEY);
            _loadOverride(m_moveUpAction, 0, MOVE_UP_BINDING_KEY);
            _loadOverride(m_moveDownAction, 0, MOVE_DOWN_BINDING_KEY);
            _loadOverride(m_moveLeftAction, 0, MOVE_LEFT_BINDING_KEY);
            _loadOverride(m_moveRightAction, 0, MOVE_RIGHT_BINDING_KEY);
            m_fireAction.Enable();
            m_interactAction.Enable();
        }

        public Vector2 ReadMovement()
        {
            var keyboardMovement = Vector2.zero;
            if (Keyboard.current != null)
            {
                keyboardMovement.x = (m_moveRightAction.IsPressed() ? 1f : 0f) - (m_moveLeftAction.IsPressed() ? 1f : 0f);
                keyboardMovement.y = (m_moveUpAction.IsPressed() ? 1f : 0f) - (m_moveDownAction.IsPressed() ? 1f : 0f);
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
        public void BeginMoveUpKeyboardRebind() => _beginKeyboardRebind(m_moveUpAction, 0, MOVE_UP_BINDING_KEY);
        public void BeginMoveDownKeyboardRebind() => _beginKeyboardRebind(m_moveDownAction, 0, MOVE_DOWN_BINDING_KEY);
        public void BeginMoveLeftKeyboardRebind() => _beginKeyboardRebind(m_moveLeftAction, 0, MOVE_LEFT_BINDING_KEY);
        public void BeginMoveRightKeyboardRebind() => _beginKeyboardRebind(m_moveRightAction, 0, MOVE_RIGHT_BINDING_KEY);

        public void ResetKeyboardBindings()
        {
            CancelRebind();
            m_fireAction.RemoveBindingOverride(1);
            m_interactAction.RemoveBindingOverride(0);
            m_moveUpAction.RemoveBindingOverride(0);
            m_moveDownAction.RemoveBindingOverride(0);
            m_moveLeftAction.RemoveBindingOverride(0);
            m_moveRightAction.RemoveBindingOverride(0);
            PlayerPrefs.DeleteKey(FIRE_BINDING_KEY);
            PlayerPrefs.DeleteKey(INTERACT_BINDING_KEY);
            PlayerPrefs.DeleteKey(MOVE_UP_BINDING_KEY);
            PlayerPrefs.DeleteKey(MOVE_DOWN_BINDING_KEY);
            PlayerPrefs.DeleteKey(MOVE_LEFT_BINDING_KEY);
            PlayerPrefs.DeleteKey(MOVE_RIGHT_BINDING_KEY);
            PlayerPrefs.Save();
            RebindStatusMessage = string.Empty;
            _useKeyboardMouse();
        }

        public void CancelRebind()
        {
            if (m_rebindingAction == null)
            {
                return;
            }

            m_rebindingAction.Enable();
            m_rebindingAction = null;
            RebindStatusMessage = string.Empty;
        }

        public void Dispose()
        {
            m_fireAction.Dispose();
            m_interactAction.Dispose();
            m_moveUpAction.Dispose();
            m_moveDownAction.Dispose();
            m_moveLeftAction.Dispose();
            m_moveRightAction.Dispose();
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

        public bool PressedGuidanceToggle()
        {
            return _pressed(Keyboard.current?.gKey, Gamepad.current?.leftStickButton);
        }

        public bool PressedDifficultyToggle()
        {
            return _pressed(Keyboard.current?.vKey, Gamepad.current?.rightStickButton);
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
            RebindStatusMessage = string.Empty;
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
                    if (_tryApplyKeyboardRebind(path))
                    {
                        return;
                    }
                }
            }
        }

        private bool _tryApplyKeyboardRebind(string path)
        {
            foreach (var binding in _primaryKeyboardBindings())
            {
                if (binding.action == m_rebindingAction ||
                    !string.Equals(path, binding.action.bindings[binding.index].effectivePath, System.StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                RebindStatusMessage = $"{_keyboardBindingName(binding.action, binding.index)} IS ALREADY ASSIGNED";
                return false;
            }

            m_rebindingAction.ApplyBindingOverride(m_rebindingIndex, path);
            PlayerPrefs.SetString(m_rebindingPreferenceKey, path);
            PlayerPrefs.Save();
            m_rebindingAction.Enable();
            m_rebindingAction = null;
            RebindStatusMessage = string.Empty;
            _useKeyboardMouse();
            return true;
        }

        private static void _loadOverride(InputAction action, int bindingIndex, string preferenceKey)
        {
            var path = PlayerPrefs.GetString(preferenceKey, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                action.ApplyBindingOverride(bindingIndex, path);
            }
        }

        private static InputAction _createMovementAction(string name, string primaryPath, string fallbackPath)
        {
            var action = new InputAction(name, InputActionType.Button);
            action.AddBinding(primaryPath);
            action.AddBinding(fallbackPath);
            action.Enable();
            return action;
        }

        private (InputAction action, int index)[] _primaryKeyboardBindings()
        {
            return new[]
            {
                (m_fireAction, 1),
                (m_interactAction, 0),
                (m_moveUpAction, 0),
                (m_moveUpAction, 1),
                (m_moveDownAction, 0),
                (m_moveDownAction, 1),
                (m_moveLeftAction, 0),
                (m_moveLeftAction, 1),
                (m_moveRightAction, 0),
                (m_moveRightAction, 1)
            };
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
