using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Owns and live-updates the runtime material palette used by the generated world.
    /// </summary>
    internal sealed class DeadSignalPalette
    {
        public DeadSignalPalette(bool highContrastEnabled)
        {
            Cyan = _createMaterial("Signal Cyan");
            CyanDim = _createMaterial("Powered Deck");
            Amber = _createMaterial("Salvage Amber");
            Red = _createMaterial("Security Red");
            RedDim = _createMaterial("Dead Zone Red");
            Magenta = _createMaterial("Sapper Magenta");
            Dark = _createMaterial("Station Black");
            Steel = _createMaterial("Station Steel");
            White = _createMaterial("Drone White");
            ApplyHighContrast(highContrastEnabled);
        }

        public Material Cyan { get; }
        public Material CyanDim { get; }
        public Material Amber { get; }
        public Material Red { get; }
        public Material RedDim { get; }
        public Material Magenta { get; }
        public Material Dark { get; }
        public Material Steel { get; }
        public Material White { get; }

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
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            return new Material(shader) { name = materialName, enableInstancing = true };
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
    }
}
