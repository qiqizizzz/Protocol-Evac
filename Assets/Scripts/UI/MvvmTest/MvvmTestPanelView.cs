/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM验收测试面板，用真实UIBase生命周期验证BindingScope双向往返
 * │  类    名: MvvmTestPanelView.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Binding;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.View;
using Framework.QTower.View;
using TMPro;
using UnityEngine.UI;
using Utils.log;

namespace UI.MvvmTest
{
    /// <summary>
    /// MVVM 验收测试面板：下行只走 BindingScope，上行只走 ViewModel 方法
    /// </summary>
    public sealed class MvvmTestPanelView : UIBase
    {
        private const string TITLE_PATH = "Txt_Title";
        private const string COUNT_PATH = "Txt_Count";
        private const string LOG_PATH = "Txt_Log";
        private const string LOCK_TOGGLE_PATH = "Tgl_Lock";

        private TMP_Text m_titleText;
        private TMP_Text m_countText;
        private TMP_Text m_logText;
        private Toggle m_lockToggle;

        private BindingScope m_scope;
        private ViewModelLease m_lease;
        private MvvmTestViewModel m_viewModel;

        private int m_propHits;
        private int m_signalHits;

        public MvvmTestViewModel ViewModel => m_viewModel;
        public ViewModelLease Lease => m_lease;
        public BindingScope Scope => m_scope;
        public int PropHits => m_propHits;
        public int SignalHits => m_signalHits;

        /// <summary>
        /// 当前面板与作用域的状态快照，供验收探针读取
        /// </summary>
        public string Snapshot()
        {
            string scopeState = m_scope == null ? "null" : $"IsActive={m_scope.IsActive} count={m_scope.ActiveBindingCount}";
            string vmState = m_viewModel == null ? "null" : (m_viewModel.IsBound ? "Bound" : "Unbound");
            string leaseState = m_lease == null ? "null" : $"{m_lease.Ownership}/{(m_lease.IsDisposed ? "Disposed" : "Alive")}";
            return $"[{scopeState}] vm={vmState} lease={leaseState} propHits={m_propHits} signalHits={m_signalHits}";
        }

        protected override void OnInit()
        {
            m_titleText = FindComponent<TMP_Text>(TITLE_PATH);
            m_countText = FindComponent<TMP_Text>(COUNT_PATH);
            m_logText = FindComponent<TMP_Text>(LOG_PATH);
            m_lockToggle = FindComponent<Toggle>(LOCK_TOGGLE_PATH);

            m_viewModel = new MvvmTestViewModel();
            m_scope = BindingScope.GetOrCreate(gameObject);

            // 三态生命周期由租约接管：OnBind 在此触发, 拆除顺序由框架写死
            m_lease = m_scope.Bind(m_viewModel);

            // 下行：走 View 扩展的文本绑定，顺带验证 QTower.MVVM.View 程序集
            m_scope.BindText(m_viewModel.Title, m_titleText);
            m_scope.Bind(m_viewModel.Count, RenderCount);
            m_scope.Bind(m_viewModel.IsLockOn, RenderLockOn);
            m_scope.Bind(m_viewModel.Ping, OnPing);

            // 上行第一阶梯：点击直接交给 ViewModel 方法，View 不持有业务状态
            FindComponent<Button>("Btn_Add").onClick.AddListener(m_viewModel.Increase);
            FindComponent<Button>("Btn_Ping").onClick.AddListener(m_viewModel.RaisePing);
            FindComponent<Button>("Btn_Lock").onClick.AddListener(m_viewModel.ToggleLockOn);

            QLog.Info($"[{nameof(MvvmTestPanelView)}] OnInit 完成 {Snapshot()}");
        }

        // 渲染计数属性，同时刷新观测日志
        private void RenderCount(int count)
        {
            m_propHits++;
            m_countText.text = $"Count = {count}";
            RefreshLog($"RenderCount({count})");
        }

        // 渲染锁定状态，用 SetIsOnWithoutNotify 避免回填触发上行
        private void RenderLockOn(bool isLockOn)
        {
            m_propHits++;
            m_lockToggle.SetIsOnWithoutNotify(isLockOn);
            RefreshLog($"RenderLockOn({isLockOn})");
        }

        // 信号绑定回调，验证订阅与收敛
        private void OnPing()
        {
            m_signalHits++;
            RefreshLog($"Ping 信号 #{m_signalHits}");
        }

        // 刷新面板内的观测文本
        private void RefreshLog(string cause)
        {
            m_logText.text = $"{cause}\n{Snapshot()}";
        }

        protected override void OnOpen(params object[] args)
        {
            QLog.Info($"[{nameof(MvvmTestPanelView)}] OnOpen {Snapshot()}");
        }

        protected override void OnClose(params object[] args)
        {
            // 关闭只停止广播, 绑定条目与视图模型状态都保留, 供下次 Open 复用
            m_scope.NotifyClosed();
            QLog.Info($"[{nameof(MvvmTestPanelView)}] OnClose {Snapshot()}");
        }

        // 收敛顺序(End -> OnUnbind -> OnDispose -> Dispose)由 ViewModelLease 决定, 业务不自己排
        protected override void OnDispose()
        {
            m_lease?.Dispose();
            m_lease = null;
            m_viewModel = null;

            QLog.Info($"[{nameof(MvvmTestPanelView)}] OnDispose 完成");
        }
    }
}