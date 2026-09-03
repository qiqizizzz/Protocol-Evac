/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd场景代理标记，声明场景Transform对应的ECS实体编号
* │  类    名: CrowdAgentView.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace Module.CrowdDemo
{
    public sealed class CrowdAgentView : MonoBehaviour
    {
        private static readonly List<CrowdAgentView> S_activeViews = new List<CrowdAgentView>();
        private static int S_version;

        [Header("Crowd ECS")]
        [Tooltip("场景对象对应的 Crowd ECS 实体编号")]
        [SerializeField] private int EntityId = -1;

        public static IReadOnlyList<CrowdAgentView> ActiveViews => S_activeViews;
        public static int Version => S_version;
        public int EntityIdValue => EntityId;

        // 判断实体编号是否已被场景中的表现对象占用
        public static bool IsEntityIdUsed(int entityId)
        {
            for (int i = 0; i < S_activeViews.Count; i++)
            {
                CrowdAgentView view = S_activeViews[i];
                if (view != null && view.EntityId == entityId)
                    return true;
            }

            return false;
        }

        // 初始化场景表现对象对应的 ECS 实体编号
        public void Initialize(int entityId)
        {
            if (entityId < 0)
            {
                QLog.Error("Crowd Agent表现初始化失败：实体编号不能为负数");
                return;
            }

            if (EntityId == entityId)
                return;

            EntityId = entityId;
            S_version++;
        }

        // 注册启用的 Crowd 场景代理
        private void OnEnable()
        {
            if (S_activeViews.Contains(this))
                return;

            S_activeViews.Add(this);
            S_version++;
        }

        // 移除停用的 Crowd 场景代理
        private void OnDisable()
        {
            if (!S_activeViews.Remove(this))
                return;

            S_version++;
        }
    }
}
