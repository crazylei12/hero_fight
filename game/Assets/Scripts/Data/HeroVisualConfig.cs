using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class BasicAttackVariantVisualConfig
    {
        public string variantKey = string.Empty;
        public GameObject projectilePrefab;
        public GameObject hitVfxPrefab;
    }

    [Serializable]
    public class HeroVisualConfig
    {
        public Sprite portrait;
        public GameObject battlePrefab;
        public RuntimeAnimatorController animatorController;
        public bool battlePrefabFacesLeftByDefault;
        public GameObject projectilePrefab;
        public bool projectileAlignToMovement;
        public Vector3 projectileEulerAngles;
        public GameObject castVfxPrefab;
        public GameObject hitVfxPrefab;
        public BasicAttackVariantVisualConfig[] basicAttackVariantVisuals = Array.Empty<BasicAttackVariantVisualConfig>();
        public AudioClip castSfx;

        public BasicAttackVariantVisualConfig FindBasicAttackVariantVisual(string variantKey)
        {
            if (string.IsNullOrWhiteSpace(variantKey)
                || basicAttackVariantVisuals == null
                || basicAttackVariantVisuals.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < basicAttackVariantVisuals.Length; i++)
            {
                var candidate = basicAttackVariantVisuals[i];
                if (candidate != null
                    && string.Equals(candidate.variantKey, variantKey, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }
    }
}
