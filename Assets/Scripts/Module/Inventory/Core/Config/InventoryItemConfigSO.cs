/*
* ┌──────────────────────────────────┐
* │  描    述: 背包物品配置数据类
* │  类    名: InventoryItemConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "配置/背包/物品定义配置")]
    public sealed class InventoryItemConfigSO : ScriptableObject
    {
        [Header("基础属性")]
        [Tooltip("全局唯一编号")]
        [SerializeField, Min(1)] private int ItemIdValue;

        [Tooltip("物品显示名")]
        [SerializeField] private string DisplayNameValue = string.Empty;

        [Tooltip("详情面板显示的物品种类")]
        [SerializeField] private string CategoryNameValue = string.Empty;

        [Tooltip("物品类型")]
        [SerializeField] private InventoryItemType ItemTypeValue = InventoryItemType.None;

        [Tooltip("是否允许 90 度旋转")]
        [SerializeField] private bool CanRotateValue = true;

        [Header("堆叠与重量")]
        [Tooltip("最大堆叠数")]
        [SerializeField, Min(1)] private int MaxStackCountValue = 1;

        [Tooltip("单件重量/kg")]
        [SerializeField, Min(0f)] private float UnitWeightValue;

        [Header("占格")]
        [SerializeField, Min(1)] private int GridWidthValue = 1;
        [SerializeField, Min(1)] private int GridHeightValue = 1;

        [Header("品质与价值")]
        [SerializeField] private InventoryItemRarity RarityValue;

        [Tooltip("单件估价, 用于战利品价值统计与阶段二出售结算")]
        [SerializeField, Min(0)] private int UnitValueValue;

        [Header("UI表现")]
        [SerializeField] private Sprite IconValue;

        [Header("详情信息")]
        [Tooltip("物品描述, 留空时详情面板不显示描述区")]
        [SerializeField, TextArea(3, 8)] private string DescriptionValue = string.Empty;

        [Tooltip("按物品种类配置的详情属性, 留空时详情面板不显示属性区")]
        [SerializeField] private InventoryItemDetailEntry[] DetailEntriesValue = Array.Empty<InventoryItemDetailEntry>();

        [Header("容器能力")]
        [Tooltip("装备后撑起的网格定义, 非容器物品留空; 野外物资箱只用容器定义, 不需要这份物品配置")]
        [SerializeField] private InventoryContainerConfigSO ContainerValue;

        [Tooltip("可穿戴到的部位, 装备槽只收对应部位的装备")]
        [SerializeField] private InventoryEquipSlot EquipSlotValue;

        public int ItemId => ItemIdValue;
        public string DisplayName => DisplayNameValue;
        public string CategoryName => CategoryNameValue;
        public InventoryItemType ItemType => ItemTypeValue;
        public bool CanRotate => CanRotateValue;
        public int MaxStackCount => MaxStackCountValue;
        public float UnitWeight => UnitWeightValue;
        public Vector2Int Size => new Vector2Int(GridWidthValue, GridHeightValue);
        public InventoryItemRarity Rarity => RarityValue;
        public int UnitValue => UnitValueValue;
        public Sprite Icon => IconValue;
        public string Description => DescriptionValue;
        public IReadOnlyList<InventoryItemDetailEntry> DetailEntries =>
            DetailEntriesValue ?? Array.Empty<InventoryItemDetailEntry>();
        public bool IsStackable => MaxStackCountValue > 1;

        public InventoryContainerConfigSO Container => ContainerValue;
        public bool IsContainer => ContainerValue != null;
        public InventoryEquipSlot EquipSlot => EquipSlotValue;

        //指定朝下的占格尺寸(用于试算)
        public Vector2Int GetSizeAt(InventoryRotation rotation)
        {
            Vector2Int size = Size;
            return (int)rotation % 2 == 1 ? new Vector2Int(size.y, size.x) : size;
        }
    }
}
