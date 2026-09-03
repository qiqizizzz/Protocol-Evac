/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd Agent群体父节点标记，注册场景中的可控群体
* │  类    名: CrowdAgentGroup.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace Module.CrowdDemo
{
    public sealed class CrowdAgentGroup : MonoBehaviour
    {
        private static readonly List<CrowdAgentGroup> S_activeGroups = new List<CrowdAgentGroup>();
        private static int S_version;

        [Header("Crowd ECS群体")]
        [Tooltip("该父节点代表的 Crowd ECS 群体编号")]
        [SerializeField] private int GroupId = 1;

        public static IReadOnlyList<CrowdAgentGroup> ActiveGroups => S_activeGroups;
        public static int Version => S_version;
        public int GroupIdValue => GroupId;
        public int AgentCount => transform.childCount;

        // 在指定 CrowdSystem 父节点下创建一个新的运行时群体
        public static CrowdAgentGroup CreateRuntimeGroup(Transform parent)
        {
            if (parent == null)
            {
                QLog.Error("创建 Crowd 群体失败：群体根节点为空");
                return null;
            }

            int groupId = AllocateGroupId();
            GameObject groupObject = new GameObject($"CrowdGroup_{groupId}");
            groupObject.SetActive(false);
            groupObject.transform.SetParent(parent, false);
            CrowdAgentGroup group = groupObject.AddComponent<CrowdAgentGroup>();
            group.GroupId = groupId;
            groupObject.SetActive(true);
            return group;
        }

        // 注册启用的 Crowd 群体父节点
        private void OnEnable()
        {
            if (S_activeGroups.Contains(this))
                return;

            S_activeGroups.Add(this);
            S_version++;
        }

        // 移除停用的 Crowd 群体父节点
        private void OnDisable()
        {
            if (!S_activeGroups.Remove(this))
                return;

            S_version++;
        }

        // 分配当前场景中未被占用的群体编号
        private static int AllocateGroupId()
        {
            int groupId = 1;
            while (IsGroupIdUsed(groupId))
                groupId++;

            return groupId;
        }

        // 判断群体编号是否已经被启用的群体占用
        private static bool IsGroupIdUsed(int groupId)
        {
            for (int i = 0; i < S_activeGroups.Count; i++)
            {
                CrowdAgentGroup group = S_activeGroups[i];
                if (group != null && group.GroupId == groupId)
                    return true;
            }

            return false;
        }
    }
}
