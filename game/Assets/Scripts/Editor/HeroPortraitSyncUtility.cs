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
        private const string SyncMenuPath = "Fight/Dev/Sync Hero Portraits From Prefab PNGs";
        private const string Stage01HeroesRoot = "Assets/Data/Stage01Demo/Heroes";
        private const string PrefabHeroesRoot = "Assets/Prefabs/Heroes";
        private const string PreferredPortraitSuffix = "_idle_front";

        [MenuItem(SyncMenuPath)]
        public static void SyncStage01DemoHeroPortraits()
        {
            var heroAssetPaths = AssetDatabase.FindAssets("t:HeroDefinition", new[] { Stage01HeroesRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var updatedCount = 0;
            var missingCount = 0;

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

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HeroPortraitSync] Synced {updatedCount} hero portrait reference(s); missing portraits for {missingCount} hero(es).");
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
                .Where(path => Path.GetFileNameWithoutExtension(path).EndsWith(PreferredPortraitSuffix, StringComparison.OrdinalIgnoreCase))
                .OrderBy(path => GetPortraitMatchScore(path, hero))
                .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return portraitAssetPaths.Count > 0 ? portraitAssetPaths[0] : null;
        }

        private static int GetPortraitMatchScore(string assetPath, HeroDefinition hero)
        {
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var displayNameCandidate = string.IsNullOrWhiteSpace(hero.displayName)
                ? null
                : $"{hero.displayName}{PreferredPortraitSuffix}";
            if (!string.IsNullOrWhiteSpace(displayNameCandidate)
                && string.Equals(fileName, displayNameCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }

            var prefabNameCandidate = hero.visualConfig?.battlePrefab != null
                ? $"{hero.visualConfig.battlePrefab.name}{PreferredPortraitSuffix}"
                : null;
            if (!string.IsNullOrWhiteSpace(prefabNameCandidate)
                && string.Equals(fileName, prefabNameCandidate, StringComparison.OrdinalIgnoreCase))
            {
                return 1;
            }

            var heroNameFromId = ExtractHeroNameFromId(hero.heroId);
            if (!string.IsNullOrWhiteSpace(heroNameFromId)
                && string.Equals(fileName, $"{heroNameFromId}{PreferredPortraitSuffix}", StringComparison.OrdinalIgnoreCase))
            {
                return 2;
            }

            return 10;
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
}
