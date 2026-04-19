namespace Fight.Data
{
    public enum UltimateConditionType
    {
        None = 0,
        EnemyCountInRange = 1,
        AllyCountInRange = 2,
        EnemyLowHealthInRange = 3,
        AllyLowHealthInRange = 4,
        SelfLowHealth = 5,
        TargetIsHighValue = 6,
        InCombatDuration = 7,
        EnemyHeroClassInRange = 8,
    }
}
