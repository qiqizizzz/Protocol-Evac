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
        public InventoryItemConfigSO CreateItem(int width, int height, bool canRotate = true, int maxStackCount = 1,
            float unitWeight = 0f, int unitValue = 0, InventoryItemRarity rarity = InventoryItemRarity.None)
        {
            int itemId = m_nextItemId++;
            InventoryItemConfigSO config = ScriptableObject.CreateInstance<InventoryItemConfigSO>();
            m_createdConfigs.Add(config);

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
            SetField(config, "BaseColorValue", new Color(0.5f, 0.5f, 0.5f, 1f));

            ItemRegistry.Register(config);
            return config;
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

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = typeof(InventoryItemConfigSO).GetField(fieldName, m_fieldFlags);
            Assert.IsNotNull(field, "配置字段缺失: " + fieldName);

            field.SetValue(target, value);
        }
    }
}
