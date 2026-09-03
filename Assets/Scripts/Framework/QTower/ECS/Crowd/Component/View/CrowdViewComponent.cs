/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd表现绑定组件，保存ECS实体对应的场景Transform
* │  类    名: CrowdViewComponent.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Component.Common;
using UnityEngine;

namespace Framework.QTower.ECS.Crowd.Component.View
{
    public sealed class CrowdViewComponent : ECSComponent
    {
        public Transform Transform;

        public override int ComType
        {
            get { return (int)CrowdComponentType.View; }
        }

        // 清理表现绑定引用
        public override void Recycle()
        {
            Transform = null;
            base.Recycle();
        }
    }
}
