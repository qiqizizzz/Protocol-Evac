/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色输入运行时上下文，保存连续输入与离散输入请求
 * │  类    名: CharacterInputContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Input;
using UnityEngine;

namespace Framework.Character.Runtime.Contexts
{
    public sealed class CharacterInputContext : ICharacterRuntimeContext
    {
        private bool m_isLockOnToggleRequested;

        public CharacterInputBuffer Buffer { get; }
        public Vector2 MoveInput { get; set; }
        public Vector2 LookInput { get; set; }
        public bool IsSprintPressed { get; set; }
        public bool IsAutoSprint { get; set; }
        public bool IsWalkMode { get; private set; }
        public bool IsSprintActive => IsSprintPressed || IsAutoSprint;
        public bool IsInputLocked { get; set; }

        // 创建角色输入运行时上下文
        public CharacterInputContext()
        {
            Buffer = new CharacterInputBuffer();
            Reset();
        }

        // 重置输入运行时数据
        public void Reset()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            IsSprintPressed = false;
            IsAutoSprint = false;
            IsWalkMode = false;
            IsInputLocked = false;
            m_isLockOnToggleRequested = false;
            Buffer.ClearAll();
        }

        // 请求切换锁定目标状态
        public void RequestLockOnToggle()
        {
            m_isLockOnToggleRequested = true;
        }

        // 消费一次锁定目标切换请求
        public bool ConsumeLockOnToggleRequest()
        {
            bool isRequested = m_isLockOnToggleRequested;
            m_isLockOnToggleRequested = false;
            return isRequested;
        }

        // 切换步行模式并在启用时取消自动疾跑
        public void ToggleWalkMode()
        {
            IsWalkMode = !IsWalkMode;
            if (IsWalkMode)
                IsAutoSprint = false;
        }
    }
}
