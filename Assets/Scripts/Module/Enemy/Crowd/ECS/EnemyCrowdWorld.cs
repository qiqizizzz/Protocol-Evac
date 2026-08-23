/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体ECS世界，负责注册群体避障系统与实体装配
 * │  类    名: EnemyCrowdWorld.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using Framework.QTower.Event.ECS;
using UnityEngine;

namespace Module.Enemy.Crowd.ECS
{
    public sealed class EnemyCrowdWorld : WorldBase
    {
        private EnemyCrowdAvoidanceSystem m_avoidanceSystem;

        public EnemyCrowdWorld()
        {
            Register();
        }

        public override void Register()
        {
            if (m_avoidanceSystem != null)
                return;

            m_avoidanceSystem = new EnemyCrowdAvoidanceSystem(this);
            AddSystem(m_avoidanceSystem);

            foreach (ECSEntity entity in Entities.Values)
                RefreshEntitySystems(entity);
        }

        // 创建一个敌人群体代理实体并写入初始数据
        public ECSEntity CreateCrowdAgent(int entityId, Vector3 position, float radius, float avoidanceRadius,
            float avoidanceWeight)
        {
            ECSEntity entity = CreateEntity(entityId);
            EnemyCrowdAgentComponent agent =
                entity.AddComponent<EnemyCrowdAgentComponent>(EnemyCrowdComponentType.Agent);
            agent.Position = position;
            agent.Radius = radius;
            agent.AvoidanceRadius = avoidanceRadius;
            agent.AvoidanceWeight = avoidanceWeight;

            entity.AddComponent<EnemyCrowdMoveIntentComponent>(EnemyCrowdComponentType.MoveIntent);
            entity.AddComponent<EnemyCrowdAvoidanceResultComponent>(EnemyCrowdComponentType.AvoidanceResult);
            return entity;
        }

        // 创建一个敌人群体障碍物实体并写入初始数据
        public ECSEntity CreateCrowdObstacle(int entityId, Vector3 position, float radius, float avoidanceWeight)
        {
            ECSEntity entity = CreateEntity(entityId);
            EnemyCrowdObstacleComponent obstacle =
                entity.AddComponent<EnemyCrowdObstacleComponent>(EnemyCrowdComponentType.Obstacle);
            obstacle.Position = position;
            obstacle.Radius = radius;
            obstacle.AvoidanceWeight = avoidanceWeight;
            return entity;
        }

        // 更新敌人群体代理的位置
        public void SetAgentPosition(int entityId, Vector3 position)
        {
            ECSEntity entity = GetEntity(entityId);
            if (entity == null)
                return;

            EnemyCrowdAgentComponent agent =
                entity.GetComponent<EnemyCrowdAgentComponent>(EnemyCrowdComponentType.Agent);
            if (agent == null)
                return;

            agent.Position = position;
        }

        // 写入敌人群体移动意图
        public void SetMoveIntent(int entityId, Vector3 preferredDirection)
        {
            ECSEntity entity = GetEntity(entityId);
            if (entity == null)
                return;

            EnemyCrowdMoveIntentComponent intent =
                entity.GetComponent<EnemyCrowdMoveIntentComponent>(EnemyCrowdComponentType.MoveIntent);
            if (intent == null)
                return;

            intent.SetPreferredDirection(preferredDirection);
        }

        // 清理敌人群体移动意图
        public void ClearMoveIntent(int entityId)
        {
            ECSEntity entity = GetEntity(entityId);
            if (entity == null)
                return;

            EnemyCrowdMoveIntentComponent intent =
                entity.GetComponent<EnemyCrowdMoveIntentComponent>(EnemyCrowdComponentType.MoveIntent);
            if (intent == null)
                return;

            intent.Clear();
        }
    }
}
