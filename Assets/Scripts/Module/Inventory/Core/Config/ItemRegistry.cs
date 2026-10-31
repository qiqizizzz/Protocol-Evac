/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品定义静态登记表                      
 * │  类    名: ItemRegistry.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Module.Inventory.Core.Config
{
    public static class ItemRegistry
    {
        private static readonly Dictionary<int, InventoryItemConfigSO> m_configs =
            new Dictionary<int, InventoryItemConfigSO>();

        public static void Register(InventoryItemConfigSO config)
        {
            m_configs[config.ItemId] = config;
        }

        public static bool TryGet(int itemId, out InventoryItemConfigSO config)
        {
            return m_configs.TryGetValue(itemId, out config);
        }
    }
}