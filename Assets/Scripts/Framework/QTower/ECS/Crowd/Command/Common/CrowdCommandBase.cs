/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体命令基类，承载公共元数据并转换为Job数据
* │  类    名: CrowdCommandBase.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using Framework.QTower.ECS.Crowd.Data;

namespace Framework.QTower.ECS.Crowd.Command.Common
{
    public abstract class CrowdCommandBase
    {
        public int CommandId { get; }
        public int GroupId { get; }
        public int TargetEntityId { get; }
        public int Priority { get; }
        public abstract CrowdCommandType CommandType { get; }
        
        protected CrowdCommandBase(int commandId, int groupId, int priority, int targetEntityId = -1)
        {
            CommandId = commandId;
            GroupId = groupId;
            TargetEntityId = targetEntityId;
            Priority = priority;
        }

        // 将托管命令转换为可供JobSystem读取的值类型数据
        public abstract CrowdCommandData ToData();
    }
}
