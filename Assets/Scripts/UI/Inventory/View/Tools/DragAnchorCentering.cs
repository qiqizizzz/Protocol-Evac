/*
 * ┌──────────────────────────────────┐
 * │  描    述: 拖影跟随规则, 拖影中心连续跟随指针, 锚点格再折算到格阵并收进容器边界
 * │  类    名: DragAnchorCentering.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace UI.Inventory.View.Tools
{
    /// <summary>
    /// 拖拽跟随规则：拖影本体按指针连续移动, 不做格阵量化, 只有锚点格折算到格阵上,
    /// 使手感顺滑且高亮与实际落点同源
    /// </summary>
    public static class DragAnchorCentering
    {
        /// <summary>
        /// 指针在物品层局部坐标中相对首格左上角的偏移, 向下为负
        /// </summary>
        /// <param name="layerRect">物品层的自身矩形, 其左上角即首格左上角</param>
        /// <param name="localPoint">指针在物品层局部坐标中的位置</param>
        public static Vector2 PointerOffset(Rect layerRect, Vector2 localPoint)
        {
            return new Vector2(localPoint.x - layerRect.xMin, localPoint.y - layerRect.yMax);
        }

        /// <summary>
        /// 指定格号的左上角相对首格左上角的像素偏移, 向下为负
        /// </summary>
        /// <param name="cell">格号</param>
        /// <param name="step">单元格边长加间距</param>
        public static Vector2 CellCornerOffset(Vector2Int cell, Vector2 step)
        {
            return new Vector2(cell.x * step.x, -cell.y * step.y);
        }

        /// <summary>
        /// 拖影左上角在物品层中的连续偏移: 让拖影中心落在指针上, 不做格阵量化
        /// </summary>
        /// <param name="pointerOffset">指针相对首格左上角的偏移</param>
        /// <param name="ghostSize">拖影当前外接矩形的像素尺寸</param>
        public static Vector2 GhostOffset(Vector2 pointerOffset, Vector2 ghostSize)
        {
            return pointerOffset - CenterToCornerOffset(ghostSize);
        }

        /// <summary>
        /// 拖影中心到其左上角的偏移, 向右为正、向下为负
        /// </summary>
        /// <param name="ghostSize">拖影当前外接矩形的像素尺寸</param>
        public static Vector2 CenterToCornerOffset(Vector2 ghostSize)
        {
            return new Vector2(ghostSize.x * 0.5f, -ghostSize.y * 0.5f);
        }

        /// <summary>
        /// 拖影左上角落在哪个格子, 取最近的格阵位置, 奇偶占格都适用
        /// </summary>
        /// <param name="ghostOffset">拖影左上角相对首格左上角的偏移</param>
        /// <param name="step">单元格边长加间距</param>
        public static Vector2Int LatticeAnchor(Vector2 ghostOffset, Vector2 step)
        {
            if (step.x <= 0f || step.y <= 0f) return Vector2Int.zero;

            return new Vector2Int(NearestCell(ghostOffset.x / step.x),
                NearestCell(-ghostOffset.y / step.y));
        }

        // 四舍五入且平局一律偏向较大格号: Mathf.RoundToInt 用的是银行家舍入, 偶数占格的拖影中心
        // 贴着格心时商恰好是 .5, 舍入结果随格号奇偶摆动, 导致锚点跳格且部分格号永远取不到
        private static int NearestCell(float value)
        {
            if (float.IsNaN(value)) return 0;

            return Mathf.FloorToInt(value + 0.5f);
        }

        /// <summary>
        /// 锚点格收进容器边界, 容器容不下该占格时贴向左上角
        /// </summary>
        /// <param name="anchor">待收边的锚点格</param>
        /// <param name="footprint">拖影在悬停容器中的占格</param>
        /// <param name="gridCount">悬停容器的格数</param>
        public static Vector2Int ClampAnchor(Vector2Int anchor, Vector2Int footprint, Vector2Int gridCount)
        {
            int maxX = Mathf.Max(0, gridCount.x - Mathf.Max(1, footprint.x));
            int maxY = Mathf.Max(0, gridCount.y - Mathf.Max(1, footprint.y));

            return new Vector2Int(Mathf.Clamp(anchor.x, 0, maxX), Mathf.Clamp(anchor.y, 0, maxY));
        }
    }
}
