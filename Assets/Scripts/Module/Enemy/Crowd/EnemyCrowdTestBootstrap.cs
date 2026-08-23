/*
 * ┌──────────────────────────────────────────────────────────┐
 * │  描    述: 敌人群体ECS测试启动器，负责在测试场景中生成障碍与代理
 * │  类    名: EnemyCrowdTestBootstrap.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Event.ECS;
using Module.Enemy.Crowd.ECS;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utils.log;

namespace Module.Enemy.Crowd
{
    public sealed class EnemyCrowdTestBootstrap : MonoBehaviour
    {
        private const int AGENT_COUNT = 8;
        private const float AGENT_SPEED = 2.5f;
        private const float AGENT_RADIUS = 0.35f;
        private const float AGENT_AVOIDANCE_RADIUS = 2.25f;
        private const float OBSTACLE_RADIUS = 0.9f;
        private const float OBSTACLE_HEIGHT = 2f;
        private const float START_X = -7f;
        private const float GOAL_X = 7f;
        private const float LANE_STEP = 1.25f;
        private const float AGENT_GROUND_Y = 0f;
        private const string AGENT_PREFAB_ADDRESS = "mini_male";
        private const string AGENT_MOVING_PARAMETER = "isMoving";

        private readonly List<AgentBinding> m_agents = new List<AgentBinding>();
        private int m_nextObstacleEntityId = 2000;
        private EnemyCrowdWorld m_world;

        private struct AgentBinding
        {
            public int EntityId;
            public Transform Transform;
            public Animator Animator;
            public float LaneZ;
            public bool MovingToRight;
        }

        // 初始化测试世界并生成障碍与代理
        private void Awake()
        {
            m_world = new EnemyCrowdWorld();
            BuildTestArena();
        }

        // 驱动测试世界并移动代理
        private void Update()
        {
            if (m_world == null)
                return;

            SyncAgentPositions();
            UpdateAgentIntents();
            m_world.Tick(Time.deltaTime);
            MoveAgents(Time.deltaTime);
            SyncAgentPositions();
        }

        // 构建测试场中的障碍物与代理
        private void BuildTestArena()
        {
            SpawnObstacle(new Vector3(-1.8f, OBSTACLE_HEIGHT * 0.5f, -1.2f), new Vector3(1.7f, OBSTACLE_HEIGHT, 1.7f));
            SpawnObstacle(new Vector3(0.2f, OBSTACLE_HEIGHT * 0.5f, 1.4f), new Vector3(1.6f, OBSTACLE_HEIGHT, 1.6f));
            SpawnObstacle(new Vector3(1.9f, OBSTACLE_HEIGHT * 0.5f, -0.8f), new Vector3(1.8f, OBSTACLE_HEIGHT, 1.8f));
            SpawnObstacle(new Vector3(3.6f, OBSTACLE_HEIGHT * 0.5f, 1.1f), new Vector3(1.5f, OBSTACLE_HEIGHT, 1.5f));

            float startLaneZ = -((AGENT_COUNT - 1) * LANE_STEP) * 0.5f;
            for (int i = 0; i < AGENT_COUNT; i++)
            {
                float laneZ = startLaneZ + i * LANE_STEP;
                SpawnAgent(1000 + i, laneZ);
            }
        }

        // 生成一个静态障碍物并注册到 ECS
        private void SpawnObstacle(Vector3 position, Vector3 scale)
        {
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = "CrowdObstacle";
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            Paint(obstacle, new Color(0.35f, 0.32f, 0.30f));

            int entityId = m_nextObstacleEntityId++;
            m_world.CreateCrowdObstacle(entityId, position, OBSTACLE_RADIUS, 1.5f);
        }

        // 生成一个群体代理并注册到 ECS
        private void SpawnAgent(int entityId, float laneZ)
        {
            Vector3 spawnPosition = new Vector3(START_X, AGENT_GROUND_Y, laneZ);
            GameObject agent = Addressables.InstantiateAsync(AGENT_PREFAB_ADDRESS, spawnPosition, Quaternion.identity)
                .WaitForCompletion();
            if (agent == null)
            {
                QLog.Error($"测试代理资源加载失败：{AGENT_PREFAB_ADDRESS}");
                return;
            }

            agent.name = $"CrowdAgent_{entityId}";
            if (!agent.TryGetComponent<Animator>(out Animator animator))
            {
                QLog.Error($"测试代理 prefab 缺少 Animator：{AGENT_PREFAB_ADDRESS}");
                Addressables.ReleaseInstance(agent);
                return;
            }

            m_world.CreateCrowdAgent(entityId, spawnPosition, AGENT_RADIUS, AGENT_AVOIDANCE_RADIUS, 1f);
            m_agents.Add(new AgentBinding
            {
                EntityId = entityId,
                Transform = agent.transform,
                Animator = animator,
                LaneZ = laneZ,
                MovingToRight = true
            });
        }

        // 同步代理当前位置到 ECS
        private void SyncAgentPositions()
        {
            for (int i = 0; i < m_agents.Count; i++)
            {
                AgentBinding binding = m_agents[i];
                if (binding.Transform == null)
                    continue;

                m_world.SetAgentPosition(binding.EntityId, binding.Transform.position);
            }
        }

        // 为每个代理写入目标移动方向
        private void UpdateAgentIntents()
        {
            for (int i = 0; i < m_agents.Count; i++)
            {
                AgentBinding binding = m_agents[i];
                if (binding.Transform == null)
                    continue;

                float targetX = binding.MovingToRight ? GOAL_X : START_X;
                Vector3 targetPosition = new Vector3(targetX, binding.Transform.position.y, binding.LaneZ);
                Vector3 moveDirection = targetPosition - binding.Transform.position;
                if (moveDirection.sqrMagnitude <= 0.25f)
                {
                    binding.MovingToRight = !binding.MovingToRight;
                    targetX = binding.MovingToRight ? GOAL_X : START_X;
                    targetPosition = new Vector3(targetX, binding.Transform.position.y, binding.LaneZ);
                    moveDirection = targetPosition - binding.Transform.position;
                }

                if (moveDirection.sqrMagnitude <= 0.0001f)
                {
                    m_world.ClearMoveIntent(binding.EntityId);
                    m_agents[i] = binding;
                    continue;
                }

                m_world.SetMoveIntent(binding.EntityId, moveDirection);
                m_agents[i] = binding;
            }
        }

        // 将 ECS 的避障结果应用到代理物体
        private void MoveAgents(float deltaTime)
        {
            for (int i = 0; i < m_agents.Count; i++)
            {
                AgentBinding binding = m_agents[i];
                if (binding.Transform == null)
                    continue;

                ECSEntity entity = m_world.GetEntity(binding.EntityId);
                if (entity == null)
                    continue;

                EnemyCrowdAvoidanceResultComponent result =
                    entity.GetComponent<EnemyCrowdAvoidanceResultComponent>(EnemyCrowdComponentType.AvoidanceResult);
                if (result == null || !result.HasAdjustedDirection)
                {
                    if (binding.Animator != null)
                        binding.Animator.SetBool(AGENT_MOVING_PARAMETER, false);

                    continue;
                }

                binding.Transform.position += result.AdjustedDirection * AGENT_SPEED * deltaTime;
                if (binding.Animator != null)
                    binding.Animator.SetBool(AGENT_MOVING_PARAMETER, true);

                m_agents[i] = binding;
            }
        }

        // 释放 Addressables 生成的测试代理
        private void OnDestroy()
        {
            for (int i = 0; i < m_agents.Count; i++)
            {
                AgentBinding binding = m_agents[i];
                if (binding.Transform == null)
                    continue;

                Addressables.ReleaseInstance(binding.Transform.gameObject);
            }

            m_agents.Clear();
            m_world = null;
        }

        // 给测试场景中的障碍物设置显示颜色
        private void Paint(GameObject target, Color color)
        {
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer == null)
                return;

            renderer.material.color = color;
        }
    }
}
