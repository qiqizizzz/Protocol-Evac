/*
* ┌──────────────────────────────────┐
* │  描    述: 野外物资箱标记组件, 挂在场景箱子实例上并自建交互触发体
* │  类    名: LootBoxController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Inventory.Core.Config.Loot;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Interaction
{
    /// <summary>
    /// 野外物资箱：实体号与掉落配置写在场景实例上，交互体在 Awake 按渲染包围盒自建
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootBoxController : MonoBehaviour, ILootInteractable
    {
        private const string INTERACT_TRIGGER_NAME = "LootInteractTrigger";
        private const string LOOT_INTERACTABLE_LAYER_NAME = "LootInteractable";
        private const float FALLBACK_TRIGGER_SIZE = 0.5f;
        private const float MIN_TRIGGER_SIZE = 0.02f;
        private const uint FNV_OFFSET_BASIS = 2166136261;
        private const uint FNV_PRIME = 16777619;

        private static int m_layerIndexCache = -2;

        [Header("标识")]
        [Tooltip("实体号, 手写且场景内唯一, 例 loot_plastic_box_03_01")]
        [SerializeField] private string EntityIdValue = string.Empty;

        [Header("掉落")]
        [Tooltip("本箱使用的野外物资箱配置")]
        [SerializeField] private LootContainerConfigSO LootConfigValue;

        [Header("交互")]
        [Tooltip("交互体在渲染包围盒外再扩出的距离(米)")]
        [SerializeField, Min(0f)] private float InteractRadiusValue = 1.2f;

        private Collider m_interactTrigger;
        private Bounds m_visualBounds;
        private bool m_hasReportedConfig;

        // ==================== 属性 ====================
        public string EntityId => EntityIdValue;
        public LootContainerConfigSO LootConfig => LootConfigValue;
        public float InteractRadius => InteractRadiusValue;

        // 选择器用的判距中心, 交互体缺失时退回自身位置, 保证永远拿到可用坐标
        public Vector3 InteractionCenter => m_interactTrigger != null
            ? m_interactTrigger.bounds.center
            : transform.position;

        // 遮挡检测使用真实箱体范围, 不拿向外扩展的物理触发体虚增上沿
        public Bounds InteractionBounds => m_interactTrigger != null
            ? m_visualBounds
            : new Bounds(transform.position, Vector3.zero);

        // ==================== 生命周期 ====================
        private void Awake()
        {
            BuildInteractTrigger();
        }

        private void OnValidate()
        {
            if (string.IsNullOrEmpty(EntityIdValue))
                QLog.Error($"实体号为空, 该物资箱无法定位容器：{name}");
        }

        private void OnDrawGizmosSelected()
        {
            if (m_interactTrigger != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireCube(m_interactTrigger.bounds.center, m_interactTrigger.bounds.size);
                return;
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, InteractRadiusValue);
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 解析物资箱交互层序号, 层名未登记时返回负值并报错一次
        /// </summary>
        public static int ResolveInteractableLayer()
        {
            if (m_layerIndexCache != -2) return m_layerIndexCache;

            m_layerIndexCache = LayerMask.NameToLayer(LOOT_INTERACTABLE_LAYER_NAME);
            if (m_layerIndexCache < 0)
                QLog.Error($"未登记 Layer {LOOT_INTERACTABLE_LAYER_NAME}, 物资箱交互体无法进入物理粗筛");

            return m_layerIndexCache;
        }

        /// <summary>
        /// 配置齐备才可交互, 缺配置只报错一次, 不做每帧刷屏
        /// </summary>
        public bool CanInteract()
        {
            if (string.IsNullOrEmpty(EntityIdValue))
            {
                ReportConfigMissing("实体号为空");
                return false;
            }

            if (LootConfigValue == null)
            {
                ReportConfigMissing($"{EntityIdValue} 未指定物资箱配置");
                return false;
            }

            if (LootConfigValue.Container == null)
            {
                ReportConfigMissing($"{EntityIdValue} 的物资箱配置缺少网格定义");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 用实体号算确定性种子, 同一只箱子重启后出货一致, 后续接服务器也能复核
        /// </summary>
        public int ResolveSeed()
        {
            uint hash = FNV_OFFSET_BASIS;
            for (int i = 0; i < EntityIdValue.Length; i++)
            {
                hash ^= EntityIdValue[i];
                hash *= FNV_PRIME;
            }

            return (int) (hash & 0x7FFFFFFF);
        }

        // ==================== Private Function ====================
        // 缺配置的统一出口, 只报第一次
        private void ReportConfigMissing(string message)
        {
            if (m_hasReportedConfig) return;

            m_hasReportedConfig = true;
            QLog.Error($"物资箱配置不完整, {message}");
        }

        /*
         * 交互体挂在自建子节点上, 且把旋转清成单位四元数:
         * Renderer.bounds 是世界轴对齐盒, 直接写给带旋转的父级 BoxCollider 会歪,
         * 世界对齐后 local size 就等于世界 size, 同时绕开 LOD0 碰撞体远处被整套禁用的问题
         */
        private void BuildInteractTrigger()
        {
            Bounds bounds = MergeRendererBounds();
            m_visualBounds = bounds;
            bounds.Expand(InteractRadiusValue * 2f);

            GameObject triggerObject = new GameObject(INTERACT_TRIGGER_NAME);
            triggerObject.transform.SetParent(transform, false);
            triggerObject.transform.position = bounds.center;
            triggerObject.transform.rotation = Quaternion.identity;
            triggerObject.transform.localScale = Vector3.one;

            int layerIndex = ResolveInteractableLayer();
            triggerObject.layer = layerIndex >= 0 ? layerIndex : 0;

            BoxCollider trigger = triggerObject.AddComponent<BoxCollider>();
            trigger.center = Vector3.zero;
            trigger.size = new Vector3(
                Mathf.Max(bounds.size.x, MIN_TRIGGER_SIZE),
                Mathf.Max(bounds.size.y, MIN_TRIGGER_SIZE),
                Mathf.Max(bounds.size.z, MIN_TRIGGER_SIZE));
            trigger.isTrigger = true;

            m_interactTrigger = trigger;
        }

        // 合并自身与子级的渲染包围盒, LOD 关掉的渲染器不参与
        private Bounds MergeRendererBounds()
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(transform.position, Vector3.one * FALLBACK_TRIGGER_SIZE);

            bool hasBounds = false;
            Bounds merged = default;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled) continue;

                if (hasBounds) merged.Encapsulate(renderer.bounds);
                else
                {
                    merged = renderer.bounds;
                    hasBounds = true;
                }
            }

            return hasBounds
                ? merged
                : new Bounds(transform.position, Vector3.one * FALLBACK_TRIGGER_SIZE);
        }
    }
}
