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
        private float m_currentHealth;
        private bool m_isGmInvincible;

        // 玩家调试面板没有独立的运行时系统
        public void Tick(float deltaTime)
        {
        }

        // 玩家调试面板没有独立的固定步长运行时系统
        public void FixedTick(float fixedDeltaTime)
        {
        }

        // 玩家调试面板没有独立的表现同步运行时系统
        public void LateTick(float deltaTime)
        {
        }

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
            m_currentHealth = m_playerController.CurrentHealth;
            m_isGmInvincible = m_playerController.IsGmInvincible;
        }

        // 根据当前页签绘制对应的玩家调试内容
        public void Draw(int activeTabIndex, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle toggleStyle,
            GUIStyle buttonStyle, GUIStyle sectionStyle, GUIStyle sectionHeaderStyle, GUIStyle inputStyle,
            GUIStyle statusStyle)
        {
            switch (activeTabIndex)
            {
                case 0:
                    DrawOverview(labelStyle, valueStyle);
                    break;
                case 1:
                    DrawPlayerControls(labelStyle, valueStyle, toggleStyle, buttonStyle);
                    break;
                case 2:
                    DrawStateMachine(labelStyle, valueStyle);
                    break;
            }
        }

        // 绘制玩家运行概览
        private void DrawOverview(GUIStyle labelStyle, GUIStyle valueStyle)
        {
            DrawInfoRow("对象", m_playerController == null ? "未找到玩家" : m_playerController.name, labelStyle, valueStyle);
            DrawInfoRow("生命", m_playerController == null ? "--" : $"{m_currentHealth:F0}", labelStyle, valueStyle);
            DrawInfoRow("无敌", m_isGmInvincible ? "已开启" : "未开启", labelStyle, valueStyle);
            DrawInfoRow("当前状态", m_currentState, labelStyle, valueStyle);
        }

        // 绘制玩家调试控制项
        private void DrawPlayerControls(GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle toggleStyle,
            GUIStyle buttonStyle)
        {
            if (m_playerController == null)
            {
                GUILayout.Label("未找到玩家", valueStyle);
                return;
            }

            DrawInfoRow("当前生命", $"{m_currentHealth:F0}", labelStyle, valueStyle);
            bool isGmInvincible = GUILayout.Toggle(m_isGmInvincible, "无敌模式", toggleStyle);
            if (isGmInvincible != m_isGmInvincible)
            {
                m_playerController.SetGmInvincible(isGmInvincible);
                Refresh();
            }

            GUILayout.Label("免伤害、免受击、免击退", labelStyle);
            if (GUILayout.Button("恢复满血", buttonStyle))
            {
                m_playerController.RestoreFullHealth();
                Refresh();
            }
        }

        // 绘制玩家状态机诊断信息
        private void DrawStateMachine(GUIStyle labelStyle, GUIStyle valueStyle)
        {
            DrawInfoRow("当前状态", m_currentState, labelStyle, valueStyle);
            DrawInfoRow("即将转换", m_nextState, labelStyle, valueStyle);
            GUILayout.Space(8f);
            GUILayout.Label("活动路径", labelStyle);
            GUILayout.Label(m_activePath, valueStyle);
        }

        // 绘制一行键值信息
        private void DrawInfoRow(string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(92f));
            GUILayout.Label(value, valueStyle);
            GUILayout.EndHorizontal();
        }
    }
}
