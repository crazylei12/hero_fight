using System;
using System.Collections.Generic;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    public sealed class SpriteSheetBattleAnimationDriver : HeroBattleAnimationDriver
    {
        private const float FacingMovementThresholdSqr = 0.0004f;
        private const float MoveStateEnterSpeed = 0.2f;
        private const float MoveStateExitSpeed = 0.08f;
        private const float DirectionThreshold = 0.05f;
        private const string IdleClipKey = "Idle";
        private const string RunClipKey = "Run";
        private const string Attack1ClipKey = "Attack1";
        private const string Attack2ClipKey = "Attack2";
        private const string RageIdleClipKey = "RageIdle";
        private const string RageAttack1ClipKey = "RageAttack1";
        private const string SkillClipKey = "Skill";
        private const string UltimateClipKey = "Ult";
        private const string HitClipKey = "Hit";
        private const string DeathClipKey = "Death";

        private readonly Dictionary<string, RuntimeClip> clips = new Dictionary<string, RuntimeClip>(StringComparer.Ordinal);
        private readonly List<Sprite> generatedSprites = new List<Sprite>();
        private RuntimeHero hero;
        private Transform visualTransform;
        private SpriteRenderer spriteRenderer;
        private RuntimeClip currentClip;
        private Vector3 baseVisualScale = Vector3.one;
        private Vector3 lastPosition;
        private Vector2 currentFacing;
        private int frameIndex;
        private float frameTimer;
        private float actionLockedUntilTime = -1f;
        private bool deathStateApplied;
        private bool isInMoveState;

        public override bool IsReady => hero != null && visualTransform != null && spriteRenderer != null && clips.Count > 0;

        public override void Initialize(RuntimeHero runtimeHero, GameObject visualInstance)
        {
            hero = runtimeHero;
            visualTransform = visualInstance != null ? visualInstance.transform : null;
            var config = visualInstance != null ? visualInstance.GetComponentInChildren<SpriteSheetBattleVisualConfig>(true) : null;
            if (hero == null || visualTransform == null || config == null)
            {
                enabled = false;
                return;
            }

            spriteRenderer = config.SpriteRenderer;
            if (spriteRenderer == null)
            {
                enabled = false;
                return;
            }

            LoadClips(config);
            if (clips.Count == 0)
            {
                enabled = false;
                return;
            }

            baseVisualScale = visualTransform.localScale;
            currentFacing = Vector2.zero;
            SetFacing(GetDefaultFacing());
            PlayClip(IdleClipKey, restart: true);
            lastPosition = hero.CurrentPosition;
        }

        public override void Sync(RuntimeHero runtimeHero)
        {
            if (!IsReady || runtimeHero == null)
            {
                return;
            }

            hero = runtimeHero;

            if (hero.IsDead)
            {
                ApplyDeathState();
                lastPosition = hero.CurrentPosition;
                TickAnimation();
                return;
            }

            if (deathStateApplied)
            {
                ResetAfterRevive();
            }

            UpdateFacing();

            var movement = hero.CurrentPosition - lastPosition;
            var actualMoveSpeed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            isInMoveState = isInMoveState
                ? actualMoveSpeed >= MoveStateExitSpeed
                : actualMoveSpeed >= MoveStateEnterSpeed;

            if (!IsActionLocked())
            {
                PlayClip(isInMoveState ? RunClipKey : ResolveIdleClipKey(), restart: false);
            }

            lastPosition = hero.CurrentPosition;
            TickAnimation();
        }

        public override void OnBattleEvent(IBattleEvent battleEvent)
        {
            if (!IsReady || battleEvent == null || hero == null)
            {
                return;
            }

            switch (battleEvent)
            {
                case AttackPerformedEvent attackEvent when Matches(attackEvent.Attacker):
                    PlayActionClip(ResolveAttackClipKey(attackEvent.VariantKey), 0.18f);
                    break;
                case SkillCastEvent skillCastEvent when Matches(skillCastEvent.Caster):
                    PlayActionClip(ResolveSkillClipKey(skillCastEvent.Skill, skillCastEvent.VariantKey), 0.22f);
                    break;
                case DamageAppliedEvent damageEvent when Matches(damageEvent.Target) && damageEvent.Target.CurrentHealth > 0f && !IsActionLocked():
                    PlayActionClip(HitClipKey, 0.08f);
                    break;
                case UnitDiedEvent diedEvent when Matches(diedEvent.Victim):
                    ApplyDeathState();
                    break;
                case UnitRevivedEvent revivedEvent when Matches(revivedEvent.Hero):
                    ResetAfterRevive();
                    break;
                case BattleEndedEvent _ when !hero.IsDead:
                    actionLockedUntilTime = -1f;
                    PlayClip(IdleClipKey, restart: true);
                    break;
            }
        }

        private void OnDestroy()
        {
            ReleaseGeneratedSprites();
        }

        private void LoadClips(SpriteSheetBattleVisualConfig config)
        {
            ReleaseGeneratedSprites();
            clips.Clear();

            foreach (var clipConfig in config.Clips)
            {
                if (clipConfig == null || string.IsNullOrWhiteSpace(clipConfig.Key))
                {
                    continue;
                }

                var textures = Resources.LoadAll<Texture2D>(CombineResourcesPath(config.ResourcesRoot, clipConfig.ResourcesFolder));
                if (textures == null || textures.Length == 0)
                {
                    continue;
                }

                Array.Sort(textures, (left, right) => string.CompareOrdinal(left != null ? left.name : string.Empty, right != null ? right.name : string.Empty));
                var sprites = new List<Sprite>(textures.Length);
                foreach (var texture in textures)
                {
                    if (texture == null)
                    {
                        continue;
                    }

                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    var sprite = Sprite.Create(
                        texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        config.SpritePivot,
                        config.PixelsPerUnit,
                        0,
                        SpriteMeshType.FullRect);
                    sprite.name = texture.name;
                    sprites.Add(sprite);
                    generatedSprites.Add(sprite);
                }

                if (sprites.Count == 0)
                {
                    continue;
                }

                clips[clipConfig.Key] = new RuntimeClip(clipConfig.Key, sprites.ToArray(), clipConfig.FramesPerSecond, clipConfig.Loop);
            }
        }

        private void PlayActionClip(string key, float minimumDurationSeconds)
        {
            var clip = ResolveClip(key);
            if (clip == null || deathStateApplied)
            {
                return;
            }

            actionLockedUntilTime = Mathf.Max(actionLockedUntilTime, Time.time + Mathf.Max(minimumDurationSeconds, clip.DurationSeconds));
            PlayClip(clip.Key, restart: true);
        }

        private void PlayClip(string key, bool restart)
        {
            var clip = ResolveClip(key);
            if (clip == null)
            {
                return;
            }

            if (!restart && currentClip == clip)
            {
                return;
            }

            currentClip = clip;
            frameIndex = 0;
            frameTimer = 0f;
            ShowFrame(0);
        }

        private RuntimeClip ResolveClip(string key)
        {
            if (!string.IsNullOrWhiteSpace(key) && clips.TryGetValue(key, out var clip))
            {
                return clip;
            }

            if (string.Equals(key, RageIdleClipKey, StringComparison.Ordinal) && clips.TryGetValue(IdleClipKey, out clip))
            {
                return clip;
            }

            if (string.Equals(key, RageAttack1ClipKey, StringComparison.Ordinal) && clips.TryGetValue(Attack1ClipKey, out clip))
            {
                return clip;
            }

            if (string.Equals(key, UltimateClipKey, StringComparison.Ordinal) && clips.TryGetValue(SkillClipKey, out clip))
            {
                return clip;
            }

            if ((string.Equals(key, Attack1ClipKey, StringComparison.Ordinal) || string.Equals(key, Attack2ClipKey, StringComparison.Ordinal))
                && clips.TryGetValue(Attack1ClipKey, out clip))
            {
                return clip;
            }

            return clips.TryGetValue(IdleClipKey, out clip) ? clip : null;
        }

        private string ResolveIdleClipKey()
        {
            return IsTemporaryOverrideVisualStateActive() && clips.ContainsKey(RageIdleClipKey)
                ? RageIdleClipKey
                : IdleClipKey;
        }

        private void TickAnimation()
        {
            if (currentClip == null || currentClip.Sprites.Length <= 1)
            {
                return;
            }

            frameTimer += Mathf.Max(0f, Time.deltaTime);
            var secondsPerFrame = 1f / Mathf.Max(0.1f, currentClip.FramesPerSecond);
            while (frameTimer >= secondsPerFrame)
            {
                frameTimer -= secondsPerFrame;
                var nextFrame = frameIndex + 1;
                if (nextFrame >= currentClip.Sprites.Length)
                {
                    nextFrame = currentClip.Loop ? 0 : currentClip.Sprites.Length - 1;
                }

                ShowFrame(nextFrame);

                if (!currentClip.Loop && nextFrame == currentClip.Sprites.Length - 1)
                {
                    frameTimer = 0f;
                    break;
                }
            }
        }

        private void ShowFrame(int index)
        {
            if (spriteRenderer == null || currentClip == null || currentClip.Sprites.Length == 0)
            {
                return;
            }

            frameIndex = Mathf.Clamp(index, 0, currentClip.Sprites.Length - 1);
            spriteRenderer.sprite = currentClip.Sprites[frameIndex];
        }

        private void ApplyDeathState()
        {
            if (deathStateApplied)
            {
                return;
            }

            deathStateApplied = true;
            actionLockedUntilTime = -1f;
            PlayClip(DeathClipKey, restart: true);
        }

        private void ResetAfterRevive()
        {
            deathStateApplied = false;
            actionLockedUntilTime = -1f;
            isInMoveState = false;
            currentFacing = Vector2.zero;
            SetFacing(GetDefaultFacing());
            PlayClip(IdleClipKey, restart: true);
            lastPosition = hero.CurrentPosition;
        }

        private void UpdateFacing()
        {
            var movement = hero.CurrentPosition - lastPosition;
            if (movement.sqrMagnitude > FacingMovementThresholdSqr)
            {
                SetFacing(ToCardinalDirection(movement));
                return;
            }

            if (hero.CurrentTarget != null && !hero.CurrentTarget.IsDead)
            {
                var toTarget = hero.CurrentTarget.CurrentPosition - hero.CurrentPosition;
                if (toTarget.sqrMagnitude > FacingMovementThresholdSqr)
                {
                    SetFacing(ToCardinalDirection(toTarget));
                }
            }
        }

        private void SetFacing(Vector2 direction)
        {
            if (visualTransform == null)
            {
                return;
            }

            if (direction == Vector2.zero)
            {
                direction = currentFacing != Vector2.zero ? currentFacing : GetDefaultFacing();
            }

            direction = ResolveHorizontalFacing(direction);
            if (currentFacing == direction)
            {
                return;
            }

            currentFacing = direction;
            var scale = baseVisualScale;
            var facesLeftByDefault = hero != null
                && hero.Definition != null
                && hero.Definition.visualConfig != null
                && hero.Definition.visualConfig.ResolveBattlePrefabFacesLeftByDefault(hero.CurrentVisualFormKey);
            var facingSign = direction.x >= 0f
                ? (facesLeftByDefault ? -1f : 1f)
                : (facesLeftByDefault ? 1f : -1f);
            scale.x = Mathf.Abs(scale.x) * facingSign;
            visualTransform.localScale = scale;
        }

        private Vector2 ResolveHorizontalFacing(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) >= DirectionThreshold)
            {
                return direction.x >= 0f ? Vector2.right : Vector2.left;
            }

            if (Mathf.Abs(currentFacing.x) >= DirectionThreshold)
            {
                return currentFacing.x >= 0f ? Vector2.right : Vector2.left;
            }

            var defaultFacing = GetDefaultFacing();
            return defaultFacing.x >= 0f ? Vector2.right : Vector2.left;
        }

        private bool IsActionLocked()
        {
            return actionLockedUntilTime > Time.time;
        }

        private Vector2 GetDefaultFacing()
        {
            return hero != null && hero.Side == TeamSide.Blue ? Vector2.right : Vector2.left;
        }

        private bool Matches(RuntimeHero candidate)
        {
            return candidate != null
                && hero != null
                && string.Equals(candidate.RuntimeId, hero.RuntimeId, StringComparison.Ordinal);
        }

        private string ResolveAttackClipKey(string variantKey)
        {
            if (IsTemporaryOverrideVisualStateActive() && clips.ContainsKey(RageAttack1ClipKey))
            {
                return RageAttack1ClipKey;
            }

            return string.Equals(variantKey, "attack_heal", StringComparison.Ordinal)
                ? Attack2ClipKey
                : Attack1ClipKey;
        }

        private string ResolveSkillClipKey(SkillData skill, string variantKey)
        {
            var baseKey = skill != null && skill.slotType == SkillSlotType.Ultimate
                ? UltimateClipKey
                : SkillClipKey;
            if (!string.IsNullOrWhiteSpace(variantKey))
            {
                var variantClipKey = $"{baseKey}_{variantKey.Trim()}";
                if (clips.ContainsKey(variantClipKey))
                {
                    return variantClipKey;
                }
            }

            return baseKey;
        }

        private bool IsTemporaryOverrideVisualStateActive()
        {
            return hero != null && hero.CurrentTemporaryOverrideSourceSkill != null;
        }

        private static Vector2 ToCardinalDirection(Vector3 vector)
        {
            if (Mathf.Abs(vector.x) >= Mathf.Abs(vector.z))
            {
                if (Mathf.Abs(vector.x) < DirectionThreshold)
                {
                    return Vector2.zero;
                }

                return vector.x >= 0f ? Vector2.right : Vector2.left;
            }

            if (Mathf.Abs(vector.z) < DirectionThreshold)
            {
                return Vector2.zero;
            }

            return vector.z >= 0f ? Vector2.up : Vector2.down;
        }

        private static string CombineResourcesPath(string root, string folder)
        {
            root = (root ?? string.Empty).Replace("\\", "/").Trim('/');
            folder = (folder ?? string.Empty).Replace("\\", "/").Trim('/');
            if (string.IsNullOrWhiteSpace(root))
            {
                return folder;
            }

            return string.IsNullOrWhiteSpace(folder) ? root : $"{root}/{folder}";
        }

        private void ReleaseGeneratedSprites()
        {
            foreach (var sprite in generatedSprites)
            {
                if (sprite == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    Destroy(sprite);
                }
                else
                {
                    DestroyImmediate(sprite);
                }
            }

            generatedSprites.Clear();
        }

        private sealed class RuntimeClip
        {
            public RuntimeClip(string key, Sprite[] sprites, float framesPerSecond, bool loop)
            {
                Key = key;
                Sprites = sprites;
                FramesPerSecond = Mathf.Max(0.1f, framesPerSecond);
                Loop = loop;
            }

            public string Key { get; }

            public Sprite[] Sprites { get; }

            public float FramesPerSecond { get; }

            public bool Loop { get; }

            public float DurationSeconds => Sprites != null && Sprites.Length > 0
                ? Sprites.Length / FramesPerSecond
                : 0.1f;
        }
    }
}
