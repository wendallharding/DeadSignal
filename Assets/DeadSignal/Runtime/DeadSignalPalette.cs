using UnityEngine;

namespace DeadSignal
{
    /// <summary>
    /// Owns the runtime material palette used by the prototype's generated world.
    /// </summary>
    internal sealed class DeadSignalPalette
    {
        public DeadSignalPalette()
        {
            Cyan = _createMaterial("Signal Cyan", new Color(0.02f, 0.92f, 1f), new Color(0f, 1.8f, 2.2f));
            CyanDim = _createMaterial("Powered Deck", new Color(0.015f, 0.18f, 0.2f), new Color(0f, 0.11f, 0.13f));
            Amber = _createMaterial("Salvage Amber", new Color(1f, 0.48f, 0.06f), new Color(2.4f, 0.65f, 0.02f));
            Red = _createMaterial("Security Red", new Color(1f, 0.035f, 0.045f), new Color(2.2f, 0.01f, 0.01f));
            RedDim = _createMaterial("Dead Zone Red", new Color(0.2f, 0.018f, 0.025f), new Color(0.14f, 0.005f, 0.005f));
            Magenta = _createMaterial("Sapper Magenta", new Color(0.92f, 0.025f, 0.62f), new Color(2.2f, 0.01f, 1.15f));
            Dark = _createMaterial("Station Black", new Color(0.012f, 0.018f, 0.026f), Color.black);
            Steel = _createMaterial("Station Steel", new Color(0.085f, 0.11f, 0.14f), new Color(0.01f, 0.018f, 0.02f));
            White = _createMaterial("Drone White", new Color(0.62f, 0.72f, 0.75f), new Color(0.03f, 0.06f, 0.07f));
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

        private static Material _createMaterial(string materialName, Color baseColor, Color emission)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var material = new Material(shader) { name = materialName, color = baseColor };
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            material.enableInstancing = true;
            return material;
        }
    }
}
