/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd点击标记表现控制器，复用标记对象并触发Shader动画
* │  类    名: CrowdClickMarkerPresenter.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections;
using UnityEngine;
using Utils.log;

namespace Module.CrowdDemo
{
    public sealed class CrowdClickMarkerPresenter : MonoBehaviour
    {
        private const float MARKER_HEIGHT_OFFSET = 0.02f;

        private static CrowdClickMarkerPresenter S_active;
        private static readonly int S_StartTimePropertyId = Shader.PropertyToID("_StartTime");
        private static readonly int S_DurationPropertyId = Shader.PropertyToID("_Duration");

        public static CrowdClickMarkerPresenter Active => S_active;

        private Transform m_transform;
        private Renderer m_renderer;
        private MaterialPropertyBlock m_propertyBlock;
        private WaitForSeconds m_displayDuration;
        private Coroutine m_hideCoroutine;

        // 注册场景中的点击标记表现源
        private void OnEnable()
        {
            if (S_active != null && S_active != this)
            {
                QLog.Error("场景中存在多个 CrowdClickMarkerPresenter");
                return;
            }

            S_active = this;
        }

        // 缓存点击标记的Renderer和Shader参数状态
        private void Awake()
        {
            m_transform = transform;

            if (!TryGetComponent<Renderer>(out m_renderer))
            {
                QLog.Error("Crowd 点击标记初始化失败：未找到 Renderer");
                enabled = false;
                return;
            }

            Material sharedMaterial = m_renderer.sharedMaterial;
            if (sharedMaterial == null)
            {
                QLog.Error("Crowd 点击标记初始化失败：Renderer 未配置材质");
                enabled = false;
                return;
            }

            float duration = sharedMaterial.GetFloat(S_DurationPropertyId);
            if (duration <= 0f)
            {
                QLog.Error("Crowd 点击标记初始化失败：Shader Duration 必须大于 0");
                enabled = false;
                return;
            }

            m_propertyBlock = new MaterialPropertyBlock();
            m_displayDuration = new WaitForSeconds(duration);
            m_renderer.enabled = false;
        }

        // 清理当前点击标记的隐藏协程引用
        private void OnDisable()
        {
            if (S_active == this)
                S_active = null;

            if (m_hideCoroutine != null)
                StopCoroutine(m_hideCoroutine);

            m_hideCoroutine = null;

            if (m_renderer != null)
                m_renderer.enabled = false;
        }

        /// <summary>
        /// 在指定地面位置显示点击标记并重新播放Shader动画
        /// </summary>
        /// <param name="worldPosition">地面命中位置</param>
        /// <param name="surfaceNormal">地面命中法线</param>
        public void Show(Vector3 worldPosition, Vector3 surfaceNormal)
        {
            if (!enabled)
                return;

            if (surfaceNormal.sqrMagnitude <= 0.0001f)
            {
                QLog.Error("显示 Crowd 点击标记失败：地面法线无效");
                return;
            }

            Vector3 normalizedNormal = surfaceNormal.normalized;
            Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, normalizedNormal);
            m_transform.SetPositionAndRotation(
                worldPosition + normalizedNormal * MARKER_HEIGHT_OFFSET,
                rotation);

            m_propertyBlock.SetFloat(S_StartTimePropertyId, Time.time);
            m_renderer.SetPropertyBlock(m_propertyBlock);
            m_renderer.enabled = true;

            if (m_hideCoroutine != null)
                StopCoroutine(m_hideCoroutine);

            m_hideCoroutine = StartCoroutine(HideAfterDurationCoroutine());
        }

        // 在Shader动画结束后关闭Renderer，避免透明Quad持续参与渲染
        private IEnumerator HideAfterDurationCoroutine()
        {
            yield return m_displayDuration;
            m_renderer.enabled = false;
            m_hideCoroutine = null;
        }
    }
}
