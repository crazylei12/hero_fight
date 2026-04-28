using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [CreateAssetMenu(fileName = "Hero_", menuName = "Fight/Data/Hero Definition")]
    public class HeroDefinition : ScriptableObject
    {
        [Header("Identity")]
        public string heroId = "warrior_001_template";
        public string displayName = "Template Hero";
        [TextArea] public string description;
        public HeroClass heroClass = HeroClass.Warrior;
        public List<HeroTag> tags = new List<HeroTag>();

        [Header("Battle")]
        public HeroStatsData baseStats = new HeroStatsData();
        public BasicAttackData basicAttack = new BasicAttackData();
        public SkillData activeSkill;
        public SkillData ultimateSkill;
        public string aiTemplateId = "warrior_default";
        public bool usesSpecialLogic;
        [TextArea] public string specialLogicNotes;

        [Header("Presentation")]
        public HeroVisualConfig visualConfig = new HeroVisualConfig();

        [Header("Notes")]
        [TextArea] public string debugNotes;
    }
}
