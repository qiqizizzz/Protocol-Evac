/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: 群体命令缓冲，保存等待群体系统消费的命令数据
* │  类    名: CrowdCommandBuffer.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Framework.QTower.ECS.Crowd.Command.Common;
using Framework.QTower.ECS.Crowd.Data;
using Utils.log;

namespace Framework.QTower.ECS.Crowd.Command
{
    public sealed class CrowdCommandBuffer
    {
        private readonly Queue<CrowdCommandData> m_commands = new Queue<CrowdCommandData>();

        public int Count
        {
            get { return m_commands.Count; }
        }

        // 将托管命令转换为数据并加入缓冲
        public void Enqueue(CrowdCommandBase command)
        {
            if (command == null)
            {
                QLog.Error("加入群体命令失败：命令为空");
                return;
            }

            m_commands.Enqueue(command.ToData());
        }

        // 取出下一条待处理命令
        public bool TryDequeue(out CrowdCommandData commandData)
        {
            if (m_commands.Count == 0)
            {
                commandData = default(CrowdCommandData);
                return false;
            }

            commandData = m_commands.Dequeue();
            return true;
        }

        // 清空尚未消费的命令
        public void Clear()
        {
            m_commands.Clear();
        }
    }
}

