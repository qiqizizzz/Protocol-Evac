/*
 * ┌──────────────────────────────────┐
 * │  描    述: 弹窗定位工具, 把详情与菜单卡片钉到鼠标旁并夹在面板内
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
    /// 物品弹窗定位工具：详情面板与右键菜单共用一套「贴着鼠标、不许出界」的摆位规则
    /// </summary>
    public static class ItemPopupPlacement
    {
        private const int ARG_COUNT = 3;

        //浮层相对面板档位的抬升量: 窗口服务给每个面板同一个 sortingOrder,
        //同档位下射线会被网格自己的 GraphicRaycaster 抢走, 浮层按钮就点不动
        private const int POPUP_SORTING_OFFSET = 10;

        /// <summary>
        /// 把浮层自身的 Canvas 抬到面板档位之上, 保证它盖得住网格也接得住自己的点击
        /// </summary>
        /// <param name="root">浮层根节点, 由窗口服务挂好 Canvas 与 GraphicRaycaster</param>
        public static void RaiseAbovePanels(GameObject root)
        {
            if (!root.TryGetComponent(out Canvas canvas))
            {
                QLog.Error($"浮层根节点缺少 Canvas, 无法抬高排序档位：{root.name}");
                return;
            }

            //档位每次打开都按当前面板档位重算, 全局改档后下一次显示照样压得住
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
        /// 把卡片钉到鼠标指针旁, 并按卡片自身尺寸收进面板范围
        /// </summary>
        /// <param name="root">铺满屏幕的弹窗根节点</param>
        /// <param name="card">卡片节点, 锚点与轴心在预制体里定为居中、左上</param>
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

            //轴心在左上, x 取右边界、y 取下边界, 卡片越界时贴着屏幕边停下
            pos.x = Mathf.Clamp(pos.x, -half.x, Mathf.Max(-half.x, half.x - size.x));
            pos.y = Mathf.Clamp(pos.y, Mathf.Max(-half.y, -half.y + size.y), half.y);

            card.anchoredPosition = pos;
        }

        //Overlay 画布传 null 才是正确解算, 只有 Camera 画布才需要世界相机
        private static Camera ResolveCamera(RectTransform root)
        {
            Canvas canvas = root.GetComponentInParent<Canvas>();
            if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay) return null;

            return canvas.worldCamera;
        }
    }
}
