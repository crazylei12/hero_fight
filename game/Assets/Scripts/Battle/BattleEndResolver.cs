using Fight.Data;

namespace Fight.Battle
{
    public static class BattleEndResolver
    {
        public static bool ShouldEnterOvertime(BattleScoreSystem scoreSystem)
        {
            return scoreSystem != null && scoreSystem.IsTied();
        }

        public static TeamSide ResolveWinner(BattleScoreSystem scoreSystem)
        {
            if (scoreSystem == null)
            {
                return TeamSide.None;
            }

            if (scoreSystem.BlueKills > scoreSystem.RedKills)
            {
                return TeamSide.Blue;
            }

            if (scoreSystem.RedKills > scoreSystem.BlueKills)
            {
                return TeamSide.Red;
            }

            return TeamSide.None;
        }
    }
}
