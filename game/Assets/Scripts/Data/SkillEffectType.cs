namespace Fight.Data
{
    public enum SkillEffectType
    {
        DirectDamage = 0,
        DirectHeal = 1,
        ApplyStatusEffects = 2,
        RepositionNearPrimaryTarget = 3,
        CreatePersistentArea = 4,
        ApplyForcedMovement = 5,
        CreateDeployableProxy = 6,
        CreateRadialSweep = 7,
        CreateReturningPathStrike = 8,
        CreateFocusFireCommand = 9,
        SwapPositionsWithPrimaryTarget = 10,
        ApplyCombatFormOverride = 11,
        CleanseStatusEffects = 12,
        ConsumeRestrictedStatusStacksDamage = 13,
        CreateCloneUnit = 14,
        CreateChanneledPathDamage = 15,
        CreateMultiPathBurst = 16,
    }
}
