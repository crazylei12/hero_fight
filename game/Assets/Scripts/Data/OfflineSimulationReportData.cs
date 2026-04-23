using System;
using System.Collections.Generic;

namespace Fight.Data
{
    public enum BattleOfflineSelectionMode
    {
        FixedInput = 0,
        RandomCatalog = 1,
    }

    [Serializable]
    public class BattleOfflineSimulationReport
    {
        public BattleOfflineSimulationRunMeta runMeta = new BattleOfflineSimulationRunMeta();
        public List<BattleOfflineSimulationMatchRecord> matches = new List<BattleOfflineSimulationMatchRecord>();
        public List<BattleOfflineSimulationHeroAggregateGroup> heroAggregatesByClass = new List<BattleOfflineSimulationHeroAggregateGroup>();
    }

    [Serializable]
    public class BattleOfflineSimulationRunMeta
    {
        public string generatedAt;
        public string selectionMode;
        public string inputAssetPath;
        public string heroCatalogPath;
        public int matchCount;
        public int completedMatchCount;
        public int seedStart;
        public float fixedDeltaTime;
        public bool exportFullLogs;
        public bool includeMatchRecords;
        public bool uniqueHeroValidation = true;
    }

    [Serializable]
    public class BattleOfflineSimulationMatchRecord
    {
        public int matchIndex;
        public int seed;
        public string winner;
        public string endReason;
        public bool enteredOvertime;
        public float elapsedTimeSeconds;
        public int blueKills;
        public int redKills;
        public List<BattleOfflineHeroReference> blueHeroes = new List<BattleOfflineHeroReference>();
        public List<BattleOfflineHeroReference> redHeroes = new List<BattleOfflineHeroReference>();
        public List<BattleOfflineHeroMatchStat> heroStats = new List<BattleOfflineHeroMatchStat>();
        public string fullLogFile;
    }

    [Serializable]
    public class BattleOfflineHeroReference
    {
        public string heroId;
        public string displayName;
        public string heroClass;
        public string side;
        public int slotIndex;
    }

    [Serializable]
    public class BattleOfflineHeroMatchStat
    {
        public string heroId;
        public string displayName;
        public string heroClass;
        public string side;
        public int slotIndex;
        public bool won;
        public int kills;
        public int deaths;
        public int assists;
        public float damageDealt;
        public float damageTaken;
        public float healingDone;
        public float shieldingDone;
        public int activeSkillCastCount;
        public int ultimateCastCount;
    }

    [Serializable]
    public class BattleOfflineSimulationHeroAggregateGroup
    {
        public string heroClass;
        public List<BattleOfflineHeroAggregate> heroes = new List<BattleOfflineHeroAggregate>();
    }

    [Serializable]
    public class BattleOfflineHeroAggregate
    {
        public string heroId;
        public string displayName;
        public string heroClass;
        public string side = "Combined";
        public int pickCount;
        public int winCount;
        public int lossCount;
        public int appearancesAsBlue;
        public int appearancesAsRed;
        public float winRate;
        public int totalKills;
        public int totalDeaths;
        public int totalAssists;
        public float totalDamageDealt;
        public float totalDamageTaken;
        public float totalHealingDone;
        public float totalShieldingDone;
        public int totalActiveSkillCastCount;
        public int totalUltimateCastCount;
        public float averageKills;
        public float averageDeaths;
        public float averageAssists;
        public float averageDamageDealt;
        public float averageDamageTaken;
        public float averageHealingDone;
        public float averageShieldingDone;
        public float averageActiveSkillCastCount;
        public float averageUltimateCastCount;
    }
}
