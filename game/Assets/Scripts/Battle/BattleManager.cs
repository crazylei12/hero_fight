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
        private BattleSessionRunner sessionRunner;

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
            if (sessionRunner == null || !sessionRunner.IsRunning)
            {
                return;
            }

            sessionRunner.Tick(Time.deltaTime);
            activeResult = sessionRunner.ActiveResult;
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

            sessionRunner = new BattleSessionRunner(inputConfig);
            context = sessionRunner.Context;
            ContextInitialized?.Invoke(context);
            activeResult = null;
            sessionRunner.Start();
        }

        public void RegisterKill(TeamSide killerSide)
        {
            sessionRunner?.RegisterKill(killerSide);
            activeResult = sessionRunner?.ActiveResult;
        }
    }
}
