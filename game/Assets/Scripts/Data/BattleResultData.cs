using System;
using System.Collections.Generic;

namespace Fight.Data
{
    [Serializable]
    public class BattleResultData
    {
        public TeamSide winner = TeamSide.None;
        public BattleEndReason endReason = BattleEndReason.None;
        public bool enteredOvertime;
        public float elapsedTimeSeconds;
        public int blueKills;
        public int redKills;
        public List<HeroBattleStatLine> heroStats = new List<HeroBattleStatLine>();
        public List<string> eventSummary = new List<string>();
    }

    [Serializable]
    public class HeroBattleStatLine
    {
        public string heroId;
        public TeamSide side = TeamSide.None;
        public int kills;
        public int deaths;
        public float damageDealt;
        public float healingDone;
    }
}
