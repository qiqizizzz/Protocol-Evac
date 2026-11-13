/*
* ┌──────────────────────────────────┐
* │  描    述: 背包测试工装, 运行时造配置并分配隔离用的容器号与物品号
* │  类    名: QInventoryTestKit.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Reflection;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Loot;
using NUnit.Framework;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 背包测试工装：ItemRegistry 是静态字典且没有清空入口, 所以每个用例领取独立编号来保证互不污染
    /// </summary>
    internal sealed class QInventoryTestKit
    {
        private const int START_CONTAINER_ID = 9000;
        private const int START_ITEM_ID = 90000;

        private static readonly BindingFlags m_fieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;
        private static int m_nextContainerId = START_CONTAINER_ID;
        private static int m_nextItemId = START_ITEM_ID;

        private readonly List<ScriptableObject> m_createdConfigs = new List<ScriptableObject>();

        // 领取一个只在本用例内有效的容器号
        public int NewContainerId() => m_nextContainerId++;

        /// <summary>
        /// 造一份运行时物品配置并登记进 ItemRegistry
        /// </summary>
        /// <param name="width">占格宽</param>
        /// <param name="height">占格高</param>
        /// <param name="canRotate">是否允许旋转</param>
        /// <param name="maxStackCount">最大堆叠</param>
        /// <param name="unitWeight">单件重量</param>
        /// <param name="unitValue">单件估价</param>
        /// <param name="rarity">稀有度</param>
        /// <param name="container">装备后撑起的容器定义, 留空代表这件物品不是容器</param>
        /// <param name="equipSlot">可穿戴到的部位, 装备槽只收对应部位的装备</param>
        /// <param name="register">是否顺手登记进 ItemRegistry, 验总配置整批登记时传 false</param>
        /// <param name="itemType">物品种类</param>
        public InventoryItemConfigSO CreateItem(int width, int height, bool canRotate = true, int maxStackCount = 1,
            float unitWeight = 0f, int unitValue = 0, InventoryItemRarity rarity = InventoryItemRarity.None,
            InventoryContainerConfigSO container = null, InventoryEquipSlot equipSlot = InventoryEquipSlot.None,
            bool register = true, InventoryItemType itemType = InventoryItemType.None)
        {
            int itemId = m_nextItemId++;
            InventoryItemConfigSO config = CreateConfig<InventoryItemConfigSO>();

            // 配置字段是 [SerializeField] 私有的, 没有公开 setter, 只能反射写入
            SetField(config, "ItemIdValue", itemId);
            SetField(config, "DisplayNameValue", "TEST_" + itemId);
            SetField(config, "ItemTypeValue", itemType);
            SetField(config, "CanRotateValue", canRotate);
            SetField(config, "MaxStackCountValue", maxStackCount);
            SetField(config, "UnitWeightValue", unitWeight);
            SetField(config, "GridWidthValue", width);
            SetField(config, "GridHeightValue", height);
            SetField(config, "RarityValue", rarity);
            SetField(config, "UnitValueValue", unitValue);
            SetField(config, "ContainerValue", container);
            SetField(config, "EquipSlotValue", equipSlot);

            // register 传 false 造出「表里有但没登记」的物品, 用来验总配置能否整批登记
            if (register) ItemRegistry.Register(config);
            return config;
        }

        /// <summary>
        /// 造一件容器类装备, 占格是它躺在别人格子里的大小, 容器是它穿戴后撑起的网格
        /// </summary>
        /// <param name="container">穿戴后撑起的容器定义</param>
        /// <param name="occupyWidth">躺在别人格子里时的占格宽</param>
        /// <param name="occupyHeight">躺在别人格子里时的占格高</param>
        /// <param name="unitValue">单件估价</param>
        /// <param name="rarity">稀有度</param>
        /// <param name="equipSlot">可穿戴到的部位, 穿进装备槽的用例必须填对部位</param>
        public InventoryItemConfigSO CreateContainerItem(InventoryContainerConfigSO container,
            int occupyWidth = 3, int occupyHeight = 3, int unitValue = 0,
            InventoryItemRarity rarity = InventoryItemRarity.None, InventoryEquipSlot equipSlot = InventoryEquipSlot.None)
        {
            return CreateItem(occupyWidth, occupyHeight, false, 1, 0f, unitValue, rarity, container, equipSlot);
        }

        /// <summary>
        /// 造一份运行时容器定义
        /// </summary>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        /// <param name="displayName">容器显示名</param>
        /// <param name="rows">异形形状行, # 实心 . 空洞, 传 null 代表满格矩形</param>
        public InventoryContainerConfigSO CreateContainer(int width, int height, string displayName = "TEST_CONTAINER",
            string[] rows = null)
        {
            InventoryContainerConfigSO container = CreateConfig<InventoryContainerConfigSO>();

            SetField(container, "DisplayNameValue", displayName);
            SetField(container, "WidthValue", width);
            SetField(container, "HeightValue", height);

            if (rows != null) SetField(container, "RowsValue", rows);
            return container;
        }

        /// <summary>
        /// 造一份运行时穿戴表, 三个穿戴槽传物品, 口袋槽直接传容器定义
        /// </summary>
        /// <param name="backpack">背包槽穿着的物品, 传 null 代表没穿</param>
        /// <param name="rig">胸挂槽穿着的物品, 传 null 代表没穿</param>
        /// <param name="pockets">口袋的固有容器定义, 传 null 代表不注册口袋</param>
        /// <param name="safeBox">安全箱槽穿着的物品, 传 null 代表没穿</param>
        /// <param name="safeBoxBase">安全箱卸下型号后的常驻底格, 传 null 代表安全箱不常驻</param>
        public InventoryLoadoutConfigSO CreateLoadout(InventoryItemConfigSO backpack,
            InventoryItemConfigSO rig = null, InventoryContainerConfigSO pockets = null,
            InventoryItemConfigSO safeBox = null, InventoryContainerConfigSO safeBoxBase = null)
        {
            InventoryLoadoutConfigSO loadout = CreateConfig<InventoryLoadoutConfigSO>();

            SetField(loadout, "BackpackValue", backpack);
            SetField(loadout, "RigValue", rig);
            SetField(loadout, "PocketsValue", pockets);
            SetField(loadout, "SafeBoxValue", safeBox);
            SetField(loadout, "SafeBoxBaseValue", safeBoxBase);
            return loadout;
        }

        /// <summary>
        /// 造一份运行时背包总配置, 底色不写就用脚本默认值
        /// </summary>
        /// <param name="loadout">要挂上去的穿戴表, 传 null 代表总配置里没有穿戴信息</param>
        /// <param name="items">物品表内容, 传 null 代表留一张空表</param>
        public InventoryConfigSO CreateInventoryConfig(InventoryLoadoutConfigSO loadout = null,
            InventoryItemConfigSO[] items = null)
        {
            InventoryConfigSO config = CreateConfig<InventoryConfigSO>();
            SetField(config, "LoadoutValue", loadout);
            SetField(config, "ItemsValue", items ?? new InventoryItemConfigSO[0]);
            return config;
        }

        /// <summary>
        /// 造一条战利品掉落条目
        /// </summary>
        /// <param name="item">掉落的物品配置</param>
        /// <param name="weight">抽取权重, 传 0 表示不参与抽取</param>
        /// <param name="countMin">单次命中的最少件数</param>
        /// <param name="countMax">单次命中的最多件数</param>
        public LootEntry CreateLootEntry(InventoryItemConfigSO item, int weight = 1, int countMin = 1, int countMax = 1)
        {
            LootEntry entry = new LootEntry();

            SetField(entry, "ItemValue", item);
            SetField(entry, "WeightValue", weight);
            SetField(entry, "CountMinValue", countMin);
            SetField(entry, "CountMaxValue", countMax);
            return entry;
        }

        /// <summary>
        /// 造一份野外物资箱配置
        /// </summary>
        /// <param name="displayName">物资箱显示名</param>
        /// <param name="container">物资箱自身的网格定义</param>
        /// <param name="rollCountMin">每次开箱的最少抽取次数</param>
        /// <param name="rollCountMax">每次开箱的最多抽取次数</param>
        /// <param name="entries">掉落表</param>
        public LootContainerConfigSO CreateLoot(string displayName, InventoryContainerConfigSO container,
            int rollCountMin, int rollCountMax, params LootEntry[] entries)
        {
            LootContainerConfigSO loot = CreateConfig<LootContainerConfigSO>();

            SetField(loot, "DisplayNameValue", displayName);
            SetField(loot, "ContainerValue", container);
            SetField(loot, "RollCountMinValue", rollCountMin);
            SetField(loot, "RollCountMaxValue", rollCountMax);
            SetField(loot, "EntriesValue", entries ?? new LootEntry[0]);
            return loot;
        }

        /// <summary>
        /// 覆盖总配置里某一档稀有度底色, 用来验证底色确实取自配置而不是写死
        /// </summary>
        /// <param name="config">背包总配置</param>
        /// <param name="rarity">要改的稀有度档位</param>
        /// <param name="color">新的物品块底板色</param>
        public void SetRarityColor(InventoryConfigSO config, InventoryItemRarity rarity, Color color)
        {
            // 总配置底色字段名固定是「档位名 + ColorValue」, 按档位直接拼
            SetField(config, rarity + "ColorValue", color);
        }

        // 按配置造一个运行时实例
        public InventoryItemInstance CreateInstance(InventoryItemConfigSO config, int stackCount = 1)
            => new InventoryItemInstance(config, stackCount);

        // 销毁本用例造出来的配置, 不在编辑器里堆临时资产
        public void Dispose()
        {
            for (int i = m_createdConfigs.Count - 1; i >= 0; i--)
            {
                ScriptableObject config = m_createdConfigs[i];
                m_createdConfigs.RemoveAt(i);
                if (config == null) continue;

                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private T CreateConfig<T>() where T : ScriptableObject
        {
            T config = ScriptableObject.CreateInstance<T>();
            m_createdConfigs.Add(config);
            return config;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, m_fieldFlags);
            Assert.IsNotNull(field, "配置字段缺失: " + fieldName);

            field.SetValue(target, value);
        }
    }
}
