/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体避障结果组件，保存修正后的移动方向
 * │  类    名: EnemyCrowdAvoidanceResultComponent.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Module.Enemy.Crowd.ECS
{
    public sealed class EnemyCrowdAvoidanceResultComponent : ECSComponent
    {
        public Vector3 AdjustedDirection;
        public bool HasAdjustedDirection;

        public override int ComType
        {
            get { return EnemyCrowdComponentType.AvoidanceResult; }
        }

        // 写入避障修正后的移动方向
        public void SetAdjustedDirection(Vector3 direction)
        {
            direction.y = 0f;
            AdjustedDirection = direction.normalized;
            HasAdjustedDirection = AdjustedDirection.sqrMagnitude > 0f;
        }

        // 清理避障结果
        public void Clear()
        {
            AdjustedDirection = Vector3.zero;
            HasAdjustedDirection = false;
        }

        // 重置避障结果组件数据
        public override void Recycle()
        {
            base.Recycle();
            Clear();
        }
    }
}
