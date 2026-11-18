/*
* ┌──────────────────────────────────┐
* │  描    述: 物品详情视图模型, 按物品类型汇总价值、规格、专属属性与描述
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
        public IReadOnlyReactiveProperty<string> Value => m_value;
        public IReadOnlyReactiveProperty<string> Specs => m_specs;
        public IReadOnlyReactiveProperty<string> DetailEntries => m_detailEntries;
        public IReadOnlyReactiveProperty<string> Description => m_description;
        public IReadOnlyReactiveProperty<bool> HasContent => m_hasContent;
        public IReadOnlyReactiveProperty<Sprite> Icon => m_icon;
        public IReadOnlyReactiveProperty<Color> AccentColor => m_accentColor;
        public ISignal HideRequested => m_hideRequested;

        private readonly IInventoryModel m_model;
        private readonly ReactiveProperty<string> m_title;
        private readonly ReactiveProperty<string> m_tags;
        private readonly ReactiveProperty<string> m_value;
        private readonly ReactiveProperty<string> m_specs;
        private readonly ReactiveProperty<string> m_detailEntries;
        private readonly ReactiveProperty<string> m_description;
        private readonly ReactiveProperty<bool> m_hasContent;
        private readonly ReactiveProperty<Sprite> m_icon;
        private readonly ReactiveProperty<Color> m_accentColor;
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
            m_value = CreateProperty(string.Empty);
            m_specs = CreateProperty(string.Empty);
            m_detailEntries = CreateProperty(string.Empty);
            m_description = CreateProperty(string.Empty);
            m_hasContent = CreateProperty(false);
            m_icon = CreateProperty<Sprite>(null);
            m_accentColor = CreateProperty(Color.white);
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

        // 关闭按钮：详情为临时浮层, 关闭时仅隐藏不销毁
        public void Close() => m_hideRequested.Fire();
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
                m_hideRequested.Fire();
                return;
            }

            InventoryItemConfigSO config = ItemRegistry.TryGet(item.ItemId, out InventoryItemConfigSO found)
                ? found
                : null;

            if (config == null) QLog.Error($"物品定义未登记：{item.ItemId}");

            m_title.Set(item.DisplayName);
            m_tags.Set(BuildTags(item, config, vo.DisplayName));
            m_value.Set(FormatNumber(item.UnitValue));
            m_specs.Set(BuildSpecs(item, config));
            string detailEntries = BuildDetailEntries(config);
            string description = config != null ? config.Description : string.Empty;
            m_detailEntries.Set(detailEntries);
            m_description.Set(description);
            m_hasContent.Set(!string.IsNullOrWhiteSpace(detailEntries) || !string.IsNullOrWhiteSpace(description));
            m_icon.Set(item.Icon);
            m_accentColor.Set(RarityColor(item.Rarity));
        }

        // 标签行：类型 · 品质 · 所在容器
        private static string BuildTags(InventoryItemVO item, InventoryItemConfigSO config, string containerName)
        {
            string typeName = config == null
                ? "未知类型"
                : config.IsContainer ? "容器" : TypeName(config.ItemType);
            return $"{typeName} · {RarityName(item.Rarity)} · {containerName}";
        }

        // 规格区只保留占格、重量和旋转, 类型相关属性走独立详情条目
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

            return builder.ToString(0, builder.Length - 1);
        }

        // 把配置里的结构化条目排成详情面板属性区
        private static string BuildDetailEntries(InventoryItemConfigSO config)
        {
            // 贵重收藏品以长描述作为唯一正文, 不展示类型属性
            if (config == null || config.ItemType == InventoryItemType.Valuable ||
                config.DetailEntries.Count == 0)
                return string.Empty;

            StringBuilder builder = new StringBuilder();
            foreach (InventoryItemDetailEntry entry in config.DetailEntries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Label) || string.IsNullOrWhiteSpace(entry.Value))
                    continue;

                builder.Append("<color=#8C9AA7>")
                    .Append(entry.Label)
                    .Append("</color>  ")
                    .Append(entry.Value)
                    .Append('\n');
            }

            return builder.Length > 0 ? builder.ToString(0, builder.Length - 1) : string.Empty;
        }

        private static void AppendSpec(StringBuilder builder, string label, string value)
            => builder.Append(label).Append("  ").Append(value).Append('\n');

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

        // 配置的中文名为 InspectorName, 运行期不可读, 此处另行映射
        private static string TypeName(InventoryItemType type) => type switch
        {
            InventoryItemType.Weapon => "武器",
            InventoryItemType.Ammo => "弹药",
            InventoryItemType.Armor => "护甲",
            InventoryItemType.Medical => "医疗物品",
            InventoryItemType.Valuable => "贵重收藏品",
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
