/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色视角运行时上下文，保存视角模式与相机角度
 * │  类    名: CharacterViewContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Core.View;
using UnityEngine;

namespace Framework.Character.Runtime.Contexts
{
    public sealed class CharacterViewContext : ICharacterRuntimeContext
    {
        public CharacterViewMode ViewMode { get; set; }
        public CharacterViewMode? TargetViewMode { get; set; }
        public float CameraYaw { get; set; }
        public float CameraPitch { get; set; }
        public Transform LockTarget { get; private set; }
        public bool IsLockOn => LockTarget != null;

        // 重置视角运行时数据
        public void Reset()
        {
            ViewMode = CharacterViewMode.FirstPerson;
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
