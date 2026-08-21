using System;
using UnityEngine;

namespace DeadSignal
{
    internal interface IComfortSettings
    {
        bool CameraImpulseEnabled { get; }
        bool ReducedFlashesEnabled { get; }
        bool HighContrastEnabled { get; }
        bool AudioEnabled { get; }

        event Action<bool> CameraImpulseChanged;
        event Action<bool> ReducedFlashesChanged;
        event Action<bool> HighContrastChanged;
        event Action<bool> AudioEnabledChanged;

        void ToggleCameraImpulse();
        void ToggleReducedFlashes();
        void ToggleHighContrast();
        void ToggleAudio();
    }

    /// <summary>
    /// Owns locally persisted presentation-comfort choices without changing gameplay rules.
    /// </summary>
    internal sealed class ComfortSettings : IComfortSettings
    {
        private const string CAMERA_IMPULSE_KEY = "DeadSignal.CameraImpulseEnabled";
        private const string REDUCED_FLASHES_KEY = "DeadSignal.ReducedFlashesEnabled";
        private const string HIGH_CONTRAST_KEY = "DeadSignal.HighContrastEnabled";
        private const string AUDIO_ENABLED_KEY = "DeadSignal.AudioEnabled";

        public ComfortSettings()
        {
            CameraImpulseEnabled = PlayerPrefs.GetInt(CAMERA_IMPULSE_KEY, 1) != 0;
            ReducedFlashesEnabled = PlayerPrefs.GetInt(REDUCED_FLASHES_KEY, 0) != 0;
            HighContrastEnabled = PlayerPrefs.GetInt(HIGH_CONTRAST_KEY, 0) != 0;
            AudioEnabled = PlayerPrefs.GetInt(AUDIO_ENABLED_KEY, 1) != 0;
        }

        public bool CameraImpulseEnabled { get; private set; }
        public bool ReducedFlashesEnabled { get; private set; }
        public bool HighContrastEnabled { get; private set; }
        public bool AudioEnabled { get; private set; }

        public event Action<bool> CameraImpulseChanged;
        public event Action<bool> ReducedFlashesChanged;
        public event Action<bool> HighContrastChanged;
        public event Action<bool> AudioEnabledChanged;

        public void ToggleCameraImpulse()
        {
            CameraImpulseEnabled = !CameraImpulseEnabled;
            _savePreference(CAMERA_IMPULSE_KEY, CameraImpulseEnabled);
            CameraImpulseChanged?.Invoke(CameraImpulseEnabled);
        }

        public void ToggleReducedFlashes()
        {
            ReducedFlashesEnabled = !ReducedFlashesEnabled;
            _savePreference(REDUCED_FLASHES_KEY, ReducedFlashesEnabled);
            ReducedFlashesChanged?.Invoke(ReducedFlashesEnabled);
        }

        public void ToggleHighContrast()
        {
            HighContrastEnabled = !HighContrastEnabled;
            _savePreference(HIGH_CONTRAST_KEY, HighContrastEnabled);
            HighContrastChanged?.Invoke(HighContrastEnabled);
        }

        public void ToggleAudio()
        {
            AudioEnabled = !AudioEnabled;
            _savePreference(AUDIO_ENABLED_KEY, AudioEnabled);
            AudioEnabledChanged?.Invoke(AudioEnabled);
        }

        private void _savePreference(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }
}
