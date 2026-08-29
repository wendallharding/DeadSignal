using UnityEngine;

namespace DeadSignal.Presentation
{
    [CreateAssetMenu(
        fileName = "ProductShellTransitionTuning",
        menuName = "Dead Signal/Tuning/Product Shell Transitions")]
    public sealed class ProductShellTransitionTuning : ScriptableObject
    {
        [SerializeField, Min(0.05f)] private float m_standardDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float m_reducedFlashesDuration = 0.28f;

        public float StandardDuration => m_standardDuration;
        public float ReducedFlashesDuration => m_reducedFlashesDuration;

        public float Duration(bool reducedFlashes)
        {
            return reducedFlashes ? m_reducedFlashesDuration : m_standardDuration;
        }

        private void OnValidate()
        {
            m_standardDuration = Mathf.Max(0.05f, m_standardDuration);
            m_reducedFlashesDuration = Mathf.Max(m_standardDuration, m_reducedFlashesDuration);
        }
    }
}
