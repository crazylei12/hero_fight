using Fight.Core;
using Fight.Data;
using Fight.Heroes;
using UnityEngine;

namespace Fight.Battle
{
    public static class BattleBasicAttackSystem
    {
        private const float ProjectileHitDistance = 0.1f;
        private const float HeroWideFallbackSearchRange = 999f;
        private static int projectileSequence;

        public static void TickProjectiles(BattleContext context, float deltaTime, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null || battleCallbacks == null || context.Projectiles.Count == 0)
            {
                return;
            }

            for (var i = context.Projectiles.Count - 1; i >= 0; i--)
            {
                var projectile = context.Projectiles[i];
                if (!IsValidTarget(projectile.Attacker, projectile.Target, projectile.EffectType, projectile.TargetType, projectile.OnHitStatusEffects))
                {
                    if (projectile.BounceChain != null && projectile.BounceChain.TotalHitCount > 0)
                    {
                        CompleteBounceChain(context, projectile.Attacker, projectile.BounceChain);
                    }

                    context.Projectiles.RemoveAt(i);
                    continue;
                }

                var targetPosition = projectile.Target.CurrentPosition;
                var offset = targetPosition - projectile.CurrentPosition;
                var distanceToTarget = offset.magnitude;
                var step = projectile.Speed * deltaTime;

                if (distanceToTarget <= ProjectileHitDistance || step >= distanceToTarget)
                {
                    projectile.CurrentPosition = targetPosition;
                    ResolveHit(
                        context,
                        projectile.Attacker,
                        projectile.SourceProxy,
                        projectile.Target,
                        projectile.ImpactAmount,
                        projectile.EffectType,
                        projectile.TargetType,
                        projectile.SameTargetStacking,
                        projectile.OnHitStatusEffects,
                        projectile.VariantKey,
                        projectile.BounceChain,
                        projectile.BounceHopIndex,
                        battleCallbacks);
                    context.Projectiles.RemoveAt(i);
                    continue;
                }

                projectile.CurrentPosition += offset.normalized * step;
            }
        }

        public static RuntimeHero SelectPreferredTargetPreview(BattleContext context, RuntimeHero attacker)
        {
            return TryResolveHeroAttack(
                context,
                attacker,
                HeroWideFallbackSearchRange,
                allowHealthyHealFallback: true,
                out var target,
                out _)
                ? target
                : null;
        }

        public static bool TryResolveHeroAttack(
            BattleContext context,
            RuntimeHero attacker,
            float selectionRange,
            out RuntimeHero target,
            out ResolvedBasicAttack resolvedAttack)
        {
            return TryResolveHeroAttack(
                context,
                attacker,
                selectionRange,
                allowHealthyHealFallback: true,
                out target,
                out resolvedAttack);
        }

        public static void BeginAttack(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeHero target,
            ResolvedBasicAttack resolvedAttack,
            IBattleSimulationCallbacks battleCallbacks)
        {
            BeginAttack(
                context,
                attacker,
                target,
                resolvedAttack,
                battleCallbacks,
                CombatActionTiming.DefaultWindupSeconds,
                CombatActionTiming.DefaultRecoverySeconds,
                consumeAttackCooldown: true);
        }

        internal static void BeginAttack(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeHero target,
            ResolvedBasicAttack resolvedAttack,
            IBattleSimulationCallbacks battleCallbacks,
            float windupSeconds,
            float recoverySeconds,
            bool consumeAttackCooldown,
            bool isActionSequenceStep = false)
        {
            if (context == null
                || attacker == null
                || target == null
                || resolvedAttack == null
                || battleCallbacks == null)
            {
                return;
            }

            if (!IsValidTarget(attacker, target, resolvedAttack.EffectType, resolvedAttack.TargetType, resolvedAttack.OnHitStatusEffects)
                || !CanExecuteAgainstTarget(attacker, target, resolvedAttack))
            {
                return;
            }

            var attackToBegin = resolvedAttack;
            if (attacker.TryConsumeTargetSwitchFirstHit(target, out var targetSwitchTrigger))
            {
                attackToBegin = resolvedAttack.WithTargetSwitchTrigger(targetSwitchTrigger);
            }

            attacker.BeginBasicAttack(target, attackToBegin, windupSeconds, recoverySeconds, consumeAttackCooldown, isActionSequenceStep);
            context.EventBus.Publish(new AttackPerformedEvent(attacker, target, attackToBegin.VariantKey));
        }

        public static void ResolvePendingAttack(BattleContext context, RuntimeHero attacker, PendingCombatAction pendingAction, IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null
                || attacker == null
                || attacker.IsDead
                || pendingAction?.Target == null
                || pendingAction.BasicAttack == null
                || battleCallbacks == null)
            {
                return;
            }

            var target = pendingAction.Target;
            var resolvedAttack = pendingAction.BasicAttack;
            if (!IsValidTarget(attacker, target, resolvedAttack.EffectType, resolvedAttack.TargetType, resolvedAttack.OnHitStatusEffects)
                || !CanExecuteAgainstTarget(attacker, target, resolvedAttack))
            {
                return;
            }

            var impactAmount = resolvedAttack.EffectType == BasicAttackEffectType.Heal
                ? HealResolver.ResolveHealAmount(attacker, resolvedAttack.PowerMultiplier)
                : DamageResolver.ResolveDamage(
                    attacker.AttackPower,
                    attacker.CriticalChance,
                    attacker.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    resolvedAttack.PowerMultiplier);

            if (resolvedAttack.EffectType == BasicAttackEffectType.Damage)
            {
                BattleDeployableProxySystem.TriggerOwnerBasicAttackProxies(context, attacker, target, battleCallbacks);
            }

            var bounceChain = CreateBounceChain(resolvedAttack);
            if (resolvedAttack.UsesProjectile)
            {
                LaunchProjectile(context, attacker, null, target, impactAmount, resolvedAttack, bounceChain, 0);
                return;
            }

