using System;
using System.Globalization;
using System.IO;
using System.Text;
using Fight.Battle;
using Fight.Data;
using UnityEditor;
using UnityEngine;

namespace Fight.Editor
{
    public static class Stage01OfflineSimulationBatch
    {
        private const string DefaultInputAssetPath = "Assets/Resources/Stage01Demo/Stage01DemoBattleInput.asset";
        private const string DefaultHeroCatalogAssetPath = "Assets/Resources/Stage01Demo/Stage01HeroCatalog.asset";
        private const string DefaultOutputRelativePath = "exports/stage01_offline_simulation/offline_simulation_report.json";
        private const string SelectionModeArg = "-fightOfflineMode";
        private const string InputAssetPathArg = "-fightOfflineInputAssetPath";
        private const string HeroCatalogAssetPathArg = "-fightOfflineHeroCatalogAssetPath";
        private const string CountArg = "-fightOfflineCount";
        private const string SeedStartArg = "-fightOfflineSeedStart";
        private const string FixedDeltaTimeArg = "-fightOfflineFixedDeltaTime";
        private const string ExportFullLogsArg = "-fightOfflineExportFullLogs";
        private const string OutputPathArg = "-fightOfflineOutputPath";
        private const string MaxTicksArg = "-fightOfflineMaxTicks";

        public static void RunFromCommandLine()
        {
            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var selectionMode = ParseSelectionMode(ReadArgument(arguments, SelectionModeArg));
                var inputAssetPath = NormalizeAssetPath(ReadArgument(arguments, InputAssetPathArg) ?? DefaultInputAssetPath);
                var heroCatalogAssetPath = NormalizeAssetPath(ReadArgument(arguments, HeroCatalogAssetPathArg) ?? DefaultHeroCatalogAssetPath);
                var outputPath = ResolveOutputPath(ReadArgument(arguments, OutputPathArg));

                var templateInput = AssetDatabase.LoadAssetAtPath<BattleInputConfig>(inputAssetPath);
                if (templateInput == null)
                {
                    throw new InvalidOperationException($"Could not load BattleInputConfig at [{inputAssetPath}].");
                }

                HeroCatalogData heroCatalog = null;
                if (selectionMode == BattleOfflineSelectionMode.RandomCatalog)
                {
                    heroCatalog = AssetDatabase.LoadAssetAtPath<HeroCatalogData>(heroCatalogAssetPath);
                    if (heroCatalog == null)
                    {
                        throw new InvalidOperationException($"Could not load HeroCatalogData at [{heroCatalogAssetPath}].");
                    }
                }
                else
                {
                    heroCatalogAssetPath = string.Empty;
                }

                var request = new BattleOfflineSimulationRequest
                {
                    SelectionMode = selectionMode,
                    TemplateInput = templateInput,
                    HeroCatalog = heroCatalog,
                    InputAssetPath = inputAssetPath,
                    HeroCatalogAssetPath = heroCatalogAssetPath,
                    MatchCount = ReadIntArgument(arguments, CountArg, 1),
                    SeedStart = ReadIntArgument(arguments, SeedStartArg, 0),
                    FixedDeltaTimeSeconds = ReadFloatArgument(arguments, FixedDeltaTimeArg, 0.05f),
                    MaxTickCount = ReadIntArgument(arguments, MaxTicksArg, 100000),
                    ExportFullLogs = ReadBoolArgument(arguments, ExportFullLogsArg, false),
                };

                var runResult = BattleOfflineSimulationService.Run(request);
                WriteOutputs(outputPath, runResult);
                Debug.Log($"[Stage01OfflineSimulation] Exported {runResult.Report.runMeta.completedMatchCount} matches to {outputPath}");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }

        private static void WriteOutputs(string outputPath, BattleOfflineSimulationRunResult runResult)
        {
            var outputDirectory = Path.GetDirectoryName(outputPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new InvalidOperationException($"Could not resolve output directory for [{outputPath}].");
            }

            Directory.CreateDirectory(outputDirectory);

            if (runResult.MatchLogs != null && runResult.MatchLogs.Count > 0)
            {
                var logsFolderName = $"{Path.GetFileNameWithoutExtension(outputPath)}_logs";
                var logsDirectory = Path.Combine(outputDirectory, logsFolderName);
                Directory.CreateDirectory(logsDirectory);

                for (var i = 0; i < runResult.MatchLogs.Count; i++)
                {
                    var matchLog = runResult.MatchLogs[i];
                    var logPath = Path.Combine(logsDirectory, matchLog.FileName);
                    File.WriteAllText(logPath, matchLog.Content ?? string.Empty, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                }
            }

            var reportJson = JsonUtility.ToJson(runResult.Report, true);
            File.WriteAllText(outputPath, reportJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static BattleOfflineSelectionMode ParseSelectionMode(string rawValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
            {
                return BattleOfflineSelectionMode.RandomCatalog;
            }

            if (string.Equals(rawValue, "fixed", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rawValue, "fixedinput", StringComparison.OrdinalIgnoreCase))
            {
                return BattleOfflineSelectionMode.FixedInput;
            }

            if (string.Equals(rawValue, "random", StringComparison.OrdinalIgnoreCase)
                || string.Equals(rawValue, "randomcatalog", StringComparison.OrdinalIgnoreCase))
            {
                return BattleOfflineSelectionMode.RandomCatalog;
            }

            throw new InvalidOperationException(
                $"Unsupported offline selection mode [{rawValue}]. Supported values: FixedInput, RandomCatalog.");
        }

        private static string ResolveOutputPath(string rawOutputPath)
        {
            var outputPath = string.IsNullOrWhiteSpace(rawOutputPath)
                ? DefaultOutputRelativePath
                : rawOutputPath;
            var repoRoot = GetRepoRoot();
            return Path.IsPathRooted(outputPath)
                ? Path.GetFullPath(outputPath)
                : Path.GetFullPath(Path.Combine(repoRoot, outputPath));
        }

        private static string NormalizeAssetPath(string assetPath)
        {
            return string.IsNullOrWhiteSpace(assetPath)
                ? string.Empty
                : assetPath.Replace("\\", "/").Trim();
        }

        private static string GetRepoRoot()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
        }

        private static string ReadArgument(string[] arguments, string argumentName)
        {
            for (var i = 0; i < arguments.Length - 1; i++)
            {
                if (string.Equals(arguments[i], argumentName, StringComparison.OrdinalIgnoreCase))
                {
                    return arguments[i + 1];
                }
            }

            return null;
        }

        private static int ReadIntArgument(string[] arguments, string argumentName, int defaultValue)
        {
            var value = ReadArgument(arguments, argumentName);
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }

        private static float ReadFloatArgument(string[] arguments, string argumentName, float defaultValue)
        {
            var value = ReadArgument(arguments, argumentName);
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
                ? result
                : defaultValue;
        }

        private static bool ReadBoolArgument(string[] arguments, string argumentName, bool defaultValue)
        {
            var value = ReadArgument(arguments, argumentName);
            return bool.TryParse(value, out var result) ? result : defaultValue;
        }
    }
}
