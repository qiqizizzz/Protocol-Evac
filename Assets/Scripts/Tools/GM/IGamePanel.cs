/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 游戏调试模块面板的统一绘制契约
 * │  类    名: IGamePanel.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Tools.GM
{
    internal interface IGamePanel
    {
        void Refresh();
        void Draw(int activeTabIndex, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle toggleStyle,
            GUIStyle buttonStyle);
    }
}
