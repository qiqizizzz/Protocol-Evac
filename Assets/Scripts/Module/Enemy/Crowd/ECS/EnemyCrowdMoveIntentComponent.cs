/*
 * ┌──────────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体移动意图组件，保存导航层输出的期望方向
 * │  类    名: EnemyCrowdMoveIntentComponent.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────┘
 */

using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Module.Enemy.Crowd.ECS
{
    public sealed class EnemyCrowdMoveIntentComponent : ECSComponent
    {
        public Vector3 PreferredDirection;
        public bool HasMoveRequest;

        public override int ComType
        {
            get { return EnemyCrowdComponentType.MoveIntent; }
        }

        // 写入导航层期望移动方向
        public void SetPreferredDirection(Vector3 direction)
        {
            direction.y = 0f;
            PreferredDirection = direction.normalized;
            HasMoveRequest = PreferredDirection.sqrMagnitude > 0f;
        }

        // 清理移动意图
        public void Clear()
        {
            PreferredDirection = Vector3.zero;
            HasMoveRequest = false;
        }

        // 重置移动意图组件数据
        public override void Recycle()
        {
            base.Recycle();
            Clear();
        }
    }
}
