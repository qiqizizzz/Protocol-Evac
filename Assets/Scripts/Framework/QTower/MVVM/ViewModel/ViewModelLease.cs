/*
 * ┌──────────────────────────────────┐
 * │  描    述: 视图模型租约, 持有视图模型并声明生死归属, 按固定顺序收敛
 * │  类    名: ViewModelLease.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Utils.log;

namespace Framework.QTower.MVVM.ViewModel
{
    /// <summary>
    /// 视图模型的生死的归属
    /// </summary>
    public enum ViewModelOwnership
    {
        // 由本作用域创建, 收敛时负责释放
        Owned,
        // 由外部注入, 收敛时只解绑绝不释放
        External
    }

    /// <summary>
    /// 视图模型租约: 把绑定作用域与视图模型绑成一条收敛链, 拆除顺序由框架写死
    /// </summary>
    public sealed class ViewModelLease
    {
        private readonly BindingScope m_scope;
        private readonly IViewModelLifecycle m_viewModel;

        public IViewModelLifecycle ViewModel => m_viewModel;
        public ViewModelOwnership Ownership { get; }
        public bool IsDisposed { get; private set; }

        internal ViewModelLease(BindingScope scope, IViewModelLifecycle viewModel, ViewModelOwnership ownership)
        {
            m_scope = scope;
            m_viewModel = viewModel;
            Ownership = ownership;
        }

        /// <summary>
        /// 面板关闭: 只通知视图模型停止广播, 绑定条目与租约本身都不动
        /// </summary>
        internal void NotifyClosed() => m_viewModel.OnUnbind();

        /// <summary>
        /// 作用域销毁: 租约就地作废, 但绝不越权释放外部注入的视图模型
        /// </summary>
        internal void MarkDisposed() => IsDisposed = true;

        /// <summary>
        /// 完整拆除, 固定顺序: 先收敛绑定, 再解绑, 最后才轮到释放视图模型
        /// </summary>
        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;

            // 颠倒这一步会让订阅继续向已释放的属性推值, 直接刷出释放告警
            m_scope.End();

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();

            if (Ownership == ViewModelOwnership.Owned)
            {
                m_viewModel.Dispose();
                return;
            }

            QLog.Info($"[{nameof(ViewModelLease)}] External 视图模型仅解绑, 生死由注入方负责：{m_viewModel.GetType().Name}");
        }
    }
}
