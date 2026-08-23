/*
 * ┌────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体避障系统，批量修正导航移动方向
 * │  类    名: EnemyCrowdAvoidanceSystem.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Module.Enemy.Crowd.ECS
{
    public sealed class EnemyCrowdAvoidanceSystem : ECSSystem
    {
        public EnemyCrowdAvoidanceSystem(WorldBase world) : base(world)
        {
        }

        public override int SysType
        {
            get { return EnemyCrowdSystemType.Avoidance; }
        }

        public override int[] RequireComponents
        {
            get
            {
                return new[]
                {
                    EnemyCrowdComponentType.Agent,
                    EnemyCrowdComponentType.MoveIntent,
                    EnemyCrowdComponentType.AvoidanceResult
                };
            }
        }

        // 批量计算敌人之间的简单分离避障方向
        public override void Tick(float deltaTime)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                int entityId = Entities[i];
                ECSEntity entity = World.GetEntity(entityId);
                if (entity == null)
                    continue;

                EnemyCrowdAgentComponent agent =
                    entity.GetComponent<EnemyCrowdAgentComponent>(EnemyCrowdComponentType.Agent);
                EnemyCrowdMoveIntentComponent intent =
                    entity.GetComponent<EnemyCrowdMoveIntentComponent>(EnemyCrowdComponentType.MoveIntent);
                EnemyCrowdAvoidanceResultComponent result =
                    entity.GetComponent<EnemyCrowdAvoidanceResultComponent>(EnemyCrowdComponentType.AvoidanceResult);

                if (agent == null || intent == null || result == null)
                    continue;

                if (!intent.HasMoveRequest)
                {
                    result.Clear();
                    continue;
                }

                Vector3 separationDirection = CalculateSeparationDirection(entityId, agent);
                Vector3 adjustedDirection = intent.PreferredDirection + separationDirection * agent.AvoidanceWeight;
                result.SetAdjustedDirection(adjustedDirection);
            }
        }

        // 计算指定实体远离附近敌人的分离方向
        private Vector3 CalculateSeparationDirection(int sourceEntityId, EnemyCrowdAgentComponent sourceAgent)
        {
            Vector3 separationDirection = Vector3.zero;

            foreach (KeyValuePair<int, ECSEntity> pair in World.Entities)
            {
                int targetEntityId = pair.Key;
                if (targetEntityId == sourceEntityId)
                    continue;

                ECSEntity targetEntity = pair.Value;
                if (targetEntity == null)
                    continue;

                Vector3 targetPosition;
                float targetRadius;
                float targetWeight;

                EnemyCrowdAgentComponent targetAgent =
                    targetEntity.GetComponent<EnemyCrowdAgentComponent>(EnemyCrowdComponentType.Agent);
                if (targetAgent != null)
                {
                    targetPosition = targetAgent.Position;
                    targetRadius = targetAgent.Radius;
                    targetWeight = targetAgent.AvoidanceWeight;
                }
                else
                {
                    EnemyCrowdObstacleComponent obstacle =
                        targetEntity.GetComponent<EnemyCrowdObstacleComponent>(EnemyCrowdComponentType.Obstacle);
                    if (obstacle == null)
                        continue;

                    targetPosition = obstacle.Position;
                    targetRadius = obstacle.Radius;
                    targetWeight = obstacle.AvoidanceWeight;
                }

                Vector3 offset = sourceAgent.Position - targetPosition;
                offset.y = 0f;
                float distanceSqr = offset.sqrMagnitude;
                float avoidRadius = Mathf.Max(0.01f, sourceAgent.AvoidanceRadius + targetRadius);
                float avoidRadiusSqr = avoidRadius * avoidRadius;
                if (distanceSqr <= 0.0001f || distanceSqr > avoidRadiusSqr)
                    continue;

                float distance = Mathf.Sqrt(distanceSqr);
                float strength = 1f - distance / avoidRadius;
                separationDirection += offset / distance * strength * sourceAgent.AvoidanceWeight * targetWeight;
            }

            return separationDirection;
        }
    }
}
