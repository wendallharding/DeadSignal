using System;
using UnityEngine;

namespace DeadSignal
{
    internal interface IComfortSettings
    {
        bool CameraImpulseEnabled { get; }
        bool ReducedFlashesEnabled { get; }

        event Action<bool> CameraImpulseChanged;
        event Action<bool> ReducedFlashesChanged;

        void ToggleCameraImpulse();
        void ToggleReducedFlashes();
    }

    /// <summary>
    /// Owns locally persisted presentation-comfort choices without changing gameplay rules.
    /// </summary>
    internal sealed class ComfortSettings : IComfortSettings
    {
        private const string CAMERA_IMPULSE_KEY = "DeadSignal.CameraImpulseEnabled";
        private const string REDUCED_FLASHES_KEY = "DeadSignal.ReducedFlashesEnabled";

        public ComfortSettings()
        {
            CameraImpulseEnabled = PlayerPrefs.GetInt(CAMERA_IMPULSE_KEY, 1) != 0;
            ReducedFlashesEnabled = PlayerPrefs.GetInt(REDUCED_FLASHES_KEY, 0) != 0;
        }

        public bool CameraImpulseEnabled { get; private set; }
        public bool ReducedFlashesEnabled { get; private set; }

        public event Action<bool> CameraImpulseChanged;
        public event Action<bool> ReducedFlashesChanged;

        public void ToggleCameraImpulse()
        {
            CameraImpulseEnabled = !CameraImpulseEnabled;
            PlayerPrefs.SetInt(CAMERA_IMPULSE_KEY, CameraImpulseEnabled ? 1 : 0);
            PlayerPrefs.Save();
            CameraImpulseChanged?.Invoke(CameraImpulseEnabled);
        }

        public void ToggleReducedFlashes()
        {
            ReducedFlashesEnabled = !ReducedFlashesEnabled;
            PlayerPrefs.SetInt(REDUCED_FLASHES_KEY, ReducedFlashesEnabled ? 1 : 0);
            PlayerPrefs.Save();
            ReducedFlashesChanged?.Invoke(ReducedFlashesEnabled);
        }
    }
}
