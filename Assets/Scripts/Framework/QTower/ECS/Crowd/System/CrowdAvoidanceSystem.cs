/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd ECS动态避障系统，基于空间哈希求解重叠分离与碰撞规避修正力
* │  类    名: CrowdAvoidanceSystem.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Data;
using Framework.QTower.ECS.Crowd.System.Common;
using Framework.QTower.Event.ECS;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.ECS.Crowd.System
{
    public sealed class CrowdAvoidanceSystem : ECSSystem
    {
        private const float MIN_DISTANCE = 0.0001f;
        private const float MIN_SQR_SPEED = 0.0001f;
        private const float MIN_SQR_FORCE = 0.000001f;
        private const float SLIP_BIAS = 0.4f;
        // 位置完全重合时按自身编号取散开方向，黄金角让不同代理不至于全部同向
        private const float GOLDEN_ANGLE = 2.399963f;

        private static readonly int[] S_RequireComponents =
        {
            (int)CrowdComponentType.Agent
        };

        private readonly CrowdSpatialHashSystem m_hashSystem;
        private readonly CrowdAvoidanceData m_avoidanceData;
        private readonly List<int> m_candidates = new List<int>();
        private readonly List<NeighborCandidate> m_neighbors = new List<NeighborCandidate>();

        #region 属性

        public override int SysType
        {
            get { return (int)CrowdSystemType.Avoidance; }
        }

        public override int[] RequireComponents
        {
            get { return S_RequireComponents; }
        }

        // 本帧全场最大身体重叠深度，为 0 表示没有穿模
        public float MaxPenetration { get; private set; }

        // 本帧是否存在非法速度，NaN 一旦进入积分不会自行恢复
        public bool HasInvalidVelocity { get; private set; }

        #endregion

        // 创建避障系统，候选邻居由空间哈希系统提供
        public CrowdAvoidanceSystem(WorldBase world, CrowdSpatialHashSystem hashSystem,
            CrowdAvoidanceData avoidanceData) : base(world)
        {
            if (hashSystem == null)
                QLog.Error("创建 Crowd 避障系统失败：空间哈希系统为空");

            if (avoidanceData == null)
                QLog.Error("创建 Crowd 避障系统失败：避障参数为空");

            m_hashSystem = hashSystem;
            m_avoidanceData = avoidanceData;
        }

        #region Public Function

        // 每个固定步长重算所有代理的避障修正力，只写 AvoidanceForce 不改位置
        public override void FixedTick(float fixedDeltaTime)
        {
            if (m_hashSystem == null || m_avoidanceData == null)
                return;

            MaxPenetration = 0f;
            HasInvalidVelocity = false;

            for (int i = 0; i < Entities.Count; i++)
            {
                ECSEntity entity = World.GetEntity(Entities[i]);
                if (entity == null)
                    continue;

                CrowdAgentComponent agent =
                    entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
                if (agent == null)
                    continue;

                CrowdAgentData data = agent.Data;

                if (IsInvalidVector(data.Velocity))
                    HasInvalidVelocity = true;

                data.AvoidanceForce = CalculateAvoidanceForce(data);
                agent.Data = data;
            }
        }

        #endregion

        #region Private Function

        // 汇总单个代理的重叠分离与碰撞规避，输出期望速度修正量
        private Vector3 CalculateAvoidanceForce(CrowdAgentData data)
        {
            CollectNeighbors(data);

            float separationMargin = Mathf.Max(0f, m_avoidanceData.SeparationMargin);
            float predictTime = Mathf.Max(0f, m_avoidanceData.PredictTime);
            Vector3 separation = Vector3.zero;
            Vector3 collision = Vector3.zero;

            for (int i = 0; i < m_neighbors.Count; i++)
            {
                NeighborCandidate candidate = m_neighbors[i];
                CrowdAgentData neighbor = candidate.Data;

                Vector3 offset = neighbor.Position - data.Position;
                offset.y = 0f;
                float distance = Mathf.Sqrt(candidate.SqrDistance);
                float contactDistance = Mathf.Max(MIN_DISTANCE,
                    data.Radius + neighbor.Radius + separationMargin);

                // 穿模深度只按身体半径统计，安全间距仅参与推力计算
                float penetration = data.Radius + neighbor.Radius - distance;
                if (penetration > MaxPenetration)
                    MaxPenetration = penetration;

                // 已经重叠或进入安全间距，直接沿分离方向顶开
                if (distance < contactDistance)
                    separation += GetAwayDirection(data, offset, distance) * (contactDistance - distance);

                Vector3 relativeVelocity = neighbor.Velocity - data.Velocity;
                float sqrRelativeSpeed = relativeVelocity.sqrMagnitude;

                // 相对速度接近零时没有相遇时刻可预测，退化为用当前位置判断
                float approachTime = 0f;
                if (sqrRelativeSpeed > MIN_SQR_SPEED)
                    approachTime = Mathf.Clamp(-Vector3.Dot(offset, relativeVelocity) / sqrRelativeSpeed,
                        0f, predictTime);

                Vector3 predictedOffset = offset + relativeVelocity * approachTime;
                predictedOffset.y = 0f;
                float predictedDistance = predictedOffset.magnitude;
                if (predictedDistance >= contactDistance)
                    continue;

                float urgency = 1f - predictedDistance / contactDistance;
                Vector3 away = GetAwayDirection(data, predictedOffset, predictedDistance);

                // 正面对冲时双方受力完全对称会互相顶死，按朝向固定选一侧让开
                Vector3 slip = Vector3.Cross(Vector3.up, away);
                if (Vector3.Dot(slip, data.Forward) < 0f)
                    slip = -slip;

                collision += (away + slip * SLIP_BIAS) * urgency;
            }

            return CombineForce(separation, collision, data.MaxSpeed);
        }

        // 从哈希候选中筛出查询半径内的邻居，按距离只保留最近若干个
        // 注意：NeighborRadius 必须大于两代理身体半径之和，否则真实重叠会被半径提前剔除
        private void CollectNeighbors(CrowdAgentData data)
        {
            float queryRadius = m_avoidanceData.NeighborRadius;
            float sqrQueryRadius = queryRadius * queryRadius;

            m_neighbors.Clear();
            m_hashSystem.QueryCandidates(data.Position, m_candidates);

            for (int i = 0; i < m_candidates.Count; i++)
            {
                // 哈希层不排除自身，相对位置为零会让归一化得到 NaN
                if (m_candidates[i] == data.EntityId)
                    continue;

                CrowdAgentData neighbor;
                if (!TryGetNeighborData(m_candidates[i], out neighbor))
                    continue;

                Vector3 offset = neighbor.Position - data.Position;
                offset.y = 0f;
                float sqrDistance = offset.sqrMagnitude;
                if (sqrDistance > sqrQueryRadius)
                    continue;

                NeighborCandidate kept;
                kept.EntityId = m_candidates[i];
                kept.SqrDistance = sqrDistance;
                kept.Data = neighbor;
                m_neighbors.Add(kept);
            }

            m_neighbors.Sort(CompareByDistance);

            int maxNeighborCount = Mathf.Max(1, m_avoidanceData.MaxNeighborCount);
            if (m_neighbors.Count > maxNeighborCount)
                m_neighbors.RemoveRange(maxNeighborCount, m_neighbors.Count - maxNeighborCount);
        }

        // 读取候选邻居的运行数据
        private bool TryGetNeighborData(int entityId, out CrowdAgentData data)
        {
            data = default(CrowdAgentData);

            ECSEntity entity = World.GetEntity(entityId);
            if (entity == null)
                return false;

            CrowdAgentComponent agent =
                entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
            if (agent == null)
                return false;

            data = agent.Data;
            return true;
        }

        // 取远离邻居的单位方向，位置重合时按自身编号散开
        private Vector3 GetAwayDirection(CrowdAgentData data, Vector3 offset, float distance)
        {
            if (distance > MIN_DISTANCE)
                return -offset / distance;

            float angle = data.EntityId * GOLDEN_ANGLE;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        // 两类修正各自归一化到最大速度量级后按权重叠加，权重为 0 即该层完全关闭
        private Vector3 CombineForce(Vector3 separation, Vector3 collision, float maxSpeed)
        {
            Vector3 force = Vector3.zero;
            float speedLimit = Mathf.Max(0f, maxSpeed);

            if (separation.sqrMagnitude > MIN_SQR_FORCE)
                force += separation.normalized * speedLimit *
                    Mathf.Max(0f, m_avoidanceData.SeparationStrength);

            if (collision.sqrMagnitude > MIN_SQR_FORCE)
                force += collision.normalized * speedLimit *
                    Mathf.Max(0f, m_avoidanceData.AvoidanceStrength);

            return force;
        }

        // 检查速度分量是否已被污染
        private bool IsInvalidVector(Vector3 velocity)
        {
            return float.IsNaN(velocity.x) || float.IsNaN(velocity.y) || float.IsNaN(velocity.z);
        }

        // 邻居候选按距离升序排列
        private static int CompareByDistance(NeighborCandidate left, NeighborCandidate right)
        {
            return left.SqrDistance.CompareTo(right.SqrDistance);
        }

        #endregion

        // 候选邻居，携带避障需要的数据副本
        private struct NeighborCandidate
        {
            public int EntityId;
            public float SqrDistance;
            public CrowdAgentData Data;
        }
    }
}
