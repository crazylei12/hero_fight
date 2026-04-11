using System;
using System.Collections.Generic;

namespace Fight.Data
{
    [Serializable]
    public class BattleTeamLoadout
    {
        public TeamSide side = TeamSide.Blue;
        public List<HeroDefinition> heroes = new List<HeroDefinition>();
    }
}
