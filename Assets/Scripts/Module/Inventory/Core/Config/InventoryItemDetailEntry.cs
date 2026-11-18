/*
* ┌──────────────────────────────────┐
* │  描    述: 物品详情属性条目, 供不同物品种类配置各自的详情字段
* │  类    名: InventoryItemDetailEntry.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Inventory.Core.Config
{
    [Serializable]
    public sealed class InventoryItemDetailEntry
    {
        [SerializeField] private string LabelValue = string.Empty;
        [SerializeField] private string ValueValue = string.Empty;

        public string Label => LabelValue;
        public string Value => ValueValue;

        public InventoryItemDetailEntry()
        {
        }

        public InventoryItemDetailEntry(string label, string value)
        {
            LabelValue = label;
            ValueValue = value;
        }
    }
}
