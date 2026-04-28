using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [CreateAssetMenu(fileName = "AthleteRoster_", menuName = "Fight/Data/Athlete Roster")]
    public class AthleteRosterData : ScriptableObject
    {
        public const string DefaultResourcesPath = "Stage01Demo/Stage02AthleteRoster";

        public List<AthleteDefinition> blueTeamAthletes = new List<AthleteDefinition>();
        public List<AthleteDefinition> redTeamAthletes = new List<AthleteDefinition>();
    }
}
