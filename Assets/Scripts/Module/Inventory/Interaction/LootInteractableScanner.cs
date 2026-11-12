/*
* ┌──────────────────────────────────┐
* │  描    述: 搜刮目标扫描器, 按键瞬间做一次物理粗筛并按目标类型取最近目标
* │  类    名: LootInteractableScanner.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
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
        private const float SCAN_RADIUS = 8f;

        // 交互契约是接口, 只能按 Type 取组件
        private static readonly Type m_interactableType = typeof(ILootInteractable);

        private readonly Collider[] m_hits = new Collider[MAX_RESULTS];
        private readonly ILootInteractable[] m_candidates = new ILootInteractable[MAX_RESULTS];

        private int m_layerMask = -1;
        private bool m_hasReportedLayer;
        private bool m_hasWarnedCapacity;

        /// <summary>
        /// 以 origin 为球心粗筛一轮, 返回指定类型中最近的可交互目标, 没有则返回 null
        /// </summary>
        /// <param name="origin">查询原点, 一般是玩家位置</param>
        public ILootInteractable ScanNearest<T>(Vector3 origin) where T : ILootInteractable
        {
            if (!TryResolveLayerMask()) return null;

            int hitCount = Physics.OverlapSphereNonAlloc(origin, SCAN_RADIUS, m_hits, m_layerMask,
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
                ILootInteractable target = hit.GetComponentInParent(m_interactableType) as ILootInteractable;
                if (target == null) continue;

                m_candidates[candidateCount++] = target;
            }

            return InteractableTargetSelector.SelectNearest<T>(m_candidates, candidateCount, origin);
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
