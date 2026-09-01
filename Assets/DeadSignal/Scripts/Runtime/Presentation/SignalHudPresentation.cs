using UnityEngine;

namespace DeadSignal.Presentation
{
    public readonly struct SignalHudPresentation
    {
        public SignalHudPresentation(SignalReserveState state, Color color, float alpha, float ratio)
        {
            State = state;
            Color = color;
            Alpha = alpha;
            Ratio = ratio;
        }

        public SignalReserveState State { get; }
        public Color Color { get; }
        public float Alpha { get; }
        public float Ratio { get; }

        public static SignalHudPresentation Evaluate(float signal, float maximumSignal, bool reducedFlashes,
            float pulseTime, SignalHudTuning tuning)
        {
            var ratio = maximumSignal > 0f ? Mathf.Clamp01(signal / maximumSignal) : 0f;
            if (ratio <= tuning.CriticalThreshold)
            {
                var pulse = reducedFlashes
                    ? 1f
                    : Mathf.Lerp(tuning.CriticalMinimumAlpha, 1f,
                        Mathf.Sin(Mathf.Max(0f, pulseTime) * tuning.CriticalPulseSpeed) * 0.5f + 0.5f);
                return new SignalHudPresentation(SignalReserveState.Critical, tuning.CriticalColor, pulse, ratio);
            }

            if (ratio <= tuning.StrainedThreshold)
            {
                return new SignalHudPresentation(SignalReserveState.Strained, tuning.StrainedColor, 1f, ratio);
            }

            return new SignalHudPresentation(SignalReserveState.Stable, tuning.StableColor, 1f, ratio);
        }
    }
}
