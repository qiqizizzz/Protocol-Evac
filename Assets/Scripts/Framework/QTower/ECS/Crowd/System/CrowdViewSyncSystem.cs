/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd表现同步系统，将ECS运动结果写入场景对象
* │  类    名: CrowdViewSyncSystem.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Component.View;
using Framework.QTower.ECS.Crowd.System.Common;
using UnityEngine;

namespace Framework.QTower.ECS.Crowd.System
{
    public sealed class CrowdViewSyncSystem : ECSSystem
    {
        private static readonly int[] S_RequireComponents =
        {
            (int)CrowdComponentType.Agent,
            (int)CrowdComponentType.View
        };

        public override int SysType
        {
            get { return (int)CrowdSystemType.ViewSync; }
        }

        public override int[] RequireComponents
        {
            get { return S_RequireComponents; }
        }

        public CrowdViewSyncSystem(WorldBase world) : base(world)
        {
        }

        // 将ECS运动结果同步到绑定的场景对象
        public override void LateTick(float deltaTime)
        {
            for (int i = 0; i < Entities.Count; i++)
            {
                ECSEntity entity = World.GetEntity(Entities[i]);
                if (entity == null)
                    continue;

                CrowdAgentComponent agent =
                    entity.GetComponent<CrowdAgentComponent>((int)CrowdComponentType.Agent);
                CrowdViewComponent view =
                    entity.GetComponent<CrowdViewComponent>((int)CrowdComponentType.View);
                if (agent == null || view == null || view.Transform == null)
                    continue;

                view.Transform.position = agent.Data.Position;
                if (agent.Data.Forward.sqrMagnitude <= 0.0001f)
                    continue;

                view.Transform.rotation = Quaternion.LookRotation(agent.Data.Forward, Vector3.up);
            }
        }
    }
}
