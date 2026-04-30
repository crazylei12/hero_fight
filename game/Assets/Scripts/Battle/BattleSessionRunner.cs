using System;
using Fight.Data;
using Fight.Heroes;

namespace Fight.Battle
{
    public interface IBattleSimulationCallbacks
    {
        void RegisterKill(TeamSide killerSide);
    }

    public sealed class BattleSessionRunner : IBattleSimulationCallbacks
    {
        private readonly BattleInputConfig inputConfig;
        private readonly BattleRandomService randomService;
        private bool hasStarted;
        private BattleResultData activeResult;

        public BattleSessionRunner(BattleInputConfig inputConfig, int? seed = null)
        {
            if (inputConfig == null)
            {
                throw new ArgumentNullException(nameof(inputConfig));
            }

            if (!inputConfig.HasValidTeamCounts())
            {
                throw new ArgumentException(
                    $"BattleInputConfig requires {BattleInputConfig.DefaultTeamSize} heroes on each side before battle start.",
                    nameof(inputConfig));
            }

            this.inputConfig = inputConfig;
            randomService = new BattleRandomService(seed);

            var runtimeHeroes = BattleBootstrapper.CreateRuntimeHeroes(inputConfig, randomService);
            Context = new BattleContext(
                inputConfig,
                new BattleClock(inputConfig.regulationDurationSeconds),
                new BattleScoreSystem(),
                randomService,
                new BattleEventBus(),
                runtimeHeroes);
            Context.EventBus.Published += battleEvent => BattleKnockUpFollowUpSystem.Capture(Context, battleEvent);
        }

        public BattleContext Context { get; }

        public BattleResultData ActiveResult => activeResult;

        public bool IsRunning => Context != null && Context.Clock.IsRunning;

        public bool HasStarted => hasStarted;

        public bool HasFinished => activeResult != null;

        public void Start()
        {
            if (hasStarted || Context == null)
            {
                return;
            }

            activeResult = null;
            Context.Clock.Start();
            hasStarted = true;
            Context.EventBus.Publish(new BattleStartedEvent(inputConfig));

            for (var i = 0; i < Context.Heroes.Count; i++)
            {
                if (Context.Heroes[i].AthleteModifier.HasAthlete)
                {
                    Context.EventBus.Publish(new AthleteModifierResolvedEvent(Context.Heroes[i], Context.Heroes[i].AthleteModifier));
                }

                Context.EventBus.Publish(new UnitSpawnedEvent(Context.Heroes[i]));
            }
        }

        public bool Tick(float deltaTime)
        {
            if (!hasStarted || Context == null || HasFinished || !Context.Clock.IsRunning)
            {
                return HasFinished;
            }

            BattleSimulationSystem.Tick(Context, deltaTime, this);
            Context.Clock.Tick(deltaTime);

            if (!Context.Clock.IsOvertime && Context.Clock.HasReachedRegulationTime())
            {
                if (BattleEndResolver.ShouldEnterOvertime(Context.ScoreSystem))
                {
                    Context.Clock.EnterOvertime();
                    Context.EventBus.Publish(new OvertimeStartedEvent());
                }
                else
                {
                    FinishBattle(BattleEndReason.TimeExpired);
                }
            }

            return HasFinished;
        }

        public BattleResultData RunToCompletion(float fixedDeltaTime, int maxTicks = 100000)
        {
            if (fixedDeltaTime <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(fixedDeltaTime), "Fixed delta time must be positive.");
            }

            if (maxTicks <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxTicks), "Max tick count must be positive.");
            }

            Start();

            for (var tickIndex = 0; tickIndex < maxTicks && !HasFinished; tickIndex++)
            {
                Tick(fixedDeltaTime);
            }

            if (!HasFinished)
            {
                throw new InvalidOperationException(
                    $"Battle session did not finish within {maxTicks} ticks at fixed delta {fixedDeltaTime:0.###}.");
            }

            return activeResult;
        }

        public void RegisterKill(TeamSide killerSide)
        {
            if (Context == null || !Context.Clock.IsRunning || HasFinished)
            {
                return;
            }

            Context.ScoreSystem.RegisterKill(killerSide);
            Context.EventBus.Publish(new ScoreChangedEvent(Context.ScoreSystem.BlueKills, Context.ScoreSystem.RedKills));

            if (Context.Clock.IsOvertime)
            {
                FinishBattle(BattleEndReason.OvertimeKill);
            }
        }

        private void FinishBattle(BattleEndReason endReason)
        {
            if (Context == null || activeResult != null)
            {
                return;
            }

            Context.Clock.Stop();
            activeResult = BuildResult(endReason);
            Context.EventBus.Publish(new BattleEndedEvent(activeResult));
        }

        private BattleResultData BuildResult(BattleEndReason endReason)
        {
            var result = new BattleResultData
            {
                winner = BattleEndResolver.ResolveWinner(Context.ScoreSystem),
                endReason = endReason,
                enteredOvertime = Context.Clock.IsOvertime,
                elapsedTimeSeconds = Context.Clock.ElapsedTimeSeconds,
                blueKills = Context.ScoreSystem.BlueKills,
                redKills = Context.ScoreSystem.RedKills,
            };

            for (var i = 0; i < Context.Heroes.Count; i++)
            {
                var hero = Context.Heroes[i];
                if (hero == null || hero.IsClone)
                {
                    continue;
                }

                result.heroStats.Add(new HeroBattleStatLine
                {
                    heroId = hero.Definition != null ? hero.Definition.heroId : string.Empty,
                    displayName = hero.Definition != null ? hero.Definition.displayName : string.Empty,
                    heroClass = hero.Definition != null ? hero.Definition.heroClass : HeroClass.Warrior,
                    side = hero.Side,
                    slotIndex = hero.SlotIndex,
                    won = hero.Side == result.winner,
                    kills = hero.Kills,
                    deaths = hero.Deaths,
                    assists = hero.Assists,
                    damageDealt = hero.DamageDealt,
                    damageTaken = hero.DamageTaken,
                    healingDone = hero.HealingDone,
                    shieldingDone = hero.ShieldingDone,
                    activeSkillCastCount = hero.ActiveSkillCastCount,
                    ultimateCastCount = hero.UltimateCastCount,
                });
            }

            return result;
        }
    }
}
