using Fight.Battle;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    public abstract class HeroBattleAnimationDriver : MonoBehaviour
    {
        public abstract bool IsReady { get; }

        public abstract void Initialize(RuntimeHero runtimeHero, GameObject visualInstance);

        public abstract void Sync(RuntimeHero runtimeHero);

        public abstract void OnBattleEvent(IBattleEvent battleEvent);
    }
}
