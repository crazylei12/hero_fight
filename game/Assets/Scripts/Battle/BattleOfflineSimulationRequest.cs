using System.Collections.Generic;
using Fight.Data;

namespace Fight.Battle
{
    public sealed class BattleOfflineSimulationRequest
    {
        public BattleOfflineSelectionMode SelectionMode { get; set; } = BattleOfflineSelectionMode.RandomCatalog;

        public BattleInputConfig TemplateInput { get; set; }

        public HeroCatalogData HeroCatalog { get; set; }

        public string InputAssetPath { get; set; }

        public string HeroCatalogAssetPath { get; set; }

        public int MatchCount { get; set; } = 1;

        public int SeedStart { get; set; }

        public float FixedDeltaTimeSeconds { get; set; } = 0.05f;

        public int MaxTickCount { get; set; } = 100000;

        public bool ExportFullLogs { get; set; }
    }

    public sealed class BattleOfflineSimulationRunResult
    {
        public BattleOfflineSimulationRunResult(
            BattleOfflineSimulationReport report,
            IReadOnlyList<BattleOfflineMatchLogExport> matchLogs)
        {
            Report = report;
            MatchLogs = matchLogs;
        }

        public BattleOfflineSimulationReport Report { get; }

        public IReadOnlyList<BattleOfflineMatchLogExport> MatchLogs { get; }
    }

    public sealed class BattleOfflineMatchLogExport
    {
        public BattleOfflineMatchLogExport(int matchIndex, int seed, string fileName, string content)
        {
            MatchIndex = matchIndex;
            Seed = seed;
            FileName = fileName;
            Content = content;
        }

        public int MatchIndex { get; }

        public int Seed { get; }

        public string FileName { get; }

        public string Content { get; }
    }
}
