using System;
using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Owns and live-updates the runtime material palette used by the generated world.
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
        public Material Dark { get; }
        public Material Steel { get; }
        public Material White { get; }
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

        public DeadSignalPalette(bool highContrastEnabled)
        {
            Cyan = _createMaterial("Signal Cyan");
            CyanDim = _createMaterial("Powered Deck");
            Amber = _createMaterial("Salvage Amber");
            Red = _createMaterial("Security Red");
            RedDim = _createMaterial("Dead Zone Red");
            Magenta = _createMaterial("Sapper Magenta");
            Deck = _createMaterial("Maintenance Deck");
            Bulkhead = _createMaterial("Maintenance Bulkhead");
            TowerHousing = _createMaterial("Signal Tower Housing");
            ExtractionHousing = _createMaterial("Extraction Dock Housing");
            ShortcutHousing = _createMaterial("Shortcut Gate Housing");
            ShortcutLocked = _createMaterial("Shortcut Gate Locked");
            SignalRouting = _createMaterial("Signal Routing");
            StationMachineHousing = _createMaterial("Station Machine Housing");
            SalvageCacheHousing = _createMaterial("Salvage Cache Housing");
            PlayerDroneHousing = _createMaterial("Maintenance Drone Housing");
            WardenHousing = _createMaterial("Security Warden Housing");
            Dark = _createMaterial("Station Black");
            Steel = _createMaterial("Station Steel");
            White = _createMaterial("Drone White");
            var deckTexture = Resources.Load<Texture2D>(MAINTENANCE_DECK_TEXTURE_RESOURCE);
            if (deckTexture != null)
            {
                Deck.mainTexture = deckTexture;
                if (Deck.HasProperty("_BaseMap"))
                {
                    Deck.SetTexture("_BaseMap", deckTexture);
                }

                if (Deck.HasProperty("_Smoothness"))
                {
                    Deck.SetFloat("_Smoothness", 0.22f);
                }
            }

            HasDeckTexture = deckTexture != null;
            var bulkheadTexture = Resources.Load<Texture2D>(MAINTENANCE_BULKHEAD_TEXTURE_RESOURCE);
            if (bulkheadTexture != null)
            {
                Bulkhead.mainTexture = bulkheadTexture;
                if (Bulkhead.HasProperty("_BaseMap"))
                {
                    Bulkhead.SetTexture("_BaseMap", bulkheadTexture);
                }

                if (Bulkhead.HasProperty("_Smoothness"))
                {
                    Bulkhead.SetFloat("_Smoothness", 0.3f);
                }
            }

            HasBulkheadTexture = bulkheadTexture != null;
            var towerTexture = Resources.Load<Texture2D>(SIGNAL_TOWER_TEXTURE_RESOURCE);
            if (towerTexture != null)
            {
                TowerHousing.mainTexture = towerTexture;
                if (TowerHousing.HasProperty("_BaseMap"))
                {
                    TowerHousing.SetTexture("_BaseMap", towerTexture);
                }

                if (TowerHousing.HasProperty("_Smoothness"))
                {
                    TowerHousing.SetFloat("_Smoothness", 0.38f);
                }
            }

            HasTowerTexture = towerTexture != null;
            var extractionTexture = Resources.Load<Texture2D>(EXTRACTION_DOCK_TEXTURE_RESOURCE);
            if (extractionTexture != null)
            {
                ExtractionHousing.mainTexture = extractionTexture;
                if (ExtractionHousing.HasProperty("_BaseMap"))
                {
                    ExtractionHousing.SetTexture("_BaseMap", extractionTexture);
                }

                if (ExtractionHousing.HasProperty("_Smoothness"))
                {
                    ExtractionHousing.SetFloat("_Smoothness", 0.32f);
                }
            }

            HasExtractionTexture = extractionTexture != null;
            var shortcutTexture = Resources.Load<Texture2D>(SHORTCUT_GATE_TEXTURE_RESOURCE);
            if (shortcutTexture != null)
            {
                ShortcutHousing.mainTexture = shortcutTexture;
                ShortcutLocked.mainTexture = shortcutTexture;
                if (ShortcutHousing.HasProperty("_BaseMap"))
                {
                    ShortcutHousing.SetTexture("_BaseMap", shortcutTexture);
                    ShortcutLocked.SetTexture("_BaseMap", shortcutTexture);
                }

                if (ShortcutHousing.HasProperty("_Smoothness"))
                {
                    ShortcutHousing.SetFloat("_Smoothness", 0.34f);
                }
            }

            HasShortcutTexture = shortcutTexture != null;
            var signalRoutingTexture = Resources.Load<Texture2D>(SIGNAL_ROUTING_TEXTURE_RESOURCE);
            if (signalRoutingTexture != null)
            {
                SignalRouting.mainTexture = signalRoutingTexture;
                if (SignalRouting.HasProperty("_BaseMap"))
                {
                    SignalRouting.SetTexture("_BaseMap", signalRoutingTexture);
                }

                if (SignalRouting.HasProperty("_Smoothness"))
                {
                    SignalRouting.SetFloat("_Smoothness", 0.4f);
                }
            }

            HasSignalRoutingTexture = signalRoutingTexture != null;
            var stationMachineTexture = Resources.Load<Texture2D>(STATION_MACHINE_TEXTURE_RESOURCE);
            if (stationMachineTexture != null)
            {
                StationMachineHousing.mainTexture = stationMachineTexture;
                if (StationMachineHousing.HasProperty("_BaseMap"))
                {
                    StationMachineHousing.SetTexture("_BaseMap", stationMachineTexture);
                }

                if (StationMachineHousing.HasProperty("_Smoothness"))
                {
                    StationMachineHousing.SetFloat("_Smoothness", 0.28f);
                }
            }

            HasStationMachineTexture = stationMachineTexture != null;
            var salvageCacheTexture = Resources.Load<Texture2D>(SALVAGE_CACHE_TEXTURE_RESOURCE);
            if (salvageCacheTexture != null)
            {
                SalvageCacheHousing.mainTexture = salvageCacheTexture;
                if (SalvageCacheHousing.HasProperty("_BaseMap"))
                {
                    SalvageCacheHousing.SetTexture("_BaseMap", salvageCacheTexture);
                }

                if (SalvageCacheHousing.HasProperty("_Smoothness"))
                {
                    SalvageCacheHousing.SetFloat("_Smoothness", 0.36f);
                }
            }

            HasSalvageCacheTexture = salvageCacheTexture != null;
            var playerDroneTexture = Resources.Load<Texture2D>(PLAYER_DRONE_TEXTURE_RESOURCE);
            if (playerDroneTexture != null)
            {
                PlayerDroneHousing.mainTexture = playerDroneTexture;
                if (PlayerDroneHousing.HasProperty("_BaseMap"))
                {
                    PlayerDroneHousing.SetTexture("_BaseMap", playerDroneTexture);
                }

                if (PlayerDroneHousing.HasProperty("_Smoothness"))
                {
                    PlayerDroneHousing.SetFloat("_Smoothness", 0.42f);
                }
            }

            HasPlayerDroneTexture = playerDroneTexture != null;
            var wardenTexture = Resources.Load<Texture2D>(SECURITY_WARDEN_TEXTURE_RESOURCE);
            if (wardenTexture != null)
            {
                WardenHousing.mainTexture = wardenTexture;
                if (WardenHousing.HasProperty("_BaseMap"))
                {
                    WardenHousing.SetTexture("_BaseMap", wardenTexture);
                }

                if (WardenHousing.HasProperty("_Smoothness"))
                {
                    WardenHousing.SetFloat("_Smoothness", 0.3f);
                }
            }

            HasWardenTexture = wardenTexture != null;
            ApplyHighContrast(highContrastEnabled);
        }

        public void ApplyHighContrast(bool enabled)
        {
            _setMaterial(Cyan,
                enabled ? new Color(0.2f, 1f, 1f) : new Color(0.02f, 0.92f, 1f),
                enabled ? new Color(0.3f, 2.8f, 3.2f) : new Color(0f, 1.8f, 2.2f));
            _setMaterial(CyanDim,
                enabled ? new Color(0.025f, 0.34f, 0.42f) : new Color(0.015f, 0.18f, 0.2f),
                enabled ? new Color(0.02f, 0.34f, 0.42f) : new Color(0f, 0.11f, 0.13f));
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
            _setMaterial(Dark,
                enabled ? Color.black : new Color(0.012f, 0.018f, 0.026f),
                Color.black);
            _setMaterial(Steel,
                enabled ? new Color(0.18f, 0.22f, 0.28f) : new Color(0.085f, 0.11f, 0.14f),
                enabled ? new Color(0.025f, 0.04f, 0.055f) : new Color(0.01f, 0.018f, 0.02f));
            _setMaterial(White,
                enabled ? Color.white : new Color(0.62f, 0.72f, 0.75f),
                enabled ? new Color(0.18f, 0.22f, 0.24f) : new Color(0.03f, 0.06f, 0.07f));
        }

        private static Material _createMaterial(string materialName)
        {
            var template = Resources.Load<Material>(RUNTIME_MATERIAL_RESOURCE);
            var material = template == null ? null : new Material(template);
            if (material == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                if (shader == null)
                {
                    throw new InvalidOperationException("DEAD SIGNAL could not load its runtime Lit material or fallback shader.");
                }

                material = new Material(shader);
            }

            material.name = materialName;
            material.enableInstancing = true;
            return material;
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

        private const string RUNTIME_MATERIAL_RESOURCE = "Materials/RuntimeLitTemplate";
        private const string MAINTENANCE_DECK_TEXTURE_RESOURCE = "Environment/MaintenanceDeckPanel";
        private const string MAINTENANCE_BULKHEAD_TEXTURE_RESOURCE = "Environment/MaintenanceBulkheadPanel";
        private const string SIGNAL_TOWER_TEXTURE_RESOURCE = "Environment/SignalTowerHousingPanel";
        private const string EXTRACTION_DOCK_TEXTURE_RESOURCE = "Environment/ExtractionDockPanel";
        private const string SHORTCUT_GATE_TEXTURE_RESOURCE = "Environment/ShortcutGatePanel";
        private const string SIGNAL_ROUTING_TEXTURE_RESOURCE = "Environment/SignalRoutingPanel";
        private const string STATION_MACHINE_TEXTURE_RESOURCE = "Environment/StationMachinePanel";
        private const string SALVAGE_CACHE_TEXTURE_RESOURCE = "Environment/SalvageCachePanel";
        private const string PLAYER_DRONE_TEXTURE_RESOURCE = "Actors/MaintenanceDronePanel";
        private const string SECURITY_WARDEN_TEXTURE_RESOURCE = "Actors/SecurityWardenPanel";
    }
}
