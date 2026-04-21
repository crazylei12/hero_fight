using System.IO;
using Fight.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class MonkVfxPrefabBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Monk Guardian Mantra Bubble VFX";
        private const string GeneratedArtFolder = "Assets/Art/VFX/Generated";
        private const string PrefabsRootFolder = "Assets/Prefabs/VFX";
        private const string SkillPrefabsFolder = PrefabsRootFolder + "/Skills";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/MonkVfxPrefabBuilder.cs";
        private const string SoftCircleSpritePath = GeneratedArtFolder + "/vfx_soft_circle.png";
        private const string GuardianMantraImpactPrefabPath = SkillPrefabsFolder + "/MonkGuardianMantraBubbleImpact.prefab";
        private const string MonkUltimateSkillAssetPath = "Assets/Data/Stage01Demo/Skills/support_003_monk/Guardian Mantra.asset";
        private const string BubbleLoopSourcePrefabPath = "Assets/Super Pixel Effects Pack 2/Prefabs/fx2_bubble_large_green_loop.prefab";
        private static readonly Vector3 GuardianMantraLocalOffset = new Vector3(0f, 0.02f, 0f);
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
        public static void BuildMonkGuardianMantraVfxPrefab()
        {
            EnsureFolder(GeneratedArtFolder);
            EnsureFolder(PrefabsRootFolder);
            EnsureFolder(SkillPrefabsFolder);

            var softCircleSprite = EnsureSoftCircleSprite();
            BuildGuardianMantraImpactPrefab(softCircleSprite);
            SyncStage01DemoAssets();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Monk Guardian Mantra bubble VFX rebuilt.");
        }

        public static void BuildMonkGuardianMantraVfxPrefabBatch()
        {
            BuildMonkGuardianMantraVfxPrefab();
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

            BuildMonkGuardianMantraVfxPrefab();
        }

        private static void BuildGuardianMantraImpactPrefab(Sprite softCircleSprite)
        {
            var bubbleLoopPrefab = LoadRequiredAsset<GameObject>(BubbleLoopSourcePrefabPath);

            var root = new GameObject("MonkGuardianMantraBubbleImpact");
            root.AddComponent<SortingGroup>();

            CreateSprite(
                root.transform,
                "GroundGlow",
                softCircleSprite,
                new Color(0.30f, 0.92f, 0.62f, 0.08f),
                -8,
                new Vector3(0f, -0.01f, 0f),
                new Vector3(0.96f, 0.78f, 1f));
            CreateSprite(
                root.transform,
                "GroundCore",
                softCircleSprite,
                new Color(0.76f, 1f, 0.90f, 0.09f),
                -4,
                new Vector3(0f, 0.01f, 0f),
                new Vector3(0.54f, 0.42f, 1f));

            var bubbleBack = InstantiateNestedPrefab(bubbleLoopPrefab, root.transform, "BubbleBack");
            ConfigureBubbleInstance(
                bubbleBack,
                new Vector3(0f, 0.03f, 0f),
                new Vector3(1.16f, 0.94f, 1f),
                new Color(0.44f, 1f, 0.74f, 0.34f),
                6);

            var bubbleCore = InstantiateNestedPrefab(bubbleLoopPrefab, root.transform, "BubbleCore");
            ConfigureBubbleInstance(
                bubbleCore,
                new Vector3(0f, 0.04f, 0f),
                new Vector3(0.92f, 0.76f, 1f),
                new Color(0.80f, 1f, 0.92f, 0.72f),
                12);

            var topHighlight = CreateSprite(
                root.transform,
                "TopHighlight",
                softCircleSprite,
                new Color(0.90f, 1f, 0.96f, 0.10f),
                16,
                new Vector3(0f, 0.20f, 0f),
                new Vector3(0.28f, 0.10f, 1f));
            topHighlight.transform.localRotation = Quaternion.Euler(0f, 0f, 4f);

            SavePrefab(root, GuardianMantraImpactPrefabPath);
        }

        private static void ConfigureBubbleInstance(
            GameObject instance,
            Vector3 localPosition,
            Vector3 localScale,
            Color tint,
            int baseOrder)
        {
            if (instance == null)
            {
                return;
            }

            instance.transform.localPosition = localPosition;
            instance.transform.localScale = localScale;
            instance.transform.localRotation = Quaternion.identity;

            var spriteRenderers = instance.GetComponentsInChildren<SpriteRenderer>(true);
            for (var i = 0; i < spriteRenderers.Length; i++)
            {
                if (spriteRenderers[i] != null)
                {
                    spriteRenderers[i].color = tint;
                }
            }

            OffsetRendererOrders(instance, baseOrder);
        }

        private static void SyncStage01DemoAssets()
        {
            var impactPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(GuardianMantraImpactPrefabPath);
            var ultimateSkill = AssetDatabase.LoadAssetAtPath<SkillData>(MonkUltimateSkillAssetPath);
            if (impactPrefab == null || ultimateSkill == null)
            {
                return;
            }

            if (ultimateSkill.castImpactVfxPrefab == impactPrefab
                && ultimateSkill.castImpactVfxLocalOffset == GuardianMantraLocalOffset
                && ultimateSkill.castImpactVfxEulerAngles == Vector3.zero
                && ultimateSkill.castImpactVfxScaleMultiplier == Vector3.one
                && !ultimateSkill.castImpactVfxAlignToTargetDirection
                && ultimateSkill.castImpactVfxScaleWithSkillArea
                && Mathf.Approximately(ultimateSkill.castImpactVfxAreaDiameterScaleMultiplier, 0.18f))
            {
                return;
            }

            ultimateSkill.castImpactVfxPrefab = impactPrefab;
            ultimateSkill.castImpactVfxLocalOffset = GuardianMantraLocalOffset;
            ultimateSkill.castImpactVfxEulerAngles = Vector3.zero;
            ultimateSkill.castImpactVfxScaleMultiplier = Vector3.one;
            ultimateSkill.castImpactVfxAlignToTargetDirection = false;
            ultimateSkill.castImpactVfxScaleWithSkillArea = true;
            ultimateSkill.castImpactVfxAreaDiameterScaleMultiplier = 0.18f;
            EditorUtility.SetDirty(ultimateSkill);
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
                    BubbleLoopSourcePrefabPath)
                > GetLatestTimestampUtc(GuardianMantraImpactPrefabPath);
        }

        private static bool AllOutputAssetsExist()
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(GuardianMantraImpactPrefabPath) != null;
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
