using NUnit.Framework;
using System;
using System.Reflection;
using UnityEngine;

namespace DeadSignal.Tests
{
    public sealed class DeadSignalInputTests
    {
        private const string FIRE_BINDING_KEY = "DeadSignal.Input.FireKeyboard";
        private const string INTERACT_BINDING_KEY = "DeadSignal.Input.InteractKeyboard";

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
                var inputType = typeof(RunModel).Assembly.GetType("DeadSignal.DeadSignalInput");
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
                var inputType = typeof(RunModel).Assembly.GetType("DeadSignal.DeadSignalInput");
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
    }
}
