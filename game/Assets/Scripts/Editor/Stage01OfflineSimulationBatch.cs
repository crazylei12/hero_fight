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
        private const string IncludeMatchRecordsArg = "-fightOfflineIncludeMatchRecords";
        private const string ProgressPathArg = "-fightOfflineProgressPath";
        private const string OutputPathArg = "-fightOfflineOutputPath";
        private const string MaxTicksArg = "-fightOfflineMaxTicks";

        public static void RunFromCommandLine()
        {
            string outputPath = string.Empty;
            string progressPath = string.Empty;
            BattleOfflineSimulationRequest request = null;

            try
            {
                var arguments = Environment.GetCommandLineArgs();
                var selectionMode = ParseSelectionMode(ReadArgument(arguments, SelectionModeArg));
                var inputAssetPath = NormalizeAssetPath(ReadArgument(arguments, InputAssetPathArg) ?? DefaultInputAssetPath);
                var heroCatalogAssetPath = NormalizeAssetPath(ReadArgument(arguments, HeroCatalogAssetPathArg) ?? DefaultHeroCatalogAssetPath);
                outputPath = ResolveOutputPath(ReadArgument(arguments, OutputPathArg));
                progressPath = ResolveOptionalRepoPath(ReadArgument(arguments, ProgressPathArg));

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

                request = new BattleOfflineSimulationRequest
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
                    IncludeMatchRecords = ReadBoolArgument(arguments, IncludeMatchRecordsArg, false),
                };
                request.ProgressCallback = snapshot => WriteProgressSafe(progressPath, snapshot);

                WriteProgressSafe(progressPath, new BattleOfflineSimulationProgressSnapshot
                {
                    status = "Starting",
                    matchCount = request.MatchCount,
                    completedMatchCount = 0,
                    activeMatchNumber = request.MatchCount > 0 ? 1 : 0,
                    currentSeed = request.SeedStart,
                    outputPath = outputPath.Replace("\\", "/"),
                    message = "Launching offline simulation.",
                    updatedAt = DateTimeOffset.Now.ToString("O"),
                });

                var runResult = BattleOfflineSimulationService.Run(request);
                WriteOutputs(outputPath, runResult);
                WriteProgressSafe(progressPath, new BattleOfflineSimulationProgressSnapshot
                {
                    status = "Completed",
                    matchCount = request.MatchCount,
                    completedMatchCount = runResult.Report.runMeta.completedMatchCount,
                    activeMatchNumber = 0,
                    currentSeed = runResult.Report.runMeta.completedMatchCount > 0
                        ? request.SeedStart + runResult.Report.runMeta.completedMatchCount - 1
                        : request.SeedStart,
                    outputPath = outputPath.Replace("\\", "/"),
                    message = $"Completed {runResult.Report.runMeta.completedMatchCount}/{request.MatchCount} matches.",
                    updatedAt = DateTimeOffset.Now.ToString("O"),
                });
                Debug.Log($"[Stage01OfflineSimulation] Exported {runResult.Report.runMeta.completedMatchCount} matches to {outputPath}");
            }
            catch (Exception exception)
            {
                WriteProgressSafe(progressPath, new BattleOfflineSimulationProgressSnapshot
                {
                    status = "Failed",
                    matchCount = request != null ? request.MatchCount : 0,
                    completedMatchCount = 0,
                    activeMatchNumber = 0,
                    currentSeed = request != null ? request.SeedStart : 0,
                    outputPath = string.IsNullOrWhiteSpace(outputPath) ? string.Empty : outputPath.Replace("\\", "/"),
                    message = exception.Message,
                    updatedAt = DateTimeOffset.Now.ToString("O"),
                });
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

                    if (matchLog.MatchIndex >= 0 && matchLog.MatchIndex < runResult.Report.matches.Count)
                    {
                        runResult.Report.matches[matchLog.MatchIndex].fullLogFile =
                            Path.Combine(logsFolderName, matchLog.FileName).Replace("\\", "/");
                    }
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

        private static string ResolveOptionalRepoPath(string rawPath)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                return string.Empty;
            }

            var repoRoot = GetRepoRoot();
            return Path.IsPathRooted(rawPath)
                ? Path.GetFullPath(rawPath)
                : Path.GetFullPath(Path.Combine(repoRoot, rawPath));
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

        private static void WriteProgressSafe(string progressPath, BattleOfflineSimulationProgressSnapshot snapshot)
        {
            if (string.IsNullOrWhiteSpace(progressPath) || snapshot == null)
            {
                return;
            }

            try
            {
                var progressDirectory = Path.GetDirectoryName(progressPath);
                if (!string.IsNullOrWhiteSpace(progressDirectory))
                {
                    Directory.CreateDirectory(progressDirectory);
                }

                var progressJson = JsonUtility.ToJson(snapshot, true);
                File.WriteAllText(progressPath, progressJson, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"[Stage01OfflineSimulation] Failed to write progress file: {exception.Message}");
            }
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
