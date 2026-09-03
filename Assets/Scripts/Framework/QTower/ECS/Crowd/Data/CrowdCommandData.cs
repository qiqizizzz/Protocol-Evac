/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体命令的JobSystem数据载荷，供命令执行系统读取
* │  类    名: CrowdCommandData.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using UnityEngine;
using Framework.QTower.ECS.Crowd.Command;
using Framework.QTower.ECS.Crowd.Command.Common;

namespace Framework.QTower.ECS.Crowd.Data
{
    public struct CrowdCommandData
    {
        public CrowdCommandType CommandType;
        public int CommandId;
        public int GroupId;
        public int TargetEntityId;
        public int Priority;

        public Vector3 TargetPosition;
    }
}
