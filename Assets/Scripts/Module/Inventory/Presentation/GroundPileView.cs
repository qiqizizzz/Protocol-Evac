/*
* ┌──────────────────────────────────┐
* │  描    述: 地面垃圾堆表现, 按容器宽高摊开堆体并复用同一只交互体
* │  类    名: GroundPileView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Interaction;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Presentation
{
    /// <summary>
    /// 地面垃圾堆表现：只吃容器号与格子宽高, 不读模型也不订阅变化, 由呈现层驱动
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class GroundPileView : MonoBehaviour, ILootInteractable
    {
        private const string HEAP_PATH = "Heap";
        private const string LABEL_PATH = "Label";

        // 一格对应的世界边长, 与背包格子的视觉尺度对齐
        private const float CELL_SIZE = 0.5f;

        // 预制体作者态是 2x2 格, 堆体缩放以它为基准
        private const float BASE_FOOTPRINT = 1f;

        // 交互体比堆体外沿多出的余量(米)
        private const float COLLIDER_MARGIN = 0.16f;

        // 预制体作者态的堆体高度(米)
        private const float BASE_HEIGHT = 0.66f;

        private const int INVALID_CONTAINER_ID = -1;

        private Transform m_heap;
        private BoxCollider m_interactTrigger;
        private GameObject m_label;
        private Transform m_labelTransform;

        private int m_containerId = INVALID_CONTAINER_ID;
        private float m_heightScale = 1f;

        // ==================== 属性 ====================
        public int ContainerId => m_containerId;

        // 物品标签节点, 文字内容交给呈现层, 表现层只负责给出位置与开关
        public Transform LabelTransform => m_labelTransform;

        // 选择器用的判距中心, 交互体缺失时退回自身位置
        public Vector3 InteractionCenter => m_interactTrigger != null
            ? m_interactTrigger.bounds.center
            : transform.position;

        // ==================== 生命周期 ====================
        private void Awake()
        {
            m_heap = transform.Find(HEAP_PATH);
            m_interactTrigger = GetComponent<BoxCollider>();
            m_labelTransform = transform.Find(LABEL_PATH);
            m_label = m_labelTransform != null ? m_labelTransform.gameObject : null;

#if UNITY_EDITOR
            if (m_heap == null) Debug.LogError($"[{nameof(GroundPileView)}] 未找到堆体节点：{name}/{HEAP_PATH}");
            if (m_interactTrigger == null) Debug.LogError($"[{nameof(GroundPileView)}] 未找到交互体：{name}");
            if (m_label == null) Debug.LogError($"[{nameof(GroundPileView)}] 未找到标签节点：{name}/{LABEL_PATH}");
#endif

            if (m_interactTrigger != null) m_interactTrigger.isTrigger = true;

            int layerIndex = LootBoxController.ResolveInteractableLayer();
            if (layerIndex >= 0) gameObject.layer = layerIndex;

            // 起堆时标签默认收起, 按 H 才亮出来
            if (m_label != null) m_label.SetActive(false);
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 按容器宽高落堆体尺度与交互体, 容器号是表现层唯一认得的身份
        /// </summary>
        /// <param name="containerId">承载这堆物品的运行时容器号</param>
        /// <param name="width">容器宽(格)</param>
        /// <param name="height">容器高(格)</param>
        public void Configure(int containerId, int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                QLog.Error($"地面堆尺寸无效, 容器：{containerId}, 尺寸：{width}x{height}");
                return;
            }

            m_containerId = containerId;

            float footprintX = width * CELL_SIZE;
            float footprintZ = height * CELL_SIZE;

            // 摊面积是主要表达, 高度只按容量档位微微抬一点, 避免堆越长越高挡住视线
            m_heightScale = ResolveHeightScale(width * height);

            if (m_heap != null)
                m_heap.localScale = new Vector3(footprintX / BASE_FOOTPRINT, m_heightScale, footprintZ / BASE_FOOTPRINT);

            if (m_interactTrigger != null)
            {
                m_interactTrigger.center = new Vector3(0f, BASE_HEIGHT * 0.5f * m_heightScale, 0f);
                m_interactTrigger.size = new Vector3(
                    footprintX + COLLIDER_MARGIN,
                    BASE_HEIGHT * m_heightScale,
                    footprintZ + COLLIDER_MARGIN);
            }
        }

        /// <summary>
        /// 按 H 时亮出或收起堆上方的物品标签
        /// </summary>
        /// <param name="visible">是否显示标签</param>
        public void ShowLabel(bool visible)
        {
            if (m_label != null) m_label.SetActive(visible);
        }

        /// <summary>
        /// 标签悬空高度, 随堆体一起长, 免得大件堆把字埋进碎块里
        /// </summary>
        public float LabelHeight => BASE_HEIGHT * m_heightScale + 0.25f;

        /// <summary>
        /// 容器号定下来了才算可交互, 表现层不判断内容多不多
        /// </summary>
        public bool CanInteract() => m_containerId != INVALID_CONTAINER_ID;

        // ==================== Private Function ====================
        // 容量档位对应的高度倍率, 只微涨不长高
        private static float ResolveHeightScale(int cellCount)
        {
            if (cellCount <= 4) return 1f;
            if (cellCount <= 9) return 1.08f;
            if (cellCount <= 12) return 1.12f;

            return 1.15f;
        }
    }
}
