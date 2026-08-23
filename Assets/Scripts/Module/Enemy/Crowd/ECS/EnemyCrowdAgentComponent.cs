/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体代理组件，保存避障计算所需的空间数据
 * │  类    名: EnemyCrowdAgentComponent.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Module.Enemy.Crowd.ECS
{
    public sealed class EnemyCrowdAgentComponent : ECSComponent
    {
        public Vector3 Position;
        public float Radius;
        public float AvoidanceRadius;
        public float AvoidanceWeight = 1f;

        public override int ComType
        {
            get { return EnemyCrowdComponentType.Agent; }
        }

        // 重置群体代理组件数据
        public override void Recycle()
        {
            base.Recycle();
            Position = Vector3.zero;
            Radius = 0f;
            AvoidanceRadius = 0f;
            AvoidanceWeight = 1f;
        }
    }
}
