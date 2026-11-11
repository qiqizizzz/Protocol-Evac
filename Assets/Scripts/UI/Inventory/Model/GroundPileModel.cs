/*
* ┌──────────────────────────────────┐
* │  描    述: 地面垃圾堆登记表实现, 维护堆的锚点与范围查询
* │  类    名: GroundPileModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    public class GroundPileModel : IGroundPileModel
    {
        //堆号前缀, 与背包侧的容器号、实例号区分开, 便于日志与调试直接辨认
        private const string ID_PREFIX = "pile-";

        //堆号序号位数
        private const int ID_DIGITS = 6;

        //无效半径查询时复用的空结果, 避免每次白分配一个数组
        private static readonly GroundPileVO[] m_emptyResults = Array.Empty<GroundPileVO>();

        //在册堆表, 按起堆顺序排列; 同屏堆数量是个位到几十的量级, 线性查就够
        private readonly List<GroundPileVO> m_piles = new List<GroundPileVO>();

        //范围查询的临时结果表, 只用于排序, 不直接交给调用方
        private readonly List<GroundPileVO> m_queryResults = new List<GroundPileVO>();

        private int m_nextSequence;

        public event Action OnChanged;

        public IReadOnlyList<GroundPileVO> Piles => m_piles;

        public void OnRegister()
        {
            m_nextSequence = 0;
        }

        public void OnUnregister()
        {
            m_piles.Clear();
            m_queryResults.Clear();
        }

        /// <summary>
        /// 起一堆并广播变化, 同一只容器不允许挂两座堆
        /// </summary>
        /// <param name="containerId">承载这堆物品的运行时容器号</param>
        /// <param name="anchorPosition">堆中心世界坐标</param>
        /// <returns>分配到的堆号, 容器号已被占用时返回 null</returns>
        public string CreatePile(int containerId, Vector3 anchorPosition)
        {
            if (TryGetByContainer(containerId, out GroundPileVO existing))
            {
                QLog.Error($"容器 {containerId} 已经登记过堆 {existing.PileId}, 拒绝重复起堆");
                return null;
            }

            string pileId = ID_PREFIX + (++m_nextSequence).ToString($"D{ID_DIGITS}");
            m_piles.Add(new GroundPileVO(pileId, containerId, anchorPosition, Time.time));

            OnChanged?.Invoke();
            return pileId;
        }

        public bool TryGet(string pileId, out GroundPileVO pile)
        {
            if (string.IsNullOrEmpty(pileId))
            {
                pile = null;
                return false;
            }

            for (int i = 0; i < m_piles.Count; i++)
            {
                if (m_piles[i].PileId != pileId) continue;

                pile = m_piles[i];
                return true;
            }

            pile = null;
            return false;
        }

        public bool TryGetByContainer(int containerId, out GroundPileVO pile)
        {
            for (int i = 0; i < m_piles.Count; i++)
            {
                if (m_piles[i].ContainerId != containerId) continue;

                pile = m_piles[i];
                return true;
            }

            pile = null;
            return false;
        }

        /// <summary>
        /// 找半径内最近的一座堆
        /// </summary>
        /// <param name="center">查询中心</param>
        /// <param name="radius">查询半径</param>
        public bool TryFindNearest(Vector3 center, float radius, out GroundPileVO pile)
        {
            pile = null;
            if (radius < 0f) return false;

            float bestSqr = radius * radius;
            for (int i = 0; i < m_piles.Count; i++)
            {
                GroundPileVO candidate = m_piles[i];
                float sqr = SqrDistance(candidate.AnchorPosition, center);
                if (sqr > bestSqr) continue;

                bestSqr = sqr;
                pile = candidate;
            }

            return pile != null;
        }

        /// <summary>
        /// 查指定半径内的堆, 结果由近到远排列
        /// </summary>
        /// <param name="center">查询中心</param>
        /// <param name="radius">查询半径</param>
        public IReadOnlyList<GroundPileVO> QueryNearby(Vector3 center, float radius)
        {
            if (radius < 0f)
            {
                QLog.Error($"地面堆查询半径无效：{radius}");
                return m_emptyResults;
            }

            float sqrRadius = radius * radius;
            m_queryResults.Clear();

            for (int i = 0; i < m_piles.Count; i++)
            {
                GroundPileVO pile = m_piles[i];
                if (SqrDistance(pile.AnchorPosition, center) > sqrRadius) continue;
                m_queryResults.Add(pile);
            }

            m_queryResults.Sort((a, b) => SqrDistance(a.AnchorPosition, center)
                .CompareTo(SqrDistance(b.AnchorPosition, center)));

            //交接的是快照数组, 复用表随时会被下一次查询清空, 不能直接递出去
            return m_queryResults.ToArray();
        }

        public void RemovePile(string pileId)
        {
            if (string.IsNullOrEmpty(pileId)) return;

            for (int i = 0; i < m_piles.Count; i++)
            {
                if (m_piles[i].PileId != pileId) continue;

                m_piles.RemoveAt(i);
                OnChanged?.Invoke();
                return;
            }
        }

        //平方距离, 排序与范围过滤都用它, 省掉开方
        private static float SqrDistance(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude;
        }
    }
}
