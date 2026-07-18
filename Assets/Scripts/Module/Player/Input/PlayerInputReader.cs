/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家输入读取器，负责读取当前帧移动输入                      
 * │  类    名: PlayerInputReader.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using UnityEngine;

namespace Module.Player.Input
{
    public class PlayerInputReader
    {
        private PlayerContext m_context;
        private PlayerInputActions m_inputActions;

        public void Init(PlayerContext context)
        {
            m_context = context;
            m_inputActions = new PlayerInputActions();
            m_inputActions.Player.Enable();
        }

        public void UnInit()
        {
            m_inputActions.Player.Disable();
            m_inputActions.Dispose();
        }

        public void Tick()
        {
            m_context.MoveInput = m_inputActions.Player.Move.ReadValue<Vector2>();
            m_context.IsSprintPressed = m_inputActions.Player.Sprint.IsPressed();
        }
    }
}