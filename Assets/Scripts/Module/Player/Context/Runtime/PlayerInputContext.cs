/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家输入运行时上下文，保存连续输入与离散输入请求
 * │  类    名: PlayerInputContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using Module.Player.Input.Buffer;
using UnityEngine;

namespace Module.Player.Context.Runtime
{
    public sealed class PlayerInputContext : IPlayerRuntimeContext
    {
        private bool m_isLockOnToggleRequested;

        public PlayerInputBuffer Buffer { get; }
        public Vector2 MoveInput { get; set; }
        public Vector2 LookInput { get; set; }
        public bool IsSprintPressed { get; set; }
        public bool IsInputLocked { get; set; }

        // 创建输入运行时上下文
        public PlayerInputContext()
        {
            Buffer = new PlayerInputBuffer();
            Reset();
        }

        // 重置输入运行时数据
        public void Reset()
        {
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            IsSprintPressed = false;
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
    }
}
