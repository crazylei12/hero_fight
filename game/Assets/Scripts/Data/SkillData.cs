using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [CreateAssetMenu(fileName = "Skill_", menuName = "Fight/Data/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "skill_001_template";
        public string displayName = "Template Skill";
        [TextArea] public string description;

        [Header("Core")]
        public SkillSlotType slotType = SkillSlotType.ActiveSkill;
        public SkillType skillType = SkillType.SingleTargetDamage;
        public SkillTargetType targetType = SkillTargetType.NearestEnemy;
        public HeroClass preferredEnemyHeroClass = HeroClass.Assassin;
        public SkillTargetType fallbackTargetType = SkillTargetType.NearestEnemy;
        [Min(0f)] public float targetPrioritySearchRadius = 0f;
        [Min(1)] public int targetPriorityRequiredUnitCount = 1;

        [Header("Numbers")]
        [Min(0.1f)] public float castRange = 4f;
        [Min(0f)] public float areaRadius = 0f;
        [Min(0f)] public float cooldownSeconds = 6f;
        [Min(0)] public int minTargetsToCast = 1;

        [Header("Effects")]
        public List<SkillEffectData> effects = new List<SkillEffectData>();
        public bool allowsSelfCast;
        public ReactiveGuardData reactiveGuard = new ReactiveGuardData();

        [Header("Action Sequence")]
        public CombatActionSequenceData actionSequence = new CombatActionSequenceData();

        [Header("Presentation")]
        public GameObject targetIndicatorVfxPrefab;
        public GameObject castProjectileVfxPrefab;
        public GameObject persistentAreaVfxPrefab;
        [Min(0.1f)] public float persistentAreaVfxScaleMultiplier = 1f;
        public Vector3 persistentAreaVfxEulerAngles = Vector3.zero;
        public SkillAreaPresentationType skillAreaPresentationType = SkillAreaPresentationType.None;

        [Header("Ultimate Decision")]
        public UltimateDecisionData ultimateDecision = new UltimateDecisionData();
    }
}
