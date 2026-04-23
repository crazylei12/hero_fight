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
        public string displayName;
        public HeroClass heroClass = HeroClass.Warrior;
        public TeamSide side = TeamSide.None;
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
}
