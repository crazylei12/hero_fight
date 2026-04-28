using System;
using UnityEngine;

namespace Fight.Data
{
    [Serializable]
    public class HeroMasteryEntry
    {
        public string heroId;
        [Range(0f, 50f)] public float mastery;
    }
}
