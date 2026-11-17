/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包格子单边与相邻格子的区域连接状态
 * │  类    名: InventorySlotEdgeKind.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace UI.Inventory
{
    public enum InventorySlotEdgeKind
    {
        //空洞格或无效格, 不绘制底色与边线
        None,

        //同一区域内部相邻, 格子底色连续并由单侧绘制灰色分隔线
        Connected,

        //区域外沿、空洞或有绘制权的不同区域交界, 底色内缩并绘制白色边界线
        Boundary,

        //不同区域交界的被动侧, 只让出间距, 由交界另一侧绘制单条白线
        BoundaryPassive
    }
}
