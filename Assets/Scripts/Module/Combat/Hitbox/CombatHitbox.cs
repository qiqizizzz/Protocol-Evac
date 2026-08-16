/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: 通用战斗命中盒，负责范围检测、目标去重与伤害提交
 * │  类    名: CombatHitbox.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Combat.Damage;
using UnityEngine;
using Utils.log;

namespace Module.Combat.Hitbox
{
    [RequireComponent(typeof(BoxCollider))]
    public sealed class CombatHitbox : MonoBehaviour
    {
        private const int OVERLAP_RESULT_CAPACITY = 32;

        [Tooltip("允许命中的目标 Layer")]
        [SerializeField] private LayerMask TargetLayers;
        
        private readonly Collider[] m_overlapResults = new Collider[OVERLAP_RESULT_CAPACITY];
        private readonly HashSet<IDamageable> m_hitTargets = new HashSet<IDamageable>();

        private BoxCollider m_boxCollider;
        private GameObject m_source;
        private float m_damage;
        private DamageReactionType m_reactionType;
        private bool m_isOpen;
        private bool m_hasWarnedCapacity;

        /// <summary>
        /// 开启命中窗口
        /// </summary>
        /// <param name="damage">本次命中伤害值</param>
        /// <param name="reactionType">本次命中的通用受击反应</param>
        /// <param name="source">伤害来源对象</param>
        public void Open(float damage, DamageReactionType reactionType, GameObject source)
        {
            if (damage <= 0f)
            {
                QLog.Error($"开启 CombatHitbox 失败，伤害值必须大于 0：{damage}");
                return;
            }

            if (source == null)
            {
                QLog.Error("开启 CombatHitbox 失败，伤害来源为空");
                return;
            }

            m_damage = damage;
            m_reactionType = reactionType;
            m_source = source;
            m_hitTargets.Clear();
            m_hasWarnedCapacity = false;
            m_isOpen = true;
        }

        // 关闭命中窗口
        public void Close()
        {
            m_isOpen = false;
            m_damage = 0f;
            m_reactionType = DamageReactionType.Light;
            m_source = null;
            m_hitTargets.Clear();
            m_hasWarnedCapacity = false;
        }
        
        #region 生命周期
        private void Awake()
        {
            m_boxCollider = GetComponent<BoxCollider>();
            if (m_boxCollider.size.x <= 0f || m_boxCollider.size.y <= 0f || m_boxCollider.size.z <= 0f)
            {
                QLog.Error("CombatHitbox 的 BoxCollider Size 必须全部大于 0");
                enabled = false;
                return;
            }

            if (TargetLayers.value == 0)
            {
                QLog.Error("CombatHitbox 未配置 TargetLayers");
                enabled = false;
                return;
            }

            // BoxCollider 仅作为可视化形状数据，不参与 Unity 物理碰撞
            m_boxCollider.enabled = false;
        }

        private void FixedUpdate()
        {
            if (!m_isOpen) return;
            
            DetectTargets();
        }

        private void OnDisable()
        {
            Close();
        }
        #endregion

        // 检测当前命中盒范围内的可受击目标
        private void DetectTargets()
        {
            Vector3 lossyScale = transform.lossyScale;
            Vector3 absoluteScale = new Vector3(
                Mathf.Abs(lossyScale.x),
                Mathf.Abs(lossyScale.y),
                Mathf.Abs(lossyScale.z));
            Vector3 hitboxCenter = transform.TransformPoint(m_boxCollider.center);
            Vector3 halfExtents = Vector3.Scale(m_boxCollider.size * 0.5f, absoluteScale);

            int hitCount = Physics.OverlapBoxNonAlloc(hitboxCenter, halfExtents, m_overlapResults,
                transform.rotation, TargetLayers.value, QueryTriggerInteraction.Ignore);

            if (hitCount == OVERLAP_RESULT_CAPACITY && !m_hasWarnedCapacity)
            {
                QLog.Warning($"CombatHitbox 检测结果达到容量上限：{OVERLAP_RESULT_CAPACITY}");
                m_hasWarnedCapacity = true;
            }

            for (int i = 0; i < hitCount; i++)
            {
                TryApplyDamage(m_overlapResults[i], hitboxCenter);
            }
        }

        // 尝试对目标造成伤害
        private void TryApplyDamage(Collider hitCollider, Vector3 hitboxCenter)
        {
            // 剔除无效目标与自身
            if (!hitCollider) return;
            if (hitCollider.transform.IsChildOf(m_source.transform)) return;

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();

            // 判断是否为有效可受伤目标或重复目标
            if (damageable == null || !m_hitTargets.Add(damageable)) return;

            Vector3 hitPoint = hitCollider.ClosestPoint(hitboxCenter);
            Vector3 hitDirection = (hitCollider.bounds.center - m_source.transform.position).normalized;

            DamageData damageData = new DamageData(m_damage, m_source, hitPoint, hitDirection, m_reactionType);
            
            damageable.TakeDamage(damageData);
        }

        private void OnDrawGizmosSelected()
        {
            BoxCollider boxCollider = GetComponent<BoxCollider>();
            Matrix4x4 previousMatrix = Gizmos.matrix;

            Gizmos.color = Color.red;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(boxCollider.center, boxCollider.size);
            Gizmos.matrix = previousMatrix;
        }
    }
}
