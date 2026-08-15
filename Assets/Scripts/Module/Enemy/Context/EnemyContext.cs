/*
 * ┌──────────────────────────────────┐
 * │  描    述: 单个敌人的跨模块运行时事实容器
 * │  类    名: EnemyContext.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Enemy.Config;
using Module.Enemy.Behavior.Config;
using Module.Enemy.Context.Runtime;
using UnityEngine;

namespace Module.Enemy.Context
{
    public sealed class EnemyContext
    {
        public Transform Transform { get; }
        public EnemyStatsConfigSO StatsConfig { get; }
        public EnemyBehaviorConfigSO BehaviorConfig { get; }
        public EnemyActionContext Action { get; }
        public EnemyTargetContext Target { get; }
        public bool IsActive { get; private set; }

        // 创建敌人的运行时上下文
        public EnemyContext(Transform transform, EnemyStatsConfigSO statsConfig, EnemyBehaviorConfigSO behaviorConfig)
        {
            Transform = transform;
            StatsConfig = statsConfig;
            BehaviorConfig = behaviorConfig;
            Action = new EnemyActionContext();
            Target = new EnemyTargetContext();
        }

        // 更新敌人的启用状态
        public void SetActive(bool isActive)
        {
            IsActive = isActive;
        }
    }
}
