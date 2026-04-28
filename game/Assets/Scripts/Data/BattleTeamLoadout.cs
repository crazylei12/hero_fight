using System;
using System.Collections.Generic;

namespace Fight.Data
{
    [Serializable]
    public class BattleTeamLoadout
    {
        public TeamSide side = TeamSide.Blue;
        public BattleUltimateTimingStrategy ultimateTimingStrategy = BattleUltimateTimingStrategy.Standard;
        public BattleUltimateComboStrategy ultimateComboStrategy = BattleUltimateComboStrategy.Standard;
        public List<HeroDefinition> heroes = new List<HeroDefinition>();
        public List<BattleParticipantBinding> participantBindings = new List<BattleParticipantBinding>();
    }
}
