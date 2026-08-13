/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 游戏调试模块面板的统一绘制契约
 * │  类    名: IGamePanel.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

namespace Tools.GM
{
    internal interface IGamePanel
    {
        void Refresh();
        void Draw();
    }
}
