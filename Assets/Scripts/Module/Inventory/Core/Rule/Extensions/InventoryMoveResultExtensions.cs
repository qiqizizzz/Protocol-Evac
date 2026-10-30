/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包移动操作码区间判断                      
 * │  类    名: InventoryMoveResultExtensions.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Rule.Type;

namespace Module.Inventory.Core.Rule.Extensions
{
    public static class InventoryMoveResultExtensions
    {
        // 是否有效
        public static bool IsValid(this InventoryMoveResult result) =>
            result is not InventoryMoveResult.None;

        // 是否成功
        public static bool IsSuccessOrSame(this InventoryMoveResult result) =>
            result is >= (InventoryMoveResult)100 and < (InventoryMoveResult)200;

        // 是否空间冲突
        public static bool IsSpatialFailure(this InventoryMoveResult result) =>
            result is >= (InventoryMoveResult)200 and < (InventoryMoveResult)300;

        // 是否业务规则限制(飘字处理)
        public static bool IsRuleFailure(this InventoryMoveResult result) =>
            result is >= (InventoryMoveResult)300 and < (InventoryMoveResult)400;

        // 是否数据异常
        public static bool IsDataDesync(this InventoryMoveResult result) =>
            result is >= (InventoryMoveResult)400;
    }
}