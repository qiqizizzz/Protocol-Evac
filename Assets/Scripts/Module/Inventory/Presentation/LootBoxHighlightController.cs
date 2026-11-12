/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱高亮控制器, 按附近可开启目标与当前交互焦点切换描边层级
* │  类    名: LootBoxHighlightController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Inventory.Interaction;
using UnityEngine;

namespace Module.Inventory.Presentation
{
    /// <summary>
    /// 物资箱高亮控制器：高亮粗筛与 F 交互查询分开, 保证远处目标可提示但不提前进入交互范围
    /// </summary>
    public sealed class LootBoxHighlightController
    {
        private readonly LootInteractableScanner m_scanner = new LootInteractableScanner();
        private readonly List<LootBoxController> m_nearbyBoxes = new List<LootBoxController>();
        private readonly HashSet<LootBoxController> m_currentBoxes = new HashSet<LootBoxController>();
        private readonly Dictionary<LootBoxController, LootBoxHighlightView> m_activeViews =
            new Dictionary<LootBoxController, LootBoxHighlightView>();
        private readonly List<LootBoxController> m_staleBoxes = new List<LootBoxController>();

        /// <summary>
        /// 刷新附近物资箱高亮, 当前 F 目标使用强描边, 其他可开启箱子使用弱描边
        /// </summary>
        /// <param name="origin">玩家胸口查询原点</param>
        /// <param name="forward">相机水平朝向</param>
        /// <param name="queryConfig">F 交互目标查询参数</param>
        /// <param name="obstacleMask">焦点目标的墙体遮挡层</param>
        /// <param name="highlightRadius">物资箱可开启提示半径</param>
        public void Refresh(Vector3 origin, Vector3 forward, InteractionQueryConfig queryConfig, LayerMask obstacleMask,
            float highlightRadius)
        {
            m_scanner.CollectNearby(origin, highlightRadius, m_nearbyBoxes);
            LootBoxController focused =
                m_scanner.ScanBest<LootBoxController>(origin, forward, queryConfig, obstacleMask) as LootBoxController;

            m_currentBoxes.Clear();
            for (int i = 0; i < m_nearbyBoxes.Count; i++)
            {
                LootBoxController box = m_nearbyBoxes[i];
                if (box == null) continue;

                m_currentBoxes.Add(box);
                LootBoxHighlightView view = ResolveView(box);
                view.SetState(ReferenceEquals(box, focused)
                    ? LootBoxHighlightView.HighlightState.Focused
                    : LootBoxHighlightView.HighlightState.Available);

                if (!m_activeViews.ContainsKey(box)) m_activeViews.Add(box, view);
            }

            HideStaleViews();
        }

        /// <summary>
        /// 关闭所有已显示描边, 面板打开或玩家离开场景时调用
        /// </summary>
        public void HideAll()
        {
            foreach (LootBoxHighlightView view in m_activeViews.Values)
            {
                if (view != null) view.SetState(LootBoxHighlightView.HighlightState.None);
            }

            m_activeViews.Clear();
            m_currentBoxes.Clear();
        }

        // 只清理上一轮存在且本轮不在范围内的箱子
        private void HideStaleViews()
        {
            m_staleBoxes.Clear();
            foreach (KeyValuePair<LootBoxController, LootBoxHighlightView> pair in m_activeViews)
            {
                if (pair.Key == null || !m_currentBoxes.Contains(pair.Key)) m_staleBoxes.Add(pair.Key);
            }

            for (int i = 0; i < m_staleBoxes.Count; i++)
            {
                LootBoxController box = m_staleBoxes[i];
                if (m_activeViews.TryGetValue(box, out LootBoxHighlightView view) && view != null)
                    view.SetState(LootBoxHighlightView.HighlightState.None);

                m_activeViews.Remove(box);
            }
        }

        // 高亮视图是运行期轻量附加组件, 首次进入提示范围时创建
        private static LootBoxHighlightView ResolveView(LootBoxController box)
        {
            if (box.TryGetComponent(out LootBoxHighlightView view)) return view;

            return box.gameObject.AddComponent<LootBoxHighlightView>();
        }
    }
}
