/*
* ┌──────────────────────────────────┐
* │  描    述: 物品详情视图模型, 汇总分类、价值、重量、占格图与描述
* │  类    名: ItemDetailViewModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Globalization;
using Framework.QTower.MVVM.View;
using Framework.QTower.MVVM.ViewModel;
using Framework.QTower.MVVM.ViewModel.Interfaces;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    public class ItemDetailViewModel : ViewModelBase
    {
        private const int NO_ITEM = -1; //当前没有要看详情物品
        private const int FOOTPRINT_COLUMNS = 6;
        private const int FOOTPRINT_ROWS = 4;
        private const int FOOTPRINT_CELL_COUNT = FOOTPRINT_COLUMNS * FOOTPRINT_ROWS;

        public IReadOnlyReactiveProperty<string> Title => m_title;
        public IReadOnlyReactiveProperty<string> Tags => m_tags;
        public IReadOnlyReactiveProperty<string> Value => m_value;
        public IReadOnlyReactiveProperty<string> WeightLabel => m_weightLabel;
        public IReadOnlyReactiveProperty<ItemFootprintPreviewVO> FootprintPreview => m_footprintPreview;
        public IReadOnlyReactiveProperty<bool> CanShowContainerLayout => m_canShowContainerLayout;
        public IReadOnlyReactiveProperty<string> Description => m_description;
        public IReadOnlyReactiveProperty<bool> HasContent => m_hasContent;
        public IReadOnlyReactiveProperty<Sprite> Icon => m_icon;
        public IReadOnlyReactiveProperty<Color> AccentColor => m_accentColor;
        public ISignal HideRequested => m_hideRequested;
        public ISignal ContainerLayoutRequested => m_containerLayoutRequested;

        public int ContainerId => m_containerId;
        public int InstanceId => m_instanceId;

        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<string> m_title;
        private readonly ReactiveProperty<string> m_tags;
        private readonly ReactiveProperty<string> m_value;
        private readonly ReactiveProperty<string> m_weightLabel;
        private readonly ReactiveProperty<ItemFootprintPreviewVO> m_footprintPreview;
        private readonly ReactiveProperty<bool> m_canShowContainerLayout;
        private readonly ReactiveProperty<string> m_description;
        private readonly ReactiveProperty<bool> m_hasContent;
        private readonly ReactiveProperty<Sprite> m_icon;
        private readonly ReactiveProperty<Color> m_accentColor;
        private readonly Signal m_hideRequested;
        private readonly Signal m_containerLayoutRequested;

        private int m_containerId;
        private int m_instanceId = NO_ITEM;

        public ItemDetailViewModel() : this(MvvmRoot.Instance.Models.Get<IInventoryModel>())
        {
        }

        public ItemDetailViewModel(IInventoryModel model)
        {
            m_model = model;

            m_title = CreateProperty(string.Empty);
            m_tags = CreateProperty(string.Empty);
            m_value = CreateProperty(string.Empty);
            m_weightLabel = CreateProperty(string.Empty);
            m_footprintPreview = CreateProperty(ItemFootprintPreviewVO.Empty);
            m_canShowContainerLayout = CreateProperty(false);
            m_description = CreateProperty(string.Empty);
            m_hasContent = CreateProperty(false);
            m_icon = CreateProperty<Sprite>(null);
            m_accentColor = CreateProperty(Color.white);
            m_hideRequested = CreateSignal();
            m_containerLayoutRequested = CreateSignal();
        }

        #region 生命周期
        protected override void OnBindCore()
        {
            m_model.OnChanged += HandleModelChanged;
        }

        protected override void OnUnbindCore()
        {
            m_model.OnChanged -= HandleModelChanged;
        }
        #endregion

        #region Public Function
        /// <summary>
        /// 打开某容器里一件物品的详情
        /// </summary>
        /// <param name="containerId">物品所在容器号</param>
        /// <param name="instanceId">物品实例号</param>
        public void Open(int containerId, int instanceId)
        {
            m_containerId = containerId;
            m_instanceId = instanceId;
            Refresh();
        }

        // 关闭按钮：详情为临时浮层, 关闭时仅隐藏不销毁
        public void Close() => m_hideRequested.Fire();

        // 请求打开当前容器物品的完整内部布局
        public void RequestContainerLayout()
        {
            if (!m_canShowContainerLayout.Value) return;

            m_containerLayoutRequested.Fire();
        }
        #endregion

        #region Private Function
        private void HandleModelChanged(int containerId)
        {
            if (containerId != m_containerId || m_instanceId == NO_ITEM) return;

            Refresh();
        }

        // 从模型读取最新物品, 物品或容器不存在时请求隐藏
        private void Refresh()
        {
            InventoryContainerVO vo = m_model.IsContainerRegistered(m_containerId)
                ? m_model.GetContainerVO(m_containerId)
                : InventoryContainerVO.Empty;

            if (!vo.TryGetItem(m_instanceId, out InventoryItemVO item))
            {
                m_canShowContainerLayout.Set(false);
                m_hideRequested.Fire();
                return;
            }

            InventoryItemConfigSO config = ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO found)
                ? found
                : null;

            if (config == null) QLog.Error($"物品定义未登记：{item.ItemId}");

            m_title.Set(item.DisplayName);
            m_tags.Set(BuildTags(config));
            m_value.Set(FormatNumber(item.UnitValue));
            m_weightLabel.Set(config != null ? FormatWeight(config.UnitWeight) + "KG" : string.Empty);
            m_footprintPreview.Set(BuildFootprintPreview(item));
            m_canShowContainerLayout.Set(config != null && config.IsContainer && config.Container != null);
            string description = config != null ? config.Description : string.Empty;
            m_description.Set(description);
            m_hasContent.Set(!string.IsNullOrWhiteSpace(description));
            m_icon.Set(item.Icon);
            m_accentColor.Set(RarityColor(item.Rarity));
        }

        // 标签只显示配置好的物品种类
        private static string BuildTags(InventoryItemConfigSO config)
            => config != null ? config.CategoryName : string.Empty;

        // 把物品自身物理占格裁切到固定 6x4 预览
        private static ItemFootprintPreviewVO BuildFootprintPreview(InventoryItemVO item)
        {
            int width = Math.Max(1, item.Size.x);
            int height = Math.Max(1, item.Size.y);

            bool[] solidCells = new bool[FOOTPRINT_CELL_COUNT];

            int visibleWidth = Math.Min(width, FOOTPRINT_COLUMNS);
            int visibleHeight = Math.Min(height, FOOTPRINT_ROWS);
            for (int y = 0; y < visibleHeight; y++)
            {
                for (int x = 0; x < visibleWidth; x++)
                {
                    int previewIndex = y * FOOTPRINT_COLUMNS + x;
                    solidCells[previewIndex] = true;
                }
            }

            return new ItemFootprintPreviewVO(false, visibleWidth, visibleHeight, solidCells,
                Array.Empty<InventorySlotEdgeKind>(), Array.Empty<InventorySlotEdgeKind>(),
                Array.Empty<InventorySlotEdgeKind>(), Array.Empty<InventorySlotEdgeKind>());
        }

        private static string FormatWeight(float weight) => weight.ToString("0.##", CultureInfo.InvariantCulture);
        private static string FormatNumber(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

        private static Color RarityColor(InventoryItemRarity rarity) => rarity switch
        {
            InventoryItemRarity.Common => new Color(0.68f, 0.72f, 0.76f, 1f),
            InventoryItemRarity.Uncommon => new Color(0.34f, 0.78f, 0.48f, 1f),
            InventoryItemRarity.Rare => new Color(0.28f, 0.62f, 1f, 1f),
            InventoryItemRarity.Epic => new Color(0.72f, 0.46f, 1f, 1f),
            InventoryItemRarity.Legendary => new Color(1f, 0.62f, 0.22f, 1f),
            _ => new Color(0.58f, 0.64f, 0.7f, 1f),
        };
        #endregion
    }
}
