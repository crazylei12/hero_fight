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
                        projectile.OnHitStatusEffects,
                        projectile.VariantKey,
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

            attacker.BeginBasicAttack(target, resolvedAttack, windupSeconds, recoverySeconds, consumeAttackCooldown, isActionSequenceStep);
            context.EventBus.Publish(new AttackPerformedEvent(attacker, target, resolvedAttack.VariantKey));
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

            if (resolvedAttack.UsesProjectile)
            {
                LaunchProjectile(context, attacker, null, target, impactAmount, resolvedAttack);
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
                resolvedAttack.OnHitStatusEffects,
                resolvedAttack.VariantKey,
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
                || proxy?.Owner?.Definition?.basicAttack == null
                || proxy.Owner.IsDead)
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
                proxy.ProjectileSpeed,
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

            if (resolvedAttack.UsesProjectile)
            {
                LaunchProjectile(context, proxy.Owner, proxy, target, impactAmount, resolvedAttack);
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
                resolvedAttack.OnHitStatusEffects,
                resolvedAttack.VariantKey,
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
                0f,
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
            float projectileSpeedOverride,
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
                projectileSpeedOverride,
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
                projectileSpeedOverride,
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
            float projectileSpeedOverride,
            float powerMultiplierScale,
            bool advanceSequenceOnUse)
        {
            var effectType = variant != null ? variant.effectType : basicAttack.effectType;
            var targetType = variant != null ? variant.targetType : basicAttack.targetType;
            var powerMultiplier = variant != null ? variant.powerMultiplier : basicAttack.damageMultiplier;
            var targetPrioritySearchRadius = variant != null ? variant.targetPrioritySearchRadius : basicAttack.targetPrioritySearchRadius;
            var onHitStatusEffects = variant != null ? variant.onHitStatusEffects : basicAttack.onHitStatusEffects;
            var projectileSpeed = projectileSpeedOverride > Mathf.Epsilon ? projectileSpeedOverride : basicAttack.projectileSpeed;

            return new ResolvedBasicAttack(
                variant != null ? variant.variantKey : string.Empty,
                effectType,
                targetType,
                powerMultiplier * Mathf.Max(0f, powerMultiplierScale),
                targetPrioritySearchRadius,
                basicAttack.usesProjectile,
                projectileSpeed,
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

            target = SelectTarget(
                context,
                attacker,
                sourcePosition,
                resolvedAttack,
                selectionRange,
                allowHealthyHealFallback);
            return target != null && CanExecuteAgainstTarget(attacker, target, resolvedAttack);
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

            return resolvedAttack.TargetType switch
            {
                BasicAttackTargetType.LowestHealthAlly => SelectLowestHealthAllyTarget(
                    context.Heroes,
                    attacker,
                    sourcePosition,
                    resolvedAttack,
                    selectionRange,
                    allowHealthyHealFallback),
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
                && resolvedAttack.TargetType == BasicAttackTargetType.LowestHealthAlly;
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

        private static void LaunchProjectile(
            BattleContext context,
            RuntimeHero attacker,
            RuntimeDeployableProxy sourceProxy,
            RuntimeHero target,
            float impactAmount,
            ResolvedBasicAttack resolvedAttack)
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
                resolvedAttack.TargetType,
                resolvedAttack.OnHitStatusEffects);

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
            System.Collections.Generic.IReadOnlyList<StatusEffectData> onHitStatusEffects,
            string variantKey,
            IBattleSimulationCallbacks battleCallbacks)
        {
            if (!IsValidTarget(attacker, target, effectType, targetType, onHitStatusEffects))
            {
                return;
            }

            if (effectType == BasicAttackEffectType.Heal)
            {
                if (ShouldRejectPositiveBasicAttackTarget(attacker, target, effectType, onHitStatusEffects))
                {
                    PublishPositiveEffectRejected(context, attacker, target, "Heal", variantKey);
                    ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                    return;
                }

                var actualHeal = target.ApplyHealing(impactAmount);
                if (actualHeal <= 0f)
                {
                    ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                    return;
                }

                BattleStatsSystem.RecordHealingContribution(context, attacker, target, actualHeal);
                context.EventBus.Publish(new HealAppliedEvent(attacker, target, actualHeal, null, target.CurrentHealth, variantKey, sourceProxy));
                ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
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
            if (actualDamage <= 0f)
            {
                ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
                return;
            }

            ApplyOnHitStatuses(context, attacker, target, onHitStatusEffects, variantKey);
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
                ? target.Side == attacker.Side
                : target.Side != attacker.Side;
            if (!targetMatches)
            {
                return false;
            }

            return effectType != BasicAttackEffectType.Heal || target.Side == attacker.Side;
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
