using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Fight.Editor
{
    public static class TrimmedBuildOptimizationUtility
    {
        private const string SlimBuildRoot = "Builds/WindowsSlim";
        private const string ExecutableName = "FightStage01.exe";

        private static readonly string[] AggressiveTextureRoots =
        {
            "Assets/Game VFX -Explosion & Crack",
            "Assets/Hovl Studio",
            "Assets/Hun0FX",
            "Assets/Lana Studio",
            "Assets/Piloto Studio",
            "Assets/Art/VFX",
        };

        private static readonly string[] ModerateTextureRoots =
        {
            "Assets/HeroEditor4D",
            "Assets/FantasyMonsters",
            "Assets/Resources/Stage01Demo/VFX",
        };

        private static readonly string[] PreserveTextureRoots =
        {
            "Assets/Resources/UI",
            "Assets/Resources/Battle",
            "Assets/Prefabs/Heroes",
        };

        [MenuItem("Fight/Build/Apply Trimmed Build Slimming")]
        public static void ApplyTrimmedBuildSlimming()
        {
            var texturePaths = AssetDatabase.FindAssets("t:Texture")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            var modifiedCount = 0;
            foreach (var texturePath in texturePaths)
            {
                if (!TryGetCompressionPreset(texturePath, out var preset))
                {
                    continue;
                }

                var importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
                if (importer == null)
                {
                    continue;
                }

                if (!ApplyTexturePreset(importer, preset))
                {
                    continue;
                }

                modifiedCount++;
                importer.SaveAndReimport();
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[Slim] Updated import settings for {modifiedCount} textures.");
        }

        [MenuItem("Fight/Build/Export Slim Windows Player")]
        public static void ExportSlimWindowsPlayer()
        {
            ApplyTrimmedBuildSlimming();

            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputDirectory = Path.Combine(projectRoot, SlimBuildRoot);
            Directory.CreateDirectory(outputDirectory);

            Stage01SampleContentBuilder.GenerateDemoContentForBuild();

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                throw new InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");
            }

            var buildOptions = new BuildPlayerOptions
            {
                scenes = enabledScenes,
                locationPathName = Path.Combine(outputDirectory, ExecutableName),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(buildOptions);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Slim Windows build failed with result {report.summary.result}.");
            }

            WritePackedAssetReport(report, outputDirectory);
            Debug.Log($"[Slim] Windows player exported to {buildOptions.locationPathName}");
        }

        private static bool TryGetCompressionPreset(string texturePath, out TextureCompressionPreset preset)
        {
            preset = default;

            if (PreserveTextureRoots.Any(root => texturePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            {
                return false;
            }

            if (AggressiveTextureRoots.Any(root => texturePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            {
                preset = new TextureCompressionPreset(maxTextureSize: 256, compressionQuality: 70);
                return true;
            }

            if (ModerateTextureRoots.Any(root => texturePath.StartsWith(root, StringComparison.OrdinalIgnoreCase)))
            {
                preset = new TextureCompressionPreset(maxTextureSize: 512, compressionQuality: 75);
                return true;
            }

            return false;
        }

        private static bool ApplyTexturePreset(TextureImporter importer, TextureCompressionPreset preset)
        {
            var changed = false;

            if (importer.npotScale != TextureImporterNPOTScale.ToNearest)
            {
                importer.npotScale = TextureImporterNPOTScale.ToNearest;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.CompressedHQ)
            {
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                changed = true;
            }

            if (!importer.crunchedCompression)
            {
                importer.crunchedCompression = true;
                changed = true;
            }

            if (importer.compressionQuality != preset.CompressionQuality)
            {
                importer.compressionQuality = preset.CompressionQuality;
                changed = true;
            }

            if (importer.maxTextureSize > preset.MaxTextureSize)
            {
                importer.maxTextureSize = preset.MaxTextureSize;
                changed = true;
            }

            var platformSettings = importer.GetPlatformTextureSettings("Standalone");
            if (!platformSettings.overridden ||
                platformSettings.maxTextureSize != preset.MaxTextureSize ||
                platformSettings.textureCompression != TextureImporterCompression.CompressedHQ ||
                platformSettings.compressionQuality != preset.CompressionQuality ||
                !platformSettings.crunchedCompression)
            {
                platformSettings.name = "Standalone";
                platformSettings.overridden = true;
                platformSettings.maxTextureSize = preset.MaxTextureSize;
                platformSettings.textureCompression = TextureImporterCompression.CompressedHQ;
                platformSettings.compressionQuality = preset.CompressionQuality;
                platformSettings.crunchedCompression = true;
                importer.SetPlatformTextureSettings(platformSettings);
                changed = true;
            }

            return changed;
        }

        private static void WritePackedAssetReport(BuildReport report, string outputDirectory)
        {
            var lines = new List<string>
            {
                $"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                $"Output: {outputDirectory}",
                $"Total Size: {report.summary.totalSize}",
                $"Build Duration: {report.summary.totalTime}",
                "",
                "Top Build Files:",
            };

            foreach (var file in report.GetFiles().OrderByDescending(file => file.size).Take(20))
            {
                lines.Add($"- {file.path} :: {FormatSize(file.size)} :: role={file.role}");
            }

            var packedAssets = report.packedAssets
                .SelectMany(packedFile => packedFile.contents.Select(content => new PackedAssetRow(
                    packedFile.shortPath,
                    content.sourceAssetPath,
                    content.type?.Name ?? "<unknown>",
                    content.packedSize)))
                .Where(row => !string.IsNullOrWhiteSpace(row.SourceAssetPath))
                .ToArray();

            lines.Add("");
            lines.Add("Top Packed Assets:");
            foreach (var row in packedAssets
                         .GroupBy(row => row.SourceAssetPath, StringComparer.OrdinalIgnoreCase)
                         .Select(group => new
                         {
                             SourceAssetPath = group.Key,
                             PackedSize = group.Sum(entry => (long)entry.PackedSize),
                             Types = string.Join(", ", group.Select(entry => entry.TypeName).Distinct().OrderBy(type => type)),
                             Files = string.Join(", ", group.Select(entry => entry.BuildFile).Distinct().OrderBy(file => file)),
                         })
                         .OrderByDescending(entry => entry.PackedSize)
                         .Take(80))
            {
                lines.Add($"- {row.SourceAssetPath} :: {FormatSize(row.PackedSize)} :: {row.Types} :: {row.Files}");
            }

            lines.Add("");
            lines.Add("Top Packed Roots:");
            foreach (var row in packedAssets
                         .GroupBy(row => GetTopLevelAssetFolder(row.SourceAssetPath), StringComparer.OrdinalIgnoreCase)
                         .Select(group => new
                         {
                             Root = group.Key,
                             PackedSize = group.Sum(entry => (long)entry.PackedSize),
                         })
                         .OrderByDescending(entry => entry.PackedSize)
                         .Take(40))
            {
                lines.Add($"- {row.Root} :: {FormatSize(row.PackedSize)}");
            }

            lines.Add("");
            lines.Add("Top Resources File Contributors:");
            foreach (var row in packedAssets
                         .Where(row => row.BuildFile.IndexOf("resources.assets", StringComparison.OrdinalIgnoreCase) >= 0)
                         .GroupBy(row => row.SourceAssetPath, StringComparer.OrdinalIgnoreCase)
                         .Select(group => new
                         {
                             SourceAssetPath = group.Key,
                             PackedSize = group.Sum(entry => (long)entry.PackedSize),
                         })
                         .OrderByDescending(entry => entry.PackedSize)
                         .Take(80))
            {
                lines.Add($"- {row.SourceAssetPath} :: {FormatSize(row.PackedSize)}");
            }

            var reportPath = Path.Combine(outputDirectory, "build_size_report.txt");
            File.WriteAllLines(reportPath, lines);
        }

        private static string GetTopLevelAssetFolder(string sourceAssetPath)
        {
            if (string.IsNullOrWhiteSpace(sourceAssetPath))
            {
                return "<unknown>";
            }

            var normalizedPath = sourceAssetPath.Replace('\\', '/');
            if (!normalizedPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            var segments = normalizedPath.Split('/');
            if (segments.Length <= 2)
            {
                return normalizedPath;
            }

            return $"Assets/{segments[1]}";
        }

        private static string FormatSize(ulong bytes)
        {
            return FormatSize((long)bytes);
        }

        private static string FormatSize(long bytes)
        {
            return $"{bytes / 1024f / 1024f:F2} MB";
        }

        private readonly struct TextureCompressionPreset
        {
            public TextureCompressionPreset(int maxTextureSize, int compressionQuality)
            {
                MaxTextureSize = maxTextureSize;
                CompressionQuality = compressionQuality;
            }

            public int MaxTextureSize { get; }

            public int CompressionQuality { get; }
        }

        private readonly struct PackedAssetRow
        {
            public PackedAssetRow(string buildFile, string sourceAssetPath, string typeName, ulong packedSize)
            {
                BuildFile = buildFile;
                SourceAssetPath = sourceAssetPath;
                TypeName = typeName;
                PackedSize = packedSize;
            }

            public string BuildFile { get; }

            public string SourceAssetPath { get; }

            public string TypeName { get; }

            public ulong PackedSize { get; }
        }
    }
}
