/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包格子着色语义                      
 * │  类    名: InventoryCellMark.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace UI.Inventory
{
    public enum InventoryCellMark
    {
        //无特殊状态, 走普通底色
        None,

        //当前选中物品的占格
        Selected,

        //落点可以直接放置
        DropValid,

        //落点会与占格物品交换
        DropSwap,

        //落点被规则拒绝
        DropInvalid
    }
}
