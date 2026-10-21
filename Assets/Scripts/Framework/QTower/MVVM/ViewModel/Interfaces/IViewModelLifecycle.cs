/*
 * ┌──────────────────────────────────┐
 * │  描    述: 视图模型生命周期接口, 定义绑定与收敛的三个状态迁移
 * │  类    名: IViewModelLifecycle.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;

namespace Framework.QTower.MVVM.ViewModel.Interfaces
{
    /// <summary>
    /// 视图模型生命周期: 由 BindingScope.Bind 统一驱动, 业务 View 不直接调用这三个方法
    /// 三个方法都要求幂等, 框架侧只在合法时机各调用一次
    /// </summary>
    public interface IViewModelLifecycle : IDisposable
    {
        // 面板打开并完成绑定后调用, 在此启动订阅与广播
        void OnBind();

        // 面板关闭后调用, 在此停止广播, 但状态需要保留供下次打开复用
        void OnUnbind();

        // 面板销毁时调用, 在此释放自有资源
        void OnDispose();
    }
}
