/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 敌人群体ECS组件类型定义
 * │  类    名: EnemyCrowdComponentType.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

namespace Module.Enemy.Crowd.ECS
{
    public static class EnemyCrowdComponentType
    {
        public const int Agent = 30001;
        public const int MoveIntent = 30002;
        public const int AvoidanceResult = 30003;
        public const int Obstacle = 30004;
    }
}
