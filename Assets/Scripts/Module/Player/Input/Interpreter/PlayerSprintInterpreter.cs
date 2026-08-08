/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家疾跑输入解释器，负责处理短按闪避与长按疾跑
 * │  类    名: PlayerSprintInterpreter.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Input.Buffer;
using Module.Player.Input.Config;
using UnityEngine;

namespace Module.Player.Input.Interpreter
{
    public sealed class PlayerSprintInterpreter
    {
        private PlayerContext m_context;
        private PlayerInputConfigSO m_inputConfig;
        private float m_sprintPressedTime;
        private bool m_isSprintPressing;

        // 初始化疾跑输入解释器
        public void Init(PlayerContext context, PlayerInputConfigSO inputConfig)
        {
            m_context = context;
            m_inputConfig = inputConfig;
        }

        // 根据本帧 Shift 输入状态更新疾跑与闪避意图
        public void Tick(bool wasPressedThisFrame, bool isPressed, bool wasReleasedThisFrame)
        {
            if (wasPressedThisFrame)
            {
                m_sprintPressedTime = Time.time;
                m_isSprintPressing = true;
            }

            if (wasReleasedThisFrame)
            {
                RecordDodgeIfSprintWasTapped();
                m_context.Input.IsSprintPressed = false;
                m_isSprintPressing = false;
                RefreshAutoRunState();
                return;
            }

            RefreshAutoRunState();
            m_context.Input.IsSprintPressed = m_isSprintPressing
                && isPressed
                && Time.time - m_sprintPressedTime >= GetSprintHoldTime()
                && !HasBackwardMoveInput();
        }

        // Shift 松开时按短按规则写入闪避缓存
        private void RecordDodgeIfSprintWasTapped()
        {
            if (!m_isSprintPressing)
                return;

            if (Time.time - m_sprintPressedTime >= GetSprintHoldTime())
                return;

            m_context.Input.Buffer.Record(PlayerBufferedInputType.Dodge, Time.time);
        }

        // 获取 Shift 长按判定时间
        private float GetSprintHoldTime()
        {
            return m_inputConfig.SprintHoldTime;
        }

        // 刷新自动疾跑的锁存与取消状态
        private void RefreshAutoRunState()
        {
            if (m_context.Input.IsAutoSprint)
            {
                if (HasBackwardMoveInput() || !HasMoveInput())
                    m_context.Input.IsAutoSprint = false;

                return;
            }

            if (m_isSprintPressing
                && Time.time - m_sprintPressedTime >= m_inputConfig.AutoRunHoldTime
                && HasForwardMoveInput())
                m_context.Input.IsAutoSprint = true;
        }

        // 判断当前是否存在任意移动输入
        private bool HasMoveInput()
        {
            return m_context.Input.MoveInput.sqrMagnitude > Mathf.Epsilon;
        }

        // 判断当前是否包含前进输入
        private bool HasForwardMoveInput()
        {
            return m_context.Input.MoveInput.y > 0f;
        }

        // 判断当前是否包含后退输入
        private bool HasBackwardMoveInput()
        {
            return m_context.Input.MoveInput.y < 0f;
        }
    }
}
