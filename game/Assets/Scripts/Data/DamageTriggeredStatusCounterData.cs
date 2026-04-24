using System;
using System.Collections.Generic;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class DamageTriggeredStatusCounterData
    {
        public bool enabled;
        public bool countBasicAttackDamage = true;
        public bool countSkillDamage = true;
        public bool countSkillAreaPulseDamage = true;
        public bool countStatusEffectDamage = true;
        public bool countCounterTriggerDamage;
        [Min(1)] public int triggerThreshold = 3;
        public bool clearCountedStatusesOnTrigger = true;
        public StatusEffectData countedStatus = new StatusEffectData();
        public List<StatusEffectData> triggerStatusEffects = new List<StatusEffectData>();
    }
}
