/*
 * ┌──────────────────────────────────┐
 * │  描    述: 群体空间哈希系统                      
 * │  类    名: CrowdSpatialHashSystem.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Data;
using Framework.QTower.ECS.Crowd.System.Common;
using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Framework.QTower.ECS.Crowd.System
{
    public sealed class CrowdSpatialHashSystem : ECSSystem
    {
        private const int CELL_KEY_SHIFT = 32;
        private const int DEFAULT_BUCKET_CAPACITY = 8;
        private const float MIN_CELL_SIZE = 0.5f;

        private static readonly int[] S_RequireComponents =
        {
            (int)CrowdComponentType.Agent
        };

        private readonly Dictionary<long, List<int>> m_cells = new Dictionary<long, List<int>>();
        private readonly CrowdAvoidanceData m_avoidanceData;

        private float m_cellSize = MIN_CELL_SIZE;
        private int m_agentCount;

        #region 字段
        public int CellCount
        {
            get
            {
                return m_cells.Count;
            }
        }

        public int AgentCount
        {
            get
            {
                return m_agentCount;
            }
        }

        public override int SysType
        {
            get
            {
                return (int)CrowdSystemType.SpatialHash;
            }
        }

        public override int[] RequireComponents
        {
            get
            {
                return S_RequireComponents;
            }
        }
        #endregion


        public CrowdSpatialHashSystem(WorldBase world, CrowdAvoidanceData avoidanceData) : base(world)
        {
            m_avoidanceData = avoidanceData;
        }

        public override void FixedTick(float fixedDeltaTime)
        {
            Rebuild();
        }

        public void QueryCandidates(Vector3 center, List<int> results)
        {
            results.Clear();

            int centerCellX = Mathf.FloorToInt(center.x / m_cellSize);
            int centerCellZ = Mathf.FloorToInt(center.z / m_cellSize);

            for (int x = centerCellX - 1; x <= centerCellX + 1; x++)
            {
                for (int z = centerCellZ - 1; z <= centerCellZ + 1; z++)
                {
                    if (!m_cells.TryGetValue(GetCellKey(x, z), out List<int> bucket))
                        continue;

                    for (int i = 0; i < bucket.Count; i++)
                        results.Add(bucket[i]);
                }
            }
        }

        // 按代理最新位置重建格子，需要即时相邻关系时由外部主动调用
        public void Rebuild()
        {
            m_cellSize = Mathf.Max(MIN_CELL_SIZE, m_avoidanceData.NeighborRadius);
            m_agentCount = 0;

            foreach (List<int> bucket in m_cells.Values)
                bucket.Clear();

            for (int i = 0; i < Entities.Count; i++)
            {
                ECSEntity entity = World.GetEntity(Entities[i]);
                if(entity == null) continue;
                
                CrowdAgentComponent agent = entity.GetComponent<CrowdAgentComponent>((int) CrowdComponentType.Agent);
                if(agent == null) continue;

                AddToCell(entity.EntityId, agent.Data.Position);
                m_agentCount++;
            }
        }

        private void AddToCell(int entityId, Vector3 position)
        {
            int cellX = Mathf.FloorToInt(position.x / m_cellSize);
            int cellZ = Mathf.FloorToInt(position.z / m_cellSize);
            long key = GetCellKey(cellX, cellZ);

            if (!m_cells.TryGetValue(key, out List<int> bucket))
            {
                bucket = new List<int>(DEFAULT_BUCKET_CAPACITY);
                m_cells.Add(key, bucket);
            }
            
            bucket.Add(entityId);
        }

        private long GetCellKey(int cellX, int cellZ)
        {
            return ((long) cellX << CELL_KEY_SHIFT) | (uint) cellZ;
        }
    }
}