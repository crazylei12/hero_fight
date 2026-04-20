using Fight.Data;
using Fight.UI;
using UnityEngine;

namespace Fight.Battle
{
    [DisallowMultipleComponent]
    public class BattleDebugSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private BattleInputConfig defaultInputConfig;
        [SerializeField] private bool startBattleOnPlay = true;
        [SerializeField] private bool addBattleHud = true;
        [SerializeField] private bool addBattleSideHeroSidebarHud = true;
        [SerializeField] private bool addBattleView = true;
        [SerializeField] private bool addDebugHud = true;
        [SerializeField] private bool addDebugLogForwarder = true;
        [SerializeField] private string fallbackResourcesPath = "Stage01Demo/Stage01DemoBattleInput";

        private void Awake()
        {
            var battleManager = GetComponent<BattleManager>();
            if (battleManager == null)
            {
                battleManager = gameObject.AddComponent<BattleManager>();
            }

            if (addBattleHud && GetComponent<BattleHud>() == null)
            {
                gameObject.AddComponent<BattleHud>();
            }

            if (addBattleSideHeroSidebarHud && GetComponent<BattleSideHeroSidebarHud>() == null)
            {
                gameObject.AddComponent<BattleSideHeroSidebarHud>();
            }

            if (addBattleView && GetComponent<BattleView>() == null)
            {
                gameObject.AddComponent<BattleView>();
            }

            if (addDebugHud && GetComponent<BattleDebugHud>() == null)
            {
                gameObject.AddComponent<BattleDebugHud>();
            }

            if (addDebugLogForwarder && GetComponent<BattleDebugLogForwarder>() == null)
            {
                gameObject.AddComponent<BattleDebugLogForwarder>();
            }

            if (defaultInputConfig == null && !string.IsNullOrWhiteSpace(fallbackResourcesPath))
            {
                defaultInputConfig = Resources.Load<BattleInputConfig>(fallbackResourcesPath);
            }

            if (defaultInputConfig != null)
            {
                battleManager.ConfigureDebugStartup(defaultInputConfig, startBattleOnPlay);
            }
            else
            {
                Debug.LogWarning($"BattleDebugSceneBootstrap could not find a BattleInputConfig. Checked serialized field and Resources/{fallbackResourcesPath}.");
            }
        }
    }
}
