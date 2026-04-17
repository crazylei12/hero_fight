using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class StatusVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Shared Status VFX Prefabs";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SharedPrefabsFolder = PrefabsRootFolder + "/Shared";
        private const string StatusResourcesFolder = "Assets/Resources/Stage01Demo/VFX/Statuses";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/StatusVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string KnockbackStatusPrefabPath = SharedPrefabsFolder + "/KnockbackStatusLoop.prefab";
        private const string KnockbackStatusResourcesPrefabPath = StatusResourcesFolder + "/KnockbackStatusLoop.prefab";
        private const string WindAuraSourcePrefabPath = "Assets/Hun0FX/FX/BuffnDebuff_vol1/FX_Buff_01_Wind.prefab";
        private static readonly Quaternion TopDownRotation = Quaternion.Euler(90f, 0f, 0f);
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

        [MenuItem(BuildMenuPath)]
        public static void BuildSharedStatusVfxPrefabs()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SharedPrefabsFolder);
            EnsureFolder(StatusResourcesFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildKnockbackStatusPrefabs(softCircleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Shared status VFX prefabs rebuilt.");
        }

        public static void BuildSharedStatusVfxPrefabsBatch()
        {
            BuildSharedStatusVfxPrefabs();
            EditorApplication.Exit(0);
        }

        private static void TryAutoBuildIfNeeded()
        {
            autoBuildScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoBuildIfNeeded();
                return;
            }

            if (!NeedsRebuild())
            {
                return;
            }

            BuildSharedStatusVfxPrefabs();
        }

        private static void BuildKnockbackStatusPrefabs(Sprite softCircleSprite)
        {
            SavePrefab(CreateKnockbackStatusRoot(softCircleSprite), KnockbackStatusPrefabPath);
            SavePrefab(CreateKnockbackStatusRoot(softCircleSprite), KnockbackStatusResourcesPrefabPath);
        }

        private static GameObject CreateKnockbackStatusRoot(Sprite softCircleSprite)
        {
            var windAuraPrefab = LoadRequiredAsset<GameObject>(WindAuraSourcePrefabPath);

            var root = new GameObject("KnockbackStatusLoop");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "RearWake",
                softCircleSprite,
                new Color(0.92f, 0.97f, 1f, 0.16f),
                -6,
                new Vector3(0f, -0.16f, 0f),
                new Vector3(0.40f, 0.72f, 1f));
            CreateSprite(
                root.transform,
                "RearWakeCore",
                softCircleSprite,
                new Color(0.97f, 1f, 1f, 0.24f),
                -4,
                new Vector3(0f, -0.04f, 0f),
                new Vector3(0.24f, 0.46f, 1f));
            CreateSprite(
                root.transform,
                "CoreHalo",
                softCircleSprite,
                new Color(0.88f, 0.95f, 1f, 0.20f),
                -2,
                new Vector3(0f, 0.10f, 0f),
                new Vector3(0.28f, 0.22f, 1f));

            var leftWake = CreateSprite(
                root.transform,
                "LeftWake",
                softCircleSprite,
                new Color(0.86f, 0.93f, 1f, 0.14f),
                -5,
                new Vector3(-0.12f, -0.05f, 0f),
                new Vector3(0.14f, 0.30f, 1f));
            leftWake.transform.localRotation = Quaternion.Euler(0f, 0f, 16f);

            var rightWake = CreateSprite(
                root.transform,
                "RightWake",
                softCircleSprite,
                new Color(0.86f, 0.93f, 1f, 0.14f),
                -5,
                new Vector3(0.12f, -0.05f, 0f),
                new Vector3(0.14f, 0.30f, 1f));
            rightWake.transform.localRotation = Quaternion.Euler(0f, 0f, -16f);

            var windAura = InstantiateNestedPrefab(windAuraPrefab, root.transform, "WindAura");
            windAura.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            windAura.transform.localScale = Vector3.one * 0.18f;
            windAura.transform.localRotation = TopDownRotation;
            OffsetRendererOrders(windAura, 10);

            SaveParticleSystemLoopingState(windAura, true);
            return root;
        }

        private static void SaveParticleSystemLoopingState(GameObject root, bool loop)
        {
            if (root == null)
            {
                return;
            }

            var particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
            for (var i = 0; i < particleSystems.Length; i++)
            {
                var particleSystem = particleSystems[i];
                if (particleSystem == null)
                {
                    continue;
                }

                var main = particleSystem.main;
                main.loop = loop;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
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
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            var name = Path.GetFileName(folderPath);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                return;
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static bool NeedsRebuild()
        {
            if (!AllOutputAssetsExist())
            {
                return true;
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SoftCircleSpritePath)
                > GetLatestTimestampUtc(KnockbackStatusPrefabPath, KnockbackStatusResourcesPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(KnockbackStatusPrefabPath) != null
                && AssetDatabase.LoadAssetAtPath<GameObject>(KnockbackStatusResourcesPrefabPath) != null;
        }

        private static System.DateTime GetLatestTimestampUtc(params string[] assetPaths)
        {
            var latest = System.DateTime.MinValue;
            for (var i = 0; i < assetPaths.Length; i++)
            {
                var assetPath = assetPaths[i];
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var fullPath = GetAbsoluteProjectPath(assetPath);
                if (!File.Exists(fullPath))
                {
                    continue;
                }

                var timestamp = File.GetLastWriteTimeUtc(fullPath);
                if (timestamp > latest)
                {
                    latest = timestamp;
                }
            }

            return latest;
        }
    }
}
