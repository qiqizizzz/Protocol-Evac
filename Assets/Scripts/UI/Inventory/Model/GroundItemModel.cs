/*
* ┌──────────────────────────────────┐
* │  描    述: 地面物品登记表实现, 维护被丢到世界里的物品快照与范围查询
* │  类    名: GroundItemModel.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace UI.Inventory
{
    public class GroundItemModel : IGroundItemModel
    {
        //登记号前缀, 与背包侧的容器号、实例号区分开, 便于日志与调试直接辨认
        private const string ID_PREFIX = "ground-";

        //登记号序号位数
        private const int ID_DIGITS = 6;

        //无效半径查询时复用的空结果, 避免每次白分配一个数组
        private static readonly GroundItemVO[] m_emptyResults = Array.Empty<GroundItemVO>();

        private readonly Dictionary<string, GroundItemVO> m_items = new Dictionary<string, GroundItemVO>();

        //范围查询的临时结果表, 只用于排序, 不直接交给调用方
        private readonly List<GroundItemVO> m_queryResults = new List<GroundItemVO>();

        private int m_nextSequence;

        public event Action OnChanged;

        public void OnRegister()
        {
            m_nextSequence = 0;
        }

        public void OnUnregister()
        {
            m_items.Clear();
            m_queryResults.Clear();
        }

        /// <summary>
        /// 登记一件落在世界里的物品, 分配登记号并广播变化
        /// </summary>
        /// <param name="containerId">承载这件物品的运行时容器号, 捡回背包要靠它做转移</param>
        /// <param name="item">物品本体快照</param>
        /// <param name="worldPosition">落点世界坐标</param>
        /// <returns>分配到的登记号, 物品快照缺失时返回 null</returns>
        public string Register(int containerId, InventoryItemVO item, Vector3 worldPosition)
        {
            if (item == null)
            {
                QLog.Error($"登记地面物品缺少物品快照, 容器：{containerId}");
                return null;
            }

            string groundItemId = ID_PREFIX + (++m_nextSequence).ToString($"D{ID_DIGITS}");
            m_items[groundItemId] = new GroundItemVO(groundItemId, containerId, item, worldPosition, Time.time);

            OnChanged?.Invoke();
            return groundItemId;
        }

        public bool TryGet(string groundItemId, out GroundItemVO item)
        {
            if (string.IsNullOrEmpty(groundItemId))
            {
                item = null;
                return false;
            }

            return m_items.TryGetValue(groundItemId, out item);
        }

        /// <summary>
        /// 查指定半径内的地面物品, 结果由近到远排列
        /// </summary>
        /// <param name="center">查询中心</param>
        /// <param name="radius">查询半径</param>
        public IReadOnlyList<GroundItemVO> QueryNearby(Vector3 center, float radius)
        {
            if (radius < 0f)
            {
                QLog.Error($"地面物品查询半径无效：{radius}");
                return m_emptyResults;
            }

            float sqrRadius = radius * radius;
            m_queryResults.Clear();

            foreach (GroundItemVO item in m_items.Values)
            {
                if ((item.WorldPosition - center).sqrMagnitude > sqrRadius) continue;
                m_queryResults.Add(item);
            }

            m_queryResults.Sort((a, b) => SqrDistance(a.WorldPosition, center)
                .CompareTo(SqrDistance(b.WorldPosition, center)));

            //交接的是快照数组, 复用表随时会被下一次查询清空, 不能直接递出去
            return m_queryResults.ToArray();
        }

        public void Unregister(string groundItemId)
        {
            if (string.IsNullOrEmpty(groundItemId)) return;
            if (!m_items.Remove(groundItemId)) return;

            OnChanged?.Invoke();
        }

        //平方距离, 排序与范围过滤都用它, 省掉开方
        private static float SqrDistance(Vector3 a, Vector3 b)
        {
            return (a - b).sqrMagnitude;
        }
    }
}
