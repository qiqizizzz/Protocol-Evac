/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人目标上下文，保存当前目标与攻击距离事实
 * │  类    名: EnemyTargetContext.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Context.Runtime
{
    public sealed class EnemyTargetContext
    {
        public Transform CurrentTarget { get; private set; }
        public bool IsInAttackRange { get; private set; }
        public bool IsInCombatObserveRange { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        // 更新目标事实并返回行为相关事实是否发生变化
        public bool UpdateTarget(Transform currentTarget, bool isInAttackRange, bool isInCombatObserveRange)
        {
            bool hasChanged = CurrentTarget != currentTarget || IsInAttackRange != isInAttackRange
                || IsInCombatObserveRange != isInCombatObserveRange;
            CurrentTarget = currentTarget;
            IsInAttackRange = isInAttackRange;
            IsInCombatObserveRange = isInCombatObserveRange;
            return hasChanged;
        }

        // 清理当前目标事实
        public void Reset()
        {
            CurrentTarget = null;
            IsInAttackRange = false;
            IsInCombatObserveRange = false;
        }
    }
}
