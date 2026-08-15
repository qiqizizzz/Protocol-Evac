/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人目标读取器，低频刷新 Hero 与攻击距离事实
 * │  类    名: EnemyTargetReader.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using Module.Enemy.Context;
using UnityEngine;

namespace Module.Enemy.Behavior.Readers
{
    public sealed class EnemyTargetReader
    {
        private const string HERO_TAG = "Hero";

        private readonly EnemyContext m_context;
        private readonly float m_refreshInterval;
        private readonly float m_attackDistanceSqr;

        private float m_refreshRemainingTime;

        // 创建敌人目标读取器并缓存行为配置
        public EnemyTargetReader(EnemyContext context)
        {
            m_context = context;
            m_refreshInterval = context.BehaviorConfig.SensorRefreshIntervalValue;
            float attackDistance = context.BehaviorConfig.AttackDistanceValue;
            m_attackDistanceSqr = attackDistance * attackDistance;
        }

        // 按配置间隔刷新目标，并返回行为事实是否发生变化
        public bool Tick(float deltaTime)
        {
            m_refreshRemainingTime -= deltaTime;
            if (m_refreshRemainingTime > 0f)
                return false;

            m_refreshRemainingTime = m_refreshInterval;
            Transform currentTarget = ResolveTarget();
            bool isInAttackRange = currentTarget != null
                && (currentTarget.position - m_context.Transform.position).sqrMagnitude <= m_attackDistanceSqr;
            return m_context.Target.UpdateTarget(currentTarget, isInAttackRange);
        }

        // 优先复用当前目标，目标失效后重新查找 Hero
        private Transform ResolveTarget()
        {
            if (m_context.Target.CurrentTarget != null)
                return m_context.Target.CurrentTarget;

            GameObject hero = GameObject.FindGameObjectWithTag(HERO_TAG);
            return hero != null ? hero.transform : null;
        }
    }
}
