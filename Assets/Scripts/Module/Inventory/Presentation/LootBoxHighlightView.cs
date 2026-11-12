/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱描边表现视图, 用独立网格副本显示可开启与聚焦高亮
* │  类    名: LootBoxHighlightView.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.log;

namespace Module.Inventory.Presentation
{
    /// <summary>
    /// 物资箱高亮视图：复用一只共享描边材质, 通过材质属性块切换弱高亮与聚焦高亮
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class LootBoxHighlightView : MonoBehaviour
    {
        public enum HighlightState
        {
            None,
            Available,
            Focused
        }

        private const string OUTLINE_MATERIAL_RESOURCE_PATH = "Shaders/LootInteractableOutline";
        private const string HIGHLIGHT_CONFIG_RESOURCE_PATH = "Config/Inventory/LootBoxHighlightConfig";
        private const string OUTLINE_NODE_PREFIX = "LootHighlightOutline";
        private static readonly int S_OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int S_OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private static readonly int S_PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int S_PulseAmountId = Shader.PropertyToID("_PulseAmount");

        private static Material s_outlineMaterial;
        private static LootBoxHighlightConfigSO s_highlightConfig;
        private static bool s_hasReportedMaterial;
        private static bool s_hasReportedConfig;

        private readonly List<MeshRenderer> m_sourceRenderers = new List<MeshRenderer>();
        private readonly List<MeshRenderer> m_outlineRenderers = new List<MeshRenderer>();

        private MaterialPropertyBlock m_propertyBlock;
        private HighlightState m_state;
        private bool m_isBuilt;

        public HighlightState State => m_state;

        /// <summary>
        /// 切换高亮层级, None 会关闭描边但保留已生成的网格副本供下次复用
        /// </summary>
        /// <param name="state">可开启、聚焦或关闭</param>
        public void SetState(HighlightState state)
        {
            if (state != HighlightState.None) BuildOutlineRenderers();
            if (!m_isBuilt) return;

            if (state != m_state) ApplyStyle(state);
            m_state = state;
            RefreshOutlineVisibility();
        }

        // 首次启用时按源渲染器建立共享网格的描边副本
        private void BuildOutlineRenderers()
        {
            if (m_isBuilt) return;
            m_isBuilt = true;

            Material material = ResolveOutlineMaterial();
            if (material == null) return;

            LootBoxHighlightConfigSO config = ResolveHighlightConfig();
            if (config == null) return;

            m_propertyBlock = new MaterialPropertyBlock();
            MeshRenderer[] sourceRenderers = GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < sourceRenderers.Length; i++)
            {
                MeshRenderer sourceRenderer = sourceRenderers[i];
                if (sourceRenderer == null) continue;
                if (!sourceRenderer.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null) continue;

                GameObject outlineObject = new GameObject($"{OUTLINE_NODE_PREFIX}_{i}");
                outlineObject.layer = gameObject.layer;
                Transform outlineTransform = outlineObject.transform;
                outlineTransform.SetParent(transform, false);

                Matrix4x4 relativeMatrix = transform.worldToLocalMatrix * sourceRenderer.transform.localToWorldMatrix;
                Vector3 localPosition = relativeMatrix.GetColumn(3);
                Vector3 localScale = new Vector3(relativeMatrix.GetColumn(0).magnitude,
                    relativeMatrix.GetColumn(1).magnitude, relativeMatrix.GetColumn(2).magnitude);
                Quaternion localRotation = Quaternion.LookRotation(relativeMatrix.GetColumn(2),
                    relativeMatrix.GetColumn(1));
                outlineTransform.SetLocalPositionAndRotation(localPosition, localRotation);
                outlineTransform.localScale = localScale;

                MeshFilter outlineFilter = outlineObject.AddComponent<MeshFilter>();
                outlineFilter.sharedMesh = meshFilter.sharedMesh;

                MeshRenderer outlineRenderer = outlineObject.AddComponent<MeshRenderer>();
                outlineRenderer.sharedMaterial = material;
                outlineRenderer.shadowCastingMode = ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
                outlineRenderer.lightProbeUsage = LightProbeUsage.Off;
                outlineRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                outlineRenderer.allowOcclusionWhenDynamic = false;
                outlineRenderer.enabled = false;

                m_sourceRenderers.Add(sourceRenderer);
                m_outlineRenderers.Add(outlineRenderer);
            }

            if (m_outlineRenderers.Count == 0)
                QLog.Error($"物资箱没有可生成描边的网格渲染器：{name}");
        }

