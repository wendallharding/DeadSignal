using UnityEngine;

namespace DeadSignal.Combat
{
    public sealed class SuppressorFieldTelegraph : MonoBehaviour
    {
        public bool HasTexture => m_activeTexture != null;
        public bool IsActiveField => m_activeRenderer != null && m_activeRenderer.enabled;
        public bool IsWarningRing => m_warningRenderer != null && m_warningRenderer.enabled;
        public float ActiveMaximumAlpha => m_activeRenderer != null ? m_activeRenderer.color.a : 0f;

        private void OnDestroy()
        {
            if (m_activeSprite != null)
            {
                Destroy(m_activeSprite);
            }
        }

        internal void Configure(Material warningMaterial)
        {
            m_activeTexture = Resources.Load<Texture2D>(ACTIVE_TEXTURE_RESOURCE);
            if (m_activeTexture != null)
            {
                m_activeSprite = Sprite.Create(
                    m_activeTexture,
                    new Rect(0f, 0f, m_activeTexture.width, m_activeTexture.height),
                    new Vector2(0.5f, 0.5f),
                    m_activeTexture.width);
                m_activeSprite.name = "Suppressor Field Active Sprite";

                var activeObject = new GameObject("Active Field Edge");
                activeObject.transform.SetParent(transform, false);
                activeObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                m_activeRenderer = activeObject.AddComponent<SpriteRenderer>();
                m_activeRenderer.sprite = m_activeSprite;
                m_activeRenderer.color = new Color(1f, 1f, 1f, 0.62f);
                m_activeRenderer.sortingOrder = 8;
            }
            else
            {
                Debug.LogWarning($"Suppressor field texture was not found at Resources/{ACTIVE_TEXTURE_RESOURCE}.", this);
            }

            var warningObject = new GameObject("Warning Boundary");
            warningObject.transform.SetParent(transform, false);
            m_warningRenderer = warningObject.AddComponent<LineRenderer>();
            m_warningRenderer.sharedMaterial = warningMaterial;
            m_warningRenderer.positionCount = CIRCLE_SEGMENTS;
            m_warningRenderer.loop = true;
            m_warningRenderer.useWorldSpace = false;
            m_warningRenderer.startWidth = 0.1f;
            m_warningRenderer.endWidth = 0.1f;
            m_warningRenderer.numCornerVertices = 2;
            m_warningRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            m_warningRenderer.receiveShadows = false;

            gameObject.SetActive(false);
        }

        public void SetState(bool visible, bool active, float radius, Vector3 center)
        {
            gameObject.SetActive(visible);
            if (!visible)
            {
                return;
            }

            transform.position = center + Vector3.up * 0.075f;
            _setCircleRadius(radius);
            m_warningRenderer.enabled = !active;
            if (m_activeRenderer != null)
            {
                m_activeRenderer.enabled = active;
                m_activeRenderer.transform.localScale = Vector3.one * radius * 2f;
            }
        }

        private void _setCircleRadius(float radius)
        {
            for (int index = 0; index < CIRCLE_SEGMENTS; index++)
            {
                var radians = index * Mathf.PI * 2f / CIRCLE_SEGMENTS;
                m_warningRenderer.SetPosition(
                    index,
                    new Vector3(Mathf.Cos(radians) * radius, 0f, Mathf.Sin(radians) * radius));
            }
        }

        private const string ACTIVE_TEXTURE_RESOURCE = "VFX/SuppressorFieldActive";
        private const int CIRCLE_SEGMENTS = 64;

        private SpriteRenderer m_activeRenderer;
        private LineRenderer m_warningRenderer;
        private Texture2D m_activeTexture;
        private Sprite m_activeSprite;
    }
}
