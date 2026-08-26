using System.Collections.Generic;
using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>
    /// Loads the authored material palette and applies accessibility color variants at runtime.
    /// </summary>
    internal sealed class DeadSignalPalette
    {
        public Material Cyan { get; }
        public Material CyanDim { get; }
        public Material Amber { get; }
        public Material Red { get; }
        public Material RedDim { get; }
        public Material Magenta { get; }
        public Material Deck { get; }
        public Material Bulkhead { get; }
        public Material TowerHousing { get; }
        public Material ExtractionHousing { get; }
        public Material ShortcutHousing { get; }
        public Material ShortcutLocked { get; }
        public Material SignalRouting { get; }
        public Material StationMachineHousing { get; }
        public Material SalvageCacheHousing { get; }
        public Material PlayerDroneHousing { get; }
        public Material WardenHousing { get; }
        public Material SapperHousing { get; }
        public Material Dark { get; }
        public Material Steel { get; }
        public Material White { get; }
        public Material PoweredTerritory { get; }
        public bool HasDeckTexture { get; }
        public bool HasBulkheadTexture { get; }
        public bool HasTowerTexture { get; }
        public bool HasExtractionTexture { get; }
        public bool HasShortcutTexture { get; }
        public bool HasSignalRoutingTexture { get; }
        public bool HasStationMachineTexture { get; }
        public bool HasSalvageCacheTexture { get; }
        public bool HasPlayerDroneTexture { get; }
        public bool HasWardenTexture { get; }
        public bool HasSapperTexture { get; }

        public DeadSignalPalette(bool highContrastEnabled)
        {
            Cyan = _loadMaterial("SignalCyan");
            CyanDim = _loadMaterial("PoweredDeck");
            Amber = _loadMaterial("SalvageAmber");
            Red = _loadMaterial("SecurityRed");
            RedDim = _loadMaterial("DeadZoneRed");
            Magenta = _loadMaterial("SapperMagenta");
            Deck = _loadMaterial("MaintenanceDeck");
            Bulkhead = _loadMaterial("MaintenanceBulkhead");
            TowerHousing = _loadMaterial("SignalTowerHousing");
            ExtractionHousing = _loadMaterial("ExtractionDockHousing");
            ShortcutHousing = _loadMaterial("ShortcutGateHousing");
            ShortcutLocked = _loadMaterial("ShortcutGateLocked");
            SignalRouting = _loadMaterial("SignalRouting");
            StationMachineHousing = _loadMaterial("StationMachineHousing");
            SalvageCacheHousing = _loadMaterial("SalvageCacheHousing");
            PlayerDroneHousing = _loadMaterial("MaintenanceDroneHousing");
            WardenHousing = _loadMaterial("SecurityWardenHousing");
            SapperHousing = _loadMaterial("SignalSapperHousing");
            Dark = _loadMaterial("StationBlack");
            Steel = _loadMaterial("StationSteel");
            White = _loadMaterial("DroneWhite");
            PoweredTerritory = _loadMaterial("PoweredTerritory");
            HasDeckTexture = Deck.mainTexture != null;
            HasBulkheadTexture = Bulkhead.mainTexture != null;
            HasTowerTexture = TowerHousing.mainTexture != null;
            HasExtractionTexture = ExtractionHousing.mainTexture != null;
            HasShortcutTexture = ShortcutHousing.mainTexture != null && ShortcutLocked.mainTexture != null;
            HasSignalRoutingTexture = SignalRouting.mainTexture != null;
            HasStationMachineTexture = StationMachineHousing.mainTexture != null;
            HasSalvageCacheTexture = SalvageCacheHousing.mainTexture != null;
            HasPlayerDroneTexture = PlayerDroneHousing.mainTexture != null;
            HasWardenTexture = WardenHousing.mainTexture != null;
            HasSapperTexture = SapperHousing.mainTexture != null;
            ApplyHighContrast(highContrastEnabled);
        }

        public void ApplyHighContrast(bool enabled)
        {
            _setMaterial(Cyan,
                enabled ? new Color(0.2f, 1f, 1f) : new Color(0.02f, 0.92f, 1f),
                enabled ? new Color(0.3f, 2.8f, 3.2f) : new Color(0f, 1.8f, 2.2f));
            _setMaterial(CyanDim,
                enabled ? new Color(0.025f, 0.24f, 0.3f) : new Color(0.012f, 0.095f, 0.12f),
                enabled ? new Color(0.02f, 0.24f, 0.3f) : new Color(0f, 0.045f, 0.065f));
            _setMaterial(Amber,
                enabled ? new Color(1f, 0.82f, 0.05f) : new Color(1f, 0.48f, 0.06f),
                enabled ? new Color(3f, 1.8f, 0.05f) : new Color(2.4f, 0.65f, 0.02f));
            _setMaterial(Red,
                enabled ? new Color(1f, 0.16f, 0.05f) : new Color(1f, 0.035f, 0.045f),
                enabled ? new Color(3f, 0.08f, 0.02f) : new Color(2.2f, 0.01f, 0.01f));
            _setMaterial(RedDim,
                enabled ? new Color(0.42f, 0.025f, 0.018f) : new Color(0.2f, 0.018f, 0.025f),
                enabled ? new Color(0.38f, 0.015f, 0.005f) : new Color(0.14f, 0.005f, 0.005f));
            _setMaterial(Magenta,
                enabled ? new Color(0.95f, 0.18f, 1f) : new Color(0.92f, 0.025f, 0.62f),
                enabled ? new Color(2.7f, 0.12f, 3f) : new Color(2.2f, 0.01f, 1.15f));
            _setMaterial(Deck,
                enabled ? new Color(0.82f, 0.9f, 0.96f) : new Color(0.58f, 0.66f, 0.72f),
                Color.black);
            _setMaterial(Bulkhead,
                enabled ? new Color(0.72f, 0.82f, 0.9f) : new Color(0.48f, 0.56f, 0.62f),
                Color.black);
            _setMaterial(TowerHousing,
                enabled ? new Color(0.95f, 0.98f, 1f) : new Color(0.72f, 0.78f, 0.82f),
                Color.black);
            _setMaterial(ExtractionHousing,
                enabled ? Color.white : new Color(0.7f, 0.78f, 0.82f),
                Color.black);
            _setMaterial(ShortcutHousing,
                enabled ? new Color(0.95f, 0.98f, 1f) : new Color(0.66f, 0.72f, 0.76f),
                Color.black);
            _setMaterial(ShortcutLocked,
                enabled ? new Color(1f, 0.34f, 0.28f) : new Color(0.52f, 0.15f, 0.16f),
                enabled ? new Color(0.4f, 0.025f, 0.015f) : new Color(0.12f, 0.005f, 0.005f));
            _setMaterial(SignalRouting,
                enabled ? Color.white : new Color(0.72f, 0.82f, 0.86f),
                enabled ? new Color(0.06f, 0.72f, 0.8f) : new Color(0.015f, 0.32f, 0.38f));
            _setMaterial(StationMachineHousing,
                enabled ? new Color(0.82f, 0.88f, 0.92f) : new Color(0.5f, 0.56f, 0.6f),
                Color.black);
            _setMaterial(SalvageCacheHousing,
                enabled ? new Color(1f, 0.92f, 0.58f) : new Color(0.82f, 0.58f, 0.28f),
                enabled ? new Color(0.55f, 0.2f, 0.01f) : new Color(0.18f, 0.05f, 0.005f));
            _setMaterial(PlayerDroneHousing,
                enabled ? Color.white : new Color(0.82f, 0.86f, 0.88f),
                Color.black);
            _setMaterial(WardenHousing,
                enabled ? new Color(0.34f, 0.38f, 0.44f) : new Color(0.16f, 0.18f, 0.21f),
                enabled ? new Color(0.16f, 0.005f, 0.005f) : new Color(0.04f, 0.002f, 0.002f));
            _setMaterial(SapperHousing,
                enabled ? new Color(0.54f, 0.48f, 0.62f) : new Color(0.21f, 0.17f, 0.25f),
                enabled ? new Color(0.14f, 0.005f, 0.09f) : new Color(0.035f, 0.001f, 0.02f));
            _setMaterial(Dark,
                enabled ? new Color(0.008f, 0.01f, 0.014f) : new Color(0.022f, 0.03f, 0.042f),
                Color.black);
            _setMaterial(Steel,
                enabled ? new Color(0.18f, 0.22f, 0.28f) : new Color(0.085f, 0.11f, 0.14f),
                enabled ? new Color(0.025f, 0.04f, 0.055f) : new Color(0.01f, 0.018f, 0.02f));
            _setMaterial(White,
                enabled ? Color.white : new Color(0.62f, 0.72f, 0.75f),
                enabled ? new Color(0.18f, 0.22f, 0.24f) : new Color(0.03f, 0.06f, 0.07f));
        }

        public void RebindHierarchy(Transform root)
        {
            if (root == null)
            {
                return;
            }

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                var changed = false;
                for (var index = 0; index < materials.Length; index++)
                {
                    var material = materials[index];
                    if (material == null ||
                        !m_runtimeMaterialsByName.TryGetValue(material.name, out var runtimeMaterial) ||
                        material == runtimeMaterial)
                    {
                        continue;
                    }

                    materials[index] = runtimeMaterial;
                    changed = true;
                }

                if (changed)
                {
                    renderer.sharedMaterials = materials;
                }
            }
        }

        public void Dispose()
        {
            foreach (var material in m_runtimeMaterials)
            {
                if (material != null)
                {
                    Object.Destroy(material);
                }
            }

            m_runtimeMaterials.Clear();
            m_runtimeMaterialsByName.Clear();
        }

        private Material _loadMaterial(string materialName)
        {
            var authoredMaterial = Resources.Load<Material>($"Materials/WorldPalette/{materialName}");
            if (authoredMaterial == null)
            {
                throw new MissingReferenceException($"Authored world material is missing: Materials/WorldPalette/{materialName}.");
            }

            var runtimeMaterial = new Material(authoredMaterial)
            {
                name = authoredMaterial.name,
                hideFlags = HideFlags.DontSave
            };
            m_runtimeMaterials.Add(runtimeMaterial);
            m_runtimeMaterialsByName.Add(materialName, runtimeMaterial);
            return runtimeMaterial;
        }

        private static void _setMaterial(Material material, Color baseColor, Color emission)
        {
            material.color = baseColor;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }
        }

        private readonly List<Material> m_runtimeMaterials = new List<Material>();
        private readonly Dictionary<string, Material> m_runtimeMaterialsByName = new Dictionary<string, Material>();
    }
}
