using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class MonkVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Monk Renewing Pulse VFX Prefab";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/MonkVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string RenewingPulseBurstPrefabPath = SkillPrefabsFolder + "/MonkRenewingPulseBurst.prefab";

        private const string RegenerationHealthAreaSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_area.prefab";
        private const string RegenerationHealthAreaLoopSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Regeneration/Regeneration_health_area_loop.prefab";
        private const string BurstRingsSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Burst_rings.prefab";
        private const string FlashDubbleCircleSourcePrefabPath = "Assets/Lana Studio/Casual RPG VFX/Prefabs/Burst/Flash_dubble_circle.prefab";

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
        public static void BuildMonkRenewingPulseVfxPrefab()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildRenewingPulseBurstPrefab(softCircleSprite);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Monk Renewing Pulse VFX prefab rebuilt.");
        }

        public static void BuildMonkRenewingPulseVfxPrefabBatch()
        {
            BuildMonkRenewingPulseVfxPrefab();
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

            BuildMonkRenewingPulseVfxPrefab();
        }

        private static void BuildRenewingPulseBurstPrefab(Sprite softCircleSprite)
        {
            var regenerationAreaPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthAreaSourcePrefabPath);
            var regenerationAreaLoopPrefab = LoadRequiredAsset<GameObject>(RegenerationHealthAreaLoopSourcePrefabPath);
            var burstRingsPrefab = LoadRequiredAsset<GameObject>(BurstRingsSourcePrefabPath);
            var flashDubbleCirclePrefab = LoadRequiredAsset<GameObject>(FlashDubbleCircleSourcePrefabPath);

            var root = new GameObject("MonkRenewingPulseBurst");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "BlessingAuraOuter",
                softCircleSprite,
                new Color(0.98f, 0.86f, 0.42f, 0.12f),
                -18,
                Vector3.zero,
                new Vector3(1.10f, 1.04f, 1f));
            CreateSprite(
                root.transform,
                "BlessingAuraInner",
                softCircleSprite,
                new Color(0.50f, 0.98f, 0.70f, 0.18f),
                -12,
                new Vector3(0f, 0.01f, 0f),
                new Vector3(0.78f, 0.74f, 1f));
            CreateSprite(
                root.transform,
                "PulseCore",
                softCircleSprite,
                new Color(1f, 0.95f, 0.74f, 0.16f),
                -8,
                new Vector3(0f, 0.02f, 0f),
                new Vector3(0.48f, 0.46f, 1f));
            CreateSprite(
                root.transform,
                "PulseEdgeGlow",
                softCircleSprite,
                new Color(0.68f, 1f, 0.80f, 0.10f),
                -6,
                Vector3.zero,
                new Vector3(0.94f, 0.90f, 1f));

            var areaLoop = InstantiateNestedPrefab(regenerationAreaLoopPrefab, root.transform, "RenewingLoop");
            areaLoop.transform.localPosition = Vector3.zero;
            areaLoop.transform.localScale = Vector3.one * 0.112f;
            areaLoop.transform.localRotation = TopDownRotation;
            ConfigureParticleSystems(areaLoop, loop: false, prewarm: false, durationCap: 0.95f, simulationSpeedFloor: 1.08f);
            OffsetRendererOrders(areaLoop, 6);

            var areaPulse = InstantiateNestedPrefab(regenerationAreaPrefab, root.transform, "RenewingPulse");
            areaPulse.transform.localPosition = Vector3.zero;
            areaPulse.transform.localScale = Vector3.one * 0.086f;
            areaPulse.transform.localRotation = TopDownRotation;
            ConfigureParticleSystems(areaPulse, loop: false, prewarm: false, durationCap: 0.85f, simulationSpeedFloor: 1.12f);
            OffsetRendererOrders(areaPulse, 10);

            var flash = InstantiateNestedPrefab(flashDubbleCirclePrefab, root.transform, "PulseFlash");
            flash.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            flash.transform.localScale = Vector3.one * 0.17f;
            ConfigureParticleSystems(flash, loop: false, prewarm: false, durationCap: 0.48f, simulationSpeedFloor: 1.25f);
            OffsetRendererOrders(flash, 14);

            var rings = InstantiateNestedPrefab(burstRingsPrefab, root.transform, "PulseRings");
            rings.transform.localPosition = new Vector3(0f, 0.03f, 0f);
            rings.transform.localScale = Vector3.one * 0.15f;
            ConfigureParticleSystems(rings, loop: false, prewarm: false, durationCap: 0.72f, simulationSpeedFloor: 1.18f);
            OffsetRendererOrders(rings, 16);

            SavePrefab(root, RenewingPulseBurstPrefabPath);
        }

        private static void ConfigureParticleSystems(
            GameObject root,
            bool loop,
            bool prewarm,
            float durationCap,
            float simulationSpeedFloor)
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
                main.prewarm = prewarm;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.duration = Mathf.Min(main.duration, durationCap);
                main.simulationSpeed = Mathf.Max(main.simulationSpeed, simulationSpeedFloor);
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

            return GetLatestTimestampUtc(
                    BuilderScriptAssetPath,
                    SoftCircleSpritePath,
                    RegenerationHealthAreaSourcePrefabPath,
                    RegenerationHealthAreaLoopSourcePrefabPath,
                    BurstRingsSourcePrefabPath,
                    FlashDubbleCircleSourcePrefabPath)
                > GetLatestTimestampUtc(RenewingPulseBurstPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(RenewingPulseBurstPrefabPath) != null;
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
