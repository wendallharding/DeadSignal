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

    public enum EnvironmentLightPowerSource
    {
        None,
        CentralTower,
        CargoCoupling,
        CoolantSeal,
        RelayFeeds,
        TransferAssembly,
        RelayTower,
        RelayPayload,
        SpineVenting,
        SpineTower,
        InductionLattice,
        FluxShunt,
        ConvergenceCalibration,
        BreakerDistribution,
        FurnaceForge,
        QuenchStabilization
    }

    [Serializable]
    public sealed class EnvironmentLightProfile
    {
        [SerializeField] private string m_name;
        [SerializeField] private EnvironmentLightRole m_role;
        [SerializeField] private Color m_color = Color.white;
        [SerializeField] private Color m_poweredColor = Color.white;
        [SerializeField, Min(0.1f)] private float m_range = 5f;
        [SerializeField, Min(0f)] private float m_intensity = 1f;
        [SerializeField, Range(0f, 1f)] private float m_dormantIntensityMultiplier = 1f;
        [SerializeField] private EnvironmentLightPowerSource m_powerSource;
        [SerializeField] private LightType m_lightType = LightType.Point;
        [SerializeField, Range(1f, 179f)] private float m_spotAngle = 60f;
        [SerializeField] private string m_cookieResource;

        public string Name => m_name;
        public EnvironmentLightRole Role => m_role;
        public Color Color => m_color;
        public Color PoweredColor => m_poweredColor;
        public float Range => m_range;
        public float Intensity => m_intensity;
        public float DormantIntensityMultiplier => m_dormantIntensityMultiplier;
        public EnvironmentLightPowerSource PowerSource => m_powerSource;
        public LightType LightType => m_lightType;
        public float SpotAngle => m_spotAngle;
        public string CookieResource => m_cookieResource;
        public bool RespondsToCentralPower => m_powerSource == EnvironmentLightPowerSource.CentralTower;

        public EnvironmentLightProfile(
            string name,
            EnvironmentLightRole role,
            Color color,
            Color poweredColor,
            float range,
            float intensity,
            float dormantIntensityMultiplier = 1f,
            EnvironmentLightPowerSource powerSource = EnvironmentLightPowerSource.None,
            LightType lightType = LightType.Point,
            float spotAngle = 60f,
            string cookieResource = null)
        {
            m_name = name;
            m_role = role;
            m_color = color;
            m_poweredColor = poweredColor;
            m_range = range;
            m_intensity = intensity;
            m_dormantIntensityMultiplier = dormantIntensityMultiplier;
            m_powerSource = powerSource;
            m_lightType = lightType;
            m_spotAngle = spotAngle;
            m_cookieResource = cookieResource;
        }

        public Color GetColor(bool powered) => m_powerSource != EnvironmentLightPowerSource.None && powered
            ? m_poweredColor
            : m_color;

        public float GetIntensity(bool powered) => m_intensity *
            (m_powerSource != EnvironmentLightPowerSource.None && !powered ? m_dormantIntensityMultiplier : 1f);

        public void Validate()
        {
            m_name = string.IsNullOrWhiteSpace(m_name) ? "Unnamed practical" : m_name.Trim();
            m_range = Mathf.Max(0.1f, m_range);
            m_intensity = Mathf.Max(0f, m_intensity);
            m_dormantIntensityMultiplier = Mathf.Clamp01(m_dormantIntensityMultiplier);
            m_lightType = m_lightType == LightType.Spot ? LightType.Spot : LightType.Point;
            m_spotAngle = Mathf.Clamp(m_spotAngle, 1f, 179f);
            m_cookieResource = string.IsNullOrWhiteSpace(m_cookieResource) ? null : m_cookieResource.Trim();
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
            new EnvironmentLightProfile("Central Coupling Task Pool", EnvironmentLightRole.DominantTask,
                new Color(1f, 0.42f, 0.08f), new Color(0.14f, 0.72f, 0.9f), 7.8f, 1.25f, 0.48f,
                EnvironmentLightPowerSource.CentralTower),
            new EnvironmentLightProfile("Dock Uplink Guidance Pool", EnvironmentLightRole.Navigation,
                new Color(0.22f, 0.62f, 0.72f), new Color(0.22f, 0.62f, 0.72f), 5.4f, 0.82f),
            new EnvironmentLightProfile("Cargo Retrieval Worklight", EnvironmentLightRole.SecondaryTask,
                new Color(0.58f, 0.26f, 0.08f), new Color(1f, 0.72f, 0.34f), 6f, 1.7f, 0.48f,
                EnvironmentLightPowerSource.CargoCoupling, LightType.Spot, 66f),
            new EnvironmentLightProfile("Coolant Threading Pool", EnvironmentLightRole.SecondaryTask,
                new Color(0.18f, 0.38f, 0.42f), new Color(0.48f, 0.86f, 0.84f), 6.5f, 1.45f, 0.42f,
                EnvironmentLightPowerSource.CoolantSeal),
            new EnvironmentLightProfile("Relay Routing Projector", EnvironmentLightRole.DominantTask,
                new Color(0.55f, 0.25f, 0.06f), new Color(0.32f, 0.72f, 0.78f), 5.8f, 1.65f, 0.38f,
                EnvironmentLightPowerSource.RelayFeeds, LightType.Spot, 52f),
            new EnvironmentLightProfile("Transfer Assembly Pool", EnvironmentLightRole.Practical,
                new Color(0.48f, 0.36f, 0.24f), new Color(0.76f, 0.88f, 0.82f), 5.8f, 1.5f, 0.36f,
                EnvironmentLightPowerSource.TransferAssembly),
            new EnvironmentLightProfile("Foundry Induction Turbine Projector", EnvironmentLightRole.DominantTask,
                new Color(0.48f, 0.22f, 0.055f), new Color(1f, 0.58f, 0.16f), 8.4f, 1.8f, 0.32f,
                EnvironmentLightPowerSource.RelayTower, LightType.Spot, 72f,
                "Environment/RelayFoundryInductionCookie"),
            new EnvironmentLightProfile("Cooling Gantry Stabilization Pool", EnvironmentLightRole.SecondaryTask,
                new Color(0.12f, 0.28f, 0.34f), new Color(0.42f, 0.82f, 0.88f), 7.2f, 1.55f, 0.3f,
                EnvironmentLightPowerSource.RelayPayload),
            new EnvironmentLightProfile("Discharge Pressure Warning Pool", EnvironmentLightRole.SecondaryTask,
                new Color(0.48f, 0.055f, 0.035f), new Color(0.94f, 0.54f, 0.12f), 6.8f, 1.62f, 0.32f,
                EnvironmentLightPowerSource.SpineVenting),
            new EnvironmentLightProfile("Spine Transfer-Bank Projector", EnvironmentLightRole.DominantTask,
                new Color(0.32f, 0.075f, 0.055f), new Color(0.2f, 0.78f, 0.9f), 8.6f, 1.74f, 0.3f,
                EnvironmentLightPowerSource.SpineTower, LightType.Spot, 68f,
                "Environment/SpineHighVoltageLaneCookie")
        };
        [SerializeField, Range(0f, 2f)] private float m_openingFixtureEmission = 0.28f;
        [SerializeField, Range(0f, 2f)] private float m_centralDormantFixtureEmission = 0.18f;
        [SerializeField, Range(0f, 2f)] private float m_centralPoweredFixtureEmission = 0.72f;
        [SerializeField] private Color m_openingTerritoryBase = new(0.012f, 0.22f, 0.28f, 0.12f);
        [SerializeField] private Color m_openingTerritoryEdge = new(0.08f, 0.72f, 0.82f, 0.42f);
        [SerializeField] private Color m_relayTerritoryBase = new(0.012f, 0.2f, 0.26f, 0.1f);
        [SerializeField] private Color m_relayTerritoryEdge = new(0.08f, 0.68f, 0.78f, 0.34f);
        [SerializeField] private Color m_spineTerritoryBase = new(0.012f, 0.18f, 0.24f, 0.08f);
        [SerializeField] private Color m_spineTerritoryEdge = new(0.08f, 0.64f, 0.76f, 0.3f);
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
        public float OpeningFixtureEmission => m_openingFixtureEmission;
        public float CentralDormantFixtureEmission => m_centralDormantFixtureEmission;
        public float CentralPoweredFixtureEmission => m_centralPoweredFixtureEmission;
        public Color OpeningTerritoryBase => m_openingTerritoryBase;
        public Color OpeningTerritoryEdge => m_openingTerritoryEdge;
        public Color RelayTerritoryBase => m_relayTerritoryBase;
        public Color RelayTerritoryEdge => m_relayTerritoryEdge;
        public Color SpineTerritoryBase => m_spineTerritoryBase;
        public Color SpineTerritoryEdge => m_spineTerritoryEdge;
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

        public void ConfigureDeepCoreProfiles()
        {
            m_landmarkLights ??= new List<EnvironmentLightProfile>();
            m_landmarkLights.RemoveAll(profile =>
                profile != null && profile.PowerSource >= EnvironmentLightPowerSource.InductionLattice);
            m_landmarkLights.Add(new EnvironmentLightProfile("Induction Lattice Charge Pool",
                EnvironmentLightRole.SecondaryTask,
                new Color(0.16f, 0.19f, 0.22f), new Color(0.72f, 0.55f, 0.2f), 6.4f, 1.3f, 0.26f,
                EnvironmentLightPowerSource.InductionLattice));
            m_landmarkLights.Add(new EnvironmentLightProfile("Flux Bypass Reroute Projector",
                EnvironmentLightRole.Practical,
                new Color(0.18f, 0.16f, 0.14f), new Color(0.76f, 0.48f, 0.14f), 6.8f, 1.42f, 0.24f,
                EnvironmentLightPowerSource.FluxShunt, LightType.Spot, 48f));
            m_landmarkLights.Add(new EnvironmentLightProfile("Convergence Calibration Aperture",
                EnvironmentLightRole.DominantTask,
                new Color(0.28f, 0.09f, 0.055f), new Color(0.94f, 0.46f, 0.14f), 8.2f, 1.7f, 0.28f,
                EnvironmentLightPowerSource.ConvergenceCalibration, LightType.Spot, 70f,
                "Environment/DeepCoreCalibrationApertureCookie"));
            m_landmarkLights.Add(new EnvironmentLightProfile("Breaker Distribution Worklights",
                EnvironmentLightRole.Navigation,
                new Color(0.22f, 0.18f, 0.13f), new Color(0.62f, 0.76f, 0.66f), 7.1f, 1.46f, 0.26f,
                EnvironmentLightPowerSource.BreakerDistribution));
            m_landmarkLights.Add(new EnvironmentLightProfile("Arc Furnace Forge Pool",
                EnvironmentLightRole.DominantTask,
                new Color(0.34f, 0.065f, 0.025f), new Color(1f, 0.34f, 0.075f), 7.8f, 1.76f, 0.3f,
                EnvironmentLightPowerSource.FurnaceForge, LightType.Spot, 74f));
            m_landmarkLights.Add(new EnvironmentLightProfile("Quench Condenser Pool",
                EnvironmentLightRole.SecondaryTask,
                new Color(0.08f, 0.16f, 0.2f), new Color(0.28f, 0.74f, 0.82f), 7.4f, 1.58f, 0.28f,
                EnvironmentLightPowerSource.QuenchStabilization));
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
            m_openingFixtureEmission = Mathf.Clamp(m_openingFixtureEmission, 0f, m_maximumEmission);
            m_centralDormantFixtureEmission = Mathf.Clamp(m_centralDormantFixtureEmission, 0f, m_maximumEmission);
            m_centralPoweredFixtureEmission = Mathf.Clamp(
                m_centralPoweredFixtureEmission, m_centralDormantFixtureEmission, m_maximumEmission);
            m_landmarkLights ??= new List<EnvironmentLightProfile>();
            foreach (var profile in m_landmarkLights)
            {
                profile?.Validate();
            }
        }
    }
}
