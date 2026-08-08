/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家视角运行时上下文，保存视角模式与相机角度
 * │  类    名: PlayerViewContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.Context.Runtime
{
    public sealed class PlayerViewContext : IPlayerRuntimeContext
    {
        public PlayerViewMode ViewMode { get; set; }
        public PlayerViewMode? TargetViewMode { get; set; }
        public float CameraYaw { get; set; }
        public float CameraPitch { get; set; }
        public Transform LockTarget { get; private set; }
        public bool IsLockOn => LockTarget != null;

        // 重置视角运行时数据
        public void Reset()
        {
            ViewMode = PlayerViewMode.FirstPerson;
            TargetViewMode = null;
            CameraYaw = 0f;
            CameraPitch = 0f;
            LockTarget = null;
        }

        // 设置当前锁定目标
        public void SetLockTarget(Transform target)
        {
            LockTarget = target;
        }

        // 清空当前锁定目标
        public void ClearLockTarget()
        {
            LockTarget = null;
        }
    }
}
