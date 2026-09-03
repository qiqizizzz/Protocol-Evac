/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd Agent生成控制器，将角色Prefab生成到群体父节点下
* │  类    名: CrowdSpawnController.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using UnityEngine;
using Utils.log;

namespace Module.CrowdDemo
{
    public sealed class CrowdSpawnController : MonoBehaviour
    {
        private const int DEFAULT_FIRST_ENTITY_ID = 1000;
        private const int MAX_SPAWN_COUNT = 500;

        private static CrowdSpawnController S_active;

        [Header("Crowd Agent生成")]
        [Tooltip("生成的 Agent 角色Prefab")]
        [SerializeField] private GameObject AgentPrefab;
        [Tooltip("同一群体内 Agent 的本地间距")]
        [SerializeField, Min(0.1f)] private float AgentSpacing = 1.2f;

        private int m_nextEntityId = DEFAULT_FIRST_ENTITY_ID;

        public static CrowdSpawnController Active => S_active;

        // 注册当前场景中的 Crowd 生成控制器
        private void OnEnable()
        {
            if (S_active != null && S_active != this)
            {
                QLog.Error("场景中存在多个 CrowdSpawnController");
                enabled = false;
                return;
            }

            S_active = this;
        }

        // 释放当前场景中的 Crowd 生成控制器
        private void OnDisable()
        {
            if (S_active == this)
                S_active = null;
        }

        // 校验生成控制器的必要Prefab引用
        private void Awake()
        {
            if (AgentPrefab == null)
            {
                QLog.Error("Crowd Agent生成失败：AgentPrefab 未配置");
                enabled = false;
                return;
            }

            if (AgentSpacing <= 0f)
            {
                QLog.Error("Crowd Agent生成失败：AgentSpacing 必须大于 0");
                enabled = false;
            }
        }

        // 向指定群体父节点下批量生成 Agent
        public int SpawnAgents(CrowdAgentGroup group, int count)
        {
            if (group == null)
            {
                QLog.Error("Crowd Agent生成失败：群体父节点为空");
                return 0;
            }

            if (AgentPrefab == null)
            {
                QLog.Error("Crowd Agent生成失败：AgentPrefab 未配置");
                return 0;
            }

            if (count <= 0 || count > MAX_SPAWN_COUNT)
            {
                QLog.Error($"Crowd Agent生成失败：数量必须在 1-{MAX_SPAWN_COUNT} 之间");
                return 0;
            }

            int existingCount = group.AgentCount;
            if (existingCount >= MAX_SPAWN_COUNT || count > MAX_SPAWN_COUNT - existingCount)
            {
                QLog.Error($"Crowd Agent生成失败：群体总数量不能超过 {MAX_SPAWN_COUNT}");
                return 0;
            }

            int totalCount = existingCount + count;
            for (int i = 0; i < count; i++)
            {
                int entityId = AllocateEntityId();
                GameObject instance = Instantiate(AgentPrefab, group.transform);
                instance.name = $"CrowdAgent_{entityId}";
                instance.transform.localPosition = CalculateFormationPosition(existingCount + i, totalCount);
                instance.transform.localRotation = Quaternion.identity;

                CrowdAgentView view = instance.GetComponent<CrowdAgentView>();
                if (view == null)
                    view = instance.AddComponent<CrowdAgentView>();

                view.Initialize(entityId);
            }

            return count;
        }

        // 为新生成的 Agent 分配未使用的实体编号
        private int AllocateEntityId()
        {
            while (CrowdAgentView.IsEntityIdUsed(m_nextEntityId))
                m_nextEntityId++;

            return m_nextEntityId++;
        }

        // 根据序号计算群体内的规则队形位置
        private Vector3 CalculateFormationPosition(int index, int totalCount)
        {
            int columnCount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Sqrt(totalCount)));
            int row = index / columnCount;
            int column = index % columnCount;
            float centerOffset = (columnCount - 1) * 0.5f;
            float rowCount = Mathf.Ceil((float)totalCount / columnCount);
            float rowCenterOffset = (rowCount - 1f) * 0.5f;
            return new Vector3((column - centerOffset) * AgentSpacing, 0f,
                (row - rowCenterOffset) * AgentSpacing);
        }
    }
}
