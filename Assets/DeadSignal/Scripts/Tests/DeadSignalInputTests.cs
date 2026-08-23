using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;
using DeadSignal.Missions;
using DeadSignal.Player;

namespace DeadSignal.Tests
{
    public sealed class DeadSignalInputTests
    {
        private const string FIRE_BINDING_KEY = "DeadSignal.Input.FireKeyboard";
        private const string INTERACT_BINDING_KEY = "DeadSignal.Input.InteractKeyboard";
        private const string MOVE_UP_BINDING_KEY = "DeadSignal.Input.MoveUpKeyboard";
        private const string MOVE_DOWN_BINDING_KEY = "DeadSignal.Input.MoveDownKeyboard";
        private const string MOVE_LEFT_BINDING_KEY = "DeadSignal.Input.MoveLeftKeyboard";
        private const string MOVE_RIGHT_BINDING_KEY = "DeadSignal.Input.MoveRightKeyboard";

        [Test]
        public void ResetKeyboardBindings_RestoresDefaultsAndClearsPersistence()
        {
            var hadFireBinding = PlayerPrefs.HasKey(FIRE_BINDING_KEY);
            var initialFireBinding = PlayerPrefs.GetString(FIRE_BINDING_KEY, string.Empty);
            var hadInteractBinding = PlayerPrefs.HasKey(INTERACT_BINDING_KEY);
            var initialInteractBinding = PlayerPrefs.GetString(INTERACT_BINDING_KEY, string.Empty);

            try
            {
                PlayerPrefs.SetString(FIRE_BINDING_KEY, "<Keyboard>/q");
                PlayerPrefs.SetString(INTERACT_BINDING_KEY, "<Keyboard>/tab");
                var inputType = typeof(RunModel).Assembly.GetType("DeadSignal.Player.DeadSignalInput");
                Assert.That(inputType, Is.Not.Null);
                var input = Activator.CreateInstance(inputType);
                Assert.That(_property<string>(inputType, input, "FireKeyboardBinding"), Is.EqualTo("Q"));
                Assert.That(_property<string>(inputType, input, "InteractKeyboardBinding"), Is.EqualTo("TAB"));

                _invoke(inputType, input, "BeginFireKeyboardRebind");
                Assert.That(_property<bool>(inputType, input, "IsRebinding"), Is.True);
                _invoke(inputType, input, "ResetKeyboardBindings");

                Assert.That(_property<bool>(inputType, input, "IsRebinding"), Is.False);
                Assert.That(_property<string>(inputType, input, "FireKeyboardBinding"), Is.EqualTo("SPACE"));
                Assert.That(_property<string>(inputType, input, "InteractKeyboardBinding"), Is.EqualTo("E"));
                Assert.That(PlayerPrefs.HasKey(FIRE_BINDING_KEY), Is.False);
                Assert.That(PlayerPrefs.HasKey(INTERACT_BINDING_KEY), Is.False);
                Assert.That(_property<InputPromptDevice>(inputType, input, "ActivePromptDevice"),
                    Is.EqualTo(InputPromptDevice.KeyboardMouse));
                ((IDisposable)input).Dispose();
            }
            finally
            {
                _restorePreference(FIRE_BINDING_KEY, hadFireBinding, initialFireBinding);
                _restorePreference(INTERACT_BINDING_KEY, hadInteractBinding, initialInteractBinding);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void KeyboardRebind_RejectsDuplicateAndPreservesExistingBinding()
        {
            var hadFireBinding = PlayerPrefs.HasKey(FIRE_BINDING_KEY);
            var initialFireBinding = PlayerPrefs.GetString(FIRE_BINDING_KEY, string.Empty);
            var hadInteractBinding = PlayerPrefs.HasKey(INTERACT_BINDING_KEY);
            var initialInteractBinding = PlayerPrefs.GetString(INTERACT_BINDING_KEY, string.Empty);

            try
            {
                PlayerPrefs.DeleteKey(FIRE_BINDING_KEY);
                PlayerPrefs.DeleteKey(INTERACT_BINDING_KEY);
                var inputType = typeof(RunModel).Assembly.GetType("DeadSignal.Player.DeadSignalInput");
                Assert.That(inputType, Is.Not.Null);
                var input = Activator.CreateInstance(inputType);

                _invoke(inputType, input, "BeginFireKeyboardRebind");
                var accepted = (bool)inputType.GetMethod("_tryApplyKeyboardRebind", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(input, new object[] { "<Keyboard>/e" });

                Assert.That(accepted, Is.False);
                Assert.That(_property<bool>(inputType, input, "IsRebinding"), Is.True);
                Assert.That(_property<string>(inputType, input, "FireKeyboardBinding"), Is.EqualTo("SPACE"));
                Assert.That(_property<string>(inputType, input, "RebindStatusMessage"), Is.EqualTo("E IS ALREADY ASSIGNED"));
                Assert.That(PlayerPrefs.HasKey(FIRE_BINDING_KEY), Is.False);

                _invoke(inputType, input, "CancelRebind");
                ((IDisposable)input).Dispose();
            }
            finally
            {
                _restorePreference(FIRE_BINDING_KEY, hadFireBinding, initialFireBinding);
                _restorePreference(INTERACT_BINDING_KEY, hadInteractBinding, initialInteractBinding);
                PlayerPrefs.Save();
            }
        }

        [Test]
        public void MovementBindings_LoadPersistedOverridesAndResetAllActions()
        {
            var preferences = _captureMovementPreferences();
            try
            {
                PlayerPrefs.SetString(MOVE_UP_BINDING_KEY, "<Keyboard>/i");
                PlayerPrefs.SetString(MOVE_DOWN_BINDING_KEY, "<Keyboard>/k");
                PlayerPrefs.SetString(MOVE_LEFT_BINDING_KEY, "<Keyboard>/j");
                PlayerPrefs.SetString(MOVE_RIGHT_BINDING_KEY, "<Keyboard>/l");
                var inputType = typeof(RunModel).Assembly.GetType("DeadSignal.Player.DeadSignalInput");
                var input = Activator.CreateInstance(inputType);

                Assert.That(_property<string>(inputType, input, "MoveUpKeyboardBinding"), Is.EqualTo("I"));
                Assert.That(_property<string>(inputType, input, "MoveDownKeyboardBinding"), Is.EqualTo("K"));
                Assert.That(_property<string>(inputType, input, "MoveLeftKeyboardBinding"), Is.EqualTo("J"));
                Assert.That(_property<string>(inputType, input, "MoveRightKeyboardBinding"), Is.EqualTo("L"));

                _invoke(inputType, input, "BeginMoveUpKeyboardRebind");
                _invoke(inputType, input, "ResetKeyboardBindings");

                Assert.That(_property<string>(inputType, input, "MoveUpKeyboardBinding"), Is.EqualTo("W"));
                Assert.That(_property<string>(inputType, input, "MoveDownKeyboardBinding"), Is.EqualTo("S"));
                Assert.That(_property<string>(inputType, input, "MoveLeftKeyboardBinding"), Is.EqualTo("A"));
                Assert.That(_property<string>(inputType, input, "MoveRightKeyboardBinding"), Is.EqualTo("D"));
                Assert.That(PlayerPrefs.HasKey(MOVE_UP_BINDING_KEY), Is.False);
                Assert.That(PlayerPrefs.HasKey(MOVE_DOWN_BINDING_KEY), Is.False);
                Assert.That(PlayerPrefs.HasKey(MOVE_LEFT_BINDING_KEY), Is.False);
                Assert.That(PlayerPrefs.HasKey(MOVE_RIGHT_BINDING_KEY), Is.False);
                ((IDisposable)input).Dispose();
            }
            finally
            {
                _restoreMovementPreferences(preferences);
            }
        }

        [Test]
        public void MovementRebind_RejectsEveryOtherPrimaryAction()
        {
            var preferences = _captureMovementPreferences();
            var hadFireBinding = PlayerPrefs.HasKey(FIRE_BINDING_KEY);
            var initialFireBinding = PlayerPrefs.GetString(FIRE_BINDING_KEY, string.Empty);
            var hadInteractBinding = PlayerPrefs.HasKey(INTERACT_BINDING_KEY);
            var initialInteractBinding = PlayerPrefs.GetString(INTERACT_BINDING_KEY, string.Empty);
            try
            {
                PlayerPrefs.DeleteKey(FIRE_BINDING_KEY);
                PlayerPrefs.DeleteKey(INTERACT_BINDING_KEY);
                PlayerPrefs.DeleteKey(MOVE_UP_BINDING_KEY);
                PlayerPrefs.DeleteKey(MOVE_DOWN_BINDING_KEY);
                PlayerPrefs.DeleteKey(MOVE_LEFT_BINDING_KEY);
                PlayerPrefs.DeleteKey(MOVE_RIGHT_BINDING_KEY);
                var inputType = typeof(RunModel).Assembly.GetType("DeadSignal.Player.DeadSignalInput");
                var input = Activator.CreateInstance(inputType);
                var tryApply = inputType.GetMethod("_tryApplyKeyboardRebind", BindingFlags.Instance | BindingFlags.NonPublic);

                _invoke(inputType, input, "BeginMoveRightKeyboardRebind");
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/space" }), Is.False);
                Assert.That(_property<string>(inputType, input, "RebindStatusMessage"), Is.EqualTo("SPACE IS ALREADY ASSIGNED"));
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/e" }), Is.False);
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/w" }), Is.False);
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/s" }), Is.False);
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/a" }), Is.False);
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/upArrow" }), Is.False);
                Assert.That(_property<string>(inputType, input, "MoveRightKeyboardBinding"), Is.EqualTo("D"));
                Assert.That((bool)tryApply.Invoke(input, new object[] { "<Keyboard>/l" }), Is.True);
                Assert.That(_property<string>(inputType, input, "MoveRightKeyboardBinding"), Is.EqualTo("L"));
                ((IDisposable)input).Dispose();
            }
            finally
            {
                _restoreMovementPreferences(preferences);
                _restorePreference(FIRE_BINDING_KEY, hadFireBinding, initialFireBinding);
                _restorePreference(INTERACT_BINDING_KEY, hadInteractBinding, initialInteractBinding);
                PlayerPrefs.Save();
            }
        }

        private static T _property<T>(Type type, object instance, string name)
        {
            return (T)type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public).GetValue(instance);
        }

        private static void _invoke(Type type, object instance, string name)
        {
            type.GetMethod(name, BindingFlags.Instance | BindingFlags.Public).Invoke(instance, null);
        }

        private static void _restorePreference(string key, bool existed, string value)
        {
            if (existed)
            {
                PlayerPrefs.SetString(key, value);
            }
            else
            {
                PlayerPrefs.DeleteKey(key);
            }
        }

        private static (string key, bool existed, string value)[] _captureMovementPreferences()
        {
            var keys = new[] { MOVE_UP_BINDING_KEY, MOVE_DOWN_BINDING_KEY, MOVE_LEFT_BINDING_KEY, MOVE_RIGHT_BINDING_KEY };
            var preferences = new (string key, bool existed, string value)[keys.Length];
            for (var i = 0; i < keys.Length; i++)
            {
                preferences[i] = (keys[i], PlayerPrefs.HasKey(keys[i]), PlayerPrefs.GetString(keys[i], string.Empty));
            }

            return preferences;
        }

        private static void _restoreMovementPreferences((string key, bool existed, string value)[] preferences)
        {
            foreach (var preference in preferences)
            {
                _restorePreference(preference.key, preference.existed, preference.value);
            }

            PlayerPrefs.Save();
        }
    }
}
