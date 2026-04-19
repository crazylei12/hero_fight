namespace Fight.Data
{
    public enum SkillTargetType
    {
        None = 0,
        NearestEnemy = 1,
        LowestHealthEnemy = 2,
        DensestEnemyArea = 3,
        LowestHealthAlly = 4,
        Self = 5,
        AllEnemies = 6,
        AllAllies = 7,
        BackmostEnemy = 8,
        HighestDamageEnemyInRange = 9,
        PriorityEnemyHeroClass = 10,
        LowestHealthRangedAlly = 11,
        ThreatenedRangedAlly = 12,
        ThreatenedRangedAllyOrEnemyDensestAnchor = 13,
    }
}
