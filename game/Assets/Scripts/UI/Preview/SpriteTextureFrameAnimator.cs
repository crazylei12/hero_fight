using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fight.UI.Preview
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteTextureFrameAnimator : MonoBehaviour
    {
        [SerializeField] private string resourcesFolder = "HeroPreview/support_004_shrinemaiden/Idle";
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private float pixelsPerUnit = 100f;
        [SerializeField] private Vector2 spritePivot = new Vector2(0.5f, 0.08f);
        [SerializeField] private bool playInEditMode = true;
        [SerializeField] private bool loop = true;

        private readonly List<Sprite> runtimeSprites = new List<Sprite>();
        private SpriteRenderer spriteRenderer;
        private string loadedFolder;
        private float loadedPixelsPerUnit;
        private Vector2 loadedPivot;
        private int frameIndex;
        private double lastFrameTime;

        public float AnimationLengthSeconds
        {
            get
            {
                var frameCount = runtimeSprites.Count;
                if (frameCount == 0 && !string.IsNullOrWhiteSpace(resourcesFolder))
                {
                    frameCount = Resources.LoadAll<Texture2D>(resourcesFolder).Length;
                }

                return frameCount <= 0 ? 0f : frameCount / Mathf.Max(0.1f, framesPerSecond);
            }
        }

        public void Configure(
            string resourcesFolder,
            float framesPerSecond,
            float pixelsPerUnit,
            Vector2 spritePivot,
            bool loop,
            bool playInEditMode = true)
        {
            this.resourcesFolder = resourcesFolder;
            this.framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            this.pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            this.spritePivot = spritePivot;
            this.loop = loop;
            this.playInEditMode = playInEditMode;

            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadSpritesIfNeeded(forceReload: true);
            ShowFrame(0);
        }

        private void OnEnable()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadSpritesIfNeeded(forceReload: true);
            ShowFrame(0);

#if UNITY_EDITOR
            EditorApplication.update -= TickInEditor;
            EditorApplication.update += TickInEditor;
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            EditorApplication.update -= TickInEditor;
#endif
            ReleaseGeneratedSprites();
        }

        private void OnValidate()
        {
            framesPerSecond = Mathf.Max(0.1f, framesPerSecond);
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);

            if (!isActiveAndEnabled)
            {
                return;
            }

            spriteRenderer = GetComponent<SpriteRenderer>();
            LoadSpritesIfNeeded(forceReload: true);
            ShowFrame(Mathf.Clamp(frameIndex, 0, Mathf.Max(0, runtimeSprites.Count - 1)));
        }

        private void Update()
        {
            if (Application.isPlaying)
            {
                TickAnimation();
            }
        }

#if UNITY_EDITOR
        private void TickInEditor()
        {
            if (this == null || Application.isPlaying)
            {
                return;
            }

            TickAnimation();
        }
#endif

        private void TickAnimation()
        {
            if (runtimeSprites.Count == 0)
            {
                LoadSpritesIfNeeded(forceReload: false);
                ShowFrame(0);
            }

            if (runtimeSprites.Count <= 1)
            {
                return;
            }

            if (!Application.isPlaying && !playInEditMode)
            {
                return;
            }

            var now = GetCurrentTime();
            var secondsPerFrame = 1.0 / Mathf.Max(0.1f, framesPerSecond);
            if (now - lastFrameTime < secondsPerFrame)
            {
                return;
            }

            lastFrameTime = now;
            var nextFrame = frameIndex + 1;
            if (nextFrame >= runtimeSprites.Count)
            {
                nextFrame = loop ? 0 : runtimeSprites.Count - 1;
            }

            ShowFrame(nextFrame);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                SceneView.RepaintAll();
            }
#endif
        }

        private void LoadSpritesIfNeeded(bool forceReload)
        {
            if (!forceReload
                && string.Equals(loadedFolder, resourcesFolder, StringComparison.Ordinal)
                && Mathf.Approximately(loadedPixelsPerUnit, pixelsPerUnit)
                && loadedPivot == spritePivot
                && runtimeSprites.Count > 0)
            {
                return;
            }

            ReleaseGeneratedSprites();
            loadedFolder = resourcesFolder;
            loadedPixelsPerUnit = pixelsPerUnit;
            loadedPivot = spritePivot;

            if (string.IsNullOrWhiteSpace(resourcesFolder))
            {
                return;
            }

            var textures = Resources.LoadAll<Texture2D>(resourcesFolder)
                .OrderBy(texture => texture.name, StringComparer.Ordinal)
                .ToArray();

            foreach (var texture in textures)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;

                var sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    spritePivot,
                    pixelsPerUnit,
                    0,
                    SpriteMeshType.FullRect);
                sprite.name = texture.name;
                runtimeSprites.Add(sprite);
            }

            frameIndex = 0;
            lastFrameTime = GetCurrentTime();
        }

        private void ShowFrame(int index)
        {
            if (spriteRenderer == null || runtimeSprites.Count == 0)
            {
                return;
            }

            frameIndex = Mathf.Clamp(index, 0, runtimeSprites.Count - 1);
            spriteRenderer.sprite = runtimeSprites[frameIndex];
        }

        private double GetCurrentTime()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return EditorApplication.timeSinceStartup;
            }
#endif
            return Time.timeAsDouble;
        }

        private void ReleaseGeneratedSprites()
        {
            foreach (var sprite in runtimeSprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(sprite);
                }
                else
                {
                    DestroyImmediate(sprite);
                }
            }

            runtimeSprites.Clear();
        }
    }
}
