/*
* ┌──────────────────────────────────┐
* │  描    述: 物品详情视图模型, 左键点中物品后汇总它的名称、标签与规格文本
* │  类    名: ItemDetailViewModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Globalization;
using System.Text;
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

        public IReadOnlyReactiveProperty<string> Title => m_title;
        public IReadOnlyReactiveProperty<string> Tags => m_tags;
        public IReadOnlyReactiveProperty<string> Specs => m_specs;
        public IReadOnlyReactiveProperty<Sprite> Icon => m_icon;
        public ISignal HideRequested => m_hideRequested;

        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<string> m_title;
        private readonly ReactiveProperty<string> m_tags;
        private readonly ReactiveProperty<string> m_specs;
        private readonly ReactiveProperty<Sprite> m_icon;
        private readonly Signal m_hideRequested;

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
            m_specs = CreateProperty(string.Empty);
            m_icon = CreateProperty<Sprite>(null);
            m_hideRequested = CreateSignal();
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

        //关闭按钮: 详情是随点随开的浮层, 关掉只收起不销毁
        public void Close() => m_hideRequested.Fire();
        #endregion

        #region Private Function
        private void HandleModelChanged(int containerId)
        {
            if (containerId != m_containerId || m_instanceId == NO_ITEM) return;

            Refresh();
        }

        //从模型取最新一件物品, 物品或容器没了就请求收起
        private void Refresh()
        {
            InventoryContainerVO vo = m_model.IsContainerRegistered(m_containerId)
                ? m_model.GetContainerVO(m_containerId)
                : InventoryContainerVO.Empty;

            if (!vo.TryGetItem(m_instanceId, out InventoryItemVO item))
            {
                m_hideRequested.Fire();
                return;
            }

            InventoryItemConfigSO config = ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO found)
                ? found
                : null;

            if (config == null) QLog.Error($"物品定义未登记：{item.ItemId}");

            m_title.Set(item.DisplayName);
            m_tags.Set(BuildTags(item, config, vo.DisplayName));
            m_specs.Set(BuildSpecs(item, config));
            m_icon.Set(item.Icon);
        }

        //标签行: 类型 · 品质 · 所在容器
        private static string BuildTags(InventoryItemVO item, InventoryItemConfigSO config, string containerName)
        {
            string typeName = config != null ? TypeName(config.ItemType) : "未知类型";
            return $"{typeName} · {RarityName(item.Rarity)} · {containerName}";
        }

        //规格行: 一行一项, 缺配置的行直接不写, 不留空白占位
        private static string BuildSpecs(InventoryItemVO item, InventoryItemConfigSO config)
        {
            StringBuilder builder = new StringBuilder();
            AppendSpec(builder, "占格", $"{item.Size.x}x{item.Size.y}");

            if (config != null)
            {
                AppendSpec(builder, "单重", FormatWeight(config.UnitWeight) + " kg");
                AppendSpec(builder, "总重", FormatWeight(config.UnitWeight * item.StackCount) + " kg");
                AppendSpec(builder, "旋转", config.CanRotate ? "可" : "不可");
            }

            AppendSpec(builder, "估价", item.UnitValue.ToString());
            if (item.StackCount > 1) AppendSpec(builder, "小计", item.TotalValue.ToString());

            return builder.ToString(0, builder.Length - 1);
        }

        private static void AppendSpec(StringBuilder builder, string label, string value)
            => builder.Append(label).Append("  ").Append(value).Append('\n');

        private static string FormatWeight(float weight) => weight.ToString("0.##", CultureInfo.InvariantCulture);

        //配置里的中文只在 InspectorName 上, 运行期取不到, 这里自己映射一层
        private static string TypeName(InventoryItemType type) => type switch
        {
            InventoryItemType.Weapon => "武器",
            InventoryItemType.Ammo => "弹药",
            InventoryItemType.Armor => "护甲",
            InventoryItemType.Medical => "医疗物品",
            InventoryItemType.Valuable => "贵重物品",
            InventoryItemType.Food => "食物饮水",
            InventoryItemType.Material => "物资工具",
            InventoryItemType.Attachment => "武器配件",
            _ => "其他",
        };

        private static string RarityName(InventoryItemRarity rarity) => rarity switch
        {
            InventoryItemRarity.Common => "普通",
            InventoryItemRarity.Uncommon => "优秀",
            InventoryItemRarity.Rare => "稀有",
            InventoryItemRarity.Epic => "史诗",
            InventoryItemRarity.Legendary => "传说",
            _ => "未分级",
        };
        #endregion
    }
}
