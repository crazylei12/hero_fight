using UnityEngine;

namespace Fight.Data
{
    [CreateAssetMenu(fileName = "BattleInput_", menuName = "Fight/Data/Battle Input")]
    public class BattleInputConfig : ScriptableObject
    {
        public const int DefaultTeamSize = 5;

        [Header("Teams")]
        public BattleTeamLoadout blueTeam = new BattleTeamLoadout { side = TeamSide.Blue };
        public BattleTeamLoadout redTeam = new BattleTeamLoadout { side = TeamSide.Red };

        [Header("Match Rules")]
        [Min(1f)] public float regulationDurationSeconds = 60f;
        [Min(0f)] public float respawnDelaySeconds = 5f;
        public bool enableBattleEventLogs = true;
        public bool enableSkills = true;

        [Header("Arena")]
        public string arenaId = Stage01ArenaSpec.ArenaId;

        public bool HasValidTeamCounts()
        {
            return blueTeam.heroes.Count == DefaultTeamSize && redTeam.heroes.Count == DefaultTeamSize;
        }
    }
}
