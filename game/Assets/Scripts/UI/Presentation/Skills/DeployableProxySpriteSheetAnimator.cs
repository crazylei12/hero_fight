using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Fight.UI.Presentation.Skills
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DeployableProxySpriteSheetAnimator : MonoBehaviour
    {
        private const string IdleClipKey = "Idle";
        private const string AttackClipKey = "Attack";

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private string resourcesRoot = "Stage01Demo/VFX/Deployables/SandemperorSandguard";
        [SerializeField] private string idleFolder = "Idle";
        [SerializeField] private string attackFolder = "Attack";
        [SerializeField, Min(0.1f)] private float idleFramesPerSecond = 7f;
        [SerializeField, Min(0.1f)] private float attackFramesPerSecond = 13f;
        [SerializeField, Min(1f)] private float pixelsPerUnit = 64f;
        [SerializeField] private Vector2 spritePivot = new Vector2(0.5f, 0.07f);
        [SerializeField] private bool playInEditMode = true;
        [SerializeField] private bool baseFacesLeft;

        private readonly Dictionary<string, RuntimeClip> clips = new Dictionary<string, RuntimeClip>(StringComparer.Ordinal);
        private readonly List<Sprite> generatedSprites = new List<Sprite>();
        private RuntimeClip currentClip;
        private int frameIndex;
        private float frameTimer;
        private double lastEditorTickTime;

        public void Configure(
            SpriteRenderer renderer,
            string rootResourcesPath,
            string idleResourcesFolder,
            string attackResourcesFolder,
            float idleFps,
            float attackFps,
            float spritePixelsPerUnit,
            Vector2 pivot,
            bool facesLeftByDefault)
        {
            spriteRenderer = renderer != null ? renderer : GetComponent<SpriteRenderer>();
            resourcesRoot = rootResourcesPath;
            idleFolder = idleResourcesFolder;
            attackFolder = attackResourcesFolder;
            idleFramesPerSecond = Mathf.Max(0.1f, idleFps);
            attackFramesPerSecond = Mathf.Max(0.1f, attackFps);
            pixelsPerUnit = Mathf.Max(1f, spritePixelsPerUnit);
            spritePivot = pivot;
            baseFacesLeft = facesLeftByDefault;
            LoadClips();
            PlayClip(IdleClipKey, restart: true);
        }

        public void SetFacingHorizontal(float horizontalDirection)
        {
            if (spriteRenderer == null || Mathf.Abs(horizontalDirection) <= 0.01f)
            {
                return;
            }

            var faceRight = horizontalDirection > 0f;
            spriteRenderer.flipX = baseFacesLeft ? faceRight : !faceRight;
        }

        public void PlayAttack(Vector3 worldDirection)
        {
            SetFacingHorizontal(worldDirection.x);
            if (!clips.ContainsKey(AttackClipKey))
            {
                PlayClip(IdleClipKey, restart: false);
                return;
            }

            PlayClip(AttackClipKey, restart: true);
        }

        private void OnEnable()
        {
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();
            LoadClips();
            PlayClip(IdleClipKey, restart: true);
            lastEditorTickTime = GetEditorTime();
        }

        private void OnDisable()
        {
            ReleaseGeneratedSprites();
        }

        private void OnDestroy()
        {
            ReleaseGeneratedSprites();
        }

        private void OnValidate()
        {
            idleFramesPerSecond = Mathf.Max(0.1f, idleFramesPerSecond);
            attackFramesPerSecond = Mathf.Max(0.1f, attackFramesPerSecond);
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
            spriteRenderer = spriteRenderer != null ? spriteRenderer : GetComponent<SpriteRenderer>();

            if (!isActiveAndEnabled)
            {
                return;
            }

            LoadClips();
            PlayClip(currentClip != null ? currentClip.Key : IdleClipKey, restart: true);
        }

        private void Update()
        {
            if (!Application.isPlaying && !playInEditMode)
            {
                return;
            }

            TickAnimation(GetDeltaTime());
        }

        private void LoadClips()
        {
            ReleaseGeneratedSprites();
            clips.Clear();
            AddClip(IdleClipKey, idleFolder, idleFramesPerSecond, loop: true);
            AddClip(AttackClipKey, attackFolder, attackFramesPerSecond, loop: false);
        }

        private void AddClip(string key, string folder, float framesPerSecond, bool loop)
        {
            var path = CombineResourcesPath(resourcesRoot, folder);
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            var textures = Resources.LoadAll<Texture2D>(path);
            if (textures == null || textures.Length == 0)
            {
                return;
            }

            Array.Sort(textures, (left, right) => string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty));
            var sprites = new List<Sprite>(textures.Length);
            foreach (var texture in textures)
            {
                if (texture == null)
                {
                    continue;
                }

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
                sprites.Add(sprite);
                generatedSprites.Add(sprite);
            }

            if (sprites.Count > 0)
            {
                clips[key] = new RuntimeClip(key, sprites.ToArray(), framesPerSecond, loop);
            }
        }

        private void PlayClip(string key, bool restart)
        {
            if (!clips.TryGetValue(key, out var clip))
            {
                if (!clips.TryGetValue(IdleClipKey, out clip))
                {
                    return;
                }
            }

            if (!restart && currentClip == clip)
            {
                return;
            }

            currentClip = clip;
            frameIndex = 0;
            frameTimer = 0f;
            ShowFrame(0);
        }

        private void TickAnimation(float deltaTime)
        {
            if (currentClip == null || currentClip.Sprites.Length <= 1)
            {
                return;
            }

            frameTimer += Mathf.Max(0f, deltaTime);
            var secondsPerFrame = 1f / Mathf.Max(0.1f, currentClip.FramesPerSecond);
            while (frameTimer >= secondsPerFrame)
            {
                frameTimer -= secondsPerFrame;
                if (frameIndex >= currentClip.Sprites.Length - 1)
                {
                    if (currentClip.Loop)
                    {
                        ShowFrame(0);
                        continue;
                    }

                    PlayClip(IdleClipKey, restart: false);
                    return;
                }

                ShowFrame(frameIndex + 1);
            }
        }

        private void ShowFrame(int index)
        {
            if (spriteRenderer == null || currentClip == null || currentClip.Sprites.Length == 0)
            {
                return;
            }

            frameIndex = Mathf.Clamp(index, 0, currentClip.Sprites.Length - 1);
            spriteRenderer.sprite = currentClip.Sprites[frameIndex];
        }

        private float GetDeltaTime()
        {
            if (Application.isPlaying)
            {
                return Time.deltaTime;
            }

            var now = GetEditorTime();
            var deltaTime = Mathf.Max(0f, (float)(now - lastEditorTickTime));
            lastEditorTickTime = now;
            return deltaTime;
        }

        private static double GetEditorTime()
        {
#if UNITY_EDITOR
            return EditorApplication.timeSinceStartup;
#else
            return Time.realtimeSinceStartupAsDouble;
#endif
        }

        private static string CombineResourcesPath(string root, string folder)
        {
            root = (root ?? string.Empty).Replace("\\", "/").Trim('/');
            folder = (folder ?? string.Empty).Replace("\\", "/").Trim('/');
            if (string.IsNullOrWhiteSpace(root))
            {
                return folder;
            }

            return string.IsNullOrWhiteSpace(folder) ? root : $"{root}/{folder}";
        }

        private void ReleaseGeneratedSprites()
        {
            foreach (var sprite in generatedSprites)
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

            generatedSprites.Clear();
        }

        private sealed class RuntimeClip
        {
            public RuntimeClip(string key, Sprite[] sprites, float framesPerSecond, bool loop)
            {
                Key = key;
                Sprites = sprites ?? Array.Empty<Sprite>();
                FramesPerSecond = Mathf.Max(0.1f, framesPerSecond);
                Loop = loop;
            }

            public string Key { get; }
            public Sprite[] Sprites { get; }
            public float FramesPerSecond { get; }
            public bool Loop { get; }
        }
    }
}
