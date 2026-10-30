/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包规则集接口                      
 * │  类    名: IInventoryRules.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace Module.Inventory.Core.Rule
{
    public interface IInventoryRules
    {
        //添加物品
        InventoryMoveResult CanAdd(InventoryContainer target, InventoryItemInstance instance, Vector2Int position,
            InventoryRotation rotation);
        
        //移动物品
        InventoryMoveResult CanMove(InventoryContainer source, InventoryContainer target,
            InventoryItemPlacement placement, Vector2Int position, InventoryRotation rotation);
        
        //旋转物品
        InventoryMoveResult CanRotate(InventoryContainer container, InventoryItemPlacement placement,
            out InventoryRotation nextRotation);
    }
}