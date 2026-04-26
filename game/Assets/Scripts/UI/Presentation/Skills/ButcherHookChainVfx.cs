using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.UI.Presentation.Skills
{
    public sealed class ButcherHookChainVfx : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer chainRenderer;
        [SerializeField] private SpriteRenderer chainShadowRenderer;
        [SerializeField] private SpriteRenderer hookHeadRenderer;
        [SerializeField] private SortingGroup sortingGroup;
        [SerializeField, Min(0.01f)] private float chainThickness = 0.26f;
        [SerializeField, Min(0f)] private float sourceInset = 0.12f;
        [SerializeField, Min(0f)] private float hookCenterBackOffset = 0.2f;
        [SerializeField, Min(0f)] private float chainEndBackOffset = 0.42f;
        [SerializeField] private float hookHeadEulerOffset = 180f;
        [SerializeField, Min(0f)] private float minVisibleDistance = 0.08f;

        private Color chainBaseColor = Color.white;
        private Color chainShadowBaseColor = Color.white;
        private Color hookHeadBaseColor = Color.white;
        private bool capturedBaseColors;

        private void Awake()
        {
            CaptureBaseColors();
        }

        public void Configure(
            SpriteRenderer chain,
            SpriteRenderer chainShadow,
            SpriteRenderer hookHead,
            SortingGroup group,
            float thickness,
            float casterInset,
            float headCenterBackOffset,
            float chainBackOffset,
            float headEulerOffset,
            float minimumVisibleDistance)
        {
            chainRenderer = chain;
            chainShadowRenderer = chainShadow;
            hookHeadRenderer = hookHead;
            sortingGroup = group;
            chainThickness = Mathf.Max(0.01f, thickness);
            sourceInset = Mathf.Max(0f, casterInset);
            hookCenterBackOffset = Mathf.Max(0f, headCenterBackOffset);
            chainEndBackOffset = Mathf.Max(0f, chainBackOffset);
            hookHeadEulerOffset = headEulerOffset;
            minVisibleDistance = Mathf.Max(0f, minimumVisibleDistance);
            CaptureBaseColors();
        }

        public void ApplyEndpoints(Vector3 sourcePosition, Vector3 hookFrontPosition, int sortingOrder, float alphaMultiplier)
        {
            CaptureBaseColors();

            var offset = hookFrontPosition - sourcePosition;
            offset.z = 0f;
            var distance = offset.magnitude;
            if (distance <= minVisibleDistance)
            {
                SetVisible(false);
                return;
            }

            SetVisible(true);

            var direction = offset / distance;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(0f, 0f, angle);

            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = sortingOrder;
            }

            if (hookHeadRenderer != null)
            {
                hookHeadRenderer.transform.position = hookFrontPosition - (direction * hookCenterBackOffset);
                hookHeadRenderer.transform.rotation = Quaternion.Euler(0f, 0f, angle + hookHeadEulerOffset);
                ApplyAlpha(hookHeadRenderer, hookHeadBaseColor, alphaMultiplier);
            }

            var chainStart = sourcePosition + (direction * sourceInset);
            var chainEnd = hookFrontPosition - (direction * chainEndBackOffset);
            var chainOffset = chainEnd - chainStart;
            chainOffset.z = 0f;
            var chainLength = Mathf.Max(0f, chainOffset.magnitude);
            if (chainLength <= minVisibleDistance)
            {
                SetChainVisible(false);
                return;
            }

            SetChainVisible(true);
            var midpoint = chainStart + (chainOffset * 0.5f);
            ApplyChainRenderer(chainRenderer, chainBaseColor, midpoint, rotation, chainLength, chainThickness, alphaMultiplier);
            ApplyChainRenderer(chainShadowRenderer, chainShadowBaseColor, midpoint, rotation, chainLength * 1.02f, chainThickness * 1.35f, alphaMultiplier);
        }

        public void SetVisible(bool visible)
        {
            SetChainVisible(visible);
            if (hookHeadRenderer != null)
            {
                hookHeadRenderer.enabled = visible;
            }
        }

        private void ApplyChainRenderer(
            SpriteRenderer renderer,
            Color baseColor,
            Vector3 position,
            Quaternion rotation,
            float length,
            float thickness,
            float alphaMultiplier)
        {
            if (renderer == null)
            {
                return;
            }

            renderer.transform.position = position;
            renderer.transform.rotation = rotation;
            renderer.size = new Vector2(length, thickness);
            ApplyAlpha(renderer, baseColor, alphaMultiplier);
        }

        private void SetChainVisible(bool visible)
        {
            if (chainRenderer != null)
            {
                chainRenderer.enabled = visible;
            }

            if (chainShadowRenderer != null)
            {
                chainShadowRenderer.enabled = visible;
            }
        }

        private void CaptureBaseColors()
        {
            if (capturedBaseColors)
            {
                return;
            }

            if (chainRenderer != null)
            {
                chainBaseColor = chainRenderer.color;
            }

            if (chainShadowRenderer != null)
            {
                chainShadowBaseColor = chainShadowRenderer.color;
            }

            if (hookHeadRenderer != null)
            {
                hookHeadBaseColor = hookHeadRenderer.color;
            }

            capturedBaseColors = true;
        }

        private static void ApplyAlpha(SpriteRenderer renderer, Color baseColor, float alphaMultiplier)
        {
            if (renderer == null)
            {
                return;
            }

            var color = baseColor;
            color.a *= Mathf.Clamp01(alphaMultiplier);
            renderer.color = color;
        }
    }
}
