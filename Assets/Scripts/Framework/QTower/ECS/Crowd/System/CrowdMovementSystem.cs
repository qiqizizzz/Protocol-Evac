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
        private const float MIN_FORWARD_SPEED = 0.15f;
        private const float MIN_FORWARD_SQR = 0.000001f;
        private const float MIN_AVOIDANCE_SQR = 0.0001f;

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

            // 叠加避障修正量后仍限制在最大速度内，交给加速度预算平滑吸收
            if (data.AvoidanceForce.sqrMagnitude > MIN_AVOIDANCE_SQR)
                desiredVelocity = Vector3.ClampMagnitude(desiredVelocity + data.AvoidanceForce,
                    Mathf.Max(0f, data.MaxSpeed));

            // 避障力只对下一帧有效，消费后清零，关闭避障系统时不会残留旧值
            data.AvoidanceForce = Vector3.zero;

            float mass = Mathf.Max(MIN_MASS, data.Mass);
            float maxAcceleration = Mathf.Max(0f, data.MaxForce) / mass;
            data.Velocity = Vector3.MoveTowards(data.Velocity, desiredVelocity,
                maxAcceleration * fixedDeltaTime);
            data.Position += data.Velocity * fixedDeltaTime;
            data.Acceleration = (data.Velocity - previousVelocity) / fixedDeltaTime;
            data.SteeringForce = desiredVelocity - data.Velocity;

            // 低速时保留上一帧朝向，其余按 MaxTurnRate 限幅转向，避免瞬时反向造成旋转抽搐
            if (data.Velocity.sqrMagnitude > MIN_FORWARD_SPEED * MIN_FORWARD_SPEED)
                data.Forward = TurnForward(data.Forward, data.Velocity.normalized,
                    data.MaxTurnRate, fixedDeltaTime);

            agent.Data = data;
        }

        // 按最大角速度把朝向绕竖直轴转向期望方向；正好对冲时 SignedAngle 固定返回 +180，转向侧确定
        private Vector3 TurnForward(Vector3 current, Vector3 desired, float maxTurnRate, float fixedDeltaTime)
        {
            if (current.sqrMagnitude <= MIN_FORWARD_SQR)
                return desired;

            float maxTurnDegrees = Mathf.Max(0f, maxTurnRate) * fixedDeltaTime;
            float angle = Vector3.SignedAngle(current, desired, Vector3.up);
            if (Mathf.Abs(angle) <= maxTurnDegrees)
                return desired;

            return Quaternion.AngleAxis(
                Mathf.Clamp(angle, -maxTurnDegrees, maxTurnDegrees), Vector3.up) * current;
        }
    }
}
