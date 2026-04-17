using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [CreateAssetMenu(fileName = "HeroCatalog_", menuName = "Fight/Data/Hero Catalog")]
    public class HeroCatalogData : ScriptableObject
    {
        public List<HeroDefinition> heroes = new List<HeroDefinition>();
    }
}
