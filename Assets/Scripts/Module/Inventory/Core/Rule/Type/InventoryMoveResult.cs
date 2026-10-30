/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品移动的放置结果                      
 * │  类    名: InventoryMoveResult.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Inventory.Core.Rule.Type
{
    public enum InventoryMoveResult
    {
        None = 0,
        
        Success = 1,
        
        //原地重复放置
        SameSpot = 2,
        
        //目标区域超出网格范围
        OutOfBounds = 3,
        
        //目标区域被占据
        Occupied = 4,
        
        //物体本身不允许旋转, 或旋转后被配置排除
        NotRotatable = 5,
        
        //目标容器放不下这个重量
        WeightExceeded = 6,
        
        //占位记录不属于给定容器(幽灵物品)
        SourceMismatch = 7
    }
}