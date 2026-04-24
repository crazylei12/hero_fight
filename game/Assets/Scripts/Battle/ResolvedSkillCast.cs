using System.Collections.Generic;
using Fight.Data;

namespace Fight.Battle
{
    public sealed class ResolvedSkillCast
    {
        public ResolvedSkillCast(SkillData skill, SkillVariantData variant = null)
        {
            Skill = skill;
            VariantKey = variant?.variantKey ?? string.Empty;
            TargetType = variant != null && variant.targetType != SkillTargetType.None
                ? variant.targetType
                : skill != null ? skill.targetType : SkillTargetType.None;
            FallbackTargetType = variant != null && variant.fallbackTargetType != SkillTargetType.None
                ? variant.fallbackTargetType
                : skill != null ? skill.fallbackTargetType : SkillTargetType.None;
            Effects = variant != null && variant.effects != null && variant.effects.Count > 0
                ? variant.effects
                : skill?.effects;
        }

        public SkillData Skill { get; }

        public string VariantKey { get; }

        public SkillTargetType TargetType { get; }

        public SkillTargetType FallbackTargetType { get; }

        public IReadOnlyList<SkillEffectData> Effects { get; }

        public bool HasVariant => !string.IsNullOrWhiteSpace(VariantKey);

        public static ResolvedSkillCast FromSkill(SkillData skill, SkillVariantData variant = null)
        {
            return skill == null ? null : new ResolvedSkillCast(skill, variant);
        }
    }
}
