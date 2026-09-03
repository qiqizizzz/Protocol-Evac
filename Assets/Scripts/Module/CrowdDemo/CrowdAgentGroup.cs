/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd Agent群体父节点标记，注册场景中的可控群体
* │  类    名: CrowdAgentGroup.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEngine;

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
    }
}
