using System;
using UnityEngine;

namespace DeadSignal
{
    internal interface IComfortSettings
    {
        bool CameraImpulseEnabled { get; }

        event Action<bool> CameraImpulseChanged;

        void ToggleCameraImpulse();
    }

    /// <summary>
    /// Owns locally persisted presentation-comfort choices without changing gameplay rules.
    /// </summary>
    internal sealed class ComfortSettings : IComfortSettings
    {
        private const string CAMERA_IMPULSE_KEY = "DeadSignal.CameraImpulseEnabled";

        public ComfortSettings()
        {
            CameraImpulseEnabled = PlayerPrefs.GetInt(CAMERA_IMPULSE_KEY, 1) != 0;
        }

        public bool CameraImpulseEnabled { get; private set; }

        public event Action<bool> CameraImpulseChanged;

        public void ToggleCameraImpulse()
        {
            CameraImpulseEnabled = !CameraImpulseEnabled;
            PlayerPrefs.SetInt(CAMERA_IMPULSE_KEY, CameraImpulseEnabled ? 1 : 0);
            PlayerPrefs.Save();
            CameraImpulseChanged?.Invoke(CameraImpulseEnabled);
        }
    }
}
