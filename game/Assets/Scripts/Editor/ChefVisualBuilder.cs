using System;
using System.IO;
using Fight.Data;
using Fight.UI;
using Fight.UI.Preview;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Fight.Editor
{
    public static class ChefVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Chef Visual";
        private const string SourceSplitRoot = "Assets/Art/Heroes/support_005_chef/split";
        private const string ResourcesRoot = "Assets/Resources/HeroPreview/support_005_chef";
        private const string ResourcesPrefix = "HeroPreview/support_005_chef";
        private const string ChefPrefabPath = "Assets/Prefabs/Heroes/support_005_chef/Chef.prefab";
        private const string ChefPortraitPath = "Assets/Prefabs/Heroes/support_005_chef/Chef_idle_front.png";
        private const string ChefHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_005_chef/Chef.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/ChefVisualBuilder.cs";
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.07f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", "Idle", "Idle", "idle", 7f, true),
            new ClipBuildSpec("Run", "Run", "Run", "run", 12f, true),
            new ClipBuildSpec("Attack1", "Attack1", "Attack1", "attack", 15f, false),
            new ClipBuildSpec("Skill", "SkillBurger", "Skill", "burger", 14f, false),
            new ClipBuildSpec("Skill_burger", "SkillBurger", "SkillBurger", "burger", 14f, false),
            new ClipBuildSpec("Skill_hotdog", "SkillHotdog", "SkillHotdog", "hotdog", 14f, false),
            new ClipBuildSpec("Skill_fries", "SkillFries", "SkillFries", "fries", 14f, false),
            new ClipBuildSpec("Ult", "Ult", "Ult", "bigmac", 12f, false),
            new ClipBuildSpec("Hit", "Hit", "Hit", "hit", 10f, false),
            new ClipBuildSpec("Death", "Death", "Death", "death", 8f, false),
        };

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
        public static void BuildChefVisual()
        {
            BuildAll();
            Debug.Log("Chef sprite-sheet visual prefab rebuilt.");
        }

        public static void BuildChefVisualBatch()
        {
            try
            {
                BuildAll();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void TryAutoBuildIfNeeded()
        {
            autoBuildScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoBuildIfNeeded();
                return;
            }

            if (NeedsRebuild())
            {
                BuildChefVisual();
                return;
            }

            SyncChefHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ChefPrefabPath) == null)
            {
                return true;
            }

            foreach (var spec in ClipBuildSpecs)
            {
                if (!AssetDatabase.IsValidFolder($"{ResourcesRoot}/{spec.ResourcesFolder}"))
                {
                    return true;
                }
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SourceSplitRoot)
                > GetLatestTimestampUtc(ChefPrefabPath, ResourcesRoot, ChefPortraitPath);
        }

        private static void BuildAll()
        {
            EnsureFolder(ResourcesRoot);
            CopyClipFrames();
            CreateChefPortrait();
            CreateChefPrefab();
            SyncChefHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CopyClipFrames()
        {
            foreach (var spec in ClipBuildSpecs)
            {
                var sourceFolder = $"{SourceSplitRoot}/{spec.SourceFolder}";
                var sourceAbsoluteFolder = ToAbsolutePath(sourceFolder);
                if (!Directory.Exists(sourceAbsoluteFolder))
                {
                    throw new DirectoryNotFoundException($"Missing Chef split frame folder: {sourceFolder}");
                }

                var sourceFiles = Directory.GetFiles(sourceAbsoluteFolder, $"{spec.FilePrefix}_*.png");
                Array.Sort(sourceFiles, StringComparer.OrdinalIgnoreCase);
                if (sourceFiles.Length == 0)
                {
                    throw new FileNotFoundException($"Chef split frame folder has no PNG frames: {sourceFolder}");
                }

                var resourcesFolder = $"{ResourcesRoot}/{spec.ResourcesFolder}";
                EnsureFolder(resourcesFolder);
                ClearGeneratedPngs(resourcesFolder);

                for (var i = 0; i < sourceFiles.Length; i++)
                {
                    var assetPath = $"{resourcesFolder}/{spec.FilePrefix}_{i:00}.png";
                    File.Copy(sourceFiles[i], ToAbsolutePath(assetPath), true);
                    ApplyFrameTextureImporter(assetPath);
                }
            }
        }

        private static void CreateChefPortrait()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_005_chef");
            var sourcePath = $"{ResourcesRoot}/Idle/idle_00.png";
            var absoluteSourcePath = ToAbsolutePath(sourcePath);
            if (!File.Exists(absoluteSourcePath))
            {
                return;
            }

            File.Copy(absoluteSourcePath, ToAbsolutePath(ChefPortraitPath), true);
            ApplyFrameTextureImporter(ChefPortraitPath);
        }

        private static void CreateChefPrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_005_chef");

            var root = new GameObject("Chef");
            root.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            root.AddComponent<SortingGroup>();

            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 0;

            var idlePreview = root.AddComponent<SpriteTextureFrameAnimator>();
            idlePreview.Configure(
                $"{ResourcesPrefix}/Idle",
                7f,
                PixelsPerUnit,
                FootPivot,
                loop: true);

            var visualConfig = root.AddComponent<SpriteSheetBattleVisualConfig>();
            ConfigureVisualConfig(visualConfig, spriteRenderer);

            PrefabUtility.SaveAsPrefabAsset(root, ChefPrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureVisualConfig(SpriteSheetBattleVisualConfig visualConfig, SpriteRenderer spriteRenderer)
        {
            var serialized = new SerializedObject(visualConfig);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("resourcesRoot").stringValue = ResourcesPrefix;
            serialized.FindProperty("pixelsPerUnit").floatValue = PixelsPerUnit;
            serialized.FindProperty("spritePivot").vector2Value = FootPivot;

            var clipsProperty = serialized.FindProperty("clips");
            clipsProperty.arraySize = ClipBuildSpecs.Length;
            for (var i = 0; i < ClipBuildSpecs.Length; i++)
            {
                var spec = ClipBuildSpecs[i];
                var clipProperty = clipsProperty.GetArrayElementAtIndex(i);
                clipProperty.FindPropertyRelative("key").stringValue = spec.ClipKey;
                clipProperty.FindPropertyRelative("resourcesFolder").stringValue = spec.ResourcesFolder;
                clipProperty.FindPropertyRelative("framesPerSecond").floatValue = spec.FramesPerSecond;
                clipProperty.FindPropertyRelative("loop").boolValue = spec.Loop;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SyncChefHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(ChefHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChefPrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            var portrait = AssetDatabase.LoadAssetAtPath<Sprite>(ChefPortraitPath);
            if (portrait != null)
            {
                hero.visualConfig.portrait = portrait;
            }

            hero.visualConfig.battlePrefab = prefab;
            hero.visualConfig.animatorController = null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            EditorUtility.SetDirty(hero);
        }

        private static void ApplyFrameTextureImporter(string assetPath)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ClearGeneratedPngs(string folderPath)
        {
            var absoluteFolder = ToAbsolutePath(folderPath);
            if (!Directory.Exists(absoluteFolder))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(absoluteFolder, "*.png"))
            {
                File.Delete(path);
            }
        }

        private static void EnsureFolder(string assetFolder)
        {
            assetFolder = assetFolder.Replace('\\', '/');
            if (assetFolder == "Assets" || AssetDatabase.IsValidFolder(assetFolder))
            {
                return;
            }

            var slashIndex = assetFolder.LastIndexOf('/');
            var parent = slashIndex > 0 ? assetFolder.Substring(0, slashIndex) : "Assets";
            var folder = slashIndex > 0 ? assetFolder.Substring(slashIndex + 1) : assetFolder;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string ToAbsolutePath(string assetPath)
        {
            var projectRoot = Path.GetDirectoryName(Application.dataPath);
            return Path.Combine(projectRoot ?? string.Empty, assetPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static DateTime GetLatestTimestampUtc(params string[] assetPaths)
        {
            var latest = DateTime.MinValue;
            if (assetPaths == null)
            {
                return latest;
            }

            foreach (var assetPath in assetPaths)
            {
                if (string.IsNullOrWhiteSpace(assetPath))
                {
                    continue;
                }

                var fullPath = ToAbsolutePath(assetPath);
                if (File.Exists(fullPath))
                {
                    latest = Max(latest, File.GetLastWriteTimeUtc(fullPath));
                    continue;
                }

                if (!Directory.Exists(fullPath))
                {
                    continue;
                }

                foreach (var filePath in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                {
                    latest = Max(latest, File.GetLastWriteTimeUtc(filePath));
                }
            }

            return latest;
        }

        private static DateTime Max(DateTime left, DateTime right)
        {
            return left >= right ? left : right;
        }

        private sealed class ClipBuildSpec
        {
            public ClipBuildSpec(
                string clipKey,
                string sourceFolder,
                string resourcesFolder,
                string filePrefix,
                float framesPerSecond,
                bool loop)
            {
                ClipKey = clipKey;
                SourceFolder = sourceFolder;
                ResourcesFolder = resourcesFolder;
                FilePrefix = filePrefix;
                FramesPerSecond = framesPerSecond;
                Loop = loop;
            }

            public string ClipKey { get; }
            public string SourceFolder { get; }
            public string ResourcesFolder { get; }
            public string FilePrefix { get; }
            public float FramesPerSecond { get; }
            public bool Loop { get; }
        }
    }
}
