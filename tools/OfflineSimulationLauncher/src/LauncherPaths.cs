using System;
using System.IO;

namespace Fight.Tools.OfflineSimulationLauncher
{
    internal static class LauncherPaths
    {
        public static string ResolveRepositoryRoot(string baseDirectory)
        {
            string currentDirectory = string.IsNullOrWhiteSpace(baseDirectory)
                ? Environment.CurrentDirectory
                : Path.GetFullPath(baseDirectory);

            for (int depth = 0; depth < 8; depth++)
            {
                if (LooksLikeRepositoryRoot(currentDirectory))
                {
                    return currentDirectory;
                }

                DirectoryInfo parent = Directory.GetParent(currentDirectory);
                if (parent == null)
                {
                    break;
                }

                currentDirectory = parent.FullName;
            }

            return string.Empty;
        }

        public static string ResolveBatchPath(string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                return string.Empty;
            }

            return Path.Combine(repoRoot, "tools", "run_stage01_offline_sim.bat");
        }

        public static string ResolveDefaultOutputPath(string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                return string.Empty;
            }

            return Path.Combine(repoRoot, "exports", "stage01_offline_simulation", "offline_simulation_report.json");
        }

        public static string ResolveProgressPath(string repoRoot)
        {
            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                return string.Empty;
            }

            string fileName = string.Format(
                "offline_launcher_progress_{0:yyyyMMdd_HHmmss_fff}.json",
                DateTime.Now);
            return Path.Combine(repoRoot, "Temp", fileName);
        }

        public static string ResolveOutputDirectory(string outputPath)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
            {
                return string.Empty;
            }

            string directoryPath = Path.GetDirectoryName(outputPath);
            return string.IsNullOrWhiteSpace(directoryPath) ? string.Empty : directoryPath;
        }

        public static string ToDisplayPath(string repoRoot, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(repoRoot))
            {
                return path;
            }

            string normalizedRepoRoot = EnsureTrailingSeparator(Path.GetFullPath(repoRoot));
            string normalizedPath = Path.GetFullPath(path);
            if (!normalizedPath.StartsWith(normalizedRepoRoot, StringComparison.OrdinalIgnoreCase))
            {
                return normalizedPath;
            }

            return normalizedPath.Substring(normalizedRepoRoot.Length);
        }

        public static string ResolveUserPath(string repoRoot, string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.IsPathRooted(path)
                ? Path.GetFullPath(path)
                : Path.GetFullPath(Path.Combine(repoRoot, path));
        }

        private static bool LooksLikeRepositoryRoot(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            {
                return false;
            }

            return File.Exists(Path.Combine(directoryPath, "tools", "run_stage01_offline_sim.bat")) &&
                   Directory.Exists(Path.Combine(directoryPath, "game", "Assets")) &&
                   Directory.Exists(Path.Combine(directoryPath, "docs"));
        }

        private static string EnsureTrailingSeparator(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return path.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal)
                ? path
                : path + Path.DirectorySeparatorChar;
        }
    }
}
