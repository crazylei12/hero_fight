using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.UI.Presentation.Skills
{
    [DisallowMultipleComponent]
    public sealed class SkillAreaFlameScatterPulseController : MonoBehaviour, ISkillAreaPulseReceiver
    {
        [SerializeField] private string flameNodePrefix = "Flame";
        [SerializeField] private float horizontalJitterRadius = 0.08f;
        [SerializeField] private float verticalJitterRadius = 0.05f;
        [SerializeField] private float scaleJitterPercent = 0.14f;
        [SerializeField] private float rotationJitterDegrees = 10f;
        [SerializeField] private float alphaMinMultiplier = 0.88f;
        [SerializeField] private float alphaMaxMultiplier = 1f;

        private Transform[] flameTransforms = Array.Empty<Transform>();
        private SpriteRenderer[] flameRenderers = Array.Empty<SpriteRenderer>();
        private Vector3[] anchorPositions = Array.Empty<Vector3>();
        private Vector3[] baseScales = Array.Empty<Vector3>();
        private float[] baseRotations = Array.Empty<float>();
        private Color[] baseColors = Array.Empty<Color>();
        private bool[] baseFlipX = Array.Empty<bool>();

        public void Configure(
            string flamePrefix,
            float xJitterRadius,
            float yJitterRadius,
            float scaleVariationPercent,
            float rotationVariationDegrees,
            float minAlphaMultiplier,
            float maxAlphaMultiplier)
        {
            flameNodePrefix = string.IsNullOrWhiteSpace(flamePrefix) ? "Flame" : flamePrefix;
            horizontalJitterRadius = Mathf.Max(0f, xJitterRadius);
            verticalJitterRadius = Mathf.Max(0f, yJitterRadius);
            scaleJitterPercent = Mathf.Clamp(scaleVariationPercent, 0f, 0.5f);
            rotationJitterDegrees = Mathf.Max(0f, rotationVariationDegrees);
            alphaMinMultiplier = Mathf.Clamp01(minAlphaMultiplier);
            alphaMaxMultiplier = Mathf.Clamp(maxAlphaMultiplier, alphaMinMultiplier, 1.25f);
        }

        private void Awake()
        {
            CacheFlamesIfNeeded();
        }

        private void OnEnable()
        {
            CacheFlamesIfNeeded();
            HandleSkillAreaPulse();
        }

        public void HandleSkillAreaPulse()
        {
            CacheFlamesIfNeeded();
            if (flameTransforms.Length == 0)
            {
                return;
            }

            var shuffledAnchors = BuildShuffledAnchorOrder(flameTransforms.Length);
            for (var i = 0; i < flameTransforms.Length; i++)
            {
                var anchorIndex = shuffledAnchors[i];
                var targetAnchor = anchorPositions[anchorIndex];
                var jitter = new Vector3(
                    UnityEngine.Random.Range(-horizontalJitterRadius, horizontalJitterRadius),
                    UnityEngine.Random.Range(-verticalJitterRadius, verticalJitterRadius),
                    0f);

                flameTransforms[i].localPosition = targetAnchor + jitter;

                var scaleMultiplier = UnityEngine.Random.Range(1f - scaleJitterPercent, 1f + scaleJitterPercent);
                flameTransforms[i].localScale = baseScales[i] * scaleMultiplier;

                var rotation = baseRotations[i] + UnityEngine.Random.Range(-rotationJitterDegrees, rotationJitterDegrees);
                flameTransforms[i].localRotation = Quaternion.Euler(0f, 0f, rotation);

                var renderer = flameRenderers[i];
                if (renderer == null)
                {
                    continue;
                }

                var color = baseColors[i];
                color.a *= UnityEngine.Random.Range(alphaMinMultiplier, alphaMaxMultiplier);
                renderer.color = color;
                renderer.flipX = UnityEngine.Random.value > 0.5f ? !baseFlipX[i] : baseFlipX[i];
            }
        }

        private void CacheFlamesIfNeeded()
        {
            if (flameTransforms.Length > 0)
            {
                return;
            }

            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                flameTransforms = Array.Empty<Transform>();
                flameRenderers = Array.Empty<SpriteRenderer>();
                anchorPositions = Array.Empty<Vector3>();
                baseScales = Array.Empty<Vector3>();
                baseRotations = Array.Empty<float>();
                baseColors = Array.Empty<Color>();
                baseFlipX = Array.Empty<bool>();
                return;
            }

            var flames = new List<SpriteRenderer>();
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer == null || !renderer.name.StartsWith(flameNodePrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                flames.Add(renderer);
            }

            flames.Sort((left, right) => string.CompareOrdinal(left.name, right.name));
            flameTransforms = new Transform[flames.Count];
            flameRenderers = flames.ToArray();
            anchorPositions = new Vector3[flames.Count];
            baseScales = new Vector3[flames.Count];
            baseRotations = new float[flames.Count];
            baseColors = new Color[flames.Count];
            baseFlipX = new bool[flames.Count];

            for (var i = 0; i < flames.Count; i++)
            {
                var renderer = flames[i];
                flameTransforms[i] = renderer.transform;
                anchorPositions[i] = renderer.transform.localPosition;
                baseScales[i] = renderer.transform.localScale;
                baseRotations[i] = renderer.transform.localEulerAngles.z;
                baseColors[i] = renderer.color;
                baseFlipX[i] = renderer.flipX;
            }
        }

        private static int[] BuildShuffledAnchorOrder(int count)
        {
            var order = new int[count];
            for (var i = 0; i < count; i++)
            {
                order[i] = i;
            }

            for (var i = 0; i < count; i++)
            {
                var swapIndex = UnityEngine.Random.Range(i, count);
                (order[i], order[swapIndex]) = (order[swapIndex], order[i]);
            }

            if (count <= 1)
            {
                return order;
            }

            var hasStationaryEntry = false;
            for (var i = 0; i < count; i++)
            {
                if (order[i] == i)
                {
                    hasStationaryEntry = true;
                    break;
                }
            }

            if (!hasStationaryEntry)
            {
                return order;
            }

            for (var i = 0; i < count; i++)
            {
                if (order[i] != i)
                {
                    continue;
                }

                var swapIndex = (i + 1) % count;
                (order[i], order[swapIndex]) = (order[swapIndex], order[i]);
            }

            return order;
        }
    }
}
