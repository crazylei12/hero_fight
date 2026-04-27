using Assets.HeroEditor4D.Common.Scripts.CharacterScripts;
using Assets.HeroEditor4D.Common.Scripts.Enums;
using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    public class HeroEditor4DBattleAnimationDriver : HeroBattleAnimationDriver
    {
        private const float FacingMovementThresholdSqr = 0.0004f;
        private const float MoveStateEnterSpeed = 0.2f;
        private const float MoveStateExitSpeed = 0.08f;
        private const float DirectionThreshold = 0.05f;
        private const float BaseAnimatorSpeed = 1f;
        private const float MovingAnimatorSpeedBoost = 1.25f;
        private const float MaxMovingAnimatorSpeed = 2.35f;
        private const float AnimatorSpeedSmoothing = 8f;
        private static readonly string[] AnimationTriggers =
        {
            "Cast",
            "Slash1H",
            "Slash2H",
            "Jab",
            "HeavySlash1H",
            "FastStab",
            "ShotBow",
            "Hit",
            "Evade",
            "Fire",
            "SecondaryShot",
        };

        private RuntimeHero hero;
        private Character4D character;
        private AnimationManager animationManager;
        private Animator animator;
        private CharacterState currentState = CharacterState.Idle;
        private Vector3 lastPosition;
        private bool deathStateApplied;
        private Vector2 currentFacing;
        private bool isInMoveState;

        public override bool IsReady => hero != null && character != null && animationManager != null && animator != null;

        public override void Initialize(RuntimeHero runtimeHero, GameObject visualInstance)
        {
            hero = runtimeHero;

            if (visualInstance == null)
            {
                enabled = false;
                return;
            }

            character = visualInstance.GetComponentInChildren<Character4D>(true);
            animationManager = visualInstance.GetComponentInChildren<AnimationManager>(true);
            animator = visualInstance.GetComponentInChildren<Animator>(true);

            if (!IsReady)
            {
                enabled = false;
                return;
            }

            var animatorController = hero.Definition.visualConfig.ResolveAnimatorController(hero.CurrentVisualFormKey);
            if (animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
            }

            character.Initialize();
            currentFacing = Vector2.zero;
            SetFacing(GetDefaultFacing());
            ForceState(CharacterState.Idle);
            animationManager.IsAction = false;
            animator.speed = BaseAnimatorSpeed;
            isInMoveState = false;
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
                return;
            }

            if (deathStateApplied)
            {
                ResetAfterRevive();
            }

            if (hero.IsUnderForcedMovement)
            {
                RestoreAnimatorSpeed();
                isInMoveState = false;
                if (!animationManager.IsAction)
                {
                    SetState(hero.CurrentTarget != null && !hero.CurrentTarget.IsDead
                        ? CharacterState.Ready
                        : CharacterState.Idle);
                }

                lastPosition = hero.CurrentPosition;
                return;
            }

            UpdateFacing();

            var movement = hero.CurrentPosition - lastPosition;
            var actualMoveSpeed = movement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

            if (!animationManager.IsAction)
            {
                isInMoveState = isInMoveState
                    ? actualMoveSpeed >= MoveStateExitSpeed
                    : actualMoveSpeed >= MoveStateEnterSpeed;

                var desiredState = isInMoveState
                    ? CharacterState.Walk
                    : hero.CurrentTarget != null && !hero.CurrentTarget.IsDead
                        ? CharacterState.Ready
                        : CharacterState.Idle;

                UpdateAnimatorSpeed(movement, isInMoveState);
                SetState(desiredState);
            }
            else
            {
                RestoreAnimatorSpeed();
            }

            lastPosition = hero.CurrentPosition;
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
                    PlayBasicAttack();
                    break;
                case SkillCastEvent skillCastEvent when Matches(skillCastEvent.Caster):
                    PlaySkillCast();
                    break;
                case DamageAppliedEvent damageEvent when Matches(damageEvent.Target) && damageEvent.Target.CurrentHealth > 0f:
                    animationManager.Hit();
                    break;
                case UnitDiedEvent diedEvent when Matches(diedEvent.Victim):
                    ApplyDeathState();
                    break;
                case UnitRevivedEvent revivedEvent when Matches(revivedEvent.Hero):
                    ResetAfterRevive();
                    break;
                case BattleEndedEvent _ when !hero.IsDead:
                    animationManager.IsAction = false;
                    SetState(CharacterState.Idle);
                    break;
            }
        }

        private void PlayBasicAttack()
        {
            var definition = hero.Definition;
            var basicAttack = definition?.basicAttack;
            if (basicAttack == null)
            {
                animationManager.Attack();
                return;
            }

            if (hero.UsesProjectileBasicAttack)
            {
                if (definition.heroClass is HeroClass.Mage or HeroClass.Support)
                {
                    TriggerCast();
                    return;
                }

                TriggerRangedAttack();
                return;
            }

            animationManager.Attack();
        }

        private void PlaySkillCast()
        {
            TriggerCast();
        }

        private void TriggerCast()
        {
            animator.SetTrigger("Cast");
            animationManager.IsAction = true;
            SetState(CharacterState.Ready);
        }

        private void TriggerRangedAttack()
        {
            switch (character.WeaponType)
            {
                case WeaponType.Bow:
                    animationManager.ShotBow();
                    break;
                case WeaponType.Crossbow:
                    animationManager.CrossbowShot();
                    break;
                case WeaponType.Firearm1H:
                case WeaponType.Firearm2H:
                case WeaponType.Paired:
                    animationManager.Fire();
                    break;
                default:
                    TriggerCast();
                    break;
            }
        }

        private void ApplyDeathState()
        {
            if (deathStateApplied)
            {
                return;
            }

            deathStateApplied = true;
            animator.speed = BaseAnimatorSpeed;
            animationManager.Die();
            currentState = CharacterState.Death;
        }

        private void ResetAfterRevive()
        {
            deathStateApplied = false;
            ResetAnimatorState();
            animationManager.IsAction = false;
            animator.speed = BaseAnimatorSpeed;
            isInMoveState = false;
            currentFacing = Vector2.zero;
            SetFacing(GetDefaultFacing());
            character.SetExpression("Default");
            ForceState(CharacterState.Idle);
            lastPosition = hero.CurrentPosition;
        }

        private void ResetAnimatorState()
        {
            for (var i = 0; i < AnimationTriggers.Length; i++)
            {
                animator.ResetTrigger(AnimationTriggers[i]);
            }

            animator.Rebind();
            animator.Update(0f);
            animationManager.SetWeaponType(character.WeaponType);
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
                    return;
                }
            }
        }

        private void SetFacing(Vector2 direction)
        {
            if (direction == Vector2.zero)
            {
                direction = currentFacing != Vector2.zero ? currentFacing : GetDefaultFacing();
            }

            if (currentFacing == direction)
            {
                return;
            }

            currentFacing = direction;
            character.SetDirection(direction);
        }

        private void SetState(CharacterState state)
        {
            if (currentState == state)
            {
                return;
            }

            ForceState(state);
        }

        private void ForceState(CharacterState state)
        {
            animationManager.SetState(state);
            currentState = state;
        }

        private void UpdateAnimatorSpeed(Vector3 movement, bool isMoving)
        {
            if (!isMoving)
            {
                RestoreAnimatorSpeed();
                return;
            }

            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            var actualMoveSpeed = movement.magnitude / deltaTime;
            var expectedMoveSpeed = Mathf.Max(0.01f, hero.MoveSpeed);
            var normalizedMoveSpeed = actualMoveSpeed / expectedMoveSpeed;
            var targetSpeed = Mathf.Clamp(normalizedMoveSpeed * MovingAnimatorSpeedBoost, BaseAnimatorSpeed, MaxMovingAnimatorSpeed);

            animator.speed = Mathf.MoveTowards(animator.speed, targetSpeed, AnimatorSpeedSmoothing * deltaTime);
        }

        private void RestoreAnimatorSpeed()
        {
            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            animator.speed = Mathf.MoveTowards(animator.speed, BaseAnimatorSpeed, AnimatorSpeedSmoothing * deltaTime);
        }

        private Vector2 GetDefaultFacing()
        {
            return hero != null && hero.Side == TeamSide.Blue ? Vector2.right : Vector2.left;
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

        private bool Matches(RuntimeHero candidate)
        {
            return candidate != null && hero.RuntimeId == candidate.RuntimeId;
        }
    }
}
