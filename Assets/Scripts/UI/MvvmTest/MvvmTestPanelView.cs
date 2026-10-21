/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM验收测试面板，只写创建模型与绑定，生命周期全部交给ViewBase
 * │  类    名: MvvmTestPanelView.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.View;
using TMPro;
using UnityEngine.UI;
using Utils.log;

namespace UI.MvvmTest
{
    /// <summary>
    /// MVVM 验收测试面板：下行只走 BindingScope，上行只走 ViewModel 方法
    /// </summary>
    public sealed class MvvmTestPanelView : ViewBase<MvvmTestViewModel>
    {
        private const string TITLE_PATH = "Txt_Title";
        private const string COUNT_PATH = "Txt_Count";
        private const string LOG_PATH = "Txt_Log";
        private const string LOCK_TOGGLE_PATH = "Tgl_Lock";
        private const string ADD_BUTTON_PATH = "Btn_Add";
        private const string PING_BUTTON_PATH = "Btn_Ping";
        private const string LOCK_BUTTON_PATH = "Btn_Lock";

        private TMP_Text m_titleText;
        private TMP_Text m_countText;
        private TMP_Text m_logText;
        private Toggle m_lockToggle;

        private Button m_addButton;
        private Button m_pingButton;
        private Button m_lockButton;

        private int m_propHits;
        private int m_signalHits;

        public int PropHits => m_propHits;
        public int SignalHits => m_signalHits;

        /// <summary>
        /// 当前面板与作用域的状态快照，供验收探针读取
        /// </summary>
        public string Snapshot()
        {
            string scopeState = Scope == null ? "scope=null" : $"IsActive={Scope.IsActive} count={Scope.ActiveBindingCount}";
            string viewModelState = ViewModel == null ? "vm=null" : (ViewModel.IsBound ? "Bound" : "Unbound");
            return $"[{scopeState}] {viewModelState} visible={IsVisible} propHits={m_propHits} signalHits={m_signalHits}";
        }

        // 视图模型由 View 负责创建, 生死交给作用域的租约
        protected override MvvmTestViewModel CreateViewModel() => new MvvmTestViewModel();

        protected override void OnAwake()
        {
            m_titleText = FindComponent<TMP_Text>(TITLE_PATH);
            m_countText = FindComponent<TMP_Text>(COUNT_PATH);
            m_logText = FindComponent<TMP_Text>(LOG_PATH);
            m_lockToggle = FindComponent<Toggle>(LOCK_TOGGLE_PATH);
            m_addButton = FindComponent<Button>(ADD_BUTTON_PATH);
            m_pingButton = FindComponent<Button>(PING_BUTTON_PATH);
            m_lockButton = FindComponent<Button>(LOCK_BUTTON_PATH);
        }

        // 绑定入口：只登记下行绑定与上行入口, 不接触三态与拆除
        protected override void BindViewModel(MvvmTestViewModel viewModel)
        {
            Scope.BindText(viewModel.Title, m_titleText);
            Scope.Bind(viewModel.Count, RenderCount);
            Scope.Bind(viewModel.IsLockOn, RenderLockOn);
            Scope.Bind(viewModel.Ping, OnPing);

            m_addButton.onClick.AddListener(viewModel.Increase);
            m_pingButton.onClick.AddListener(viewModel.RaisePing);
            m_lockButton.onClick.AddListener(viewModel.ToggleLockOn);

            QLog.Info($"[{nameof(MvvmTestPanelView)}] BindViewModel 完成 {Snapshot()}");
        }

        protected override void OnShow(params object[] args)
        {
            QLog.Info($"[{nameof(MvvmTestPanelView)}] OnShow {Snapshot()}");
        }

        protected override void OnClose(params object[] args)
        {
            QLog.Info($"[{nameof(MvvmTestPanelView)}] OnClose {Snapshot()}");
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
    }
}
