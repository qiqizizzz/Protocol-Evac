/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家输入读取器，负责读取当前帧移动输入                      
 * │  类    名: PlayerInputReader.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.Input;
using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.Input.Buffer;
using UnityEngine;
using Utils.log;

namespace Module.Player.Input
{
    public class PlayerInputReader
    {
        private const float DEFAULT_SPRINT_HOLD_TIME = 0.2f;

        private PlayerContext m_context;
        private PlayerInputConfigSO m_inputConfig;
        private PlayerInputActions m_inputActions;
        private float m_sprintPressedTime;
        private bool m_isSprintPressing;

        // 初始化玩家输入读取器
        public void Init(PlayerContext context, PlayerInputConfigSO inputConfig)
        {
            m_context = context;
            m_inputConfig = inputConfig;
            if (m_inputConfig == null)
                QLog.Error("初始化玩家输入读取器失败：InputConfig 为空");

            m_inputActions = new PlayerInputActions();
            m_inputActions.Player.Enable();
        }

        // 释放玩家输入读取器
        public void UnInit()
        {
            if (m_inputActions == null)
                return;

            m_inputActions.Player.Disable();
            m_inputActions.Dispose();
        }

        // 读取当前帧玩家输入
        public void Tick()
        {
            m_context.MoveInput = m_inputActions.Player.Move.ReadValue<Vector2>();
            updateSprintAndDodgeInput();
            recordBufferedInputs();
            m_context.LookInput = m_inputActions.Player.Look.ReadValue<Vector2>();
            m_context.TargetViewMode = m_inputActions.Player.SwitchToFirstPerson.WasPressedThisFrame()
                ? PlayerViewMode.FirstPerson
                : m_inputActions.Player.SwitchToThirdPerson.WasPressedThisFrame()
                    ? PlayerViewMode.ThirdPerson
                    : null;
        }

        // 记录当前帧可缓存的离散输入
        private void recordBufferedInputs()
        {
            recordBufferedInput(PlayerBufferedInputType.Jump, m_inputActions.Player.Jump.WasPressedThisFrame());
        }

        // 更新 Shift 短按闪避与长按疾跑输入
        private void updateSprintAndDodgeInput()
        {
            if (m_inputActions.Player.Sprint.WasPressedThisFrame())
            {
                m_sprintPressedTime = Time.time;
                m_isSprintPressing = true;
            }

            if (m_inputActions.Player.Sprint.WasReleasedThisFrame())
            {
                recordDodgeIfSprintWasTapped();
                m_context.IsSprintPressed = false;
                m_isSprintPressing = false;
                return;
            }

            m_context.IsSprintPressed = m_isSprintPressing
                && m_inputActions.Player.Sprint.IsPressed()
                && Time.time - m_sprintPressedTime >= getSprintHoldTime();
        }

        // Shift 松开时按短按规则写入闪避缓存
        private void recordDodgeIfSprintWasTapped()
        {
            if (!m_isSprintPressing)
                return;

            if (Time.time - m_sprintPressedTime >= getSprintHoldTime())
                return;

            m_context.InputBuffer.Record(PlayerBufferedInputType.Dodge, Time.time);
        }

        // 获取 Shift 长按判定时间
        private float getSprintHoldTime()
        {
            return m_inputConfig != null ? m_inputConfig.SprintHoldTime : DEFAULT_SPRINT_HOLD_TIME;
        }

        // 按条件写入离散输入缓存
        private void recordBufferedInput(PlayerBufferedInputType inputType, bool wasPressedThisFrame)
        {
            if (!wasPressedThisFrame)
                return;

            m_context.InputBuffer.Record(inputType, Time.time);
        }
    }
}
