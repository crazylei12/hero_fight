using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Fight.Editor
{
    public static class WindowsBuildExporter
    {
        private const string BuildRoot = "Builds/Windows";
        private const string ExecutableName = "FightStage01.exe";

        public static void ExportWindowsPlayer()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            var outputDirectory = Path.Combine(projectRoot, BuildRoot);
            Directory.CreateDirectory(outputDirectory);

            Stage01SampleContentBuilder.GenerateDemoContentForBuild();

            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                throw new System.InvalidOperationException("No enabled scenes were found in EditorBuildSettings.");
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
                throw new System.InvalidOperationException($"Windows build failed with result {report.summary.result}.");
            }

            Debug.Log($"[Build] Windows player exported to {buildOptions.locationPathName}");
        }
    }
}
