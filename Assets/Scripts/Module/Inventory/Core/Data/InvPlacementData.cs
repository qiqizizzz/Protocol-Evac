/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品占格数据类                      
 * │  类    名: InvPlacementData.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config;
using UnityEngine;

namespace Module.Inventory.Core.Data
{
    public class InvPlacementData
    {
        public int InstanceId { get; }
        public InvItemConfigSO Definition { get; }
        public Vector2Int Position { get; private set; }
        public bool Rotated { get; private set; }
    }
}