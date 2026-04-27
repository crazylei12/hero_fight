using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    public enum SkillVariantSelectionMode
    {
        None = 0,
        RandomSingle = 1,
    }

    [Serializable]
    public class SkillVariantData
    {
        public string variantKey = string.Empty;
        public SkillTargetType targetType = SkillTargetType.None;
        public SkillTargetType fallbackTargetType = SkillTargetType.None;
        public List<SkillEffectData> effects = new List<SkillEffectData>();
    }

    [CreateAssetMenu(fileName = "Skill_", menuName = "Fight/Data/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("Identity")]
        public string skillId = "skill_001_template";
        public string displayName = "Template Skill";
        [TextArea] public string description;

        [Header("Core")]
        public SkillSlotType slotType = SkillSlotType.ActiveSkill;
        public SkillActivationMode activationMode = SkillActivationMode.Active;
        public SkillType skillType = SkillType.SingleTargetDamage;
        public SkillTargetType targetType = SkillTargetType.NearestEnemy;
        public HeroClass preferredEnemyHeroClass = HeroClass.Assassin;
        public SkillTargetType fallbackTargetType = SkillTargetType.NearestEnemy;
        [Min(0f)] public float targetPrioritySearchRadius = 0f;
        [Min(0f)] public float minimumTargetDistance = 0f;
        [Min(1)] public int targetPriorityRequiredUnitCount = 1;

        [Header("Variants")]
        public SkillVariantSelectionMode variantSelectionMode = SkillVariantSelectionMode.None;
        public List<SkillVariantData> variants = new List<SkillVariantData>();

        [Header("Passive")]
        public PassiveSkillData passiveSkill = new PassiveSkillData();
        public DamageTriggeredStatusCounterData damageTriggeredStatusCounter = new DamageTriggeredStatusCounterData();

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

        [Header("Temporary Overrides")]
        public SkillTemporaryOverrideData temporaryOverride = new SkillTemporaryOverrideData();

        [Header("Presentation")]
        public GameObject targetIndicatorVfxPrefab;
        public GameObject castImpactVfxPrefab;
        public Vector3 castImpactVfxLocalOffset = Vector3.zero;
        public Vector3 castImpactVfxEulerAngles = Vector3.zero;
        public Vector3 castImpactVfxScaleMultiplier = Vector3.one;
        public bool castImpactVfxAlignToTargetDirection;
        public bool castImpactVfxScaleWithSkillArea;
        [Min(0.1f)] public float castImpactVfxAreaDiameterScaleMultiplier = 1f;
        public GameObject castProjectileVfxPrefab;
        public GameObject dashTravelVfxPrefab;
        public Vector3 dashTravelVfxLocalOffset = Vector3.zero;
        [Min(0f)] public float dashTravelVfxForwardOffset = 0f;
        public Vector3 dashTravelVfxEulerAngles = Vector3.zero;
        public Vector3 dashTravelVfxScaleMultiplier = Vector3.one;
        [Min(0f)] public float dashTravelVfxPathWidthScaleMultiplier = 1f;
        public GameObject persistentAreaVfxPrefab;
        [Min(0.1f)] public float persistentAreaVfxScaleMultiplier = 1f;
        public Vector3 persistentAreaVfxEulerAngles = Vector3.zero;
        public SkillAreaPresentationType skillAreaPresentationType = SkillAreaPresentationType.None;

        [Header("Ultimate Decision")]
        public UltimateDecisionData ultimateDecision = new UltimateDecisionData();
    }
}
