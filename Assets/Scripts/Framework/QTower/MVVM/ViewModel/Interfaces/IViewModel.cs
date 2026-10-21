/*
 * ┌──────────────────────────────────┐
 * │  描    述: 视图模型契约，在生命周期之上补充可查询的绑定状态
 * │  类    名: IViewModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.ViewModel.Interfaces;

namespace Framework.QTower.MVVM.ViewModel
{
    /// <summary>
    /// 视图模型契约：框架侧只认这个接口，绑定与租约据此拒绝重复绑定与已释放的模型
    /// </summary>
    public interface IViewModel : IViewModelLifecycle
    {
        // 当前是否处于已绑定状态，由框架的 OnBind/OnUnbind 驱动
        bool IsBound { get; }

        // 是否已被拆除，拆除后的模型不允许再次绑定
        bool IsDisposed { get; }
    }
}
