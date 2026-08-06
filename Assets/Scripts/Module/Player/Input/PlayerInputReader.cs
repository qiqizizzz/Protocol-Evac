/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家输入读取器，负责读取当前帧移动输入                      
 * │  类    名: PlayerInputReader.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.Input.Buffer;
using Module.Player.Input.Config;
using Module.Player.Input.Interpreter;
using UnityEngine;

namespace Module.Player.Input
{
    public class PlayerInputReader
    {
        private PlayerContext m_context;
        private PlayerInputActions m_inputActions;
        private PlayerSprintInterpreter m_sprintInterpreter;

        // 初始化玩家输入读取器
        public void Init(PlayerContext context, PlayerInputConfigSO inputConfig)
        {
            m_context = context;
            m_inputActions = new PlayerInputActions();
            m_sprintInterpreter = new PlayerSprintInterpreter();
            m_sprintInterpreter.Init(m_context, inputConfig);
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
            //WASD: 移动
            m_context.MoveInput = m_inputActions.Player.Move.ReadValue<Vector2>();
            //Shift: 短按闪避，长按疾跑
            m_sprintInterpreter.Tick(
                m_inputActions.Player.Sprint.WasPressedThisFrame(),
                m_inputActions.Player.Sprint.IsPressed(),
                m_inputActions.Player.Sprint.WasReleasedThisFrame());
            //Space: 跳跃
            RecordBufferedInput(PlayerBufferedInputType.Jump, m_inputActions.Player.Jump.WasPressedThisFrame());
            //鼠标左键: 普通攻击
            RecordBufferedInput(PlayerBufferedInputType.NormalAttack, m_inputActions.Player.Attack.WasPressedThisFrame());
            //鼠标移动: 视角
            m_context.LookInput = m_inputActions.Player.Look.ReadValue<Vector2>();
            //F1/F3: 切换视角
            m_context.TargetViewMode = m_inputActions.Player.SwitchToFirstPerson.WasPressedThisFrame()
                ? PlayerViewMode.FirstPerson
                : m_inputActions.Player.SwitchToThirdPerson.WasPressedThisFrame()
                    ? PlayerViewMode.ThirdPerson
                    : null;
        }

        // 按条件写入离散输入缓存
        private void RecordBufferedInput(PlayerBufferedInputType inputType, bool wasPressedThisFrame)
        {
            if (!wasPressedThisFrame)
                return;

            m_context.InputBuffer.Record(inputType, Time.time);
        }
    }
}
