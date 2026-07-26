/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家输入读取器，负责读取当前帧移动输入                      
 * │  类    名: PlayerInputReader.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.Input
{
    public class PlayerInputReader
    {
        private PlayerContext m_context;
        private PlayerInputActions m_inputActions;

        // 初始化玩家输入读取器
        public void Init(PlayerContext context)
        {
            m_context = context;
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
            m_context.IsSprintPressed = m_inputActions.Player.Sprint.IsPressed();
            m_context.IsJumpPressed = m_inputActions.Player.Jump.WasPressedThisFrame();
            m_context.LookInput = m_inputActions.Player.Look.ReadValue<Vector2>();
            m_context.TargetViewMode = m_inputActions.Player.SwitchToFirstPerson.WasPressedThisFrame()
                ? PlayerViewMode.FirstPerson
                : m_inputActions.Player.SwitchToThirdPerson.WasPressedThisFrame()
                    ? PlayerViewMode.ThirdPerson
                    : null;
        }
    }
}
