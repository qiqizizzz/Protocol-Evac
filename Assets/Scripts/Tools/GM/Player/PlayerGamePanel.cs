/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家模块调试面板，显示玩家状态机诊断信息
 * │  类    名: PlayerGamePanel.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Core;
using Module.Player.HFSM;
using UnityEngine;

namespace Tools.GM
{
    internal sealed class PlayerGamePanel : IGamePanel
    {
        private PlayerController m_playerController;
        private string m_currentState = "未找到玩家";
        private string m_nextState = "无";
        private string m_activePath = "无";

        // 刷新玩家状态诊断数据
        public void Refresh()
        {
            if (m_playerController == null)
                m_playerController = Object.FindObjectOfType<PlayerController>();

            if (m_playerController == null)
                return;

            m_currentState = m_playerController.CurrentStateId.ToString();
            m_nextState = m_playerController.NextStateId == PlayerStateId.None
                ? "无"
                : m_playerController.NextStateId.ToString();
            m_activePath = string.Join(" > ", m_playerController.ActiveStatePath);
        }

        // 绘制玩家状态信息
        public void Draw()
        {
            GUILayout.Space(8f);
            GUILayout.Label("Player");
            GUILayout.Label($"当前状态：{m_currentState}");
            GUILayout.Label($"即将转换：{m_nextState}");
            GUILayout.Label($"状态路径：{m_activePath}");
        }
    }
}
