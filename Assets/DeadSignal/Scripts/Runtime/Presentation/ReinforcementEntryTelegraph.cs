using UnityEngine;

namespace DeadSignal.Presentation
{
    /// <summary>
    /// Marks the authored entrance committed to an announced security response.
    /// </summary>
    public sealed class ReinforcementEntryTelegraph : MonoBehaviour
    {
        private Renderer m_ringRenderer;
        private Renderer m_barRenderer;

        public bool IsVisible => gameObject.activeSelf;
        public bool IsBlocked { get; private set; }
        public Vector3 EntryPosition => transform.position;

        public void Configure(Material warningMaterial, Material blockedMaterial)
        {
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "Entry Warning Ring";
            ring.transform.SetParent(transform, false);
            ring.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            ring.transform.localScale = new Vector3(2.2f, 0.025f, 2.2f);
            Destroy(ring.GetComponent<Collider>());
            m_ringRenderer = ring.GetComponent<Renderer>();

            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bar.name = "Entry Warning Bar";
            bar.transform.SetParent(transform, false);
            bar.transform.localPosition = new Vector3(0f, 0.07f, 0f);
            bar.transform.localScale = new Vector3(2.8f, 0.08f, 0.18f);
            Destroy(bar.GetComponent<Collider>());
            m_barRenderer = bar.GetComponent<Renderer>();

            m_warningMaterial = warningMaterial;
            m_blockedMaterial = blockedMaterial;
            SetState(false, Vector3.zero, false, 0f);
        }

        public void SetState(bool visible, Vector3 entryPosition, bool blocked, float warningProgress)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                IsBlocked = false;
                return;
            }

            transform.position = entryPosition;
            IsBlocked = blocked;
            var material = blocked ? m_blockedMaterial : m_warningMaterial;
            m_ringRenderer.sharedMaterial = material;
            m_barRenderer.sharedMaterial = material;
            var scale = Mathf.Lerp(0.75f, 1f, Mathf.Clamp01(warningProgress));
            transform.localScale = new Vector3(scale, 1f, scale);
        }

        private Material m_warningMaterial;
        private Material m_blockedMaterial;
    }
}
