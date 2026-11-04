/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM运行根节点，持有模型仓库与窗口服务的宿主
 * │  类    名: MvvmRoot.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Model;
using UnityEngine;
using UnityEngine.UI;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    public sealed class MvvmRoot : MonoBehaviour
    {
        private const string VIEW_ROOT_NAME = "ViewRoot";
        private const int DEFAULT_SORTING_ORDER = 50;
        
        private static MvvmRoot m_instance;
        
        private Transform m_viewRoot;
        private Canvas m_viewCanvas;
        private int m_sortingOrder = DEFAULT_SORTING_ORDER;

        public static MvvmRoot Instance => m_instance;

        public Transform ViewRoot => m_viewRoot;
        public ModelRegistry Models { get; private set; }
        public WindowService Windows { get; private set; }

        public static MvvmRoot Create(Transform parent = null, string rootName = "MvvmRoot", int sortingOrder = DEFAULT_SORTING_ORDER)
        {
            if (m_instance != null) return m_instance;

            GameObject rootObject = new GameObject(rootName, typeof(RectTransform));
            if (parent != null)
                rootObject.transform.SetParent(parent, false);

            // 根节点必须铺满挂载点, 否则 ViewRoot 拿不到有效父级尺寸, 面板的拉伸锚点会按 0x0 计算
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

        private void Awake()
        {
            if (m_instance != null && m_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            m_instance = this;

            GameObject viewNode = new GameObject(VIEW_ROOT_NAME, typeof(RectTransform));
            viewNode.transform.SetParent(transform, false);
            m_viewRoot = viewNode.transform;

            // ViewRoot 必须有有效尺寸, 面板的拉伸锚点全部依赖它, 尺寸不对整屏 UI 就会从屏幕中心溢出
            RectTransform viewRect = viewNode.transform as RectTransform;
            if (viewRect.parent is RectTransform)
            {
                FillParentRect(viewRect);
            }
            else
            {
                QLog.Warning($"[{nameof(MvvmRoot)}] {VIEW_ROOT_NAME} 的父级不是 RectTransform, 按屏幕尺寸铺满");
                viewRect.sizeDelta = new Vector2(Screen.width, Screen.height);
            }

            m_viewCanvas = viewNode.AddComponent<Canvas>();
            m_viewCanvas.overrideSorting = true;
            m_viewCanvas.sortingOrder = DEFAULT_SORTING_ORDER;

            m_viewCanvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1 |
                                                    AdditionalCanvasShaderChannels.Normal |
                                                    AdditionalCanvasShaderChannels.Tangent;
            
            viewNode.AddComponent<GraphicRaycaster>();
            
            Models = new ModelRegistry();
            Windows = new WindowService(m_viewRoot);
        }

        // 挂载顺序不确定, Start 时父级一定已就位, 再补一次铺满与排序
        private void Start()
        {
            FillParentRect(transform as RectTransform);
            FillParentRect(m_viewRoot as RectTransform);

            // Canvas 在没有父级时创建会被当作根 Canvas, 之后被嵌套进场景 Canvas 时 Unity 会把
            // overrideSorting 打回 false、sortingOrder 打回 0, 必须等父级就位后重新应用一次
            ApplySortingOrder(m_sortingOrder);
        }

        private void OnDestroy()
        {
            Windows?.DestroyAll();
            Models?.Shutdown();

            if (m_instance != this) return;

            m_viewCanvas = null;
            m_viewRoot = null;
            m_instance = null;
        }
        
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

        private void ApplySortingOrder(int sortingOrder)
        {
            m_sortingOrder = sortingOrder;

            if (m_viewCanvas == null)
            {
                QLog.Error($"[{nameof(MvvmRoot)}] 排序应用失败：{VIEW_ROOT_NAME} 上没有 Canvas");
                return;
            }

            // 嵌套 Canvas 只有开启 overrideSorting, 自身的 sortingOrder 才生效
            m_viewCanvas.overrideSorting = true;
            m_viewCanvas.sortingOrder = m_sortingOrder;
        }
    }
}
