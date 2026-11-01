/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包提示语义                      
 * │  类    名: InventoryHintKind.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace UI.Inventory
{
    public enum InventoryHintKind
    {
        //未选中任何物品
        Idle,
        
        //已选中物品
        Selected,
        
        //物品已放到目标格子
        Placed,
        
        //被规则拒绝, 参照 InventoryViewModel.LastMoveResult
        Failed,
        
        //数据异常 (幽灵引用/归属冲突)
        Desync
    }
}