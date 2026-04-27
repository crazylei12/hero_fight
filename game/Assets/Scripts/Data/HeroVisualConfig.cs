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
    public class HeroFormVisualConfig
    {
        public string formKey = string.Empty;
        public GameObject battlePrefab;
        public RuntimeAnimatorController animatorController;
        public bool battlePrefabFacesLeftByDefault;
        public GameObject projectilePrefab;
        public bool projectileAlignToMovement;
        public Vector3 projectileEulerAngles;
        public BasicAttackVariantVisualConfig[] basicAttackVariantVisuals = Array.Empty<BasicAttackVariantVisualConfig>();
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
        public HeroFormVisualConfig[] formVisuals = Array.Empty<HeroFormVisualConfig>();
        public AudioClip castSfx;

        public BasicAttackVariantVisualConfig FindBasicAttackVariantVisual(string variantKey)
        {
            return FindBasicAttackVariantVisual(basicAttackVariantVisuals, variantKey);
        }

        public BasicAttackVariantVisualConfig FindBasicAttackVariantVisual(string formKey, string variantKey)
        {
            var formVisual = FindFormVisual(formKey);
            var formVariantVisual = FindBasicAttackVariantVisual(formVisual?.basicAttackVariantVisuals, variantKey);
            return formVariantVisual ?? FindBasicAttackVariantVisual(variantKey);
        }

        public HeroFormVisualConfig FindFormVisual(string formKey)
        {
            if (string.IsNullOrWhiteSpace(formKey) || formVisuals == null || formVisuals.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < formVisuals.Length; i++)
            {
                var candidate = formVisuals[i];
                if (candidate != null
                    && string.Equals(candidate.formKey, formKey, StringComparison.Ordinal))
                {
                    return candidate;
                }
            }

            return null;
        }

        public GameObject ResolveBattlePrefab(string formKey)
        {
            var formVisual = FindFormVisual(formKey);
            return formVisual != null && formVisual.battlePrefab != null
                ? formVisual.battlePrefab
                : battlePrefab;
        }

        public RuntimeAnimatorController ResolveAnimatorController(string formKey)
        {
            var formVisual = FindFormVisual(formKey);
            if (formVisual != null && formVisual.battlePrefab != null)
            {
                return formVisual.animatorController;
            }

            return formVisual != null && formVisual.animatorController != null
                ? formVisual.animatorController
                : animatorController;
        }

        public bool ResolveBattlePrefabFacesLeftByDefault(string formKey)
        {
            var formVisual = FindFormVisual(formKey);
            return formVisual != null && formVisual.battlePrefab != null
                ? formVisual.battlePrefabFacesLeftByDefault
                : battlePrefabFacesLeftByDefault;
        }

        public GameObject ResolveProjectilePrefab(string formKey, string variantKey)
        {
            var variantVisual = FindBasicAttackVariantVisual(formKey, variantKey);
            if (variantVisual?.projectilePrefab != null)
            {
                return variantVisual.projectilePrefab;
            }

            var formVisual = FindFormVisual(formKey);
            return formVisual != null && formVisual.projectilePrefab != null
                ? formVisual.projectilePrefab
                : projectilePrefab;
        }

        public bool ResolveProjectileAlignToMovement(string formKey)
        {
            var formVisual = FindFormVisual(formKey);
            return formVisual != null ? formVisual.projectileAlignToMovement : projectileAlignToMovement;
        }

        public Vector3 ResolveProjectileEulerAngles(string formKey)
        {
            var formVisual = FindFormVisual(formKey);
            return formVisual != null ? formVisual.projectileEulerAngles : projectileEulerAngles;
        }

        private static BasicAttackVariantVisualConfig FindBasicAttackVariantVisual(
            BasicAttackVariantVisualConfig[] visuals,
            string variantKey)
        {
            if (string.IsNullOrWhiteSpace(variantKey)
                || visuals == null
                || visuals.Length == 0)
            {
                return null;
            }

            for (var i = 0; i < visuals.Length; i++)
            {
                var candidate = visuals[i];
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
