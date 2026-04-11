using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class HeroVisualConfig
    {
        public Sprite portrait;
        public GameObject battlePrefab;
        public RuntimeAnimatorController animatorController;
        public GameObject projectilePrefab;
        public GameObject castVfxPrefab;
        public GameObject hitVfxPrefab;
        public AudioClip castSfx;
    }
}
