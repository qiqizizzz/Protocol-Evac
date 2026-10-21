/*
 * ┌──────────────────────────────────┐
 * │  描    述: 视图基类，接管绑定作用域与视图模型租约的完整生命周期
 * │  类    名: ViewBase.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.View.Interfaces;
using Framework.QTower.MVVM.ViewModel;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    /// <summary>
    /// 视图基类：业务面板只写 CreateViewModel 与 BindViewModel，作用域与租约由基类统一驱动
    /// </summary>
    /// <typeparam name="TViewModel">本面板对应的视图模型</typeparam>
    public abstract class ViewBase<TViewModel> : MonoBehaviour, IView where TViewModel : class, IViewModel
    {
        private BindingScope m_scope;
        private ViewModelLease m_lease;
        private TViewModel m_viewModel;

        private bool m_isVisible;

        public TViewModel ViewModel => m_viewModel;
        public BindingScope Scope => m_scope;
        public bool IsVisible => m_isVisible;
        public bool IsBound => m_lease != null && !m_lease.IsDisposed && m_viewModel != null && m_viewModel.IsBound;

        /// <summary>
        /// 首次显示时由框架调用，子类在此创建视图模型
        /// </summary>
        protected abstract TViewModel CreateViewModel();

        /// <summary>
        /// 绑定入口，子类只写 scope.Bind 系列，不接触三态与租约
        /// </summary>
        /// <param name="viewModel">已接上的视图模型</param>
        protected abstract void BindViewModel(TViewModel viewModel);

        #region 生命周期
        private void Awake()
        {
            EnsureScope();
            OnAwake();
        }

        // 组件初始化后的扩展点，取节点引用放这里
        protected virtual void OnAwake()
        {
        }

        protected virtual void OnDestroy()
        {
            // 拆除顺序由租约固定：收敛绑定 -> OnUnbind -> OnDispose -> Dispose
            m_lease?.Dispose();
            m_lease = null;
            m_viewModel = null;

            OnViewDestroyed();
        }

        // 视图模型已被框架拆除后的扩展点
        protected virtual void OnViewDestroyed()
        {
        }
        #endregion

        #region 窗口动作
        /// <summary>
        /// 显示面板：首次调用完成视图模型创建与绑定，之后调用只重放已有绑定
        /// </summary>
        /// <param name="args">打开参数</param>
        public void Show(params object[] args)
        {
            if (!EnsureViewModel()) return;

            // 上一次 Close 已解绑, 先让视图模型回到绑定态, 显示时再由作用域重放绑定条目
            if (!m_viewModel.IsBound)
                m_scope.NotifyReopened();

            SetVisible(true);

            OnShow(args);
        }

        /// <summary>
        /// 隐藏面板：只断显示，视图模型保持绑定态，状态原地保留
        /// </summary>
        public void Hide()
        {
            if (!m_isVisible) return;

            SetVisible(false);
            OnHide();
        }

        /// <summary>
        /// 关闭面板：视图模型解绑，实例与绑定条目保留供下次 Show 复用
        /// </summary>
        /// <param name="args">关闭参数</param>
        public void Close(params object[] args)
        {
            OnClose(args);

            if (m_scope != null)
                m_scope.NotifyClosed();

            SetVisible(false);
            OnHide();
        }
        #endregion

        #region 派生扩展点
        protected virtual void OnShow(params object[] args)
        {
        }

        protected virtual void OnHide()
        {
        }

        protected virtual void OnClose(params object[] args)
        {
        }

        // 显隐动作，子类可覆写以接入淡入淡出等表现
        protected virtual void SetVisible(bool isVisible)
        {
            m_isVisible = isVisible;
            gameObject.SetActive(isVisible);
        }
        #endregion

        #region 内部函数
        // 作用域惰性获取：物体在隐藏状态下被创建时 Awake 会延后, 显示前必须兜住
        private BindingScope EnsureScope()
        {
            if (m_scope != null) return m_scope;

            m_scope = BindingScope.GetOrCreate(gameObject);
            return m_scope;
        }

        // 首次显示创建视图模型并挂上租约, 失败时已报错, 返回 false 让调用方直接中止
        private bool EnsureViewModel()
        {
            if (EnsureScope() == null)
            {
                QLog.Error($"显示失败：{GetType().Name} 的绑定作用域为空");
                return false;
            }

            if (m_lease != null)
            {
                if (m_lease.IsDisposed)
                {
                    QLog.Error($"显示失败：{GetType().Name} 的租约已拆除，实例不可复用，请重新创建面板");
                    return false;
                }

                return true;
            }

            TViewModel created = CreateViewModel();
            if (created == null)
            {
                QLog.Error($"显示失败：{GetType().Name} 未创建出视图模型");
                return false;
            }

            m_viewModel = created;
            m_lease = m_scope.Bind(created);
            if (m_lease == null)
            {
                m_viewModel = null;
                return false;
            }

            BindViewModel(created);
            return true;
        }
        #endregion

        /// <summary>
        /// 按路径取组件，路径缺失时在编辑器下直接报错
        /// </summary>
        /// <param name="path">相对本面板的节点路径</param>
        protected T FindComponent<T>(string path) where T : Component
        {
            Transform node = transform.Find(path);
            if (node == null)
            {
                QLog.Error($"节点未找到：{path}，请检查 {gameObject.name} 的层级");
                return null;
            }

            T component = node.GetComponent<T>();
            if (component == null)
                QLog.Error($"组件未找到：{path} 上没有 {typeof(T).Name}");

            return component;
        }
    }
}
