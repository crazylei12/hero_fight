using Fight.UI.Flow;
using UnityEngine;

namespace Fight.Battle
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BattleManager))]
    public class BattleEventLogRecorder : MonoBehaviour
    {
        private BattleManager battleManager;
        private BattleEventBus boundEventBus;
        private readonly BattleLogSession logSession = new BattleLogSession();

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            if (battleManager != null)
            {
                battleManager.ContextInitialized += OnContextInitialized;
            }
        }

        private void Update()
        {
            BindToBattleEvents(battleManager != null ? battleManager.Context?.EventBus : null);
        }

        private void OnDisable()
        {
            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
                boundEventBus = null;
            }
        }

        private void OnDestroy()
        {
            if (battleManager != null)
            {
                battleManager.ContextInitialized -= OnContextInitialized;
            }
        }

        private void OnContextInitialized(BattleContext context)
        {
            BindToBattleEvents(context?.EventBus);
        }

        private void BindToBattleEvents(BattleEventBus eventBus)
        {
            if (eventBus == null || ReferenceEquals(boundEventBus, eventBus))
            {
                return;
            }

            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
            }

            boundEventBus = eventBus;
            boundEventBus.Published += OnBattleEvent;
        }

        private void OnBattleEvent(IBattleEvent battleEvent)
        {
            logSession.HandleBattleEvent(battleEvent);
            if (battleEvent is BattleEndedEvent)
            {
                GameFlowState.StoreBattleLogExport(logSession.CurrentBattleLogId, logSession.BuildExportText());
            }
        }
    }
}
