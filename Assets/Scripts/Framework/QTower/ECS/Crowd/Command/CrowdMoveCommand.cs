/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体移动命令，向指定世界位置发布移动目标
* │  类    名: CrowdMoveCommand.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.ECS.Crowd.Command.Common;
using UnityEngine;
using Framework.QTower.ECS.Crowd.Data;

namespace Framework.QTower.ECS.Crowd.Command
{
    public sealed class CrowdMoveCommand : CrowdCommandBase
    {
        public override CrowdCommandType CommandType => CrowdCommandType.Move;
        public Vector3 TargetPosition { get; }

        public CrowdMoveCommand(int commandId, int groupId, Vector3 targetPosition, int priority = 0,
            int targetEntityId = -1)
            : base(commandId, groupId, priority, targetEntityId)
        {
            TargetPosition = targetPosition;
        }

        public override CrowdCommandData ToData()
        {
            return new CrowdCommandData
            {
                CommandType = CommandType,
                CommandId = CommandId,
                GroupId = GroupId,
                TargetEntityId = TargetEntityId,
                Priority = Priority,
                TargetPosition = TargetPosition
            };
        }
    }
}