        // 共享材质只调属性块, 不为每只箱子复制材质实例
        private void ApplyStyle(HighlightState state)
        {
            if (state == HighlightState.None) return;

            LootBoxHighlightConfigSO config = ResolveHighlightConfig();
            if (config == null) return;

            Color color;
            float width;
            float pulseSpeed;
            float pulseAmount;

            if (state == HighlightState.Focused)
            {
                color = config.FocusedColor;
                width = config.FocusedWidth;
                pulseSpeed = config.FocusedPulseSpeed;
                pulseAmount = config.FocusedPulseAmount;
            }
            else
            {
                color = config.AvailableColor;
                width = config.AvailableWidth;
                pulseSpeed = config.AvailablePulseSpeed;
                pulseAmount = config.AvailablePulseAmount;
            }

            m_propertyBlock.Clear();
            m_propertyBlock.SetColor(S_OutlineColorId, color);
            m_propertyBlock.SetFloat(S_OutlineWidthId, width);
            m_propertyBlock.SetFloat(S_PulseSpeedId, pulseSpeed);
            m_propertyBlock.SetFloat(S_PulseAmountId, pulseAmount);

            for (int i = 0; i < m_outlineRenderers.Count; i++)
            {
                MeshRenderer outlineRenderer = m_outlineRenderers[i];
                if (outlineRenderer != null) outlineRenderer.SetPropertyBlock(m_propertyBlock);
            }
        }

        // 源 LOD 或网格渲染器关闭时, 对应描边副本也要同步隐藏
        private void RefreshOutlineVisibility()
        {
            bool isVisible = m_state != HighlightState.None;
            int count = Mathf.Min(m_sourceRenderers.Count, m_outlineRenderers.Count);
            for (int i = 0; i < count; i++)
            {
                MeshRenderer outlineRenderer = m_outlineRenderers[i];
                if (outlineRenderer == null) continue;

                MeshRenderer sourceRenderer = m_sourceRenderers[i];
                outlineRenderer.enabled = isVisible && sourceRenderer != null && sourceRenderer.enabled;
            }
        }

        // 首次加载共享描边材质, 失败只报一次并让高亮保持关闭
        private static Material ResolveOutlineMaterial()
        {
            if (s_outlineMaterial != null) return s_outlineMaterial;

            s_outlineMaterial = Resources.Load<Material>(OUTLINE_MATERIAL_RESOURCE_PATH);
            if (s_outlineMaterial != null || s_hasReportedMaterial) return s_outlineMaterial;

            s_hasReportedMaterial = true;
            QLog.Error($"物资箱高亮材质加载失败：Resources/{OUTLINE_MATERIAL_RESOURCE_PATH}");
            return null;
        }

        // 首次加载高亮配置, 缺失时主动报错并让全部高亮保持关闭
        private static LootBoxHighlightConfigSO ResolveHighlightConfig()
        {
            if (s_highlightConfig != null) return s_highlightConfig;

            s_highlightConfig = Resources.Load<LootBoxHighlightConfigSO>(HIGHLIGHT_CONFIG_RESOURCE_PATH);
            if (s_highlightConfig != null || s_hasReportedConfig) return s_highlightConfig;

            s_hasReportedConfig = true;
            QLog.Error($"物资箱高亮配置加载失败：Resources/{HIGHLIGHT_CONFIG_RESOURCE_PATH}");
            return null;
        }
    }
}
