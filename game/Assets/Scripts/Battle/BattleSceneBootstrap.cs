using Fight.Data;
using Fight.UI;
using Fight.UI.Flow;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Fight.Battle
{
    [DisallowMultipleComponent]
    public class BattleSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleInputConfig defaultInputConfig;
        [SerializeField] private bool startBattleOnPlay = true;
        [SerializeField] private bool addBattleHud = true;
        [SerializeField] private bool addBattleView = true;
        [SerializeField] private bool addBattleEventLogRecorder = true;
        [SerializeField] private string fallbackResourcesPath = GameFlowState.DefaultInputResourcesPath;
        [SerializeField] private string resultSceneName = "Result";
        [SerializeField] private float resultSceneDelaySeconds = 1.35f;

        private BattleManager battleManager;
        private BattleEventBus boundEventBus;
        private float pendingResultLoadDelaySeconds = -1f;

        private void Awake()
        {
            battleManager = GetComponent<BattleManager>();
            if (battleManager == null)
            {
                battleManager = gameObject.AddComponent<BattleManager>();
            }

            if (addBattleHud && GetComponent<BattleCanvasHud>() == null)
            {
                gameObject.AddComponent<BattleCanvasHud>();
            }

            if (addBattleView && GetComponent<BattleView>() == null)
            {
                gameObject.AddComponent<BattleView>();
            }

            if (addBattleEventLogRecorder && GetComponent<BattleEventLogRecorder>() == null)
            {
                gameObject.AddComponent<BattleEventLogRecorder>();
            }

            var startupInput = GameFlowState.ConsumePendingBattleInput();
            if (startupInput == null)
            {
                startupInput = defaultInputConfig;
            }

            if (startupInput == null && !string.IsNullOrWhiteSpace(fallbackResourcesPath))
            {
                startupInput = Resources.Load<BattleInputConfig>(fallbackResourcesPath);
            }

            if (startupInput != null)
            {
                battleManager.ConfigureStartup(startupInput, startBattleOnPlay);
                GameFlowState.RememberLastUsedInput(startupInput);
            }
            else
            {
                Debug.LogWarning($"BattleSceneBootstrap could not find a BattleInputConfig. Checked prepared flow input, serialized field, and Resources/{fallbackResourcesPath}.");
            }
        }

        private void Update()
        {
            BindToBattleEvents();

            if (pendingResultLoadDelaySeconds < 0f)
            {
                return;
            }

            pendingResultLoadDelaySeconds -= Time.unscaledDeltaTime;
            if (pendingResultLoadDelaySeconds <= 0f)
            {
                pendingResultLoadDelaySeconds = -1f;
                SceneManager.LoadScene(resultSceneName);
            }
        }

        private void OnDisable()
        {
            if (boundEventBus != null)
            {
                boundEventBus.Published -= OnBattleEvent;
                boundEventBus = null;
            }
        }

        private void BindToBattleEvents()
        {
            var eventBus = battleManager != null ? battleManager.Context?.EventBus : null;
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
            if (battleEvent is BattleStartedEvent startedEvent)
            {
                GameFlowState.RememberLastUsedInput(startedEvent.Input);
                return;
            }

            if (battleEvent is BattleEndedEvent endedEvent)
            {
                GameFlowState.StoreBattleResult(endedEvent.Result);
                pendingResultLoadDelaySeconds = Mathf.Max(0.05f, resultSceneDelaySeconds);
            }
        }
    }
}
