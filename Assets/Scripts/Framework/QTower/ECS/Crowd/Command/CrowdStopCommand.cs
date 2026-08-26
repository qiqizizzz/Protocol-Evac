/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体停止命令，清除当前目标并请求平滑减速停止
* │  类    名: CrowdStopCommand.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.ECS.Crowd.Command.Common;
using UnityEngine;
using Framework.QTower.ECS.Crowd.Data;

namespace Framework.QTower.ECS.Crowd.Command
{
    public sealed class CrowdStopCommand : CrowdCommandBase
    {
        public override CrowdCommandType CommandType => CrowdCommandType.Stop;

        public CrowdStopCommand(int commandId, int groupId, int priority = 0)
            : base(commandId, groupId, priority)
        {
        }
        
        public override CrowdCommandData ToData()
        {
            return new CrowdCommandData
            {
                CommandType = CommandType,
                CommandId = CommandId,
                GroupId = GroupId,
                Priority = Priority,
                TargetPosition = Vector3.zero
            };
        }
    }
}

