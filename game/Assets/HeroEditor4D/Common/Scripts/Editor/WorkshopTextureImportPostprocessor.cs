using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;

namespace Assets.HeroEditor4D.Common.Scripts.Editor
{
    /// <summary>
    /// Keeps Fantasy Workshop exported textures compatible with HeroEditor4D collections.
    /// </summary>
    public class WorkshopTextureImportPostprocessor : AssetPostprocessor
    {
        private const string Root = "Assets/HeroEditor4D/FantasyHeroes/";

        private static readonly string[] RequiredMarkers =
        {
            "/Icons/Equipment/",
            "/Sprites/Equipment/"
        };

        private void OnPreprocessTexture()
        {
            if (assetImporter is TextureImporter importer)
            {
                ApplyWorkshopSettings(importer, assetPath);
            }
        }

        [MenuItem("Tools/HeroEditor4D/Fix Fantasy Workshop Texture Imports")]
        public static void FixFantasyWorkshopTextureImports()
        {
            var texturePaths = Directory
                .GetFiles(Path.Combine("Assets", "HeroEditor4D", "FantasyHeroes"), "*.png", SearchOption.AllDirectories)
                .Select(path => path.Replace("\\", "/"))
                .Where(IsFantasyWorkshopTexturePath)
                .ToList();

            var changedPaths = new List<string>();

            foreach (var path in texturePaths)
            {
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;

                if (importer == null) continue;

                if (ApplyWorkshopSettings(importer, path))
                {
                    changedPaths.Add(path);
                }
            }

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (var path in changedPaths)
                {
                    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.SaveAssets();

            UnityEngine.Debug.Log($"Fantasy Workshop texture import fix complete. Updated {changedPaths.Count} texture(s).");
        }

        private static bool ApplyWorkshopSettings(TextureImporter importer, string path)
        {
            if (!IsFantasyWorkshopTexturePath(path))
            {
                return false;
            }

            var changed = false;

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                changed = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            return changed;
        }

        private static bool IsFantasyWorkshopTexturePath(string path)
        {
            if (!path.StartsWith(Root) || !path.EndsWith(".png"))
            {
                return false;
            }

            if (!path.Contains("/Workshop/"))
            {
                return false;
            }

            return RequiredMarkers.Any(path.Contains);
        }
    }
}
