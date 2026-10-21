/*
 * ┌──────────────────────────────────┐
 * │  描    述: 视图契约，定义 MVVM 面板对外的显示与关闭动作
 * │  类    名: IView.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.QTower.MVVM.View.Interfaces
{
    /// <summary>
    /// 视图契约：窗口服务只认这个接口，屏蔽具体面板的实现差异
    /// </summary>
    public interface IView
    {
        // 当前是否处于显示状态
        bool IsVisible { get; }

        // 当前是否已接上视图模型
        bool IsBound { get; }

        // 显示面板，首次显示时由框架完成视图模型的创建与绑定
        void Show(params object[] args);

        // 隐藏面板，绑定条目与视图模型状态全部保留
        void Hide();

        // 关闭面板，视图模型解绑但实例保留供下次复用
        void Close(params object[] args);
    }
}
