/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd ECS运行时世界，负责装配群体实体与命令系统
* │  类    名: CrowdWorld.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Command;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Component.View;
using Framework.QTower.ECS.Crowd.System;
using Framework.QTower.ECS.Crowd.System.Common;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.ECS.Crowd.Core
{
    public sealed class CrowdWorld : WorldBase
    {
        private const float DEFAULT_AGENT_RADIUS = 0.35f;
        private const float DEFAULT_AGENT_MASS = 1f;
        private const float DEFAULT_AGENT_MAX_SPEED = 2.5f;
        private const float DEFAULT_AGENT_MAX_FORCE = 10f;
        private const float DEFAULT_AGENT_MAX_TURN_RATE = 360f;
        private const float DEFAULT_AGENT_SLOWING_RADIUS = 0.75f;

        private static CrowdWorld S_active;
        private int m_nextCommandId = 1;

        public CrowdCommandBuffer CommandBuffer { get; }
        public static CrowdWorld Active => S_active;
        public int SelectedGroupId { get; private set; }

        // 创建群体世界并注册默认命令系统
        public CrowdWorld()
        {
            S_active = this;
            CommandBuffer = new CrowdCommandBuffer();
            Register();
        }

        // 注册 Crowd ECS 的基础系统
        public override void Register()
        {
            if (Systems.ContainsKey((int)CrowdSystemType.CommandApply))
                return;

            AddSystem(new CrowdCommandApplySystem(this, CommandBuffer));
            AddSystem(new CrowdMovementSystem(this));
            AddSystem(new CrowdViewSyncSystem(this));
        }

        // 创建或获取一个可被 GM 控制的群体代理
        public ECSEntity CreateAgent(int entityId)
        {
            return CreateAgent(entityId, Vector3.zero);
        }

        // 创建或获取一个带初始位置的群体代理
        public ECSEntity CreateAgent(int entityId, Vector3 position)
        {
            return CreateAgent(entityId, position, 0);
        }

        // 创建或获取一个带初始位置和群体编号的群体代理
        public ECSEntity CreateAgent(int entityId, Vector3 position, int groupId)
        {
            ECSEntity entity = CreateEntity(entityId);
            CrowdAgentComponent agent =
                entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
            if (agent == null)
            {
                agent = entity.AddComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
                agent.Data.Position = position;
                agent.Data.GroupId = groupId;
                agent.Data.Radius = DEFAULT_AGENT_RADIUS;
                agent.Data.Mass = DEFAULT_AGENT_MASS;
                agent.Data.MaxSpeed = DEFAULT_AGENT_MAX_SPEED;
                agent.Data.MaxForce = DEFAULT_AGENT_MAX_FORCE;
                agent.Data.MaxTurnRate = DEFAULT_AGENT_MAX_TURN_RATE;
                agent.Data.SlowingRadius = DEFAULT_AGENT_SLOWING_RADIUS;
                agent.Data.Forward = Vector3.forward;
            }

            agent.Data.GroupId = groupId;

            return entity;
        }

        // 设置地面点击和GM当前控制的群体编号
        public void SetSelectedGroup(int groupId)
        {
            SelectedGroupId = groupId;
        }

        // 向指定群体发布移动命令
        public void EnqueueMoveGroup(int groupId, Vector3 targetPosition)
        {
            CommandBuffer.Enqueue(new CrowdMoveCommand(m_nextCommandId++, groupId, targetPosition));
        }

        // 向指定群体发布停止命令
        public void EnqueueStopGroup(int groupId)
        {
            CommandBuffer.Enqueue(new CrowdStopCommand(m_nextCommandId++, groupId));
        }

        // 将场景表现对象绑定到指定群体代理
        public bool BindAgentView(int entityId, Transform viewTransform)
        {
            return BindAgentView(entityId, 0, viewTransform);
        }

        // 将场景表现对象绑定到指定群体代理并同步父节点群体编号
        public bool BindAgentView(int entityId, int groupId, Transform viewTransform)
        {
            if (viewTransform == null)
            {
                QLog.Error("绑定 Crowd Agent 表现失败：Transform 为空");
                return false;
            }

            ECSEntity entity = CreateAgent(entityId, viewTransform.position, groupId);
            CrowdAgentComponent agent =
                entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
            CrowdViewComponent viewComponent =
                entity.GetComponent<CrowdViewComponent>((int)CrowdComponentType.View);
            if (viewComponent == null)
            {
                if (agent != null && !agent.Data.HasTarget && agent.Data.Velocity.sqrMagnitude <= 0.0001f)
                    agent.Data.Position = viewTransform.position;

                viewComponent = entity.AddComponent<CrowdViewComponent>((int)CrowdComponentType.View);
            }

            viewComponent.Transform = viewTransform;
            return true;
        }

        // 解除指定群体代理与场景表现对象的绑定
        public void UnbindAgentView(int entityId, Transform viewTransform)
        {
            if (!Entities.TryGetValue(entityId, out ECSEntity entity))
                return;

            CrowdViewComponent viewComponent =
                entity.GetComponent<CrowdViewComponent>((int)CrowdComponentType.View);
            if (viewComponent == null)
                return;

            if (viewTransform != null && viewComponent.Transform != viewTransform)
                return;

            entity.RemoveComponent((int)CrowdComponentType.View);
        }
    }
}
