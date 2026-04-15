using System.Collections.Generic;
using Fight.Data;
using Fight.Heroes;

namespace Fight.Battle
{
    public class BattleContext
    {
        public BattleContext(BattleInputConfig input, BattleClock clock, BattleScoreSystem scoreSystem, BattleRandomService randomService, BattleEventBus eventBus, List<RuntimeHero> heroes)
        {
            Input = input;
            Clock = clock;
            ScoreSystem = scoreSystem;
            RandomService = randomService;
            EventBus = eventBus;
            Heroes = heroes;
            Projectiles = new List<RuntimeBasicAttackProjectile>();
            SkillAreas = new List<RuntimeSkillArea>();
        }

        public BattleInputConfig Input { get; }

        public BattleClock Clock { get; }

        public BattleScoreSystem ScoreSystem { get; }

        public BattleRandomService RandomService { get; }

        public BattleEventBus EventBus { get; }

        public List<RuntimeHero> Heroes { get; }

        public List<RuntimeBasicAttackProjectile> Projectiles { get; }

        public List<RuntimeSkillArea> SkillAreas { get; }
    }
}
