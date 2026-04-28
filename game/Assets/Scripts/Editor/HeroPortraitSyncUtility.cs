using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Fight.Data;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    public static class HeroPortraitSyncUtility
    {
        private const string SyncMenuPath = "Fight/Dev/Sync Hero Portraits From Idle First Frames";
        private const string HeroPreviewRoot = "Assets/Resources/HeroPreview";
        private const string IdleClipFolderName = "Idle";
        private const string IdleFirstFrameFileName = "idle_00.png";
        private const string PrefabHeroesRoot = "Assets/Prefabs/Heroes";
        private const string FrontPortraitSuffix = "_idle_front";
        private const string MonsterPortraitSuffix = "_idle_monster";
        private static bool autoSyncScheduled;
        private static bool autoSyncInProgress;

        [InitializeOnLoadMethod]
        private static void ScheduleInitialAutoSync()
        {
            if (Application.isBatchMode)
            {
                return;
            }

            ScheduleAutoSync();
        }

        [MenuItem(SyncMenuPath)]
        public static void SyncStage01DemoHeroPortraits()
        {
            SyncHeroPortraits(logSummary: true);
        }

        public static void ScheduleAutoSync()
        {
            if (autoSyncScheduled || autoSyncInProgress)
            {
                return;
            }

            autoSyncScheduled = true;
            EditorApplication.delayCall += RunScheduledAutoSync;
        }

        public static bool CouldAffectPortraitSync(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return false;
            }

            var normalizedAssetPath = assetPath.Replace("\\", "/");
            if (normalizedAssetPath.StartsWith(HeroPreviewRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedAssetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase);
            }

            if (normalizedAssetPath.StartsWith(PrefabHeroesRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedAssetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                    || normalizedAssetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);
            }

            if (!normalizedAssetPath.EndsWith(".asset", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return AssetDatabase.GetMainAssetTypeAtPath(normalizedAssetPath) == typeof(HeroDefinition);
        }

        private static void RunScheduledAutoSync()
        {
            autoSyncScheduled = false;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                ScheduleAutoSync();
                return;
            }

            SyncHeroPortraits(logSummary: false);
        }

        private static void SyncHeroPortraits(bool logSummary)
        {
            var heroAssetPaths = AssetDatabase.FindAssets("t:HeroDefinition")
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var updatedCount = 0;
            var missingCount = 0;

            autoSyncInProgress = true;
            try
            {
                foreach (var heroAssetPath in heroAssetPaths)
                {
                    var hero = AssetDatabase.LoadAssetAtPath<HeroDefinition>(heroAssetPath);
                    if (hero == null)
                    {
                        continue;
                    }

                    if (TryAssignPortraitFromPrefabFolder(hero))
                    {
                        updatedCount++;
                        continue;
                    }

                    if (hero.visualConfig == null || hero.visualConfig.portrait == null)
                    {
                        missingCount++;
                    }
                }
            }
            finally
            {
                autoSyncInProgress = false;
            }

            if (updatedCount > 0)
            {
                AssetDatabase.SaveAssets();
            }

            if (logSummary || updatedCount > 0)
            {
                Debug.Log($"[HeroPortraitSync] Synced {updatedCount} hero portrait reference(s); missing portraits for {missingCount} hero(es).");
            }
        }

        public static void SyncStage01DemoHeroPortraitsBatch()
        {
            try
            {
                SyncStage01DemoHeroPortraits();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        public static bool TryAssignPortraitFromPrefabFolder(HeroDefinition hero)
        {
            if (hero == null || string.IsNullOrWhiteSpace(hero.heroId))
            {
                return false;
            }

            var portraitAssetPath = ResolvePortraitAssetPath(hero);
            if (string.IsNullOrWhiteSpace(portraitAssetPath))
            {
                return false;
            }

            EnsurePortraitImportedAsSprite(portraitAssetPath);

            var portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitAssetPath);
            if (portraitSprite == null)
            {
                Debug.LogWarning($"[HeroPortraitSync] Could not load sprite portrait at [{portraitAssetPath}] for hero [{hero.heroId}].");
                return false;
            }

            hero.visualConfig ??= new HeroVisualConfig();
            if (hero.visualConfig.portrait == portraitSprite)
            {
                return false;
            }

            hero.visualConfig.portrait = portraitSprite;
            EditorUtility.SetDirty(hero);
            return true;
        }

        private static string ResolvePortraitAssetPath(HeroDefinition hero)
        {
            var idleFirstFramePath = ResolveIdleFirstFrameAssetPath(hero);
            if (!string.IsNullOrWhiteSpace(idleFirstFramePath))
            {
                return idleFirstFramePath;
            }

            foreach (var folderPath in EnumerateCandidatePortraitFolders(hero))
            {
                if (string.IsNullOrWhiteSpace(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
                {
                    continue;
                }

                var portraitPath = FindBestPortraitAssetInFolder(folderPath, hero);
                if (!string.IsNullOrWhiteSpace(portraitPath))
                {
                    return portraitPath;
                }
            }

            return null;
        }

        private static string ResolveIdleFirstFrameAssetPath(HeroDefinition hero)
        {
            if (hero == null || string.IsNullOrWhiteSpace(hero.heroId))
            {
                return null;
            }

            var idleFolderPath = $"{HeroPreviewRoot}/{hero.heroId}/{IdleClipFolderName}";
            var exactFirstFramePath = $"{idleFolderPath}/{IdleFirstFrameFileName}";
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(exactFirstFramePath) != null)
            {
                return exactFirstFramePath;
            }

            if (!AssetDatabase.IsValidFolder(idleFolderPath))
            {
                return null;
            }

            var idleFramePaths = AssetDatabase.FindAssets("t:Texture2D", new[] { idleFolderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .OrderBy(GetIdleFrameSortKey)
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return idleFramePaths.Count > 0 ? idleFramePaths[0] : null;
        }

        private static IEnumerable<string> EnumerateCandidatePortraitFolders(HeroDefinition hero)
        {
            yield return $"{PrefabHeroesRoot}/{hero.heroId}";

            if (hero.visualConfig?.battlePrefab == null)
            {
                yield break;
            }

            var battlePrefabPath = AssetDatabase.GetAssetPath(hero.visualConfig.battlePrefab);
            if (string.IsNullOrWhiteSpace(battlePrefabPath))
            {
                yield break;
            }

            var battlePrefabFolder = Path.GetDirectoryName(battlePrefabPath)?.Replace("\\", "/");
            if (!string.IsNullOrWhiteSpace(battlePrefabFolder))
            {
                yield return battlePrefabFolder;
            }
        }

        private static string FindBestPortraitAssetInFolder(string folderPath, HeroDefinition hero)
        {
            var portraitAssetPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                .Where(path => GetPortraitSuffixPriority(path) < int.MaxValue)
                .OrderBy(path => GetPortraitMatchScore(path, hero))
                .ThenBy(path => GetPortraitSuffixPriority(path))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return portraitAssetPaths.Count > 0 ? portraitAssetPaths[0] : null;
        }

        private static int GetPortraitMatchScore(string assetPath, HeroDefinition hero)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var portraitSuffix = GetPortraitSuffix(assetPath);
            if (string.IsNullOrWhiteSpace(portraitSuffix))
            {
                return 100;
            }

            var displayNameCandidate = string.IsNullOrWhiteSpace(hero.displayName)
                ? null
                : $"{hero.displayName}{portraitSuffix}";
            if (!string.IsNullOrWhiteSpace(displayNameCandidate)
                && string.Equals(fileName, displayNameCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var prefabNameCandidate = hero.visualConfig?.battlePrefab != null
                ? $"{hero.visualConfig.battlePrefab.name}{portraitSuffix}"
                : null;
            if (!string.IsNullOrWhiteSpace(prefabNameCandidate)
                && string.Equals(fileName, prefabNameCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            var heroNameFromId = ExtractHeroNameFromId(hero.heroId);
            if (!string.IsNullOrWhiteSpace(heroNameFromId)
                && string.Equals(fileName, $"{heroNameFromId}{portraitSuffix}", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 10;
        }

        private static int GetPortraitSuffixPriority(string assetPath)
        {
            return GetPortraitSuffix(assetPath) switch
            {
                FrontPortraitSuffix => 0,
                MonsterPortraitSuffix => 1,
                _ => int.MaxValue,
            };
        }

        private static string GetPortraitSuffix(string assetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (fileName.EndsWith(FrontPortraitSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return FrontPortraitSuffix;
            }

            if (fileName.EndsWith(MonsterPortraitSuffix, StringComparison.OrdinalIgnoreCase))
            {
                return MonsterPortraitSuffix;
            }

            return null;
        }

        private static int GetIdleFrameSortKey(string assetPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return int.MaxValue;
            }

            var lastSeparatorIndex = fileName.LastIndexOf('_');
            if (lastSeparatorIndex < 0 || lastSeparatorIndex >= fileName.Length - 1)
            {
                return int.MaxValue - 1;
            }

            return int.TryParse(fileName.Substring(lastSeparatorIndex + 1), out var frameIndex)
                ? frameIndex
                : int.MaxValue - 1;
        }

        private static string ExtractHeroNameFromId(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return null;
            }

            var lastSeparatorIndex = heroId.LastIndexOf('_');
            if (lastSeparatorIndex < 0 || lastSeparatorIndex >= heroId.Length - 1)
            {
                return heroId;
            }

            var rawName = heroId.Substring(lastSeparatorIndex + 1);
            if (string.IsNullOrWhiteSpace(rawName))
            {
                return heroId;
            }

            return char.ToUpperInvariant(rawName[0]) + rawName.Substring(1);
        }

        private static void EnsurePortraitImportedAsSprite(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite
                && importer.spriteImportMode == SpriteImportMode.Single)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    internal sealed class HeroPortraitSyncAssetPostprocessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            if (importedAssets.Any(HeroPortraitSyncUtility.CouldAffectPortraitSync)
                || movedAssets.Any(HeroPortraitSyncUtility.CouldAffectPortraitSync))
            {
                HeroPortraitSyncUtility.ScheduleAutoSync();
            }
        }
    }
}
