using System.Collections.Generic;
using DG.Tweening;
using Fight.Battle;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.UI.Presentation.Skills
{
    public sealed class FireSeaSkillAreaPresentationController : SkillAreaPresentationController
    {
        private const int EmberCount = 28;

        private static Sprite circleSprite;

        private readonly List<Tween> fireRainTweens = new List<Tween>();

        private SortingGroup sortingGroup;
        private SpriteRenderer[] customEffectSprites;
        private Color[] customEffectBaseColors;
        private SpriteRenderer[] fireRainSprites;
        private Renderer[] renderers;
        private float ambientStrength = 0.62f;
        private float pulseStrength;
        private Tween ambientTween;
        private Tween pulseTween;

        public override Renderer[] Renderers => renderers;

        public void Initialize()
        {
            sortingGroup = gameObject.GetComponent<SortingGroup>();
            if (sortingGroup == null)
            {
                sortingGroup = gameObject.AddComponent<SortingGroup>();
            }

            EnsureCircleSprite();

            var floorWarmth = CreateSprite("FloorWarmth", new Color(0.98f, 0.26f, 0.08f, 0.24f), 0, Vector3.zero, new Vector3(1.08f, 0.88f, 1f), -7f);
            var floorHeat = CreateSprite("FloorHeat", new Color(1f, 0.42f, 0.1f, 0.22f), 1, new Vector3(0f, -0.01f, 0f), new Vector3(0.84f, 0.64f, 1f), 11f);
            var fireBand = CreateSprite("FireBand", new Color(1f, 0.55f, 0.12f, 0.18f), 2, new Vector3(0f, 0.01f, 0f), new Vector3(0.92f, 0.72f, 1f), -18f);
            var centerSmoke = CreateSprite("CenterSmoke", new Color(1f, 0.72f, 0.22f, 0.12f), 3, new Vector3(0f, 0.02f, 0f), new Vector3(0.5f, 0.36f, 1f), 0f);

            customEffectSprites = new[] { floorWarmth, floorHeat, fireBand, centerSmoke };
            customEffectBaseColors = new[]
            {
                floorWarmth.color,
                floorHeat.color,
                fireBand.color,
                centerSmoke.color,
            };

            for (var i = 0; i < customEffectSprites.Length; i++)
            {
                var sprite = customEffectSprites[i];
                if (sprite == null)
                {
                    continue;
                }

                var color = sprite.color;
                color.a = 0f;
                sprite.color = color;
            }

            fireRainSprites = CreateFireRainSprites();
            renderers = BuildRendererCache(customEffectSprites, fireRainSprites);

            ambientTween = DOVirtual.Float(0.62f, 1f, 0.42f, value => ambientStrength = value)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);

            DOVirtual.Float(0f, 1f, 0.22f, value => ambientStrength = Mathf.Max(ambientStrength, value))
                .SetEase(Ease.OutSine)
                .SetLink(gameObject);

            StartFireRain();
        }

        public override Vector3 GetScaledSize(RuntimeSkillArea area, Vector3 defaultAreaScale)
        {
            var multiplier = area?.Skill != null ? Mathf.Max(0.1f, area.Skill.persistentAreaVfxScaleMultiplier) : 1f;
            return new Vector3(defaultAreaScale.x * multiplier, defaultAreaScale.y * multiplier, 1f);
        }

        public override void Sync(RuntimeSkillArea area, Vector3 position, int sortingOrder, float expiryFadeSeconds)
        {
            transform.position = position;
            if (sortingGroup != null)
            {
                sortingGroup.sortingOrder = sortingOrder;
            }

            UpdateColors(area, expiryFadeSeconds);
        }

        public override void RestartPulse()
        {
            pulseTween?.Kill();
            pulseStrength = 1f;
            pulseTween = DOVirtual.Float(1f, 0f, 0.24f, value => pulseStrength = value)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject);
        }

        public override void Cleanup()
        {
            ambientTween?.Kill();
            pulseTween?.Kill();
            for (var i = 0; i < fireRainTweens.Count; i++)
            {
                fireRainTweens[i]?.Kill();
            }

            fireRainTweens.Clear();
            ambientTween = null;
            pulseTween = null;
            pulseStrength = 0f;
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        private static void EnsureCircleSprite()
        {
            if (circleSprite != null)
            {
                return;
            }

            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            var center = (size - 1) / 2f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    pixels[(y * size) + x] = (dx * dx) + (dy * dy) <= radius * radius
                        ? Color.white
                        : Color.clear;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            circleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        }

        private SpriteRenderer CreateSprite(string name, Color color, int order, Vector3 localPosition, Vector3 localScale, float localRotationZ)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = circleSprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            return renderer;
        }

        private static Renderer[] BuildRendererCache(SpriteRenderer[] mainSprites, SpriteRenderer[] emberSprites)
        {
            var results = new List<Renderer>(mainSprites.Length + emberSprites.Length);
            results.AddRange(mainSprites);
            results.AddRange(emberSprites);
            return results.ToArray();
        }

        private SpriteRenderer[] CreateFireRainSprites()
        {
            var sprites = new SpriteRenderer[EmberCount];
            for (var i = 0; i < sprites.Length; i++)
            {
                sprites[i] = CreateSprite(
                    $"FireRain_{i:D2}",
                    new Color(0.96f, 0.34f, 0.1f, 0f),
                    6 + (i % 3),
                    Vector3.zero,
                    new Vector3(0.013f, 0.06f, 1f),
                    UnityEngine.Random.Range(-8f, 8f));
            }

            return sprites;
        }

        private void StartFireRain()
        {
            if (fireRainSprites == null)
            {
                return;
            }

            for (var i = 0; i < fireRainSprites.Length; i++)
            {
                StartFireRainLoop(fireRainSprites[i], i * 0.04f);
            }
        }

        private void StartFireRainLoop(SpriteRenderer ember, float initialDelay)
        {
            if (ember == null)
            {
                return;
            }

            ember.transform.localPosition = GetFireRainStartLocalPosition();
            ember.transform.localScale = new Vector3(UnityEngine.Random.Range(0.011f, 0.018f), UnityEngine.Random.Range(0.05f, 0.09f), 1f);

            var sequence = DOTween.Sequence()
                .SetDelay(initialDelay)
                .AppendCallback(() =>
                {
                    if (ember == null)
                    {
                        return;
                    }

                    var launch = GetFireRainStartLocalPosition();
                    var impact = GetFireRainImpactLocalPosition(launch.x);
                    ember.transform.localPosition = launch;
                    ember.transform.localScale = new Vector3(UnityEngine.Random.Range(0.011f, 0.018f), UnityEngine.Random.Range(0.05f, 0.09f), 1f);
                    ember.color = new Color(0.96f, 0.34f, 0.1f, 0f);

                    ember.transform.DOLocalMove(impact, UnityEngine.Random.Range(0.24f, 0.38f))
                        .SetEase(Ease.InQuad)
                        .SetLink(ember.gameObject);
                    ember.DOColor(new Color(0.98f, 0.42f, 0.12f, 0.82f), UnityEngine.Random.Range(0.05f, 0.09f))
                        .SetEase(Ease.OutSine)
                        .SetLink(ember.gameObject);
                    ember.DOColor(new Color(0.78f, 0.1f, 0.04f, 0f), UnityEngine.Random.Range(0.14f, 0.2f))
                        .SetDelay(UnityEngine.Random.Range(0.13f, 0.18f))
                        .SetEase(Ease.InSine)
                        .SetLink(ember.gameObject);
                })
                .AppendInterval(UnityEngine.Random.Range(0.18f, 0.42f))
                .SetLoops(-1)
                .SetLink(ember.gameObject);

            fireRainTweens.Add(sequence);
        }

        private void UpdateColors(RuntimeSkillArea area, float expiryFadeSeconds)
        {
            if (customEffectSprites == null)
            {
                return;
            }

            var expiryWindow = Mathf.Min(expiryFadeSeconds, area != null ? area.TotalDurationSeconds : 0f);
            var expiryFade = expiryWindow > 0f
                ? Mathf.Clamp01(area.RemainingDurationSeconds / expiryWindow)
                : 1f;
            var combinedIntensity = Mathf.Clamp01(ambientStrength + pulseStrength);

            for (var i = 0; i < customEffectSprites.Length; i++)
            {
                var sprite = customEffectSprites[i];
                if (sprite == null)
                {
                    continue;
                }

                var baseColor = customEffectBaseColors != null && i < customEffectBaseColors.Length
                    ? customEffectBaseColors[i]
                    : sprite.color;
                var hotColor = i switch
                {
                    0 => new Color(1f, 0.46f, 0.1f, baseColor.a * 1.15f),
                    1 => new Color(1f, 0.58f, 0.14f, baseColor.a * 1.2f),
                    2 => new Color(1f, 0.66f, 0.18f, baseColor.a * 1.25f),
                    _ => new Color(1f, 0.78f, 0.24f, baseColor.a * 1.1f),
                };
                var pulseBlend = i == 3
                    ? Mathf.Clamp01(combinedIntensity * 0.85f)
                    : Mathf.Clamp01(0.25f + (combinedIntensity * 0.95f));
                var targetColor = Color.Lerp(baseColor, hotColor, pulseBlend);
                targetColor.a *= expiryFade;
                sprite.color = targetColor;
            }

            if (fireRainSprites == null)
            {
                return;
            }

            var emberTint = Color.Lerp(
                new Color(0.9f, 0.2f, 0.06f, 0f),
                new Color(1f, 0.48f, 0.14f, 0f),
                Mathf.Clamp01(0.4f + combinedIntensity * 0.6f));
            var emberAlphaScale = Mathf.Lerp(0.65f, 1f, Mathf.Clamp01(ambientStrength + (pulseStrength * 0.8f))) * expiryFade;

            for (var i = 0; i < fireRainSprites.Length; i++)
            {
                var ember = fireRainSprites[i];
                if (ember == null)
                {
                    continue;
                }

                var color = ember.color;
                color.r = emberTint.r;
                color.g = emberTint.g;
                color.b = emberTint.b;
                color.a *= emberAlphaScale;
                ember.color = color;
            }
        }

        private static Vector3 GetFireRainStartLocalPosition()
        {
            var x = UnityEngine.Random.Range(-0.48f, 0.48f);
            var y = UnityEngine.Random.Range(0.5f, 0.78f);
            return new Vector3(x, y, 0f);
        }

        private static Vector3 GetFireRainImpactLocalPosition(float startX)
        {
            var drift = UnityEngine.Random.Range(-0.08f, 0.08f);
            var y = UnityEngine.Random.Range(-0.42f, 0.3f);
            return new Vector3(Mathf.Clamp(startX + drift, -0.5f, 0.5f), y, 0f);
        }
    }
}
