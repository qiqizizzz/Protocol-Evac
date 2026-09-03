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
        // 驱动调试面板关联的运行时模块
        void Tick(float deltaTime);

        // 驱动调试面板关联模块的固定步长逻辑
        void FixedTick(float fixedDeltaTime);

        // 驱动调试面板关联模块的表现同步逻辑
        void LateTick(float deltaTime);

        void Refresh();
        void Draw(int activeTabIndex, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle toggleStyle,
            GUIStyle buttonStyle);
    }
}
