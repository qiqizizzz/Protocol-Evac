/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家运行时上下文入口，组合各领域子上下文与玩家根节点
 * │  类    名: PlayerContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Module.Player.Context.Runtime;
using UnityEngine;

namespace Module.Player.Context
{
    public sealed class PlayerContext
    {
        public Transform Transform { get; }
        public PlayerInputContext Input { get; }
        public PlayerMovementContext Movement { get; }
        public PlayerViewContext View { get; }
        public PlayerActionContext Action { get; }
        public PlayerDamageContext Damage { get; }

        // 创建玩家运行时上下文入口
        public PlayerContext(Transform transform)
        {
            Transform = transform;
            Input = new PlayerInputContext();
            Movement = new PlayerMovementContext();
            View = new PlayerViewContext();
            Action = new PlayerActionContext();
            Damage = new PlayerDamageContext();
            ResetRunTimeData();
        }

        // 重置全部玩家运行时数据
        public void ResetRunTimeData()
        {
            Input.Reset();
            Movement.Reset();
            View.Reset();
            View.CameraYaw = Transform.eulerAngles.y;
            Action.Reset();
            Damage.Reset();
        }
    }
}
