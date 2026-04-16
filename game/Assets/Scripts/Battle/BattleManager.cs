using System;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public class BattleManager : MonoBehaviour
    {
        [SerializeField] private BattleInputConfig defaultInputConfig;
        [SerializeField] private bool autoStartOnPlay = true;

        private BattleContext context;
        private BattleResultData activeResult;

        public event Action<BattleContext> ContextInitialized;

        public BattleContext Context => context;

        public BattleResultData ActiveResult => activeResult;

        public BattleInputConfig DefaultInputConfig => defaultInputConfig;

        public int ActiveHeroCount => context != null ? context.Heroes.Count : 0;

        public void ConfigureStartup(BattleInputConfig inputConfig, bool shouldAutoStart)
        {
            defaultInputConfig = inputConfig;
            autoStartOnPlay = shouldAutoStart;
        }

        public void ConfigureDebugStartup(BattleInputConfig inputConfig, bool shouldAutoStart)
        {
            ConfigureStartup(inputConfig, shouldAutoStart);
        }

        private void Start()
        {
            if (autoStartOnPlay && defaultInputConfig != null)
            {
                StartBattle(defaultInputConfig);
            }
        }

        private void Update()
        {
            if (context == null || !context.Clock.IsRunning)
            {
                return;
            }

            BattleSimulationSystem.Tick(context, Time.deltaTime, this);
            context.Clock.Tick(Time.deltaTime);

            if (!context.Clock.IsOvertime && context.Clock.HasReachedRegulationTime())
            {
                if (BattleEndResolver.ShouldEnterOvertime(context.ScoreSystem))
                {
                    context.Clock.EnterOvertime();
                    context.EventBus.Publish(new OvertimeStartedEvent());
                    return;
                }

                FinishBattle(BattleEndReason.TimeExpired);
            }
        }

        public void StartBattle(BattleInputConfig inputConfig)
        {
            if (inputConfig == null)
            {
                Debug.LogWarning("BattleManager received a null BattleInputConfig.");
                return;
            }

            if (!inputConfig.HasValidTeamCounts())
            {
                Debug.LogWarning($"BattleInputConfig requires {BattleInputConfig.DefaultTeamSize} heroes on each side before battle start.");
                return;
            }

            var runtimeHeroes = BattleBootstrapper.CreateRuntimeHeroes(inputConfig);
            context = new BattleContext(
                inputConfig,
                new BattleClock(inputConfig.regulationDurationSeconds),
                new BattleScoreSystem(),
                new BattleRandomService(),
                new BattleEventBus(),
                runtimeHeroes);

            ContextInitialized?.Invoke(context);
            activeResult = null;
            context.Clock.Start();
            context.EventBus.Publish(new BattleStartedEvent(inputConfig));

            for (var i = 0; i < runtimeHeroes.Count; i++)
            {
                context.EventBus.Publish(new UnitSpawnedEvent(runtimeHeroes[i]));
            }
        }

        public void RegisterKill(TeamSide killerSide)
        {
            if (context == null || !context.Clock.IsRunning)
            {
                return;
            }

            context.ScoreSystem.RegisterKill(killerSide);
            context.EventBus.Publish(new ScoreChangedEvent(context.ScoreSystem.BlueKills, context.ScoreSystem.RedKills));

            if (context.Clock.IsOvertime)
            {
                FinishBattle(BattleEndReason.OvertimeKill);
            }
        }

        private void FinishBattle(BattleEndReason endReason)
        {
            if (context == null)
            {
                return;
            }

            context.Clock.Stop();

            activeResult = new BattleResultData
            {
                winner = BattleEndResolver.ResolveWinner(context.ScoreSystem),
                endReason = endReason,
                enteredOvertime = context.Clock.IsOvertime,
                elapsedTimeSeconds = context.Clock.ElapsedTimeSeconds,
                blueKills = context.ScoreSystem.BlueKills,
                redKills = context.ScoreSystem.RedKills,
            };

            for (var i = 0; i < context.Heroes.Count; i++)
            {
                var hero = context.Heroes[i];
                activeResult.heroStats.Add(new HeroBattleStatLine
                {
                    heroId = hero.Definition != null ? hero.Definition.heroId : string.Empty,
                    side = hero.Side,
                    kills = hero.Kills,
                    deaths = hero.Deaths,
                    damageDealt = hero.DamageDealt,
                    healingDone = hero.HealingDone,
                });
            }

            context.EventBus.Publish(new BattleEndedEvent(activeResult));
        }
    }
}
