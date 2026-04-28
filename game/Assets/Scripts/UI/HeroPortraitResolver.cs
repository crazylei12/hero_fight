using System;
using System.Collections.Generic;
using Fight.Data;
using UnityEngine;

namespace Fight.UI
{
    public static class HeroPortraitResolver
    {
        private const string IdleFirstFrameResourceFormat = "HeroPreview/{0}/Idle/idle_00";
        private static readonly Dictionary<string, Sprite> IdleFirstFrameCache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static Sprite ResolvePortrait(HeroDefinition hero)
        {
            if (hero == null)
            {
                return null;
            }

            var idleFirstFrame = ResolveIdleFirstFrame(hero.heroId);
            return idleFirstFrame != null ? idleFirstFrame : hero.visualConfig?.portrait;
        }

        private static Sprite ResolveIdleFirstFrame(string heroId)
        {
            if (string.IsNullOrWhiteSpace(heroId))
            {
                return null;
            }

            if (IdleFirstFrameCache.TryGetValue(heroId, out var cachedSprite))
            {
                return cachedSprite;
            }

            var sprite = Resources.Load<Sprite>(string.Format(IdleFirstFrameResourceFormat, heroId));
            if (sprite != null)
            {
                IdleFirstFrameCache[heroId] = sprite;
            }

            return sprite;
        }
    }
}
