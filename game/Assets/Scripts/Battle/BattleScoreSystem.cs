using Fight.Data;

namespace Fight.Battle
{
    public class BattleScoreSystem
    {
        public int BlueKills { get; private set; }

        public int RedKills { get; private set; }

        public void RegisterKill(TeamSide killerSide)
        {
            if (killerSide == TeamSide.Blue)
            {
                BlueKills++;
                return;
            }

            if (killerSide == TeamSide.Red)
            {
                RedKills++;
            }
        }

        public bool IsTied()
        {
            return BlueKills == RedKills;
        }
    }
}
