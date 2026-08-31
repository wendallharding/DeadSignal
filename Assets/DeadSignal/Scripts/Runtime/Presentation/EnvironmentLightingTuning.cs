using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal.Presentation
{
    public enum EnvironmentLightRole
    {
        DominantTask,
        SecondaryTask,
        Practical,
        Navigation
    }

    [Serializable]
    public sealed class EnvironmentLightProfile
    {
        [SerializeField] private string m_name;
        [SerializeField] private EnvironmentLightRole m_role;
        [SerializeField] private Color m_color = Color.white;
        [SerializeField, Min(0.1f)] private float m_range = 5f;
        [SerializeField, Min(0f)] private float m_intensity = 1f;
        [SerializeField, Range(0f, 1f)] private float m_dormantIntensityMultiplier = 1f;

        public string Name => m_name;
        public EnvironmentLightRole Role => m_role;
        public Color Color => m_color;
        public float Range => m_range;
        public float Intensity => m_intensity;
        public float DormantIntensityMultiplier => m_dormantIntensityMultiplier;

        public EnvironmentLightProfile(
            string name,
            EnvironmentLightRole role,
            Color color,
            float range,
            float intensity,
            float dormantIntensityMultiplier = 1f)
        {
            m_name = name;
            m_role = role;
            m_color = color;
            m_range = range;
            m_intensity = intensity;
            m_dormantIntensityMultiplier = dormantIntensityMultiplier;
        }

        public void Validate()
        {
            m_name = string.IsNullOrWhiteSpace(m_name) ? "Unnamed practical" : m_name.Trim();
            m_range = Mathf.Max(0.1f, m_range);
            m_intensity = Mathf.Max(0f, m_intensity);
            m_dormantIntensityMultiplier = Mathf.Clamp01(m_dormantIntensityMultiplier);
        }
    }

    /// <summary>
    /// Owns the adjustable environment-light, grade, emission, accessibility, and performance budgets.
    /// Room-specific lighting passes consume these values without becoming gameplay-visibility authority.
    /// </summary>
    [CreateAssetMenu(fileName = "EnvironmentLightingTuning", menuName = "Dead Signal/Tuning/Environment Lighting")]
    public sealed class EnvironmentLightingTuning : ScriptableObject
    {
        [Header("Global key and ambient floor")]
        [SerializeField] private Color m_keyLightColor = new(0.38f, 0.52f, 0.65f, 1f);
        [SerializeField, Min(0f)] private float m_keyLightIntensity = 1.35f;
        [SerializeField] private LightShadows m_keyLightShadows = LightShadows.Soft;
        [SerializeField] private Color m_ambientFloor = new(0.045f, 0.055f, 0.07f, 1f);
        [SerializeField] private Color m_highContrastAmbientFloor = new(0.075f, 0.085f, 0.1f, 1f);
        [SerializeField] private Color m_cameraBackground = new(0.002f, 0.004f, 0.008f, 1f);

        [Header("Atmosphere")]
        [SerializeField] private bool m_fogEnabled;
        [SerializeField] private Color m_fogColor = new(0.012f, 0.018f, 0.028f, 1f);
        [SerializeField, Range(0f, 0.1f)] private float m_fogDensity = 0.012f;

        [Header("Post processing")]
        [SerializeField, Min(0f)] private float m_bloomIntensity = 0.28f;
        [SerializeField, Min(0f)] private float m_bloomThreshold = 1.15f;
        [SerializeField, Range(0f, 1f)] private float m_bloomScatter = 0.5f;
        [SerializeField, Min(0f)] private float m_reducedFlashesBloomIntensity = 0.16f;
        [SerializeField] private float m_postExposure = 0.08f;
        [SerializeField, Range(-100f, 100f)] private float m_contrast = 8f;
        [SerializeField, Range(-100f, 100f)] private float m_saturation = -5f;
        [SerializeField, Range(0f, 1f)] private float m_poweredVignette = 0.14f;
        [SerializeField, Range(0f, 1f)] private float m_deadZoneVignette = 0.22f;
        [SerializeField, Range(0.01f, 1f)] private float m_vignetteSmoothness = 0.48f;
        [SerializeField, Min(0.01f)] private float m_vignetteResponse = 0.15f;

        [Header("Practical-light roles")]
        [SerializeField] private List<EnvironmentLightProfile> m_landmarkLights = new()
        {
            new EnvironmentLightProfile("Tower Signal Pool", EnvironmentLightRole.DominantTask,
                new Color(0.05f, 0.75f, 1f), 7f, 1f, 0.35f),
            new EnvironmentLightProfile("Extraction Guidance Pool", EnvironmentLightRole.Navigation,
                new Color(0.08f, 0.9f, 1f), 6f, 1f),
            new EnvironmentLightProfile("Salvage Annex Worklight", EnvironmentLightRole.SecondaryTask,
                new Color(1f, 0.48f, 0.08f), 5f, 1f),
            new EnvironmentLightProfile("Security Bay Alarm", EnvironmentLightRole.Practical,
                new Color(1f, 0.08f, 0.12f), 5f, 1f)
        };
        [SerializeField, Range(0f, 0.25f)] private float m_practicalPulseDepth = 0.12f;
        [SerializeField, Range(0f, 0.25f)] private float m_reducedFlashesPulseDepth = 0.035f;
        [SerializeField, Min(0f)] private float m_practicalPulseSpeed = 2.2f;

        [Header("Readability and performance ceilings")]
        [SerializeField, Min(0f)] private float m_maximumEmission = 3.2f;
        [SerializeField, Min(0f)] private float m_maximumPoweredRouteEmission = 0.8f;
        [SerializeField, Range(1, 12)] private int m_maximumVisibleRealtimeLights = 5;
        [SerializeField, Range(0, 4)] private int m_maximumShadowedRealtimeLights = 1;

        public Color KeyLightColor => m_keyLightColor;
        public float KeyLightIntensity => m_keyLightIntensity;
        public LightShadows KeyLightShadows => m_keyLightShadows;
        public Color AmbientFloor => m_ambientFloor;
        public Color HighContrastAmbientFloor => m_highContrastAmbientFloor;
        public Color CameraBackground => m_cameraBackground;
        public bool FogEnabled => m_fogEnabled;
        public Color FogColor => m_fogColor;
        public float FogDensity => m_fogDensity;
        public float BloomIntensity => m_bloomIntensity;
        public float BloomThreshold => m_bloomThreshold;
        public float BloomScatter => m_bloomScatter;
        public float ReducedFlashesBloomIntensity => m_reducedFlashesBloomIntensity;
        public float PostExposure => m_postExposure;
        public float Contrast => m_contrast;
        public float Saturation => m_saturation;
        public float PoweredVignette => m_poweredVignette;
        public float DeadZoneVignette => m_deadZoneVignette;
        public float VignetteSmoothness => m_vignetteSmoothness;
        public float VignetteResponse => m_vignetteResponse;
        public IReadOnlyList<EnvironmentLightProfile> LandmarkLights => m_landmarkLights;
        public float PracticalPulseDepth => m_practicalPulseDepth;
        public float ReducedFlashesPulseDepth => m_reducedFlashesPulseDepth;
        public float PracticalPulseSpeed => m_practicalPulseSpeed;
        public float MaximumEmission => m_maximumEmission;
        public float MaximumPoweredRouteEmission => m_maximumPoweredRouteEmission;
        public int MaximumVisibleRealtimeLights => m_maximumVisibleRealtimeLights;
        public int MaximumShadowedRealtimeLights => m_maximumShadowedRealtimeLights;

        public Color ClampEmission(Color emission)
        {
            var peak = emission.maxColorComponent;
            return peak > m_maximumEmission && peak > 0f ? emission * (m_maximumEmission / peak) : emission;
        }

        private void OnValidate()
        {
            m_keyLightIntensity = Mathf.Max(0f, m_keyLightIntensity);
            m_fogDensity = Mathf.Clamp(m_fogDensity, 0f, 0.1f);
            m_bloomIntensity = Mathf.Max(0f, m_bloomIntensity);
            m_bloomThreshold = Mathf.Max(0f, m_bloomThreshold);
            m_bloomScatter = Mathf.Clamp01(m_bloomScatter);
            m_reducedFlashesBloomIntensity = Mathf.Clamp(m_reducedFlashesBloomIntensity, 0f, m_bloomIntensity);
            m_poweredVignette = Mathf.Clamp01(m_poweredVignette);
            m_deadZoneVignette = Mathf.Max(m_poweredVignette, Mathf.Clamp01(m_deadZoneVignette));
            m_vignetteSmoothness = Mathf.Clamp(m_vignetteSmoothness, 0.01f, 1f);
            m_vignetteResponse = Mathf.Max(0.01f, m_vignetteResponse);
            m_practicalPulseDepth = Mathf.Clamp(m_practicalPulseDepth, 0f, 0.25f);
            m_reducedFlashesPulseDepth = Mathf.Clamp(m_reducedFlashesPulseDepth, 0f, m_practicalPulseDepth);
            m_practicalPulseSpeed = Mathf.Max(0f, m_practicalPulseSpeed);
            m_maximumEmission = Mathf.Max(0f, m_maximumEmission);
            m_maximumPoweredRouteEmission = Mathf.Clamp(m_maximumPoweredRouteEmission, 0f, m_maximumEmission);
            m_maximumVisibleRealtimeLights = Mathf.Clamp(m_maximumVisibleRealtimeLights, 1, 12);
            m_maximumShadowedRealtimeLights = Mathf.Clamp(
                m_maximumShadowedRealtimeLights, 0, m_maximumVisibleRealtimeLights);
            m_landmarkLights ??= new List<EnvironmentLightProfile>();
            foreach (var profile in m_landmarkLights)
            {
                profile?.Validate();
            }
        }
    }
}
