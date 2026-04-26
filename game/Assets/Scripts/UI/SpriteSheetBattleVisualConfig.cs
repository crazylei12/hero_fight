using System;
using UnityEngine;

namespace Fight.UI
{
    [Serializable]
    public sealed class SpriteSheetBattleClipConfig
    {
        [SerializeField] private string key = "Idle";
        [SerializeField] private string resourcesFolder = "Idle";
        [SerializeField] private float framesPerSecond = 8f;
        [SerializeField] private bool loop = true;

        public string Key => key;

        public string ResourcesFolder => resourcesFolder;

        public float FramesPerSecond => Mathf.Max(0.1f, framesPerSecond);

        public bool Loop => loop;
    }

    [DisallowMultipleComponent]
    public sealed class SpriteSheetBattleVisualConfig : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private string resourcesRoot = "HeroPreview/support_004_shrinemaiden";
        [SerializeField] private float pixelsPerUnit = 32f;
        [SerializeField] private Vector2 spritePivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private SpriteSheetBattleClipConfig[] clips = Array.Empty<SpriteSheetBattleClipConfig>();

        public SpriteRenderer SpriteRenderer => spriteRenderer != null
            ? spriteRenderer
            : GetComponentInChildren<SpriteRenderer>(true);

        public string ResourcesRoot => resourcesRoot;

        public float PixelsPerUnit => Mathf.Max(1f, pixelsPerUnit);

        public Vector2 SpritePivot => spritePivot;

        public SpriteSheetBattleClipConfig[] Clips => clips ?? Array.Empty<SpriteSheetBattleClipConfig>();

        private void OnValidate()
        {
            pixelsPerUnit = Mathf.Max(1f, pixelsPerUnit);
        }
    }
}
