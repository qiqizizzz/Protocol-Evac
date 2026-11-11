/*
 * ┌──────────────────────────────────┐
 * │  描    述: 物品右键菜单视图, 贴着鼠标摆一张按钮卡片, 点卡片外任意处收起, 拆分与丢弃转交对应处理链路
* │  类    名: UIItemMenuPanel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Cysharp.Threading.Tasks;
using Framework.QTower.MVVM.View;
using UI.Inventory.View.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Inventory.View
{
    public sealed class UIItemMenuPanel : ViewBase<ItemMenuViewModel>
    {
        private const string DISMISS_PATH = "Btn_Dismiss";
        private const string CARD_PATH = "Panel_Menu";
        private const string ROTATE_PATH = CARD_PATH + "/Btn_Rotate";
        private const string SPLIT_PATH = CARD_PATH + "/Btn_Split";
        private const string DROP_PATH = CARD_PATH + "/Btn_Drop";

        // 卡片左上角相对鼠标的偏移量, 与详情面板一致
        private static readonly Vector2 m_cursorOffset = new Vector2(12f, -12f);

        private RectTransform m_rect;
        private RectTransform m_cardRect;
        private Button m_dismissButton;
        private Button m_rotateButton;
        private Button m_splitButton;
        private Button m_dropButton;

        // 拆分弹窗预制体首次加载需等待一帧, 期间的重复请求忽略
        private bool m_splitOpening;

        #region 生命周期
        protected override ItemMenuViewModel CreateViewModel() => new ItemMenuViewModel();

        protected override void OnAwake()
        {
            m_rect = (RectTransform)transform;
            m_cardRect = FindComponent<RectTransform>(CARD_PATH);
            m_dismissButton = FindComponent<Button>(DISMISS_PATH);
            m_rotateButton = FindComponent<Button>(ROTATE_PATH);
            m_splitButton = FindComponent<Button>(SPLIT_PATH);
            m_dropButton = FindComponent<Button>(DROP_PATH);
        }

        protected override void BindViewModel(ItemMenuViewModel viewModel)
        {
            m_dismissButton.onClick.AddListener(viewModel.Dismiss);
            m_rotateButton.onClick.AddListener(viewModel.Rotate);
            m_splitButton.onClick.AddListener(viewModel.Split);
            m_dropButton.onClick.AddListener(viewModel.Drop);

            // 不可旋转时按钮保留但置为不可用, 用于提示该物品不支持旋转
            Scope.Bind(viewModel.CanRotate, interactable => m_rotateButton.interactable = interactable);
            // 不足两份时没有可拆的余量, 同样保留按钮并置灰
            Scope.Bind(viewModel.CanSplit, interactable => m_splitButton.interactable = interactable);
            // 丢弃对任何在容器里的物品都可用, 只有查不到物品时才置灰
            Scope.Bind(viewModel.CanDrop, interactable => m_dropButton.interactable = interactable);
            Scope.Bind(viewModel.SplitRequested, () => ShowSplitAsync(viewModel).Forget());
            // 落点由运行期宿主提供, 视图只把目标容器与实例号交出去
            Scope.Bind(viewModel.DropRequested,
                () => UIInventoryHotkey.TryDropItemAtPlayer(viewModel.TargetContainerId, viewModel.TargetInstanceId));
            Scope.Bind(viewModel.Dismissed, Hide);
        }

        // 每次右键对应不同物品, 可用性与位置均在 OnShow 重新计算
        protected override void OnShow(params object[] args)
        {
            if (!ItemPopupPlacement.TryReadCursorArgs(args, out int containerId, out int instanceId,
                    out Vector3 screenPos))
                return;

            // 菜单卡片位于网格之上, 排序低于面板时旋转按钮无法接收点击
            ItemPopupPlacement.RaiseAbovePanels(gameObject);
            transform.SetAsLastSibling();
            ViewModel.Open(containerId, instanceId);

            // 卡片高度由 VerticalLayoutGroup 计算, 先重建布局再取实际尺寸定位
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_cardRect);
            ItemPopupPlacement.AnchorToCursor(m_rect, m_cardRect, screenPos, m_cursorOffset);
        }
        #endregion

        #region Private Function
        // 菜单收起后按同一目标与鼠标位置打开拆分弹窗
        private async UniTaskVoid ShowSplitAsync(ItemMenuViewModel viewModel)
        {
            if (m_splitOpening) return;
            m_splitOpening = true;

            await MvvmRoot.Instance.Windows.ShowAsync<UIItemSplitPanel>(viewModel.TargetContainerId,
                viewModel.TargetInstanceId, Input.mousePosition);

            m_splitOpening = false;
        }
        #endregion
    }
}
