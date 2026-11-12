/*
* ┌──────────────────────────────────┐
* │  描    述: 搜刮目标扫描器, 共用物理粗筛并按按键语义选择最近或评分最高的目标
* │  类    名: LootInteractableScanner.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Interaction
{
    /// <summary>
    /// 搜刮扫描器：交互时一次性 OverlapSphere 粗筛, 物资箱与地面堆都会进候选, 再按调用方要求的类型筛选
    /// </summary>
    public sealed class LootInteractableScanner
    {
        private const int MAX_RESULTS = 32;
        private const float SCAN_RADIUS_MARGIN = 0.5f;

        // 交互契约是接口, 只能按 Type 取组件
        private static readonly Type S_InteractableType = typeof(ILootInteractable);

        private readonly Collider[] m_hits = new Collider[MAX_RESULTS];
        private readonly RaycastHit[] m_obstacleHits = new RaycastHit[MAX_RESULTS];
        private readonly ILootInteractable[] m_candidates = new ILootInteractable[MAX_RESULTS];

        private int m_layerMask = -1;
        private bool m_hasReportedLayer;
        private bool m_hasWarnedCapacity;

        /// <summary>
        /// 以 origin 为球心粗筛一轮, 返回指定类型中最近的可交互目标, 没有则返回 null
        /// </summary>
        /// <param name="origin">查询原点, 一般是玩家位置</param>
        /// <param name="scanRadius">物理粗筛半径</param>
        public ILootInteractable ScanNearest<T>(Vector3 origin, float scanRadius) where T : ILootInteractable
        {
            int candidateCount = CollectCandidates(origin, scanRadius);
            if (candidateCount == 0) return null;

            return InteractableTargetSelector.SelectNearest<T>(m_candidates, candidateCount, origin);
        }

        /// <summary>
        /// 收集粗筛范围内所有配置齐全的指定类型目标, 供与交互判定分开的高亮扫描使用
        /// </summary>
        /// <param name="origin">查询原点</param>
        /// <param name="scanRadius">物理粗筛半径</param>
        /// <param name="results">复用的结果列表, 调用时先清空</param>
        /// <returns>收集到的目标数量</returns>
        public int CollectNearby<T>(Vector3 origin, float scanRadius, List<T> results) where T : ILootInteractable
        {
            if (results == null) throw new ArgumentNullException(nameof(results));

            results.Clear();
            int candidateCount = CollectCandidates(origin, scanRadius);
            for (int i = 0; i < candidateCount; i++)
            {
                if (m_candidates[i] is T target && target.CanInteract()) results.Add(target);
            }

            return results.Count;
        }

        /// <summary>
        /// 按距离、相机夹角与视线选择最符合玩家意图的指定类型目标
        /// </summary>
        /// <param name="origin">查询原点, 一般是玩家胸口</param>
        /// <param name="forward">相机水平朝向</param>
        /// <param name="config">距离、夹角、近身兜底与评分参数</param>
        /// <param name="obstacleMask">墙体遮挡检测层</param>
        public ILootInteractable ScanBest<T>(Vector3 origin, Vector3 forward, InteractionQueryConfig config,
            LayerMask obstacleMask) where T : ILootInteractable
        {
            float scanRadius = Mathf.Max(0f, config.MaxDistance) + SCAN_RADIUS_MARGIN;
            int candidateCount = CollectCandidates(origin, scanRadius);
            if (candidateCount == 0) return null;

            return InteractableTargetSelector.SelectBest<T>(m_candidates, candidateCount, origin, forward, config,
                target => HasLineOfSight(origin, target, obstacleMask));
        }

        // 共用物理粗筛, 同一目标被多个碰撞体命中时只保留一次
        private int CollectCandidates(Vector3 origin, float scanRadius)
        {
            if (!TryResolveLayerMask()) return 0;

            int hitCount = Physics.OverlapSphereNonAlloc(origin, scanRadius, m_hits, m_layerMask,
                QueryTriggerInteraction.Collide);

            // 缓冲写满说明还有候选没被取回, 只告警一次, 免得每帧刷屏
            if (hitCount >= MAX_RESULTS && !m_hasWarnedCapacity)
            {
                m_hasWarnedCapacity = true;
                QLog.Warning($"交互体粗筛命中数达到上限 {MAX_RESULTS}, 结果可能被截断");
            }

            int candidateCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = m_hits[i];
                if (hit == null) continue;

                // 交互体是目标自建的子节点, 契约实现在父级身上
                ILootInteractable target = hit.GetComponentInParent(S_InteractableType) as ILootInteractable;
                if (target == null || ContainsCandidate(candidateCount, target)) continue;

                m_candidates[candidateCount++] = target;
            }

            return candidateCount;
        }

        // 判断本轮候选缓冲里是否已经收过同一目标
        private bool ContainsCandidate(int count, ILootInteractable target)
        {
            for (int i = 0; i < count; i++)
            {
                if (ReferenceEquals(m_candidates[i], target)) return true;
            }

            return false;
        }

        // 中心点与偏上沿任一条射线通畅即可交互, 避免半高掩体误挡整只箱子
        private bool HasLineOfSight(Vector3 origin, ILootInteractable target, LayerMask obstacleMask)
        {
            if (!(target is Component targetComponent)) return true;

            Bounds bounds = target.InteractionBounds;
            if (HasClearLine(origin, bounds.center, targetComponent.transform, obstacleMask)) return true;

            Vector3 upperPoint = bounds.center + Vector3.up * (bounds.extents.y * 0.5f);
            return HasClearLine(origin, upperPoint, targetComponent.transform, obstacleMask);
        }

        // 目标自身及其子级碰撞体不参与遮挡判定
        private bool HasClearLine(Vector3 origin, Vector3 endpoint, Transform targetTransform, LayerMask obstacleMask)
        {
            Vector3 delta = endpoint - origin;
            float distance = delta.magnitude;
            if (distance <= 0.0001f) return true;

            int hitCount = Physics.RaycastNonAlloc(origin, delta / distance, m_obstacleHits, distance,
                obstacleMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = m_obstacleHits[i].collider;
                if (hitCollider == null) continue;
                if (hitCollider.transform == targetTransform || hitCollider.transform.IsChildOf(targetTransform))
                    continue;

                return false;
            }

            return true;
        }

        // 交互层掩码只在首轮解析一次, 层没登记时明确报错而不是退回全层扫描
        private bool TryResolveLayerMask()
        {
            if (m_layerMask >= 0) return true;

            int layerIndex = LootBoxController.ResolveInteractableLayer();
            if (layerIndex < 0)
            {
                if (!m_hasReportedLayer)
                {
                    m_hasReportedLayer = true;
                    QLog.Error("物资箱交互层不可用, 本轮扫描跳过");
                }

                return false;
            }

            m_layerMask = 1 << layerIndex;
            return true;
        }
    }
}
