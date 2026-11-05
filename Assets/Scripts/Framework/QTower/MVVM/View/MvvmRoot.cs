/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM运行根节点，持有模型仓库与窗口服务的宿主
 * │  类    名: MvvmRoot.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Model;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    public sealed class MvvmRoot : MonoBehaviour
    {
        // 面板 Canvas 的默认排序档位, 高于 MVC 常驻 HUD
        internal const int DEFAULT_SORTING_ORDER = 50;

        private static MvvmRoot m_instance;

        private int m_sortingOrder = DEFAULT_SORTING_ORDER;

        public static MvvmRoot Instance => m_instance;

        public ModelRegistry Models { get; private set; }
        public WindowService Windows { get; private set; }

        public static MvvmRoot Create(Transform parent = null, string rootName = "MvvmRoot",
            int sortingOrder = DEFAULT_SORTING_ORDER)
        {
            if (m_instance != null) return m_instance;

            GameObject rootObject = new GameObject(rootName, typeof(RectTransform));
            if (parent != null)
                rootObject.transform.SetParent(parent, false);

            // 根节点必须铺满挂载点, 面板直接挂在它下面, 父级尺寸无效时拉伸锚点会按 0x0 计算
            FillParentRect(rootObject.transform as RectTransform);

            rootObject.AddComponent<MvvmRoot>();

            if (m_instance == null)
            {
                QLog.Error($"[{nameof(MvvmRoot)}] 根节点创建失败：{rootName}");
                return null;
            }

            m_instance.ApplySortingOrder(sortingOrder);
            return m_instance;
        }

        // ==================== 生命周期 ====================
        private void Awake()
        {
            if (m_instance != null && m_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            m_instance = this;

            Models = new ModelRegistry();
            Windows = new WindowService(transform, m_sortingOrder);
        }

        // 挂载顺序不确定, Start 时父级一定已就位, 再补一次铺满与排序
        private void Start()
        {
            FillParentRect(transform as RectTransform);
            ApplySortingOrder(m_sortingOrder);
        }

        private void OnDestroy()
        {
            Windows?.DestroyAll();
            Models?.Shutdown();

            if (m_instance != this) return;

            m_instance = null;
        }

        // ==================== Private Function ====================
        // 自身 RectTransform 铺满父级, 父级不是 RectTransform 时保持原样
        internal static void FillParentRect(RectTransform rect)
        {
            if (rect == null) return;
            if (!(rect.parent is RectTransform)) return;

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        // 排序档位下发给窗口服务, 由它在每个面板自己的 Canvas 上落地
        internal void ApplySortingOrder(int sortingOrder)
        {
            m_sortingOrder = sortingOrder;
            Windows?.ApplySortingOrder(sortingOrder);
        }
    }
}