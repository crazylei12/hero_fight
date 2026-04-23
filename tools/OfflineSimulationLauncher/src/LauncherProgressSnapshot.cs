namespace Fight.Tools.OfflineSimulationLauncher
{
    internal sealed class LauncherProgressSnapshot
    {
        public string status { get; set; }
        public int matchCount { get; set; }
        public int completedMatchCount { get; set; }
        public int activeMatchNumber { get; set; }
        public int currentSeed { get; set; }
        public string outputPath { get; set; }
        public string message { get; set; }
        public string updatedAt { get; set; }
    }
}
