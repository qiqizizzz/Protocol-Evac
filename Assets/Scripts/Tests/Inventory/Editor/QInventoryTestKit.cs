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
        /// <param name="register">是否顺手登记进 ItemRegistry, 验总配置整批登记时传 false</param>
        public InventoryItemConfigSO CreateItem(int width, int height, bool canRotate = true, int maxStackCount = 1,
            float unitWeight = 0f, int unitValue = 0, InventoryItemRarity rarity = InventoryItemRarity.None,
            InventoryContainerConfigSO container = null, bool register = true)
        {
            int itemId = m_nextItemId++;
            InventoryItemConfigSO config = CreateConfig<InventoryItemConfigSO>();

            // 配置字段是 [SerializeField] 私有的, 没有公开 setter, 只能反射写入
            SetField(config, "ItemIdValue", itemId);
            SetField(config, "DisplayNameValue", "TEST_" + itemId);
            SetField(config, "ItemTypeValue", InventoryItemType.None);
            SetField(config, "CanRotateValue", canRotate);
            SetField(config, "MaxStackCountValue", maxStackCount);
            SetField(config, "UnitWeightValue", unitWeight);
            SetField(config, "GridWidthValue", width);
            SetField(config, "GridHeightValue", height);
            SetField(config, "RarityValue", rarity);
            SetField(config, "UnitValueValue", unitValue);
            SetField(config, "ContainerValue", container);

            // register 传 false 造出「表里有但没登记」的物品, 用来验总配置能否整批登记
            if (register) ItemRegistry.Register(config);
            return config;
        }

        // 造一件容器类装备, 占格是它躺在别人格子里的大小, 容器是它穿戴后撑起的网格
        public InventoryItemConfigSO CreateContainerItem(InventoryContainerConfigSO container,
            int occupyWidth = 3, int occupyHeight = 3, int unitValue = 0,
            InventoryItemRarity rarity = InventoryItemRarity.None)
        {
            return CreateItem(occupyWidth, occupyHeight, false, 1, 0f, unitValue, rarity, container);
        }

        // 造一份运行时容器定义
        public InventoryContainerConfigSO CreateContainer(int width, int height, string displayName = "TEST_CONTAINER")
        {
            InventoryContainerConfigSO container = CreateConfig<InventoryContainerConfigSO>();

            SetField(container, "DisplayNameValue", displayName);
            SetField(container, "WidthValue", width);
            SetField(container, "HeightValue", height);
            return container;
        }

        /// <summary>
        /// 造一份运行时穿戴表, 三个穿戴槽传物品, 口袋槽直接传容器定义
        /// </summary>
        /// <param name="backpack">背包槽穿着的物品, 传 null 代表没穿</param>
        /// <param name="rig">胸挂槽穿着的物品, 传 null 代表没穿</param>
        /// <param name="pockets">口袋的固有容器定义, 传 null 代表不注册口袋</param>
        /// <param name="safeBox">安全箱槽穿着的物品, 传 null 代表没穿</param>
        public InventoryLoadoutConfigSO CreateLoadout(InventoryItemConfigSO backpack,
            InventoryItemConfigSO rig = null, InventoryContainerConfigSO pockets = null,
            InventoryItemConfigSO safeBox = null)
        {
            InventoryLoadoutConfigSO loadout = CreateConfig<InventoryLoadoutConfigSO>();

            SetField(loadout, "BackpackValue", backpack);
            SetField(loadout, "RigValue", rig);
            SetField(loadout, "PocketsValue", pockets);
            SetField(loadout, "SafeBoxValue", safeBox);
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
