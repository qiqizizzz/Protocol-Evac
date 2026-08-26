/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 群体代理ECS组件，承载CrowdAgentData并接入基础ECS实体                     
 * │  类    名: CrowdAgentComponent.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Data;
using Framework.QTower.ECS.Crowd.Component.Common;

namespace Framework.QTower.ECS.Crowd.Component
{
    public sealed class CrowdAgentComponent : ECSComponent
    {
        public CrowdAgentData Data;

        public override int ComType
        {
            get { return (int)CrowdComponentType.Agent; }
        }

        public override void OnAdd()
        {
            Data.EntityId = EntityId;
        }

        public override void Recycle()
        {
            Data = default(CrowdAgentData);
            base.Recycle();
        }
    }
}
