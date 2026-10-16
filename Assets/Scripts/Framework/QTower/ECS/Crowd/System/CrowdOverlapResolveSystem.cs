/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd ECS重叠修正系统，在运动积分后按硬约束分离穿模的代理位置
* │  类    名: CrowdOverlapResolveSystem.cs
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
    public sealed class CrowdOverlapResolveSystem : ECSSystem
    {
        private const int RESOLVE_ITERATION = 2;
        private const float MIN_DISTANCE = 0.0001f;
        private const float MIN_MASS = 0.01f;
        // 位置完全重合时按自身编号取散开方向，黄金角让不同代理不至于全部同向
        private const float GOLDEN_ANGLE = 2.399963f;

        private static readonly int[] S_RequireComponents =
        {
            (int)CrowdComponentType.Agent
        };

        private readonly CrowdSpatialHashSystem m_hashSystem;
        private readonly CrowdAvoidanceData m_avoidanceData;
        private readonly List<int> m_candidates = new List<int>();

        #region 属性

        public override int SysType
        {
            get { return (int)CrowdSystemType.OverlapResolve; }
        }

        public override int[] RequireComponents
        {
            get { return S_RequireComponents; }
        }

        // 全部修正结束后残存的最大重叠深度，为 0 说明没有穿模
        public float RemainingPenetration { get; private set; }

        // 首轮参与修正的重叠代理对数量
        public int CorrectedPairCount { get; private set; }

        #endregion

        // 创建重叠修正系统，相邻查询复用空间哈希系统
        public CrowdOverlapResolveSystem(WorldBase world, CrowdSpatialHashSystem hashSystem,
            CrowdAvoidanceData avoidanceData) : base(world)
        {
            if (hashSystem == null)
                QLog.Error("创建 Crowd 重叠修正系统失败：空间哈希系统为空");

            if (avoidanceData == null)
                QLog.Error("创建 Crowd 重叠修正系统失败：避障参数为空");

            m_hashSystem = hashSystem;
            m_avoidanceData = avoidanceData;
        }

        #region Public Function

        // 运动积分之后修正位置，重叠强度为 0 时只测量不写入，等同于该系统不存在
        public override void FixedTick(float fixedDeltaTime)
        {
            if (m_hashSystem == null || m_avoidanceData == null)
                return;

            RemainingPenetration = 0f;
            CorrectedPairCount = 0;

            float strength = Mathf.Clamp01(m_avoidanceData.OverlapStrength);
            if (strength > 0f)
            {
                for (int iteration = 0; iteration < RESOLVE_ITERATION; iteration++)
                {
                    // 位置已被运动系统改写，必须重建哈希才能查到最新的相邻关系
                    m_hashSystem.Rebuild();
                    ResolveOnce(strength, iteration == 0);
                }
            }

            m_hashSystem.Rebuild();
            RemainingPenetration = ResolveOnce(0f, false);
        }

        #endregion

        #region Private Function

        // 单轮分离每一对重叠代理，返回本轮观测到的最大重叠深度
        private float ResolveOnce(float strength, bool countPair)
        {
            float maxPenetration = 0f;

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
                m_hashSystem.QueryCandidates(data.Position, m_candidates);

                bool isChanged = false;
                for (int c = 0; c < m_candidates.Count; c++)
                {
                    // 只由编号较小的一方处理，保证每对只算一次同时天然排除自身
                    if (m_candidates[c] <= data.EntityId)
                        continue;

                    CrowdAgentComponent otherAgent = TryGetAgent(m_candidates[c]);
                    if (otherAgent == null)
                        continue;

                    CrowdAgentData other = otherAgent.Data;

                    Vector3 offset = other.Position - data.Position;
                    offset.y = 0f;
                    float distance = offset.magnitude;
                    float contactDistance = Mathf.Max(MIN_DISTANCE, data.Radius + other.Radius);
                    if (distance >= contactDistance)
                        continue;

                    if (countPair)
                        CorrectedPairCount++;

                    float penetration = contactDistance - distance;
                    if (penetration > maxPenetration)
                        maxPenetration = penetration;

                    Vector3 away = GetAwayDirection(data, offset, distance);
                    float selfMass = Mathf.Max(MIN_MASS, data.Mass);
                    float otherMass = Mathf.Max(MIN_MASS, other.Mass);
                    // 按质量比例分配位移，重的代理让路更少
                    float otherShare = selfMass / (selfMass + otherMass);

                    Vector3 push = away * (penetration * strength);
                    data.Position -= push * (1f - otherShare);
                    other.Position += push * otherShare;
                    isChanged = true;

                    // 立即写回，同一轮内后续代理读到的是已修正的位置
                    otherAgent.Data = other;
                }

                if (isChanged)
                    agent.Data = data;
            }

            return maxPenetration;
        }

        // 读取候选代理的运行组件
        private CrowdAgentComponent TryGetAgent(int entityId)
        {
            ECSEntity entity = World.GetEntity(entityId);
            if (entity == null)
                return null;

            return entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
        }

        // 取指向对方的单位方向，位置重合时按自身编号散开
        private Vector3 GetAwayDirection(CrowdAgentData data, Vector3 offset, float distance)
        {
            if (distance > MIN_DISTANCE)
                return offset / distance;

            float angle = data.EntityId * GOLDEN_ANGLE;
            return new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }

        #endregion
    }
}
