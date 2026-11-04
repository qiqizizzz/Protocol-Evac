/*
* ┌──────────────────────────────────┐
* │  描    述: 背包系统总配置, 全局唯一的背包参数入口
* │  类    名: InventoryConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using Module.Inventory.Core.Config.Type;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "InventoryConfig", menuName = "配置/背包/总配置")]
    public sealed class InventoryConfigSO : ScriptableObject
    {
        [Header("物品表")]
        [Tooltip("全部可入包的物品定义, 装配时整批登记进 ItemRegistry; 野外物资箱能掉出的物品也要在这里")]
        [SerializeField] private InventoryItemConfigSO[] ItemsValue;

        [Header("初始穿戴")]
        [Tooltip("角色四个容器槽位的默认装备, 装配处按它注册身上的网格")]
        [SerializeField] private InventoryLoadoutConfigSO LoadoutValue;

        [Header("稀有度底色")]
        [Tooltip("无品质档位, 也是查不到档位时的兜底观感")]
        [SerializeField] private Color NoneColorValue = new Color(0.16f, 0.18f, 0.21f, 0.95f);

        [Tooltip("普通")]
        [SerializeField] private Color CommonColorValue = new Color(0.34f, 0.36f, 0.40f, 0.92f);

        [Tooltip("优秀")]
        [SerializeField] private Color UncommonColorValue = new Color(0.20f, 0.44f, 0.28f, 0.92f);

        [Tooltip("稀有")]
        [SerializeField] private Color RareColorValue = new Color(0.16f, 0.34f, 0.58f, 0.92f);

        [Tooltip("史诗")]
        [SerializeField] private Color EpicColorValue = new Color(0.36f, 0.20f, 0.54f, 0.92f);

        [Tooltip("传说")]
        [SerializeField] private Color LegendaryColorValue = new Color(0.58f, 0.42f, 0.12f, 0.92f);

        public InventoryLoadoutConfigSO Loadout => LoadoutValue;

        /// <summary>
        /// 查稀有度对应的物品块底板色, 全工程唯一的底色出口
        /// </summary>
        /// <param name="rarity">物品稀有度</param>
        public Color GetRarityColor(InventoryItemRarity rarity)
        {
            return rarity switch
            {
                InventoryItemRarity.None => NoneColorValue,
                InventoryItemRarity.Common => CommonColorValue,
                InventoryItemRarity.Uncommon => UncommonColorValue,
                InventoryItemRarity.Rare => RareColorValue,
                InventoryItemRarity.Epic => EpicColorValue,
                InventoryItemRarity.Legendary => LegendaryColorValue,
                _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, "未定义的稀有度档位")
            };
        }

        /// <summary>
        /// 把物品表整批登记进查表字典, 这是正式加载路径, 取代编辑器扫盘捷径
        /// </summary>
        public void RegisterItems()
        {
            if (ItemsValue == null || ItemsValue.Length == 0)
            {
                QLog.Error("物品表为空, 背包解析不了任何物品");
                return;
            }

            for (int i = 0; i < ItemsValue.Length; i++)
            {
                if (ItemsValue[i] == null)
                {
                    QLog.Error($"物品表第 {i} 项为空槽, 请检查总配置");
                    continue;
                }

                ItemRegistry.Register(ItemsValue[i]);
            }
        }
    }
}
