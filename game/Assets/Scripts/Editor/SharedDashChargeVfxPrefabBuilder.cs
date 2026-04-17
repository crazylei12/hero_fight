using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class SharedDashChargeVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Shared Dash Charge VFX";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string SharedPrefabFolder = "Assets/Prefabs/VFX/Shared";
        private const string SharedResourcesFolder = "Assets/Resources/Stage01Demo/VFX/Shared";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string DashChargeTrailPrefabPath = SharedPrefabFolder + "/DashChargeTrail.prefab";
        private const string DashChargeTrailResourcesPrefabPath = SharedResourcesFolder + "/DashChargeTrail.prefab";

        [MenuItem(BuildMenuPath)]
        public static void BuildSharedDashChargeVfx()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(SharedPrefabFolder);
            EnsureFolder(SharedResourcesFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildDashChargeTrailPrefab(softCircleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Shared dash charge VFX prefab rebuilt.");
        }

        public static void BuildSharedDashChargeVfxBatch()
        {
            BuildSharedDashChargeVfx();
        }

        private static void BuildDashChargeTrailPrefab(Sprite softCircleSprite)
        {
            var root = new GameObject("DashChargeTrail");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "WakeShadow",
                softCircleSprite,
                new Color(0.98f, 0.55f, 0.14f, 0.14f),
                -24,
                new Vector3(-0.34f, 0f, 0f),
                new Vector3(1.28f, 0.26f, 1f));
            CreateSprite(
                root.transform,
                "WakeOuter",
                softCircleSprite,
                new Color(1f, 0.82f, 0.28f, 0.22f),
                -16,
                new Vector3(-0.18f, 0f, 0f),
                new Vector3(0.98f, 0.22f, 1f));
            CreateSprite(
                root.transform,
                "WakeInner",
                softCircleSprite,
                new Color(1f, 0.96f, 0.72f, 0.28f),
                -10,
                new Vector3(-0.08f, 0f, 0f),
                new Vector3(0.52f, 0.14f, 1f));
            CreateSprite(
                root.transform,
                "UpperWing",
                softCircleSprite,
                new Color(1f, 0.88f, 0.44f, 0.18f),
                -8,
                new Vector3(-0.08f, 0.14f, 0f),
                new Vector3(0.64f, 0.1f, 1f),
                18f);
            CreateSprite(
                root.transform,
                "LowerWing",
                softCircleSprite,
                new Color(1f, 0.88f, 0.44f, 0.18f),
                -8,
                new Vector3(-0.08f, -0.14f, 0f),
                new Vector3(0.64f, 0.1f, 1f),
                -18f);
            CreateSprite(
                root.transform,
                "CoreFlash",
                softCircleSprite,
                new Color(1f, 0.98f, 0.84f, 0.34f),
                -2,
                new Vector3(0.02f, 0f, 0f),
                new Vector3(0.34f, 0.18f, 1f));
            CreateSprite(
                root.transform,
                "FrontFlare",
                softCircleSprite,
                new Color(1f, 0.98f, 0.9f, 0.38f),
                4,
                new Vector3(0.2f, 0f, 0f),
                new Vector3(0.22f, 0.16f, 1f));

            SavePrefab(root, DashChargeTrailPrefabPath);
            RefreshResourcesCopy(DashChargeTrailPrefabPath, DashChargeTrailResourcesPrefabPath);
        }

        private static void RefreshResourcesCopy(string sourcePrefabPath, string destinationPrefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(destinationPrefabPath) != null)
            {
                AssetDatabase.DeleteAsset(destinationPrefabPath);
            }

            if (!AssetDatabase.CopyAsset(sourcePrefabPath, destinationPrefabPath))
            {
                throw new IOException($"Could not copy prefab from {sourcePrefabPath} to {destinationPrefabPath}.");
            }
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                var texture = BuildSoftCircleTexture(128);
                File.WriteAllBytes(GetAbsoluteProjectPath(SoftCircleSpritePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SoftCircleSpritePath, ImportAssetOptions.ForceSynchronousImport);

                var importer = AssetImporter.GetAtPath(SoftCircleSpritePath) as TextureImporter;
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spriteImportMode = SpriteImportMode.Single;
                    importer.alphaIsTransparency = true;
                    importer.mipmapEnabled = false;
                    importer.wrapMode = TextureWrapMode.Clamp;
                    importer.filterMode = FilterMode.Bilinear;
                    importer.SaveAndReimport();
                }
            }

            return LoadRequiredAsset<Sprite>(SoftCircleSpritePath);
        }

        private static Texture2D BuildSoftCircleTexture(int size)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
            };

            var pixels = new Color[size * size];
            var center = (size - 1) * 0.5f;
            var radius = size * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt((dx * dx) + (dy * dy));
                    var alpha = distance <= radius
                        ? 1f - Mathf.Clamp01((distance - (radius * 0.62f)) / Mathf.Max(1f, radius * 0.38f))
                        : 0f;
                    pixels[(y * size) + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            int sortingOrder,
            Vector3 localPosition,
            Vector3 localScale,
            float localRotationZ = 0f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, localRotationZ);

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }

        private static T LoadRequiredAsset<T>(string assetPath) where T : Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            if (asset == null)
            {
                throw new FileNotFoundException($"Missing asset at path: {assetPath}");
            }

            return asset;
        }

        private static void SavePrefab(GameObject root, string assetPath)
        {
            PrefabUtility.SaveAsPrefabAsset(root, assetPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureFolder(string folderPath)
        {
            var segments = folderPath.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
            }
        }

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath);
        }
    }
}
