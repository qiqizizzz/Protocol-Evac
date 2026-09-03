/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd ECS运动系统，负责代理速度积分与目标抵达处理
* │  类    名: CrowdMovementSystem.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Data;
using Framework.QTower.ECS.Crowd.System.Common;
using UnityEngine;

namespace Framework.QTower.ECS.Crowd.System
{
    public sealed class CrowdMovementSystem : ECSSystem
    {
        private const float ARRIVAL_DISTANCE = 0.05f;
        private const float MIN_MASS = 0.01f;
        private const float MIN_SPEED = 0.01f;

        private static readonly int[] S_RequireComponents =
        {
            (int)CrowdComponentType.Agent
        };

        public override int SysType
        {
            get { return (int)CrowdSystemType.Movement; }
        }

        public override int[] RequireComponents
        {
            get { return S_RequireComponents; }
        }

        public CrowdMovementSystem(WorldBase world) : base(world)
        {
        }

        // 按固定步长推进所有群体代理
        public override void FixedTick(float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0f)
                return;

            for (int i = 0; i < Entities.Count; i++)
            {
                ECSEntity entity = World.GetEntity(Entities[i]);
                if (entity == null)
                    continue;

                CrowdAgentComponent agent =
                    entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
                if (agent == null)
                    continue;

                UpdateAgent(agent, fixedDeltaTime);
            }
        }

        // 更新单个代理的目标速度、减速和位置
        private void UpdateAgent(CrowdAgentComponent agent, float fixedDeltaTime)
        {
            CrowdAgentData data = agent.Data;
            Vector3 previousVelocity = data.Velocity;
            Vector3 desiredVelocity = Vector3.zero;

            if (data.HasTarget)
            {
                Vector3 offset = data.TargetPosition - data.Position;
                offset.y = 0f;
                float distance = offset.magnitude;
                if (distance <= ARRIVAL_DISTANCE)
                {
                    data.Position = data.TargetPosition;
                    data.Velocity = Vector3.zero;
                    data.Acceleration = -previousVelocity / fixedDeltaTime;
                    data.SteeringForce = -previousVelocity;
                    data.HasTarget = false;
                    agent.Data = data;
                    return;
                }

                float speed = Mathf.Max(0f, data.MaxSpeed);
                if (data.SlowingRadius > ARRIVAL_DISTANCE)
                    speed *= Mathf.Clamp01(distance / data.SlowingRadius);

                desiredVelocity = offset / distance * speed;
            }

            float mass = Mathf.Max(MIN_MASS, data.Mass);
            float maxAcceleration = Mathf.Max(0f, data.MaxForce) / mass;
            data.Velocity = Vector3.MoveTowards(data.Velocity, desiredVelocity,
                maxAcceleration * fixedDeltaTime);
            data.Position += data.Velocity * fixedDeltaTime;
            data.Acceleration = (data.Velocity - previousVelocity) / fixedDeltaTime;
            data.SteeringForce = desiredVelocity - data.Velocity;

            if (data.Velocity.sqrMagnitude > MIN_SPEED * MIN_SPEED)
                data.Forward = data.Velocity.normalized;

            agent.Data = data;
        }
    }
}
