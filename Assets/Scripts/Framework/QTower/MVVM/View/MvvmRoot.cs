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

        public static MvvmRoot Instance => m_instance;

        public Transform ViewRoot => m_viewRoot;
        public ModelRegistry Models { get; private set; }
        public WindowService Windows { get; private set; }

        public static MvvmRoot Create(Transform parent = null, string rootName = "MvvmRoot", int sortingOrder = DEFAULT_SORTING_ORDER)
        {
            if (m_instance != null) return m_instance;

            GameObject rootObject = new GameObject(rootName);
            if (parent != null)
                rootObject.transform.SetParent(parent, false);

            rootObject.AddComponent<MvvmRoot>();

            if (m_instance == null)
            {
                QLog.Error($"[{nameof(MvvmRoot)}] 根节点创建失败：{rootName}");
                return null;
            }

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

        private void OnDestroy()
        {
            Windows?.DestroyAll();
            Models?.Shutdown();

            if (m_instance != this) return;

            m_viewCanvas = null;
            m_viewRoot = null;
            m_instance = null;
        }
        
        private void ApplySortingOrder(int sortingOrder)
        {
            if (m_viewCanvas == null)
            {
                QLog.Error($"[{nameof(MvvmRoot)}] 排序应用失败：{VIEW_ROOT_NAME} 上没有 Canvas");
                return;
            }

            m_viewCanvas.sortingOrder = sortingOrder;
        }
    }
}
