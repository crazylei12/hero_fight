using System.IO;
using Fight.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class LongshotVfxPrefabBuilder
    {
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string ProjectilePrefabsFolder = PrefabsRootFolder + "/Projectiles";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";

        private const string ProjectilePrefabPath = ProjectilePrefabsFolder + "/LongshotBasicAttackProjectile.prefab";
        private const string LongshotHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/marksman_001_longshot/Longshot.asset";

        private const string ArrowSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_arrow_small_yellow.prefab";
        private const string LightSparkSourcePrefabPath = "Assets/Super Pixel Projectiles Pack 3/Prefabs/pj3_light_spark_small_yellow.prefab";

        private static bool autoBuildScheduled;

        [InitializeOnLoadMethod]
        private static void ScheduleAutoBuildIfNeeded()
        {
            if (Application.isBatchMode || autoBuildScheduled)
            {
                return;
            }

            autoBuildScheduled = true;
            EditorApplication.delayCall += TryAutoBuildIfNeeded;
        }

        public static void BuildLongshotVfxPrefabsBatch()
        {
            BuildLongshotVfxPrefabs();
            EditorApplication.Exit(0);
        }

        public static void BuildLongshotVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(ProjectilePrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildProjectilePrefab(softCircleSprite);
            SyncStage01DemoAssets();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Longshot VFX prefabs rebuilt.");
        }

        private static void TryAutoBuildIfNeeded()
        {
            autoBuildScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoBuildIfNeeded();
                return;
            }

            BuildLongshotVfxPrefabs();
        }

        private static void SyncStage01DemoAssets()
        {
            var projectilePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePrefabPath);
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(LongshotHeroAssetPath);
            if (hero == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            hero.visualConfig.projectilePrefab = projectilePrefab;
            hero.visualConfig.projectileAlignToMovement = projectilePrefab != null;
            hero.visualConfig.projectileEulerAngles = Vector3.zero;
            hero.visualConfig.hitVfxPrefab = null;
            EditorUtility.SetDirty(hero);
        }

        private static void BuildProjectilePrefab(Sprite softCircleSprite)
        {
            var arrowPrefab = LoadRequiredAsset<GameObject>(ArrowSourcePrefabPath);
            var lightSparkPrefab = LoadRequiredAsset<GameObject>(LightSparkSourcePrefabPath);

            var root = new GameObject("LongshotBasicAttackProjectile");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "TrailBloom",
                softCircleSprite,
                new Color(1f, 0.84f, 0.32f, 0.14f),
                1,
                new Vector3(-0.15f, 0f, 0f),
                new Vector3(0.42f, 0.18f, 1f));
            CreateSprite(
                root.transform,
                "SpeedStreak",
                softCircleSprite,
                new Color(1f, 0.94f, 0.66f, 0.34f),
                2,
                new Vector3(-0.08f, 0f, 0f),
                new Vector3(0.26f, 0.09f, 1f));
            CreateSprite(
                root.transform,
                "ArrowGlow",
                softCircleSprite,
                new Color(1f, 0.95f, 0.72f, 0.5f),
                4,
                new Vector3(0.05f, 0f, 0f),
                new Vector3(0.18f, 0.18f, 1f));

            var trailingSpark = InstantiateNestedPrefab(lightSparkPrefab, root.transform, "TrailingSpark");
            trailingSpark.transform.localPosition = new Vector3(-0.14f, 0f, 0f);
            trailingSpark.transform.localScale = Vector3.one * 0.12f;
            OffsetRendererOrders(trailingSpark, 6);

            var arrow = InstantiateNestedPrefab(arrowPrefab, root.transform, "ArrowCore");
            arrow.transform.localPosition = new Vector3(0.02f, 0f, 0f);
            arrow.transform.localScale = Vector3.one * 0.27f;
            OffsetRendererOrders(arrow, 10);

            var tipSpark = InstantiateNestedPrefab(lightSparkPrefab, root.transform, "TipSpark");
            tipSpark.transform.localPosition = new Vector3(0.17f, 0f, 0f);
            tipSpark.transform.localScale = Vector3.one * 0.09f;
            OffsetRendererOrders(tipSpark, 13);

            SavePrefab(root, ProjectilePrefabPath);
        }

        private static Sprite EnsureSoftCircleSprite()
        {
            if (!File.Exists(GetAbsoluteProjectPath(SoftCircleSpritePath)))
            {
                var texture = BuildSoftCircleTexture(128);
                File.WriteAllBytes(GetAbsoluteProjectPath(SoftCircleSpritePath), texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(SoftCircleSpritePath, ImportAssetOptions.ForceSynchronousImport);

                if (AssetImporter.GetAtPath(SoftCircleSpritePath) is TextureImporter importer)
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

        private static GameObject InstantiateNestedPrefab(GameObject sourcePrefab, Transform parent, string name)
        {
            var instance = PrefabUtility.InstantiatePrefab(sourcePrefab) as GameObject;
            if (instance == null)
            {
                throw new System.InvalidOperationException($"Could not instantiate prefab at {AssetDatabase.GetAssetPath(sourcePrefab)}");
            }

            instance.name = name;
            instance.transform.SetParent(parent, false);
            return instance;
        }

        private static void OffsetRendererOrders(GameObject root, int baseOrder)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers == null || renderers.Length == 0)
            {
                return;
            }

            var minOrder = renderers[0].sortingOrder;
            for (var i = 1; i < renderers.Length; i++)
            {
                minOrder = Mathf.Min(minOrder, renderers[i].sortingOrder);
            }

            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sortingOrder = baseOrder + (renderers[i].sortingOrder - minOrder);
            }
        }

        private static SpriteRenderer CreateSprite(
            Transform parent,
            string name,
            Sprite sprite,
            Color color,
            int sortingOrder,
            Vector3 localPosition,
            Vector3 localScale)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

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
