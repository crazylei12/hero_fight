using Fight.Data;
using UnityEngine;

namespace Fight.Battle
{
    [RequireComponent(typeof(BattleManager))]
    public class BattleDebugLogForwarder : MonoBehaviour
    {
        private BattleManager battleManager;
        private BattleEventBus boundEventBus;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
        }

        private void Update()
        {
            var eventBus = battleManager.Context?.EventBus;
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

        private void OnDisable()
        {
            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
            }
        }

        private void OnBattleEvent(IBattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case BattleStartedEvent _:
                    Debug.Log("[Battle] Started");
                    break;
                case UnitSpawnedEvent spawned:
                    Debug.Log($"[Battle] Spawned {FormatHeroLabel(spawned.Hero)}");
                    break;
                case ScoreChangedEvent scoreChanged:
                    Debug.Log($"[Battle] Score Blue {scoreChanged.BlueKills} - {scoreChanged.RedKills} Red");
                    break;
                case OvertimeStartedEvent _:
                    Debug.Log("[Battle] Overtime started");
                    break;
                case BattleEndedEvent ended:
                    Debug.Log($"[Battle] Ended. Winner={ended.Result.winner}, Reason={ended.Result.endReason}");
                    break;
            }
        }

        private static string FormatHeroLabel(Heroes.RuntimeHero hero)
        {
            if (hero == null)
            {
                return "none";
            }

            var displayName = hero.Definition != null && !string.IsNullOrWhiteSpace(hero.Definition.displayName)
                ? hero.Definition.displayName
                : "UnknownHero";
            return $"{displayName}[{hero.Side}|{hero.RuntimeId}]";
        }
    }
}
