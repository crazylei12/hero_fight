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
    public static class WindchimeBellcasterVisualBuilder
    {
        private const string BuildMenuPath = "Fight/Stage 01/Build Windchime Bellcaster Visual";
        private const string SourceResourceRoot = "Assets/Resources/HeroPreview/chatgpt_20260425_bellcaster";
        private const string WindchimeResourceRoot = "Assets/Resources/HeroPreview/support_002_windchime_bellcaster";
        private const string WindchimeResourcePrefix = "HeroPreview/support_002_windchime_bellcaster";
        private const string WindchimePrefabPath = "Assets/Prefabs/Heroes/support_002_windchime/WindchimeBellcaster.prefab";
        private const string WindchimeHeroAssetPath = "Assets/Data/Stage01Demo/Heroes/support_002_windchime/Windchime.asset";
        private const string BuilderScriptAssetPath = "Assets/Scripts/Editor/WindchimeBellcasterVisualBuilder.cs";
        private const float PixelsPerUnit = 64f;

        private static readonly Vector2 FootPivot = new Vector2(0.5f, 0.08f);

        private static readonly ClipBuildSpec[] ClipBuildSpecs =
        {
            new ClipBuildSpec("Idle", 7f, true, Source("Action01")),
            new ClipBuildSpec("Run", 10f, true, Source("Action02")),
            new ClipBuildSpec("Attack1", 12f, false, Source("Action04")),
            new ClipBuildSpec("Skill", 12f, false, Source("Action05")),
            new ClipBuildSpec("Ult", 12f, false, Source("Action06"), Source("Action09")),
            new ClipBuildSpec("Hit", 8f, false, Source("Action07")),
            new ClipBuildSpec("Death", 8f, false, Source("Action08")),
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
        public static void BuildWindchimeBellcasterVisual()
        {
            EnsureMappedResourceFolders();
            CreateWindchimePrefab();
            SyncWindchimeHeroAsset();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Windchime Bellcaster visual prefab rebuilt.");
        }

        public static void BuildWindchimeBellcasterVisualBatch()
        {
            BuildWindchimeBellcasterVisual();
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

            if (NeedsRebuild())
            {
                BuildWindchimeBellcasterVisual();
                return;
            }

            SyncWindchimeHeroAsset();
            AssetDatabase.SaveAssets();
        }

        private static bool NeedsRebuild()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WindchimePrefabPath) == null)
            {
                return true;
            }

            foreach (var spec in ClipBuildSpecs)
            {
                if (!AssetDatabase.IsValidFolder($"{WindchimeResourceRoot}/{spec.ClipKey}"))
                {
                    return true;
                }
            }

            return GetLatestTimestampUtc(BuilderScriptAssetPath, SourceResourceRoot)
                > GetLatestTimestampUtc(WindchimePrefabPath, WindchimeResourceRoot);
        }

        private static void EnsureMappedResourceFolders()
        {
            EnsureFolder(WindchimeResourceRoot);
            foreach (var spec in ClipBuildSpecs)
            {
                var targetFolder = $"{WindchimeResourceRoot}/{spec.ClipKey}";
                EnsureFolder(targetFolder);
                ClearGeneratedPngs(targetFolder);

                var frameIndex = 0;
                foreach (var sourceFolder in spec.SourceFolders)
                {
                    var sourceAbsoluteFolder = GetAbsoluteProjectPath($"{SourceResourceRoot}/{sourceFolder.FolderName}");
                    if (!Directory.Exists(sourceAbsoluteFolder))
                    {
                        throw new DirectoryNotFoundException($"Missing source animation folder: {SourceResourceRoot}/{sourceFolder.FolderName}");
                    }

                    var sourcePngs = Directory.GetFiles(sourceAbsoluteFolder, "*.png");
                    Array.Sort(sourcePngs, StringComparer.Ordinal);
                    foreach (var sourcePng in sourcePngs)
                    {
                        var targetAssetPath = $"{targetFolder}/{spec.FilePrefix}_{frameIndex:00}.png";
                        File.Copy(sourcePng, GetAbsoluteProjectPath(targetAssetPath), overwrite: true);
                        ApplyFrameTextureImporter(targetAssetPath);
                        frameIndex++;
                    }
                }
            }
        }

        private static void CreateWindchimePrefab()
        {
            EnsureFolder("Assets/Prefabs/Heroes/support_002_windchime");

            var root = new GameObject("WindchimeBellcaster");
            root.transform.localScale = new Vector3(1.35f, 1.35f, 1f);
            root.AddComponent<SortingGroup>();

            var spriteRenderer = root.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = 0;

            var idlePreview = root.AddComponent<SpriteTextureFrameAnimator>();
            idlePreview.Configure(
                $"{WindchimeResourcePrefix}/Idle",
                7f,
                PixelsPerUnit,
                FootPivot,
                loop: true);

            var visualConfig = root.AddComponent<SpriteSheetBattleVisualConfig>();
            ConfigureVisualConfig(visualConfig, spriteRenderer);

            PrefabUtility.SaveAsPrefabAsset(root, WindchimePrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void ConfigureVisualConfig(SpriteSheetBattleVisualConfig visualConfig, SpriteRenderer spriteRenderer)
        {
            var serialized = new SerializedObject(visualConfig);
            serialized.FindProperty("spriteRenderer").objectReferenceValue = spriteRenderer;
            serialized.FindProperty("resourcesRoot").stringValue = WindchimeResourcePrefix;
            serialized.FindProperty("pixelsPerUnit").floatValue = PixelsPerUnit;
            serialized.FindProperty("spritePivot").vector2Value = FootPivot;

            var clipsProperty = serialized.FindProperty("clips");
            clipsProperty.arraySize = ClipBuildSpecs.Length;
            for (var i = 0; i < ClipBuildSpecs.Length; i++)
            {
                var spec = ClipBuildSpecs[i];
                var clipProperty = clipsProperty.GetArrayElementAtIndex(i);
                clipProperty.FindPropertyRelative("key").stringValue = spec.ClipKey;
                clipProperty.FindPropertyRelative("resourcesFolder").stringValue = spec.ClipKey;
                clipProperty.FindPropertyRelative("framesPerSecond").floatValue = spec.FramesPerSecond;
                clipProperty.FindPropertyRelative("loop").boolValue = spec.Loop;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SyncWindchimeHeroAsset()
        {
            var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(WindchimeHeroAssetPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WindchimePrefabPath);
            if (hero == null || prefab == null)
            {
                return;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            if (hero.visualConfig.battlePrefab == prefab
                && hero.visualConfig.animatorController == null
                && !hero.visualConfig.battlePrefabFacesLeftByDefault)
            {
                return;
            }

            hero.visualConfig.battlePrefab = prefab;
            hero.visualConfig.animatorController = null;
            hero.visualConfig.battlePrefabFacesLeftByDefault = false;
            EditorUtility.SetDirty(hero);
        }

        private static SourceFolder Source(string folderName)
        {
            return new SourceFolder(folderName);
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
            var absoluteFolder = GetAbsoluteProjectPath(folderPath);
            if (!Directory.Exists(absoluteFolder))
            {
                return;
            }

            foreach (var path in Directory.GetFiles(absoluteFolder, "*.png"))
            {
                File.Delete(path);
                var metaPath = $"{path}.meta";
                if (File.Exists(metaPath))
                {
                    File.Delete(metaPath);
                }
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/');
            if (folderPath == "Assets" || AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var slashIndex = folderPath.LastIndexOf('/');
            var parent = slashIndex > 0 ? folderPath[..slashIndex] : "Assets";
            var folder = slashIndex > 0 ? folderPath[(slashIndex + 1)..] : folderPath;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folder);
        }

        private static string GetAbsoluteProjectPath(string assetPath)
        {
            var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
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

                var fullPath = GetAbsoluteProjectPath(assetPath);
                if (File.Exists(fullPath))
                {
                    latest = Max(latest, File.GetLastWriteTimeUtc(fullPath));
                }
                else if (Directory.Exists(fullPath))
                {
                    foreach (var filePath in Directory.GetFiles(fullPath, "*", SearchOption.AllDirectories))
                    {
                        latest = Max(latest, File.GetLastWriteTimeUtc(filePath));
                    }
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
            public ClipBuildSpec(string clipKey, float framesPerSecond, bool loop, params SourceFolder[] sourceFolders)
            {
                ClipKey = clipKey;
                FilePrefix = clipKey.ToLowerInvariant();
                FramesPerSecond = framesPerSecond;
                Loop = loop;
                SourceFolders = sourceFolders ?? Array.Empty<SourceFolder>();
            }

            public string ClipKey { get; }
            public string FilePrefix { get; }
            public float FramesPerSecond { get; }
            public bool Loop { get; }
            public SourceFolder[] SourceFolders { get; }
        }

        private readonly struct SourceFolder
        {
            public SourceFolder(string folderName)
            {
                FolderName = folderName;
            }

            public string FolderName { get; }
        }
    }
}
