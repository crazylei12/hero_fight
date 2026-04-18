using Fight.Battle;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.UI
{
    public class GenericAnimatorBattleAnimationDriver : HeroBattleAnimationDriver
    {
        private const float FacingMovementThresholdSqr = 0.0004f;
        private const float MoveStateEnterSpeed = 0.2f;
        private const float MoveStateExitSpeed = 0.08f;
        private const float DirectionThreshold = 0.05f;
        private const float RunStateThreshold = 0.82f;
        private const float BasicAttackLockSeconds = 0.28f;
        private const float SkillCastLockSeconds = 0.38f;
        private const string AttackParameterName = "Attack";
        private const string ActionParameterName = "Action";
        private const string StateParameterName = "State";
        private const string SpeedParameterName = "Speed";
        private const int IdleStateValue = 0;
        private const int ReadyStateValue = 1;
        private const int WalkStateValue = 2;
        private const int RunStateValue = 3;
        private const int DeathStateValue = 9;

        private RuntimeHero hero;
        private Animator animator;
        private Transform visualTransform;
        private Vector3 baseVisualScale = Vector3.one;
        private Vector3 lastPosition;
        private Vector2 currentFacing;
        private float actionLockedUntilTime = -1f;
        private bool deathStateApplied;
        private bool isInMoveState;
        private bool hasAttackTrigger;
        private bool hasActionBool;
        private bool hasStateInt;
        private bool hasSpeedFloat;

        public override bool IsReady => hero != null && animator != null && visualTransform != null;

        public override void Initialize(RuntimeHero runtimeHero, GameObject visualInstance)
        {
            hero = runtimeHero;
            visualTransform = visualInstance != null ? visualInstance.transform : null;
            animator = visualInstance != null ? visualInstance.GetComponentInChildren<Animator>(true) : null;

            if (!IsReady)
            {
                enabled = false;
                return;
            }

            if (hero.Definition?.visualConfig?.animatorController != null)
            {
                animator.runtimeAnimatorController = hero.Definition.visualConfig.animatorController;
            }

            CacheAnimatorParameters();
            baseVisualScale = visualTransform.localScale;
            currentFacing = Vector2.zero;
            SetFacing(GetDefaultFacing());
            ResetAnimatorState();
            SetAction(false);
            SetState(IdleStateValue);
            SetSpeed(1f);
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

            UpdateFacing();

            var movement = hero.CurrentPosition - lastPosition;
            var deltaTime = Mathf.Max(Time.deltaTime, 0.0001f);
            var actualMoveSpeed = movement.magnitude / deltaTime;
            isInMoveState = isInMoveState
                ? actualMoveSpeed >= MoveStateExitSpeed
                : actualMoveSpeed >= MoveStateEnterSpeed;

            var normalizedMoveSpeed = hero.MoveSpeed > Mathf.Epsilon
                ? actualMoveSpeed / Mathf.Max(hero.MoveSpeed, 0.01f)
                : 0f;
            var desiredState = DetermineDesiredState(normalizedMoveSpeed);

            SetSpeed(isInMoveState ? Mathf.Max(0.35f, normalizedMoveSpeed) : 1f);
            SetAction(IsActionLocked());
            SetState(desiredState);
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
                    TriggerAttack(BasicAttackLockSeconds);
                    break;
                case SkillCastEvent skillCastEvent when Matches(skillCastEvent.Caster):
                    TriggerAttack(SkillCastLockSeconds);
                    break;
                case UnitDiedEvent diedEvent when Matches(diedEvent.Victim):
                    ApplyDeathState();
                    break;
                case UnitRevivedEvent revivedEvent when Matches(revivedEvent.Hero):
                    ResetAfterRevive();
                    break;
                case BattleEndedEvent _ when !hero.IsDead:
                    actionLockedUntilTime = -1f;
                    SetAction(false);
                    SetState(IdleStateValue);
                    SetSpeed(1f);
                    break;
            }
        }

        private void TriggerAttack(float actionLockSeconds)
        {
            if (deathStateApplied)
            {
                return;
            }

            actionLockedUntilTime = Mathf.Max(actionLockedUntilTime, Time.time + Mathf.Max(0.01f, actionLockSeconds));
            SetAction(true);
            if (hasAttackTrigger)
            {
                animator.SetTrigger(AttackParameterName);
            }
        }

        private void ApplyDeathState()
        {
            if (deathStateApplied)
            {
                return;
            }

            deathStateApplied = true;
            actionLockedUntilTime = -1f;
            ResetAttackTrigger();
            SetAction(false);
            SetState(DeathStateValue);
            SetSpeed(1f);
        }

        private void ResetAfterRevive()
        {
            deathStateApplied = false;
            actionLockedUntilTime = -1f;
            isInMoveState = false;
            currentFacing = Vector2.zero;
            ResetAnimatorState();
            SetFacing(GetDefaultFacing());
            SetAction(false);
            SetState(IdleStateValue);
            SetSpeed(1f);
            lastPosition = hero.CurrentPosition;
        }

        private int DetermineDesiredState(float normalizedMoveSpeed)
        {
            if (deathStateApplied)
            {
                return DeathStateValue;
            }

            if (isInMoveState)
            {
                return normalizedMoveSpeed >= RunStateThreshold
                    ? RunStateValue
                    : WalkStateValue;
            }

            return hero.CurrentTarget != null && !hero.CurrentTarget.IsDead
                ? ReadyStateValue
                : IdleStateValue;
        }

        private void ResetAnimatorState()
        {
            ResetAttackTrigger();
            animator.Rebind();
            animator.Update(0f);
        }

        private void ResetAttackTrigger()
        {
            if (hasAttackTrigger)
            {
                animator.ResetTrigger(AttackParameterName);
            }
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

            if (currentFacing == direction)
            {
                return;
            }

            currentFacing = direction;
            var scale = baseVisualScale;
            scale.x = Mathf.Abs(scale.x) * (direction.x >= 0f ? 1f : -1f);
            visualTransform.localScale = scale;
        }

        private void SetAction(bool value)
        {
            if (hasActionBool)
            {
                animator.SetBool(ActionParameterName, value);
            }
        }

        private void SetState(int value)
        {
            if (hasStateInt)
            {
                animator.SetInteger(StateParameterName, value);
            }
        }

        private void SetSpeed(float value)
        {
            if (hasSpeedFloat)
            {
                animator.SetFloat(SpeedParameterName, Mathf.Max(0.1f, value));
            }
        }

        private bool IsActionLocked()
        {
            return actionLockedUntilTime > Time.time;
        }

        private void CacheAnimatorParameters()
        {
            var parameters = animator.parameters;
            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                if (parameter.name == AttackParameterName && parameter.type == AnimatorControllerParameterType.Trigger)
                {
                    hasAttackTrigger = true;
                }
                else if (parameter.name == ActionParameterName && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasActionBool = true;
                }
                else if (parameter.name == StateParameterName && parameter.type == AnimatorControllerParameterType.Int)
                {
                    hasStateInt = true;
                }
                else if (parameter.name == SpeedParameterName && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasSpeedFloat = true;
                }
            }
        }

        private Vector2 GetDefaultFacing()
        {
            return hero != null && hero.Side == TeamSide.Blue ? Vector2.right : Vector2.left;
        }

        private bool Matches(RuntimeHero candidate)
        {
            return candidate != null
                && hero != null
                && string.Equals(candidate.RuntimeId, hero.RuntimeId, System.StringComparison.Ordinal);
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
    }
}
