/*
 * ┌──────────────────────────────────┐
 * │  描    述: 弹窗定位工具, 详情贴在物品占格旁, 菜单与拆分卡片贴在鼠标旁
 * │  类    名: ItemPopupPlacement.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;
using Framework.QTower.MVVM.View;
using Utils.log;

namespace UI.Inventory.View.Tools
{
    /// <summary>
    /// 物品弹窗定位工具：详情面板与右键菜单共用贴随鼠标且不超出边界的定位规则
    /// </summary>
    public static class ItemPopupPlacement
    {
        private const int ARG_COUNT = 3;
        private const int ITEM_RECT_ARG_COUNT = 4;

        // 浮层相对面板排序的抬升量：窗口服务为每个面板分配相同的 sortingOrder,
        // 同排序时射线由网格的 GraphicRaycaster 接收, 浮层按钮无法点击
        private const int POPUP_SORTING_OFFSET = 10;

        /// <summary>
        /// 将浮层 Canvas 排序提升到面板之上, 使其覆盖网格并接收自身点击
        /// </summary>
        /// <param name="root">浮层根节点, 由窗口服务添加 Canvas 与 GraphicRaycaster</param>
        public static void RaiseAbovePanels(GameObject root)
        {
            if (!root.TryGetComponent(out Canvas canvas))
            {
                QLog.Error($"浮层根节点缺少 Canvas, 无法抬高排序档位：{root.name}");
                return;
            }

            // 每次打开按当前面板排序重算, 全局排序变更后仍保持覆盖
            canvas.overrideSorting = true;
            canvas.sortingOrder = MvvmRoot.Instance.Windows.SortingOrder + POPUP_SORTING_OFFSET;
        }

        /// <summary>
        /// 读出弹窗打开参数：容器号、实例号、鼠标屏幕坐标
        /// </summary>
        /// <param name="args">ShowAsync 透传的打开参数</param>
        /// <param name="containerId">物品所在容器号</param>
        /// <param name="instanceId">物品实例号</param>
        /// <param name="screenPos">鼠标屏幕坐标</param>
        public static bool TryReadCursorArgs(object[] args, out int containerId, out int instanceId,
            out Vector3 screenPos)
        {
            containerId = 0;
            instanceId = 0;
            screenPos = Vector3.zero;

            if (args == null || args.Length < ARG_COUNT)
            {
                QLog.Error($"弹窗打开参数不足: 依次需要 容器号 实例号 鼠标位置, 实际 {(args == null ? 0 : args.Length)} 个");
                return false;
            }

            if (args[0] is not int first || args[1] is not int second || args[2] is not Vector3 mouse)
            {
                QLog.Error("弹窗打开参数类型不符: 依次应为 int, int, Vector3");
                return false;
            }

            containerId = first;
            instanceId = second;
            screenPos = mouse;
            return true;
        }

        /// <summary>
        /// 读出详情弹窗打开参数：容器号、实例号、物品完整占格的屏幕矩形与是否优先左侧
        /// </summary>
        /// <param name="args">ShowAsync 透传的打开参数</param>
        /// <param name="containerId">物品所在容器号</param>
        /// <param name="instanceId">物品实例号</param>
        /// <param name="itemScreenRect">物品完整占格在屏幕空间中的矩形</param>
        /// <param name="preferLeft">是否优先将卡片放在物品左侧</param>
        public static bool TryReadItemRectArgs(object[] args, out int containerId, out int instanceId,
            out Rect itemScreenRect, out bool preferLeft)
        {
            containerId = 0;
            instanceId = 0;
            itemScreenRect = default;
            preferLeft = false;

            if (args == null || args.Length < ITEM_RECT_ARG_COUNT)
            {
                QLog.Error($"详情弹窗打开参数不足: 依次需要 容器号 实例号 物品屏幕矩形 优先左侧, 实际 {(args == null ? 0 : args.Length)} 个");
                return false;
            }

            if (args[0] is not int first || args[1] is not int second || args[2] is not Rect rect ||
                args[3] is not bool prefer)
            {
                QLog.Error("详情弹窗打开参数类型不符: 依次应为 int, int, Rect, bool");
                return false;
            }

            containerId = first;
            instanceId = second;
            itemScreenRect = rect;
            preferLeft = prefer;
            return true;
        }

        /// <summary>
        /// 将卡片定位到鼠标附近, 并按卡片尺寸限制在面板范围内
        /// </summary>
        /// <param name="root">铺满屏幕的弹窗根节点</param>
        /// <param name="card">卡片节点, 预制体中锚点与轴心为居中、左上</param>
        /// <param name="screenPos">鼠标屏幕坐标</param>
        /// <param name="offset">相对鼠标的避让偏移</param>
        public static void AnchorToCursor(RectTransform root, RectTransform card, Vector3 screenPos, Vector2 offset)
        {
            if (root == null || card == null) return;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPos, ResolveCamera(root),
                    out Vector2 localPoint))
                return;

            Vector2 half = root.rect.size * 0.5f;
            Vector2 size = card.rect.size;
            Vector2 pos = localPoint + offset;

            // 轴心在左上, x 取右边界、y 取下边界, 卡片越界时贴屏幕边界
            pos.x = Mathf.Clamp(pos.x, -half.x, Mathf.Max(-half.x, half.x - size.x));
            pos.y = Mathf.Clamp(pos.y, Mathf.Max(-half.y, -half.y + size.y), half.y);

            card.anchoredPosition = pos;
        }

        /// <summary>
        /// 将卡片定位到物品完整占格旁, 按优先方向选择左右侧, 并限制在面板范围内
        /// </summary>
        /// <param name="root">铺满屏幕的弹窗根节点</param>
        /// <param name="card">卡片节点, 锚点居中、轴心左上</param>
        /// <param name="itemScreenRect">物品完整占格在屏幕空间中的矩形</param>
        /// <param name="gap">卡片与物品占格之间的间距</param>
        /// <param name="preferLeft">是否优先将卡片放在物品左侧</param>
        public static void AnchorToScreenRect(RectTransform root, RectTransform card, Rect itemScreenRect, float gap,
            bool preferLeft = false)
        {
            if (root == null || card == null) return;

            Camera camera = ResolveCamera(root);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(root, itemScreenRect.min, camera,
                    out Vector2 localMin) ||
                !RectTransformUtility.ScreenPointToLocalPointInRectangle(root, itemScreenRect.max, camera,
                    out Vector2 localMax))
                return;

            Rect itemRect = Rect.MinMaxRect(
                Mathf.Min(localMin.x, localMax.x),
                Mathf.Min(localMin.y, localMax.y),
                Mathf.Max(localMin.x, localMax.x),
                Mathf.Max(localMin.y, localMax.y));

            Vector2 half = root.rect.size * 0.5f;
            Vector2 size = card.rect.size;
            float rightX = itemRect.xMax + gap;
            float leftX = itemRect.xMin - gap - size.x;
            bool rightFits = rightX + size.x <= half.x;
            bool leftFits = leftX >= -half.x;
            float x;

            // 左侧装备优先向左展开, 空间不足时回退到另一侧
            if (preferLeft && leftFits)
                x = leftX;
            else if (rightFits)
                x = rightX;
            else if (leftFits)
                x = leftX;
            else
                x = Mathf.Clamp(rightX, -half.x, Mathf.Max(-half.x, half.x - size.x));

            float y = Mathf.Clamp(itemRect.yMax, Mathf.Max(-half.y, -half.y + size.y), half.y);
            card.anchoredPosition = new Vector2(x, y);
        }

        // Overlay 画布需传 null, 仅 Camera 画布需要世界相机
        private static Camera ResolveCamera(RectTransform root)
        {
            Canvas canvas = root.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

            return canvas.worldCamera;
        }
    }
}
