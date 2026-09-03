/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体命令应用系统，将命令写入对应Agent组件
* │  类    名: CrowdCommandApplySystem.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Command;
using Framework.QTower.ECS.Crowd.Command.Common;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Data;
using Framework.QTower.ECS.Crowd.System.Common;

namespace Framework.QTower.ECS.Crowd.System
{
    public sealed class CrowdCommandApplySystem : ECSSystem
    {
        private static readonly int[] S_RequireComponents =
        {
            (int)CrowdComponentType.Agent
        };

        private readonly CrowdCommandBuffer m_commandBuffer;

        public override int SysType
        {
            get { return (int)CrowdSystemType.CommandApply; }
        }

        public override int[] RequireComponents
        {
            get { return S_RequireComponents; }
        }

        public CrowdCommandApplySystem(WorldBase world, CrowdCommandBuffer commandBuffer)
            : base(world)
        {
            m_commandBuffer = commandBuffer;
        }

        public override void Tick(float deltaTime)
        {
            if (m_commandBuffer == null)
                return;

            while (m_commandBuffer.TryDequeue(out CrowdCommandData commandData))
                ApplyCommand(commandData);
        }

        // 将单条命令应用到目标群体的Agent组件
        private void ApplyCommand(CrowdCommandData commandData)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                ECSEntity entity = World.GetEntity(Entities[i]);
                if (entity == null)
                    continue;

                CrowdAgentComponent agentComponent =
                    entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
                if (agentComponent == null)
                    continue;

                if (commandData.TargetEntityId >= 0)
                {
                    if (entity.EntityId != commandData.TargetEntityId)
                        continue;
                }
                else if (agentComponent.Data.GroupId != commandData.GroupId)
                    continue;

                if (commandData.CommandType == CrowdCommandType.Move)
                {
                    agentComponent.Data.TargetPosition = commandData.TargetPosition;
                    agentComponent.Data.TargetEntityId = -1;
                    agentComponent.Data.HasTarget = true;
                }
                else if (commandData.CommandType == CrowdCommandType.Stop)
                {
                    agentComponent.Data.TargetEntityId = -1;
                    agentComponent.Data.HasTarget = false;
                }
            }
        }
    }
}
