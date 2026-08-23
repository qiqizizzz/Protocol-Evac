/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体障碍物组件，保存静态障碍的空间数据
 * │  类    名: EnemyCrowdObstacleComponent.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Module.Enemy.Crowd.ECS
{
    public sealed class EnemyCrowdObstacleComponent : ECSComponent
    {
        public Vector3 Position;
        public float Radius;
        public float AvoidanceWeight = 1f;

        public override int ComType
        {
            get { return EnemyCrowdComponentType.Obstacle; }
        }

        // 重置障碍物组件数据
        public override void Recycle()
        {
            base.Recycle();
            Position = Vector3.zero;
            Radius = 0f;
            AvoidanceWeight = 1f;
        }
    }
}
