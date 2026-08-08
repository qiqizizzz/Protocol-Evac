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
                return;
            }

            m_context.Input.IsSprintPressed = m_isSprintPressing
                && isPressed
                && Time.time - m_sprintPressedTime >= GetSprintHoldTime();
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
    }
}