            ResolveHit(
                context,
                attacker,
                null,
                target,
                impactAmount,
                resolvedAttack.EffectType,
                resolvedAttack.TargetType,
                resolvedAttack.SameTargetStacking,
                resolvedAttack.OnHitStatusEffects,
                resolvedAttack.VariantKey,
                bounceChain,
                0,
                battleCallbacks);
        }

        public static bool TryResolveProxyAttack(
            BattleContext context,
            RuntimeDeployableProxy proxy,
            out RuntimeHero target,
            out ResolvedBasicAttack resolvedAttack)
        {
            target = null;
            resolvedAttack = null;
            if (context?.Heroes == null
                || proxy?.Owner?.Definition?.basicAttack == null)
            {
                return false;
            }

            return TryResolveAttackSelection(
                context,
                proxy.Owner,
                proxy.CurrentPosition,
                proxy.Owner.Definition.basicAttack,
                proxy.GetClampedBasicAttackVariantIndex(),
                proxy.AttackRange,
                proxy.AttackRange,
                false,
                true,
                proxy.Owner.Definition.basicAttack.usesProjectile,
                proxy.ProjectileSpeed,
                proxy.Owner.CurrentVisualFormKey,
                proxy.PowerMultiplierScale,
                proxy,
                out target,
                out resolvedAttack);
        }

        public static void FireProxyAttack(
            BattleContext context,
            RuntimeDeployableProxy proxy,
            RuntimeHero target,
            ResolvedBasicAttack resolvedAttack,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (context == null
                || proxy?.Owner == null
                || target == null
                || resolvedAttack == null
                || battleCallbacks == null)
            {
                return;
            }

            if (!IsValidTarget(proxy.Owner, target, resolvedAttack.EffectType, resolvedAttack.TargetType, resolvedAttack.OnHitStatusEffects)
                || !CanExecuteAgainstTarget(proxy.Owner, target, resolvedAttack))
            {
                return;
            }

            if (resolvedAttack.AdvanceSequenceOnUse)
            {
                proxy.AdvanceBasicAttackVariantIndex();
            }

            var impactAmount = resolvedAttack.EffectType == BasicAttackEffectType.Heal
                ? HealResolver.ResolveHealAmount(proxy.Owner, resolvedAttack.PowerMultiplier)
                : DamageResolver.ResolveDamage(
                    proxy.Owner.AttackPower,
                    proxy.Owner.CriticalChance,
                    proxy.Owner.CriticalDamageMultiplier,
                    target.Defense,
                    context.RandomService,
                    resolvedAttack.PowerMultiplier);

            var bounceChain = CreateBounceChain(resolvedAttack);
            if (resolvedAttack.UsesProjectile)
            {
                LaunchProjectile(context, proxy.Owner, proxy, target, impactAmount, resolvedAttack, bounceChain, 0);
                return;
            }

            ResolveHit(
                context,
                proxy.Owner,
                proxy,
                target,
                impactAmount,
                resolvedAttack.EffectType,
                resolvedAttack.TargetType,
                resolvedAttack.SameTargetStacking,
                resolvedAttack.OnHitStatusEffects,
                resolvedAttack.VariantKey,
                bounceChain,
                0,
                battleCallbacks);
        }

        private static bool TryResolveHeroAttack(
            BattleContext context,
            RuntimeHero attacker,
            float selectionRange,
            bool allowHealthyHealFallback,
            out RuntimeHero target,
            out ResolvedBasicAttack resolvedAttack)
        {
            target = null;
            resolvedAttack = null;
            if (context?.Heroes == null
                || attacker?.Definition?.basicAttack == null
                || attacker.IsDead)
            {
                return false;
            }

            return TryResolveAttackSelection(
                context,
                attacker,
                attacker.CurrentPosition,
                attacker.Definition.basicAttack,
                attacker.GetClampedBasicAttackVariantIndex(),
                selectionRange,
                HeroWideFallbackSearchRange,
                true,
                allowHealthyHealFallback,
                attacker.UsesProjectileBasicAttack,
                attacker.CurrentBasicAttackProjectileSpeed,
                attacker.CurrentVisualFormKey,
                1f,
                null,
                out target,
                out resolvedAttack);
        }

        private static bool TryResolveAttackSelection(
            BattleContext context,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            BasicAttackData basicAttack,
            int sequenceVariantIndex,
            float selectionRange,
            float missingTargetFallbackSearchRange,
            bool allowForcedEnemyTarget,
            bool allowHealthyHealFallback,
            bool usesProjectileOverride,
            float projectileSpeedOverride,
            string visualFormKey,
            float powerMultiplierScale,
            RuntimeDeployableProxy sourceProxy,
            out RuntimeHero target,
            out ResolvedBasicAttack resolvedAttack)
        {
            target = null;
            resolvedAttack = null;
            if (context?.Heroes == null
                || attacker == null
                || basicAttack == null)
            {
                return false;
            }

            var hasVariantSequence = basicAttack.variants != null && basicAttack.variants.Count > 0;
            var variantIndex = hasVariantSequence
                ? Mathf.Clamp(sequenceVariantIndex, 0, basicAttack.variants.Count - 1)
                : 0;
            var currentVariant = hasVariantSequence ? basicAttack.variants[variantIndex] : null;
            var currentAttack = BuildResolvedAttack(
                basicAttack,
                currentVariant,
                sourcePosition,
                usesProjectileOverride,
                projectileSpeedOverride,
                visualFormKey,
                powerMultiplierScale,
                advanceSequenceOnUse: hasVariantSequence);

            if (TryResolveSelectionForAttack(
                    context,
                    attacker,
                    sourcePosition,
                    currentAttack,
                    selectionRange,
                    allowForcedEnemyTarget,
                    allowHealthyHealFallback,
                    out target))
            {
                resolvedAttack = currentAttack;
                return true;
            }

            if (!hasVariantSequence || currentVariant == null)
            {
                return false;
            }

            var fallbackVariantIndex = currentVariant.missingTargetFallbackVariantIndex;
            if (fallbackVariantIndex < 0 || fallbackVariantIndex >= basicAttack.variants.Count)
            {
                return false;
            }

            if (HasLegalTargetForAttack(
                    context,
                    attacker,
                    sourcePosition,
                    currentAttack,
                    missingTargetFallbackSearchRange,
                    allowForcedEnemyTarget))
            {
                return false;
            }

            var fallbackVariant = basicAttack.variants[fallbackVariantIndex];
            var fallbackAttack = BuildResolvedAttack(
                basicAttack,
                fallbackVariant,
                sourcePosition,
                usesProjectileOverride,
                projectileSpeedOverride,
                visualFormKey,
                powerMultiplierScale,
                advanceSequenceOnUse: false);
            if (!TryResolveSelectionForAttack(
                    context,
                    attacker,
                    sourcePosition,
                    fallbackAttack,
                    selectionRange,
                    allowForcedEnemyTarget: false,
                    allowHealthyHealFallback,
                    out target))
            {
                return false;
            }

            resolvedAttack = fallbackAttack;
            return true;
        }

        private static ResolvedBasicAttack BuildResolvedAttack(
            BasicAttackData basicAttack,
            BasicAttackVariantData variant,
            Vector3 launchPosition,
            bool usesProjectileOverride,
            float projectileSpeedOverride,
            string visualFormKey,
            float powerMultiplierScale,
            bool advanceSequenceOnUse)
        {
            var effectType = variant != null ? variant.effectType : basicAttack.effectType;
            var targetType = variant != null ? variant.targetType : basicAttack.targetType;
            var powerMultiplier = variant != null ? variant.powerMultiplier : basicAttack.damageMultiplier;
            var targetPrioritySearchRadius = variant != null ? variant.targetPrioritySearchRadius : basicAttack.targetPrioritySearchRadius;
            var onHitStatusEffects = variant != null ? variant.onHitStatusEffects : basicAttack.onHitStatusEffects;
            var projectileSpeed = projectileSpeedOverride > Mathf.Epsilon ? projectileSpeedOverride : basicAttack.projectileSpeed;
            var bounce = basicAttack.bounce;

            return new ResolvedBasicAttack(
                variant != null ? variant.variantKey : string.Empty,
                effectType,
                targetType,
                powerMultiplier * Mathf.Max(0f, powerMultiplierScale),
                targetPrioritySearchRadius,
                usesProjectileOverride,
                projectileSpeed,
                visualFormKey,
                bounce != null ? bounce.maxAdditionalTargets : 0,
                bounce != null ? bounce.searchRadius : 0f,
                bounce != null ? bounce.powerMultiplier * Mathf.Max(0f, powerMultiplierScale) : 0f,
                bounce != null ? bounce.bounceVariantKey : string.Empty,
                basicAttack.sameTargetStacking,
                onHitStatusEffects,
                launchPosition,
                advanceSequenceOnUse);
        }

        private static bool TryResolveSelectionForAttack(
            BattleContext context,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            ResolvedBasicAttack resolvedAttack,
            float selectionRange,
            bool allowForcedEnemyTarget,
            bool allowHealthyHealFallback,
            out RuntimeHero target)
        {
            target = null;
            if (context?.Heroes == null || attacker == null || resolvedAttack == null)
            {
                return false;
            }

            if (allowForcedEnemyTarget
                && attacker.TryGetForcedEnemyTarget(out var forcedTarget)
                && IsValidTarget(attacker, forcedTarget, resolvedAttack.EffectType, resolvedAttack.TargetType, resolvedAttack.OnHitStatusEffects)
                && IsWithinRange(sourcePosition, forcedTarget, selectionRange)
                && CanExecuteAgainstTarget(attacker, forcedTarget, resolvedAttack))
            {
                target = forcedTarget;
                return true;
            }

            if (TrySelectFocusFireTarget(context, attacker, sourcePosition, resolvedAttack, selectionRange, out target))
            {
                return true;
            }

            if (TrySelectSameTargetStackTarget(attacker, sourcePosition, resolvedAttack, selectionRange, out var retainedTarget))
            {
                target = retainedTarget;
                return true;
            }

            target = SelectTarget(
                context,
                attacker,
                sourcePosition,
                resolvedAttack,
                selectionRange,
                allowHealthyHealFallback);
            return target != null && CanExecuteAgainstTarget(attacker, target, resolvedAttack);
        }

        private static bool TrySelectSameTargetStackTarget(
            RuntimeHero attacker,
            Vector3 sourcePosition,
            ResolvedBasicAttack resolvedAttack,
            float selectionRange,
            out RuntimeHero target)
        {
            target = null;
            var stacking = resolvedAttack?.SameTargetStacking;
            if (!IsSameTargetStackingEnabled(stacking)
                || attacker == null
                || resolvedAttack.TargetType != BasicAttackTargetType.NearestEnemy
                || stacking.targetRetentionRange <= Mathf.Epsilon)
            {
                return false;
            }

            var currentTarget = attacker.CurrentTarget;
            var retentionRange = Mathf.Min(Mathf.Max(0f, selectionRange), stacking.targetRetentionRange);
            if (!IsValidTarget(attacker, currentTarget, resolvedAttack.EffectType, resolvedAttack.TargetType, resolvedAttack.OnHitStatusEffects)
                || !IsWithinRange(sourcePosition, currentTarget, retentionRange)
                || !CanExecuteAgainstTarget(attacker, currentTarget, resolvedAttack))
            {
                return false;
            }

            target = currentTarget;
            return true;
        }

        private static bool HasLegalTargetForAttack(
            BattleContext context,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            ResolvedBasicAttack resolvedAttack,
            float searchRange,
            bool allowForcedEnemyTarget)
        {
            return TryResolveSelectionForAttack(
                context,
                attacker,
                sourcePosition,
                resolvedAttack,
                searchRange,
                allowForcedEnemyTarget,
                allowHealthyHealFallback: false,
                out _);
        }

        private static RuntimeHero SelectTarget(
            BattleContext context,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            ResolvedBasicAttack resolvedAttack,
            float selectionRange,
            bool allowHealthyHealFallback)
        {
            if (context?.Heroes == null || attacker == null || resolvedAttack == null)
            {
                return null;
            }

            if (TrySelectFocusFireTarget(context, attacker, sourcePosition, resolvedAttack, selectionRange, out var focusFireTarget))
            {
                return focusFireTarget;
            }

            return resolvedAttack.TargetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => SelectLowestHealthAllyTarget(
                    context.Heroes,
                    attacker,
                    sourcePosition,
                    resolvedAttack,
                    selectionRange,
                    allowHealthyHealFallback),
                BasicAttackTargetType.MissingOnHitStatusOrExpiringAlly => SelectStatusCoverageAllyTarget(
                    context.Heroes,
                    attacker,
                    sourcePosition,
                    selectionRange,
                    resolvedAttack.EffectType,
                    resolvedAttack.OnHitStatusEffects,
                    fallbackAttack: resolvedAttack,
                    allowHealthyFallback: allowHealthyHealFallback),
                BasicAttackTargetType.ThreateningEnemyNearRangedAlly => BattleAiDirector.SelectThreateningEnemyNearRangedAllyTarget(
                        context.Heroes,
                        attacker,
                        resolvedAttack.TargetPrioritySearchRadius > Mathf.Epsilon
                            ? resolvedAttack.TargetPrioritySearchRadius
                            : selectionRange)
                    ?? SelectNearestEnemyTarget(context.Heroes, attacker, sourcePosition, selectionRange),
                _ => SelectNearestEnemyTarget(context.Heroes, attacker, sourcePosition, selectionRange),
            };
        }

        private static bool TrySelectFocusFireTarget(
            BattleContext context,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            ResolvedBasicAttack resolvedAttack,
            float selectionRange,
            out RuntimeHero target)
        {
            target = null;
            if (!CanPreferFocusFireTarget(resolvedAttack)
                || !BattleFocusFireCommandSystem.TryGetMarkedTarget(
                    context,
                    attacker,
                    sourcePosition,
                    selectionRange,
                    out var focusFireTarget)
                || !IsValidTarget(
                    attacker,
                    focusFireTarget,
                    resolvedAttack.EffectType,
                    resolvedAttack.TargetType,
                    resolvedAttack.OnHitStatusEffects)
                || !CanExecuteAgainstTarget(attacker, focusFireTarget, resolvedAttack))
            {
                return false;
            }

            target = focusFireTarget;
            return true;
        }

        private static bool CanPreferFocusFireTarget(ResolvedBasicAttack resolvedAttack)
        {
            return resolvedAttack != null
                && resolvedAttack.EffectType == BasicAttackEffectType.Damage
                && resolvedAttack.TargetType != BasicAttackTargetType.LowestHealthAlly
                && resolvedAttack.TargetType != BasicAttackTargetType.MissingOnHitStatusOrExpiringAlly;
        }

        private static bool CanExecuteAgainstTarget(RuntimeHero attacker, RuntimeHero target, ResolvedBasicAttack resolvedAttack)
        {
            if (target == null || resolvedAttack == null)
            {
                return false;
            }

            if (ShouldRejectPositiveBasicAttackTarget(attacker, target, resolvedAttack.EffectType, resolvedAttack.OnHitStatusEffects))
            {
                return false;
            }

            return AllowsHealthyHealAnchor(resolvedAttack)
                || CanApplyEffectToTarget(target, resolvedAttack.EffectType);
        }

        private static bool AllowsHealthyHealAnchor(ResolvedBasicAttack resolvedAttack)
        {
            return resolvedAttack != null
                && resolvedAttack.EffectType == BasicAttackEffectType.Heal
                && (resolvedAttack.TargetType == BasicAttackTargetType.LowestHealthAlly
                    || resolvedAttack.TargetType == BasicAttackTargetType.MissingOnHitStatusOrExpiringAlly);
        }

        private static RuntimeHero SelectNearestEnemyTarget(
            System.Collections.Generic.IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            float selectionRange)
        {
            RuntimeHero bestTarget = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsPotentialEnemyTarget(attacker, candidate)
                    || !IsWithinRange(sourcePosition, candidate, selectionRange))
                {
                    continue;
                }

                var distance = Vector3.Distance(sourcePosition, candidate.CurrentPosition);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private static RuntimeHero SelectLowestHealthAllyTarget(
            System.Collections.Generic.IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            ResolvedBasicAttack resolvedAttack,
            float selectionRange,
            bool allowHealthyFallback)
        {
            RuntimeHero bestInjured = null;
            var lowestInjuredHealth = float.MaxValue;
            var lowestInjuredRatio = float.MaxValue;
            var bestInjuredDistance = float.MaxValue;
            RuntimeHero nearestHealthyAlly = null;
            var nearestHealthyDistance = float.MaxValue;
            RuntimeHero selfTarget = null;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsPotentialAllyTarget(attacker, candidate)
                    || !IsWithinRange(sourcePosition, candidate, selectionRange))
                {
                    continue;
                }

                if (ShouldRejectPositiveBasicAttackTarget(attacker, candidate, resolvedAttack.EffectType, resolvedAttack.OnHitStatusEffects))
                {
                    continue;
                }

                var distance = Vector3.Distance(sourcePosition, candidate.CurrentPosition);
                var healthRatio = candidate.MaxHealth > 0f
                    ? candidate.CurrentHealth / candidate.MaxHealth
                    : 1f;
                var isInjured = healthRatio < 1f - Mathf.Epsilon;

                if (candidate == attacker)
                {
                    selfTarget = candidate;
                }

                if (isInjured
                    && IsBetterLowestHealthAllyCandidate(
                        candidate.CurrentHealth,
                        healthRatio,
                        distance,
                        lowestInjuredHealth,
                        lowestInjuredRatio,
                        bestInjuredDistance))
                {
                    lowestInjuredHealth = candidate.CurrentHealth;
                    lowestInjuredRatio = healthRatio;
                    bestInjuredDistance = distance;
                    bestInjured = candidate;
                }

                if (!allowHealthyFallback || isInjured || candidate == attacker || distance >= nearestHealthyDistance)
                {
                    continue;
                }

                nearestHealthyDistance = distance;
                nearestHealthyAlly = candidate;
            }

            return bestInjured ?? (allowHealthyFallback ? nearestHealthyAlly ?? selfTarget : null);
        }

        private static bool IsBetterLowestHealthAllyCandidate(
            float currentHealth,
            float healthRatio,
            float distance,
            float bestCurrentHealth,
            float bestHealthRatio,
            float bestDistance)
        {
            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
        }

        private static bool IsPotentialEnemyTarget(RuntimeHero attacker, RuntimeHero candidate)
        {
            return attacker != null
                && candidate != null
                && !candidate.IsDead
                && candidate.Side != attacker.Side
                && candidate.CanBeDirectTargeted;
        }

        private static bool IsPotentialAllyTarget(RuntimeHero attacker, RuntimeHero candidate)
        {
            return attacker != null
                && candidate != null
                && !candidate.IsDead
                && candidate.Side == attacker.Side
                && candidate.CanBeDirectTargeted;
        }

        private static bool IsWithinRange(Vector3 sourcePosition, RuntimeHero target, float range)
        {
            return target != null && Vector3.Distance(sourcePosition, target.CurrentPosition) <= Mathf.Max(0f, range);
        }

        private static RuntimeBasicAttackBounceChain CreateBounceChain(ResolvedBasicAttack resolvedAttack)
        {
            if (resolvedAttack == null || !resolvedAttack.HasBounce)
            {
                return null;
            }

            return new RuntimeBasicAttackBounceChain(
                resolvedAttack.MaxAdditionalBounceTargets,
                resolvedAttack.BounceSearchRadius,
                resolvedAttack.BouncePowerMultiplier,
                resolvedAttack.ProjectileSpeed,
                resolvedAttack.EffectType,
                resolvedAttack.TargetType,
                resolvedAttack.OnHitStatusEffects,
                resolvedAttack.VariantKey,
                resolvedAttack.VisualFormKey,
                resolvedAttack.BounceVariantKey);
        }

        private static void TryContinueBounceChain(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero currentTarget,
            RuntimeBasicAttackBounceChain bounceChain)
        {
            if (context == null
                || attacker == null
                || currentTarget == null
                || bounceChain == null
                || bounceChain.IsCompleted)
            {
                return;
            }

            if (!bounceChain.HasRemainingBounces)
            {
                CompleteBounceChain(context, attacker, bounceChain);
                return;
            }

            var nextTarget = SelectBounceTarget(context, attacker, currentTarget, bounceChain);
            if (nextTarget == null)
            {
                CompleteBounceChain(context, attacker, bounceChain);
                return;
            }

            bounceChain.ConsumeBounce();
            var impactAmount = bounceChain.EffectType == BasicAttackEffectType.Heal
                ? HealResolver.ResolveHealAmount(attacker, bounceChain.PowerMultiplier)
                : DamageResolver.ResolveDamage(
                    attacker.AttackPower,
                    attacker.CriticalChance,
                    attacker.CriticalDamageMultiplier,
                    nextTarget.Defense,
                    context.RandomService,
                    bounceChain.PowerMultiplier);
            LaunchBounceProjectile(context, attacker, sourceProxy, currentTarget.CurrentPosition, nextTarget, impactAmount, bounceChain);
        }

        private static RuntimeHero SelectBounceTarget(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeHero anchorTarget,
            RuntimeBasicAttackBounceChain bounceChain)
        {
            if (context?.Heroes == null
                || attacker == null
                || anchorTarget == null
                || bounceChain == null
                || bounceChain.SearchRadius <= Mathf.Epsilon)
            {
                return null;
            }

            return bounceChain.TargetType == BasicAttackTargetType.LowestHealthAlly
                ? SelectLowestHealthBounceAllyTarget(context.Heroes, attacker, anchorTarget.CurrentPosition, bounceChain)
                : bounceChain.TargetType == BasicAttackTargetType.MissingOnHitStatusOrExpiringAlly
                    ? SelectStatusCoverageBounceAllyTarget(context.Heroes, attacker, anchorTarget.CurrentPosition, bounceChain)
                    : SelectNearestBounceEnemyTarget(context.Heroes, attacker, anchorTarget.CurrentPosition, bounceChain);
        }

        private static RuntimeHero SelectNearestBounceEnemyTarget(
            System.Collections.Generic.IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero attacker,
            Vector3 anchorPosition,
            RuntimeBasicAttackBounceChain bounceChain)
        {
            RuntimeHero bestTarget = null;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsPotentialEnemyTarget(attacker, candidate)
                    || bounceChain.HasAlreadyHit(candidate)
                    || !IsWithinRange(anchorPosition, candidate, bounceChain.SearchRadius)
                    || !CanExecuteBounceAgainstTarget(attacker, candidate, bounceChain))
                {
                    continue;
                }

                var distance = Vector3.Distance(anchorPosition, candidate.CurrentPosition);
                if (distance >= bestDistance)
                {
                    continue;
                }

                bestDistance = distance;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private static RuntimeHero SelectLowestHealthBounceAllyTarget(
            System.Collections.Generic.IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero attacker,
            Vector3 anchorPosition,
            RuntimeBasicAttackBounceChain bounceChain)
        {
            RuntimeHero bestTarget = null;
            var bestHealth = float.MaxValue;
            var bestRatio = float.MaxValue;
            var bestDistance = float.MaxValue;
            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsPotentialAllyTarget(attacker, candidate)
                    || bounceChain.HasAlreadyHit(candidate)
                    || !IsWithinRange(anchorPosition, candidate, bounceChain.SearchRadius)
                    || !CanExecuteBounceAgainstTarget(attacker, candidate, bounceChain))
                {
                    continue;
                }

                var distance = Vector3.Distance(anchorPosition, candidate.CurrentPosition);
                var healthRatio = candidate.MaxHealth > 0f
                    ? candidate.CurrentHealth / candidate.MaxHealth
                    : 1f;
                if (!IsBetterLowestHealthAllyCandidate(
                        candidate.CurrentHealth,
                        healthRatio,
                        distance,
                        bestHealth,
                        bestRatio,
                        bestDistance))
                {
                    continue;
                }

                bestHealth = candidate.CurrentHealth;
                bestRatio = healthRatio;
                bestDistance = distance;
                bestTarget = candidate;
            }

            return bestTarget;
        }

        private static bool CanExecuteBounceAgainstTarget(RuntimeHero attacker, RuntimeHero target, RuntimeBasicAttackBounceChain bounceChain)
        {
            if (target == null || bounceChain == null)
            {
                return false;
            }

            if (ShouldRejectPositiveBasicAttackTarget(attacker, target, bounceChain.EffectType, bounceChain.OnHitStatusEffects))
            {
                return false;
            }

            return AllowsHealthyHealBounceAnchor(bounceChain)
                || CanApplyEffectToTarget(target, bounceChain.EffectType);
        }

        private static bool AllowsHealthyHealBounceAnchor(RuntimeBasicAttackBounceChain bounceChain)
        {
            return bounceChain != null
                && bounceChain.EffectType == BasicAttackEffectType.Heal
                && (bounceChain.TargetType == BasicAttackTargetType.LowestHealthAlly
                    || bounceChain.TargetType == BasicAttackTargetType.MissingOnHitStatusOrExpiringAlly);
        }

        private static void LaunchBounceProjectile(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            Vector3 startPosition,
            RuntimeHero target,
            float impactAmount,
            RuntimeBasicAttackBounceChain bounceChain)
        {
            if (context == null || attacker == null || target == null || bounceChain == null)
            {
                return;
            }

            var projectileId = $"basic_attack_{projectileSequence++}";
            var variantKey = string.IsNullOrWhiteSpace(bounceChain.BounceVariantKey)
                ? bounceChain.PrimaryVariantKey
                : bounceChain.BounceVariantKey;
            var projectile = new RuntimeBasicAttackProjectile(
                projectileId,
                attacker,
                sourceProxy,
                target,
                startPosition,
                bounceChain.ProjectileSpeed,
                impactAmount,
                bounceChain.EffectType,
                variantKey,
                bounceChain.VisualFormKey,
                bounceChain.TargetType,
                null,
                bounceChain.OnHitStatusEffects,
                bounceChain,
                bounceChain.BounceHitCount + 1);

            context.Projectiles.Add(projectile);
            context.EventBus.Publish(new BasicAttackProjectileLaunchedEvent(projectile));
        }

        private static void CompleteBounceChain(BattleContext context, RuntimeHero attacker, RuntimeBasicAttackBounceChain bounceChain)
        {
            if (context?.EventBus == null || attacker == null || bounceChain == null || bounceChain.IsCompleted)
            {
                return;
            }

            bounceChain.MarkCompleted();
            context.EventBus.Publish(new BasicAttackBounceChainResolvedEvent(
                attacker,
                bounceChain.ChainId,
                bounceChain.BounceHitCount,
                bounceChain.TotalHitCount,
                bounceChain.FirstTarget,
                bounceChain.LastTarget));
        }

        private static void LaunchProjectile(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero target,
            float impactAmount,
            ResolvedBasicAttack resolvedAttack,
            RuntimeBasicAttackBounceChain bounceChain,
            int bounceHopIndex)
        {
            var projectileId = $"basic_attack_{projectileSequence++}";
            var projectile = new RuntimeBasicAttackProjectile(
                projectileId,
                attacker,
                sourceProxy,
                target,
                resolvedAttack.LaunchPosition,
                resolvedAttack.ProjectileSpeed,
                impactAmount,
                resolvedAttack.EffectType,
                resolvedAttack.VariantKey,
                resolvedAttack.VisualFormKey,
                resolvedAttack.TargetType,
                resolvedAttack.SameTargetStacking,
                resolvedAttack.OnHitStatusEffects,
                bounceChain,
                bounceHopIndex);

            context.Projectiles.Add(projectile);
            context.EventBus.Publish(new BasicAttackProjectileLaunchedEvent(projectile));
        }

        private static void ResolveHit(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero target,
            float impactAmount,
            BasicAttackEffectType effectType,
            BasicAttackTargetType targetType,
            BasicAttackSameTargetStackData sameTargetStacking,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects,
            string variantKey,
            RuntimeBasicAttackBounceChain bounceChain,
            int bounceHopIndex,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (!IsValidTarget(attacker, target, effectType, targetType, onHitStatusEffects))
            {
                return;
            }

            bounceChain?.TryRegisterHit(target, bounceHopIndex > 0);

            if (effectType == BasicAttackEffectType.Heal)
            {
                if (ShouldRejectPositiveBasicAttackTarget(attacker, target, effectType, onHitStatusEffects))
                {
                    PublishPositiveEffectRejected(context, attacker, target, "Heal", variantKey);
                    ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                    TryContinueBounceChain(context, attacker, sourceProxy, target, bounceChain);
                    return;
                }

                var actualHeal = target.ApplyHealing(impactAmount);
                if (actualHeal <= 0f)
                {
                    ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                    TryContinueBounceChain(context, attacker, sourceProxy, target, bounceChain);
                    return;
                }

                BattleStatsSystem.RecordHealingContribution(context, attacker, target, actualHeal);
                context.EventBus.Publish(new HealAppliedEvent(attacker, target, actualHeal, null, target.CurrentHealth, variantKey, sourceProxy));
                ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                TryContinueBounceChain(context, attacker, sourceProxy, target, bounceChain);
                return;
            }

            var actualDamage = BattleDamageSystem.ApplyResolvedDamage(
                context,
                battleCallbacks,
                attacker,
                target,
                impactAmount,
                DamageSourceKind.BasicAttack,
                null,
                variantKey,
                sourceProxy);
            TryRecordSameTargetBasicAttackHit(attacker, sourceProxy, target, effectType, sameTargetStacking, bounceHopIndex);
            TryApplyBasicAttackOnHitEffect(context, battleCallbacks, attacker, sourceProxy, target, effectType, variantKey, bounceHopIndex);
            if (actualDamage <= 0f)
            {
                ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                TryContinueBounceChain(context, attacker, sourceProxy, target, bounceChain);
                return;
            }

            ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
            TryContinueBounceChain(context, attacker, sourceProxy, target, bounceChain);
        }

        private static void TryRecordSameTargetBasicAttackHit(
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero target,
            BasicAttackEffectType effectType,
            BasicAttackSameTargetStackData sameTargetStacking,
            int bounceHopIndex)
        {
            if (attacker == null
                || sourceProxy != null
                || target == null
                || target.IsDead
                || effectType != BasicAttackEffectType.Damage
                || bounceHopIndex != 0
                || !IsSameTargetStackingEnabled(sameTargetStacking))
            {
                return;
            }

            attacker.RecordSameTargetBasicAttackHit(target, sameTargetStacking);
        }

        private static void TryApplyBasicAttackOnHitEffect(
            BattleContext context,
            IBattleSimulationCallbacks battleCallbacks,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero target,
            BasicAttackEffectType effectType,
            string variantKey,
            int bounceHopIndex)
        {
            var onHitEffect = attacker?.Definition?.basicAttack?.onHitEffect;
            if (context == null
                || battleCallbacks == null
                || attacker == null
                || attacker.IsDead
                || sourceProxy != null
                || target == null
                || target.Side == attacker.Side
                || !target.CanBeDirectTargeted
                || effectType != BasicAttackEffectType.Damage
                || bounceHopIndex != 0
                || onHitEffect == null
                || !onHitEffect.HasAnyEffect)
            {
                return;
            }

            var bonusDamageMultiplier = Mathf.Max(0f, onHitEffect.bonusDamagePowerMultiplier);
            var selfHealBaseMultiplier = Mathf.Max(0f, onHitEffect.selfHealBasePowerMultiplier);
            var selfHealMissingHealthMultiplier = Mathf.Max(0f, onHitEffect.selfHealMissingHealthPowerMultiplier);
            if (attacker.TryGetCurrentBasicAttackOnHitOverride(
                    out var overrideSourceSkill,
                    out var overrideBonusDamageMultiplier,
                    out var overrideSelfHealBaseMultiplier,
                    out var overrideSelfHealMissingHealthMultiplier))
            {
                bonusDamageMultiplier = overrideBonusDamageMultiplier;
                selfHealBaseMultiplier = overrideSelfHealBaseMultiplier;
                selfHealMissingHealthMultiplier = overrideSelfHealMissingHealthMultiplier;
            }

            var sourceSkill = ResolveBasicAttackOnHitSourceSkill(attacker, overrideSourceSkill);
            ApplyBasicAttackSelfHealthCost(context, attacker, onHitEffect, sourceSkill, variantKey);
            ApplyBasicAttackOnHitBonusDamage(
                context,
                battleCallbacks,
                attacker,
                target,
                sourceSkill,
                variantKey,
                bonusDamageMultiplier);
            ApplyBasicAttackOnHitSelfHeal(
                context,
                attacker,
                sourceSkill,
                variantKey,
                selfHealBaseMultiplier,
                selfHealMissingHealthMultiplier);
        }

        private static SkillData ResolveBasicAttackOnHitSourceSkill(RuntimeHero attacker, SkillData overrideSourceSkill)
        {
            var activeSkill = attacker?.Definition?.activeSkill;
            return activeSkill != null ? activeSkill : overrideSourceSkill;
        }

        private static void ApplyBasicAttackSelfHealthCost(
            BattleContext context,
            RuntimeHero attacker,
            BasicAttackOnHitEffectData onHitEffect,
            SkillData sourceSkill,
            string variantKey)
        {
            if (context == null
                || attacker == null
                || attacker.IsDead
                || onHitEffect == null
                || onHitEffect.selfCurrentHealthCostRatio <= Mathf.Epsilon)
            {
                return;
            }

            var healthCost = attacker.CurrentHealth * Mathf.Clamp01(onHitEffect.selfCurrentHealthCostRatio);
            var actualCost = attacker.ApplyHealthCost(healthCost, Mathf.Max(0f, onHitEffect.minimumSelfHealthAfterCost));
            if (actualCost <= Mathf.Epsilon)
            {
                return;
            }

            context.EventBus?.Publish(new SelfHealthCostAppliedEvent(
                attacker,
                actualCost,
                sourceSkill,
                attacker.CurrentHealth,
                variantKey));
        }

        private static void ApplyBasicAttackOnHitBonusDamage(
            BattleContext context,
            IBattleSimulationCallbacks battleCallbacks,
            RuntimeHero attacker,
            RuntimeHero target,
            SkillData sourceSkill,
            string variantKey,
            float bonusDamageMultiplier)
        {
            if (context == null
                || battleCallbacks == null
                || attacker == null
                || attacker.IsDead
                || target == null
                || target.IsDead
                || bonusDamageMultiplier <= Mathf.Epsilon)
            {
                return;
            }

            var bonusDamage = DamageResolver.ResolveRawDamage(attacker.AttackPower * bonusDamageMultiplier, target.Defense);
            BattleDamageSystem.ApplyResolvedDamage(
                context,
                battleCallbacks,
                attacker,
                target,
                bonusDamage,
                DamageSourceKind.Skill,
                sourceSkill,
                variantKey);
        }

        private static void ApplyBasicAttackOnHitSelfHeal(
            BattleContext context,
            RuntimeHero attacker,
            SkillData sourceSkill,
            string variantKey,
            float selfHealBaseMultiplier,
            float selfHealMissingHealthMultiplier)
        {
            if (context == null
                || attacker == null
                || attacker.IsDead
                || (selfHealBaseMultiplier <= Mathf.Epsilon && selfHealMissingHealthMultiplier <= Mathf.Epsilon))
            {
                return;
            }

            var missingHealthRatio = attacker.MaxHealth > Mathf.Epsilon
                ? Mathf.Clamp01((attacker.MaxHealth - attacker.CurrentHealth) / attacker.MaxHealth)
                : 0f;
            var healMultiplier = Mathf.Max(0f, selfHealBaseMultiplier)
                + missingHealthRatio * Mathf.Max(0f, selfHealMissingHealthMultiplier);
            var healAmount = attacker.AttackPower * healMultiplier;
            var actualHeal = attacker.ApplyHealing(healAmount);
            if (actualHeal <= Mathf.Epsilon)
            {
                return;
            }

            BattleStatsSystem.RecordHealingContribution(context, attacker, attacker, actualHeal);
            context.EventBus?.Publish(new HealAppliedEvent(
                attacker,
                attacker,
                actualHeal,
                sourceSkill,
                attacker.CurrentHealth,
                variantKey));
        }

        private static bool IsSameTargetStackingEnabled(BasicAttackSameTargetStackData stacking)
        {
            return stacking != null
                && stacking.enabled
                && stacking.maxStacks > 0
                && stacking.magnitudePerStack > Mathf.Epsilon;
        }

        private static bool CanApplyEffectToTarget(RuntimeHero target, BasicAttackEffectType effectType)
        {
            if (target == null)
            {
                return false;
            }

            return effectType != BasicAttackEffectType.Heal
                || target.CurrentHealth < target.MaxHealth - Mathf.Epsilon;
        }

        private static bool IsValidTarget(
            RuntimeHero attacker,
            RuntimeHero target,
            BasicAttackEffectType effectType,
            BasicAttackTargetType targetType,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects)
        {
            if (attacker == null || target == null || target.IsDead || !target.CanBeDirectTargeted)
            {
                return false;
            }

            var targetMatches = targetType == BasicAttackTargetType.LowestHealthAlly
                || targetType == BasicAttackTargetType.MissingOnHitStatusOrExpiringAlly
                ? target.Side == attacker.Side
                : target.Side != attacker.Side;
            if (!targetMatches)
            {
                return false;
            }

            return effectType != BasicAttackEffectType.Heal || target.Side == attacker.Side;
        }

        private static RuntimeHero SelectStatusCoverageBounceAllyTarget(
            System.Collections.Generic.IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero attacker,
            Vector3 anchorPosition,
            RuntimeBasicAttackBounceChain bounceChain)
        {
            return SelectStatusCoverageAllyTarget(
                heroes,
                attacker,
                anchorPosition,
                bounceChain.SearchRadius,
                bounceChain.EffectType,
                bounceChain.OnHitStatusEffects,
                fallbackAttack: null,
                allowHealthyFallback: true,
                bounceChain);
        }

        private static RuntimeHero SelectStatusCoverageAllyTarget(
            System.Collections.Generic.IReadOnlyList<RuntimeHero> heroes,
            RuntimeHero attacker,
            Vector3 sourcePosition,
            float selectionRange,
            BasicAttackEffectType effectType,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects,
            ResolvedBasicAttack fallbackAttack,
            bool allowHealthyFallback,
            RuntimeBasicAttackBounceChain bounceChain = null)
        {
            if (heroes == null || attacker == null)
            {
                return null;
            }

            var hasCoverageStatuses = HasTrackableCoverageStatuses(onHitStatusEffects);
            if (!hasCoverageStatuses)
            {
                if (fallbackAttack != null)
                {
                    return SelectLowestHealthAllyTarget(
                        heroes,
                        attacker,
                        sourcePosition,
                        fallbackAttack,
                        selectionRange,
                        allowHealthyFallback);
                }

                return SelectLowestHealthBounceAllyTarget(heroes, attacker, sourcePosition, bounceChain);
            }

            RuntimeHero bestMissing = null;
            var bestMissingStatusCount = int.MinValue;
            var bestMissingHealth = float.MaxValue;
            var bestMissingRatio = float.MaxValue;
            var bestMissingDistance = float.MaxValue;

            RuntimeHero bestCovered = null;
            var bestCoveredRemainingDuration = float.MaxValue;
            var bestCoveredHealth = float.MaxValue;
            var bestCoveredRatio = float.MaxValue;
            var bestCoveredDistance = float.MaxValue;

            for (var i = 0; i < heroes.Count; i++)
            {
                var candidate = heroes[i];
                if (!IsPotentialAllyTarget(attacker, candidate)
                    || !IsWithinRange(sourcePosition, candidate, selectionRange)
                    || ShouldRejectPositiveBasicAttackTarget(attacker, candidate, effectType, onHitStatusEffects))
                {
                    continue;
                }

                var distance = Vector3.Distance(sourcePosition, candidate.CurrentPosition);
                var healthRatio = candidate.MaxHealth > 0f
                    ? candidate.CurrentHealth / candidate.MaxHealth
                    : 1f;
                var missingStatusCount = CountMissingCoverageStatuses(candidate, attacker, onHitStatusEffects, out var shortestRemainingDuration);

                if (missingStatusCount > 0)
                {
                    if (!IsBetterMissingCoverageCandidate(
                            missingStatusCount,
                            candidate.CurrentHealth,
                            healthRatio,
                            distance,
                            bestMissingStatusCount,
                            bestMissingHealth,
                            bestMissingRatio,
                            bestMissingDistance))
                    {
                        continue;
                    }

                    bestMissingStatusCount = missingStatusCount;
                    bestMissingHealth = candidate.CurrentHealth;
                    bestMissingRatio = healthRatio;
                    bestMissingDistance = distance;
                    bestMissing = candidate;
                    continue;
                }

                if (!IsBetterExpiringCoverageCandidate(
                        shortestRemainingDuration,
                        candidate.CurrentHealth,
                        healthRatio,
                        distance,
                        bestCoveredRemainingDuration,
                        bestCoveredHealth,
                        bestCoveredRatio,
                        bestCoveredDistance))
                {
                    continue;
                }

                bestCoveredRemainingDuration = shortestRemainingDuration;
                bestCoveredHealth = candidate.CurrentHealth;
                bestCoveredRatio = healthRatio;
                bestCoveredDistance = distance;
                bestCovered = candidate;
            }

            return bestMissing ?? bestCovered;
        }

        private static bool HasTrackableCoverageStatuses(System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects)
        {
            if (onHitStatusEffects == null)
            {
                return false;
            }

            for (var i = 0; i < onHitStatusEffects.Count; i++)
            {
                var status = onHitStatusEffects[i];
                if (status != null && status.effectType != StatusEffectType.None)
                {
                    return true;
                }
            }

            return false;
        }

        private static int CountMissingCoverageStatuses(
            RuntimeHero target,
            RuntimeHero attacker,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects,
            out float shortestRemainingDuration)
        {
            shortestRemainingDuration = float.MaxValue;
            if (target == null || attacker == null || onHitStatusEffects == null)
            {
                return 0;
            }

            var missingCount = 0;
            for (var i = 0; i < onHitStatusEffects.Count; i++)
            {
                var status = onHitStatusEffects[i];
                if (status == null || status.effectType == StatusEffectType.None)
                {
                    continue;
                }

                if (StatusEffectSystem.CountMatchingStatuses(target, status, attacker) <= 0)
                {
                    missingCount++;
                    continue;
                }

                if (StatusEffectSystem.TryGetShortestMatchingStatusRemainingDuration(target, status, attacker, out var remainingDuration))
                {
                    shortestRemainingDuration = Mathf.Min(shortestRemainingDuration, remainingDuration);
                }
            }

            if (shortestRemainingDuration == float.MaxValue)
            {
                shortestRemainingDuration = 0f;
            }

            return missingCount;
        }

        private static bool IsBetterMissingCoverageCandidate(
            int missingStatusCount,
            float currentHealth,
            float healthRatio,
            float distance,
            int bestMissingStatusCount,
            float bestCurrentHealth,
            float bestHealthRatio,
            float bestDistance)
        {
            if (missingStatusCount > bestMissingStatusCount)
            {
                return true;
            }

            if (missingStatusCount != bestMissingStatusCount)
            {
                return false;
            }

            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
        }

        private static bool IsBetterExpiringCoverageCandidate(
            float remainingDuration,
            float currentHealth,
            float healthRatio,
            float distance,
            float bestRemainingDuration,
            float bestCurrentHealth,
            float bestHealthRatio,
            float bestDistance)
        {
            if (remainingDuration < bestRemainingDuration - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(remainingDuration - bestRemainingDuration) > Mathf.Epsilon)
            {
                return false;
            }

            if (currentHealth < bestCurrentHealth - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(currentHealth - bestCurrentHealth) > Mathf.Epsilon)
            {
                return false;
            }

            if (healthRatio < bestHealthRatio - Mathf.Epsilon)
            {
                return true;
            }

            if (Mathf.Abs(healthRatio - bestHealthRatio) > Mathf.Epsilon)
            {
                return false;
            }

            return distance < bestDistance;
        }

        private static void ApplyOnHitStatuses(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeHero target,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects,
            string variantKey)
        {
            if (context?.EventBus == null
                || target == null
                || target.IsDead
                || onHitStatusEffects == null)
            {
                return;
            }

            for (var i = 0; i < onHitStatusEffects.Count; i++)
            {
                var status = onHitStatusEffects[i];
                if (status == null)
                {
                    continue;
                }

                if (ShouldRejectPositiveBasicAttackStatus(attacker, target, status))
                {
                    PublishPositiveEffectRejected(context, attacker, target, status.effectType.ToString(), variantKey);
                    continue;
                }

                var previousShield = status.effectType == StatusEffectType.Shield
                    ? StatusEffectSystem.GetTotalShield(target)
                    : 0f;
                if (!target.ApplyStatusEffect(status, attacker, null, attacker, out var appliedStatus))
                {
                    continue;
                }

                var appliedSource = appliedStatus?.Source ?? attacker;
                BattleStatsSystem.RecordStatusContribution(context, appliedSource, target, status);
                if (status.effectType == StatusEffectType.Shield)
                {
                    var shieldDelta = Mathf.Max(0f, StatusEffectSystem.GetTotalShield(target) - previousShield);
                    BattleStatsSystem.RecordShieldContribution(context, appliedSource, target, shieldDelta);
                }

                context.EventBus.Publish(new StatusAppliedEvent(
                    appliedSource,
                    target,
                    status.effectType,
                    status.durationSeconds,
                    appliedStatus?.Magnitude ?? status.magnitude,
                    null,
                    appliedStatus?.AppliedBy ?? attacker));
            }
        }

        private static bool ShouldRejectPositiveBasicAttackTarget(
            RuntimeHero attacker,
            RuntimeHero target,
            BasicAttackEffectType effectType,
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects)
        {
            if (attacker == null
                || target == null
                || attacker == target
                || attacker.Side != target.Side
                || target.CanReceivePositiveEffectsFrom(attacker))
            {
                return false;
            }

            return effectType == BasicAttackEffectType.Heal
                || StatusEffectSystem.HasPositiveStatusEffect(onHitStatusEffects);
        }

        private static bool ShouldRejectPositiveBasicAttackStatus(RuntimeHero attacker, RuntimeHero target, StatusEffectData status)
        {
            return attacker != null
                && target != null
                && attacker != target
                && attacker.Side == target.Side
                && !target.CanReceivePositiveEffectsFrom(attacker)
                && StatusEffectSystem.IsPositiveStatusEffect(status);
        }

        private static void PublishPositiveEffectRejected(BattleContext context, RuntimeHero attacker, RuntimeHero target, string effectLabel, string variantKey)
        {
            context?.EventBus?.Publish(new PositiveEffectRejectedEvent(
                attacker,
                target,
                effectLabel,
                null,
                variantKey));
        }
    }
}
