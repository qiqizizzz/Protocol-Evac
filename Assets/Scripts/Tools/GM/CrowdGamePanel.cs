/*
* ┌────────────────────────────────────────────────────────────┐
* │  描    述: Crowd ECS调试面板，选择群体并控制Agent生成与移动
* │  类    名: CrowdGamePanel.cs
* │  创    建: By qiqizizzz
* └────────────────────────────────────────────────────────────┘
*/

using System.Collections.Generic;
using Framework.QTower.Event.ECS;
using Framework.QTower.ECS.Crowd.Component;
using Framework.QTower.ECS.Crowd.Component.Common;
using Framework.QTower.ECS.Crowd.Core;
using Framework.QTower.ECS.Crowd.Data;
using Framework.QTower.ECS.Crowd.System;
using Framework.QTower.ECS.Crowd.System.Common;
using Module.CrowdDemo;
using UnityEngine;
using Utils.log;

namespace Tools.GM
{
    internal sealed class CrowdGamePanel : IGamePanel
    {
        private readonly CrowdWorld m_world = new CrowdWorld();
        private readonly List<CrowdAgentGroup> m_groups = new List<CrowdAgentGroup>();
        private readonly List<CrowdAgentView> m_agentSources = new List<CrowdAgentView>();
        private readonly HashSet<int> m_boundViewIds = new HashSet<int>();

        private const int NEW_AGENT_SOURCE_ID = -1;
        private const float STRENGTH_MIN = 0f;
        private const float STRENGTH_MAX = 3f;
        private const float NEIGHBOR_RADIUS_MIN = 0.5f;
        private const float NEIGHBOR_RADIUS_MAX = 6f;
        private const float SEPARATION_MARGIN_MAX = 1f;
        private const float PREDICT_TIME_MIN = 0.05f;
        private const float PREDICT_TIME_MAX = 3f;
        private const float NEIGHBOR_COUNT_MIN = 1f;
        private const float NEIGHBOR_COUNT_MAX = 24f;
        private const float OVERLAP_STRENGTH_MAX = 1f;
        private const float DEFAULT_SEPARATION_STRENGTH = 1f;
        private const float DEFAULT_AVOIDANCE_STRENGTH = 1f;
        private string m_spawnCountInput = "10";
        private string m_status = "未发送命令";
        private int m_entityCount;
        private int m_pendingCommandCount;
        private int m_selectedGroupId;
        private int m_groupVersion = -1;
        private int m_viewVersion = -1;
        private int m_agentSourceVersion = -1;
        private int m_selectedAgentSourceId = NEW_AGENT_SOURCE_ID;
        private bool m_isGroupDropdownOpen;
        private bool m_isAgentDropdownOpen;
        private Vector2 m_groupDropdownScrollPosition;
        private Vector2 m_agentDropdownScrollPosition;
        private CrowdSpatialHashSystem m_hashSystem;
        private CrowdAvoidanceSystem m_avoidanceSystem;
        private CrowdOverlapResolveSystem m_overlapSystem;
        private bool m_isSystemResolved;
        private int m_hashAgentCount;
        private int m_hashCellCount;
        private float m_maxPenetration;
        private bool m_hasInvalidVelocity;
        private float m_remainingPenetration;
        private int m_correctedPairCount;

        // 驱动 Crowd ECS 世界执行待处理命令
        public void Tick(float deltaTime)
        {
            RefreshGroupsIfNeeded();
            BindViewsIfNeeded();
            RefreshAgentSourcesIfNeeded();
            m_world.Tick(deltaTime);
        }

        // 驱动 Crowd ECS 世界的固定步长运动系统
        public void FixedTick(float fixedDeltaTime)
        {
            m_world.FixedTick(fixedDeltaTime);
        }

        // 将 Crowd ECS 结果同步到场景表现对象
        public void LateTick(float deltaTime)
        {
            m_world.LateTick(deltaTime);
        }

        // 刷新 Crowd ECS 调试数据
        public void Refresh()
        {
            RefreshGroupsIfNeeded();
            m_entityCount = m_world.Entities.Count;
            m_pendingCommandCount = m_world.CommandBuffer.Count;
            RefreshAvoidanceStats();
        }

        // 根据当前页签绘制 Crowd ECS 调试内容
        public void Draw(int activeTabIndex, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle toggleStyle,
            GUIStyle buttonStyle, GUIStyle sectionStyle, GUIStyle sectionHeaderStyle, GUIStyle inputStyle,
            GUIStyle statusStyle)
        {
            if (activeTabIndex != 3)
                return;

            DrawCrowdControls(labelStyle, valueStyle, buttonStyle, sectionStyle, sectionHeaderStyle, inputStyle,
                statusStyle);
        }

        // 绘制分区后的群体选择、生成和停止控制
        private void DrawCrowdControls(GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle buttonStyle,
            GUIStyle sectionStyle, GUIStyle sectionHeaderStyle, GUIStyle inputStyle, GUIStyle statusStyle)
        {
            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("运行状态", sectionHeaderStyle);
            DrawInfoRow("ECS 实体", m_entityCount.ToString(), labelStyle, valueStyle);
            DrawInfoRow("待处理命令", m_pendingCommandCount.ToString(), labelStyle, valueStyle);
            DrawInfoRow("空间哈希", $"{m_hashAgentCount} 代理 / {m_hashCellCount} 格子", labelStyle, valueStyle);
            DrawInfoRow("避障前穿模", m_maxPenetration.ToString("0.000"), labelStyle, valueStyle);
            DrawInfoRow("残余穿模", m_remainingPenetration.ToString("0.000"), labelStyle, valueStyle);
            DrawInfoRow("修正对数", m_correctedPairCount.ToString(), labelStyle, valueStyle);
            if (m_hasInvalidVelocity)
                GUILayout.Label("检测到非法速度，位置可能已被 NaN 污染", valueStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("避障调参", sectionHeaderStyle);
            DrawAvoidanceControls(labelStyle, valueStyle, buttonStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("群体目标", sectionHeaderStyle);
            GUILayout.Label("群体父节点", labelStyle);
            DrawGroupDropdown(valueStyle, buttonStyle);
            if (GUILayout.Button("+ 新建群体父节点", buttonStyle))
                CreateNewGroup();

            CrowdAgentGroup selectedGroup = GetSelectedGroup();
            if (selectedGroup == null)
            {
                GUILayout.Label("场景中没有可用的 CrowdAgentGroup 父节点", valueStyle);
                GUILayout.Label(m_status, statusStyle);
                GUILayout.EndVertical();
                return;
            }

            DrawInfoRow("名称", selectedGroup.name, labelStyle, valueStyle);
            DrawInfoRow("Group ID", selectedGroup.GroupIdValue.ToString(), labelStyle, valueStyle);
            DrawInfoRow("当前 Agent", selectedGroup.AgentCount.ToString(), labelStyle, valueStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("Agent 生成", sectionHeaderStyle);
            GUILayout.Label("Agent 来源", labelStyle);
            DrawAgentSourceDropdown(buttonStyle);
            GUILayout.Label("生成数量", labelStyle);
            GUILayout.BeginHorizontal();
            m_spawnCountInput = GUILayout.TextField(m_spawnCountInput, inputStyle);
            if (GUILayout.Button("生成 Agent", buttonStyle, GUILayout.Width(124f)))
                SpawnSelectedGroup();
            GUILayout.EndHorizontal();
            GUILayout.Label($"{GetNewAgentSourceLabel()}；已有 Agent 会复制其当前模型与组件配置", labelStyle);
            GUILayout.EndVertical();

            GUILayout.BeginVertical(sectionStyle);
            GUILayout.Label("移动控制", sectionHeaderStyle);
            GUILayout.Label("点击地面让当前群体前往目标位置", valueStyle);
            if (GUILayout.Button("停止当前群体", buttonStyle))
                StopSelectedGroup();
            GUILayout.Label(m_status, statusStyle);
            GUILayout.EndVertical();
        }

        // 绘制可展开的群体父节点下拉列表
        private void DrawGroupDropdown(GUIStyle valueStyle, GUIStyle buttonStyle)
        {
            CrowdAgentGroup selectedGroup = GetSelectedGroup();
            string selectedLabel = selectedGroup == null
                ? "请选择群体"
                : $"{selectedGroup.name}  (ID {selectedGroup.GroupIdValue})";

            if (GUILayout.Button(selectedLabel + (m_isGroupDropdownOpen ? "  ▲" : "  ▼"), buttonStyle))
                m_isGroupDropdownOpen = !m_isGroupDropdownOpen;

            if (!m_isGroupDropdownOpen)
                return;

            float dropdownHeight = Mathf.Min(160f, Mathf.Max(34f, m_groups.Count * 32f));
            m_groupDropdownScrollPosition = GUILayout.BeginScrollView(m_groupDropdownScrollPosition, false, true,
                GUILayout.Height(dropdownHeight));
            for (int i = 0; i < m_groups.Count; i++)
            {
                CrowdAgentGroup group = m_groups[i];
                if (group == null)
                    continue;

                string optionLabel = $"{group.name}  (ID {group.GroupIdValue}, {group.AgentCount} 个)";
                if (!GUILayout.Button(optionLabel, buttonStyle))
                    continue;

                SelectGroup(group);
                m_isGroupDropdownOpen = false;
            }

            GUILayout.EndScrollView();
        }

        // 绘制 Agent 来源下拉列表
        private void DrawAgentSourceDropdown(GUIStyle buttonStyle)
        {
            CrowdAgentView selectedView = GetSelectedAgentSource();
            string selectedLabel = selectedView == null
                ? GetNewAgentSourceLabel()
                : $"复制 {selectedView.name}（实体 {selectedView.EntityIdValue}）";

            if (GUILayout.Button(selectedLabel + (m_isAgentDropdownOpen ? "  ▲" : "  ▼"), buttonStyle))
                m_isAgentDropdownOpen = !m_isAgentDropdownOpen;

            if (!m_isAgentDropdownOpen)
                return;

            int optionCount = m_agentSources.Count + 1;
            float dropdownHeight = Mathf.Min(180f, Mathf.Max(34f, optionCount * 32f));
            m_agentDropdownScrollPosition = GUILayout.BeginScrollView(m_agentDropdownScrollPosition, false, true,
                GUILayout.Height(dropdownHeight));

            if (GUILayout.Button(GetNewAgentSourceLabel(), buttonStyle))
            {
                m_selectedAgentSourceId = NEW_AGENT_SOURCE_ID;
                m_isAgentDropdownOpen = false;
            }

            for (int i = 0; i < m_agentSources.Count; i++)
            {
                CrowdAgentView view = m_agentSources[i];
                if (view == null)
                    continue;

                string optionLabel = $"复制 {view.name}（实体 {view.EntityIdValue}）";
                if (!GUILayout.Button(optionLabel, buttonStyle))
                    continue;

                m_selectedAgentSourceId = view.EntityIdValue;
                m_isAgentDropdownOpen = false;
            }

            GUILayout.EndScrollView();
        }

        // 获取新建 Agent 使用的实际预制体显示名
        private string GetNewAgentSourceLabel()
        {
            CrowdSpawnController spawnController = CrowdSpawnController.Active;
            return spawnController == null
                ? "新建 Agent（未配置）"
                : $"新建 Agent（{spawnController.AgentPrefabName}）";
        }

        // 在群体注册表变化时刷新下拉列表
        private void RefreshGroupsIfNeeded()
        {
            if (m_groupVersion == CrowdAgentGroup.Version)
                return;

            m_groups.Clear();
            IReadOnlyList<CrowdAgentGroup> activeGroups = CrowdAgentGroup.ActiveGroups;
            for (int i = 0; i < activeGroups.Count; i++)
            {
                CrowdAgentGroup group = activeGroups[i];
                if (group == null)
                    continue;

                m_groups.Add(group);
            }

            CrowdAgentGroup selectedGroup = GetSelectedGroup();
            if (selectedGroup == null && m_groups.Count > 0)
                SelectGroup(m_groups[0]);

            m_groupVersion = CrowdAgentGroup.Version;
        }

        // 将指定群体设为GM和地面输入的当前目标
        private void SelectGroup(CrowdAgentGroup group)
        {
            if (group == null)
                return;

            m_selectedGroupId = group.GroupIdValue;
            m_world.SetSelectedGroup(m_selectedGroupId);
            m_status = $"当前群体已切换为 {group.name}";
        }

        // 创建新的群体父节点并切换为当前目标
        private void CreateNewGroup()
        {
            Transform groupRoot = GetGroupRoot();
            if (groupRoot == null)
            {
                m_status = "未找到 CrowdSystem 群体根节点，无法创建新群体";
                return;
            }

            CrowdAgentGroup group = CrowdAgentGroup.CreateRuntimeGroup(groupRoot);
            if (group == null)
            {
                m_status = "创建群体失败，请检查 Console";
                return;
            }

            RefreshGroupsIfNeeded();
            SelectGroup(group);
            m_status = $"已创建 {group.name}，Group ID 为 {group.GroupIdValue}";
        }

        // 获取场景中所有群体共用的 CrowdSystem 父节点
        private Transform GetGroupRoot()
        {
            if (m_groups.Count > 0 && m_groups[0] != null)
                return m_groups[0].transform.parent;

            CrowdSpawnController spawnController = CrowdSpawnController.Active;
            return spawnController == null ? null : spawnController.transform.parent;
        }

        // 在场景表现集合变化时刷新 Agent 来源列表
        private void RefreshAgentSourcesIfNeeded()
        {
            if (m_agentSourceVersion == CrowdAgentView.Version)
                return;

            m_agentSources.Clear();
            IReadOnlyList<CrowdAgentView> activeViews = CrowdAgentView.ActiveViews;
            bool selectedSourceStillExists = m_selectedAgentSourceId == NEW_AGENT_SOURCE_ID;
            for (int i = 0; i < activeViews.Count; i++)
            {
                CrowdAgentView view = activeViews[i];
                if (view == null || view.EntityIdValue < 0)
                    continue;

                m_agentSources.Add(view);
                if (view.EntityIdValue == m_selectedAgentSourceId)
                    selectedSourceStillExists = true;
            }

            if (!selectedSourceStillExists)
                m_selectedAgentSourceId = NEW_AGENT_SOURCE_ID;

            m_agentSourceVersion = CrowdAgentView.Version;
        }

        // 获取当前选择的 Agent 来源表现对象
        private CrowdAgentView GetSelectedAgentSource()
        {
            if (m_selectedAgentSourceId == NEW_AGENT_SOURCE_ID)
                return null;

            for (int i = 0; i < m_agentSources.Count; i++)
            {
                CrowdAgentView view = m_agentSources[i];
                if (view != null && view.EntityIdValue == m_selectedAgentSourceId)
                    return view;
            }

            return null;
        }

        // 获取当前选中的群体父节点
        private CrowdAgentGroup GetSelectedGroup()
        {
            for (int i = 0; i < m_groups.Count; i++)
            {
                CrowdAgentGroup group = m_groups[i];
                if (group != null && group.GroupIdValue == m_selectedGroupId)
                    return group;
            }

            return null;
        }

        // 按GM输入数量生成当前群体的Agent
        private void SpawnSelectedGroup()
        {
            CrowdAgentGroup group = GetSelectedGroup();
            if (group == null)
            {
                m_status = "请先选择有效的群体父节点";
                return;
            }

            if (!int.TryParse(m_spawnCountInput, out int count))
            {
                m_status = "生成数量必须是整数";
                return;
            }

            CrowdSpawnController spawnController = CrowdSpawnController.Active;
            if (spawnController == null)
            {
                QLog.Error("Crowd Agent生成失败：场景中未找到 CrowdSpawnController");
                m_status = "场景中未配置 CrowdSpawnController";
                return;
            }

            CrowdAgentView template = GetSelectedAgentSource();
            if (m_selectedAgentSourceId != NEW_AGENT_SOURCE_ID && template == null)
            {
                m_status = "所选 Agent 已不可用，请重新选择来源";
                return;
            }

            int spawnedCount = spawnController.SpawnAgents(group, count, template);
            string sourceLabel = template == null ? spawnController.AgentPrefabName : template.name;
            m_status = $"已在 {group.name} 下生成 {spawnedCount} 个 Agent（来源：{sourceLabel}）";
        }

        // 停止当前选中群体的移动
        private void StopSelectedGroup()
        {
            m_world.EnqueueStopGroup(m_selectedGroupId);
            m_status = $"已向 Group {m_selectedGroupId} 发布停止命令";
        }

        // 在场景表现集合变化后刷新ECS绑定
        private void BindViewsIfNeeded()
        {
            if (m_viewVersion == CrowdAgentView.Version)
                return;

            HashSet<int> activeViewIds = new HashSet<int>();
            IReadOnlyList<CrowdAgentView> activeViews = CrowdAgentView.ActiveViews;
            for (int i = 0; i < activeViews.Count; i++)
            {
                CrowdAgentView view = activeViews[i];
                if (view == null || view.EntityIdValue < 0)
                    continue;

                CrowdAgentGroup group = view.GetComponentInParent<CrowdAgentGroup>();
                if (group == null)
                {
                    QLog.Error($"Crowd Agent 缺少群体父节点：实体 {view.EntityIdValue}");
                    continue;
                }

                if (!activeViewIds.Add(view.EntityIdValue))
                {
                    QLog.Error($"Crowd Agent 表现绑定重复：实体 {view.EntityIdValue}");
                    continue;
                }

                m_world.BindAgentView(view.EntityIdValue, group.GroupIdValue, view.transform);
                m_boundViewIds.Add(view.EntityIdValue);
            }

            List<int> removedViewIds = new List<int>();
            foreach (int boundViewId in m_boundViewIds)
            {
                if (!activeViewIds.Contains(boundViewId))
                    removedViewIds.Add(boundViewId);
            }

            for (int i = 0; i < removedViewIds.Count; i++)
            {
                int entityId = removedViewIds[i];
                m_world.DeleteEntity(entityId);
                m_boundViewIds.Remove(entityId);
            }

            m_viewVersion = CrowdAgentView.Version;
        }

        // 读取避障与哈希系统的运行统计，系统引用只在首次解析
        private void RefreshAvoidanceStats()
        {
            if (!m_isSystemResolved)
            {
                m_hashSystem = FindSystem<CrowdSpatialHashSystem>((int) CrowdSystemType.SpatialHash);
                m_avoidanceSystem = FindSystem<CrowdAvoidanceSystem>((int) CrowdSystemType.Avoidance);
                m_overlapSystem = FindSystem<CrowdOverlapResolveSystem>((int) CrowdSystemType.OverlapResolve);
                m_isSystemResolved = true;
            }

            if (m_hashSystem != null)
            {
                m_hashAgentCount = m_hashSystem.AgentCount;
                m_hashCellCount = m_hashSystem.CellCount;
            }

            if (m_avoidanceSystem != null)
            {
                m_maxPenetration = m_avoidanceSystem.MaxPenetration;
                m_hasInvalidVelocity = m_avoidanceSystem.HasInvalidVelocity;
            }

            if (m_overlapSystem != null)
            {
                m_remainingPenetration = m_overlapSystem.RemainingPenetration;
                m_correctedPairCount = m_overlapSystem.CorrectedPairCount;
            }
        }

        // 按系统编号取系统，取不到说明 Register 漏注册，直接报错暴露配置缺失
        private T FindSystem<T>(int sysType) where T : ECSSystem
        {
            ECSSystem system;
            if (!m_world.Systems.TryGetValue(sysType, out system))
            {
                QLog.Error($"Crowd 世界未注册系统 {sysType}，无法读取 {typeof(T).Name} 统计");
                return null;
            }

            T typedSystem = system as T;
            if (typedSystem == null)
                QLog.Error($"Crowd 系统 {sysType} 类型不符，期望 {typeof(T).Name}，实际 {system.GetType().Name}");

            return typedSystem;
        }

        // 绘制避障权重与查询参数，滑杆结果统一走 Clamp 兜住越界
        private void DrawAvoidanceControls(GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle buttonStyle)
        {
            CrowdAvoidanceData data = m_world.AvoidanceData;

            data.SeparationStrength = DrawSliderRow("分离权重", data.SeparationStrength,
                STRENGTH_MIN, STRENGTH_MAX, labelStyle, valueStyle);
            data.AvoidanceStrength = DrawSliderRow("规避权重", data.AvoidanceStrength,
                STRENGTH_MIN, STRENGTH_MAX, labelStyle, valueStyle);
            data.NeighborRadius = DrawSliderRow("邻居半径", data.NeighborRadius,
                NEIGHBOR_RADIUS_MIN, NEIGHBOR_RADIUS_MAX, labelStyle, valueStyle);
            data.SeparationMargin = DrawSliderRow("安全间距", data.SeparationMargin,
                STRENGTH_MIN, SEPARATION_MARGIN_MAX, labelStyle, valueStyle);
            data.PredictTime = DrawSliderRow("预测时长", data.PredictTime,
                PREDICT_TIME_MIN, PREDICT_TIME_MAX, labelStyle, valueStyle);
            data.MaxNeighborCount = (int) DrawSliderRow("邻居上限", data.MaxNeighborCount,
                NEIGHBOR_COUNT_MIN, NEIGHBOR_COUNT_MAX, labelStyle, valueStyle);
            data.OverlapStrength = DrawSliderRow("重叠修正", data.OverlapStrength,
                STRENGTH_MIN, OVERLAP_STRENGTH_MAX, labelStyle, valueStyle);
            data.Clamp();

            GUILayout.Label("邻居半径须大于两代理半径与安全间距之和，否则重叠会被半径提前剔除", labelStyle);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("权重归零", buttonStyle))
                SetAvoidanceWeights(data, 0f, 0f);
            if (GUILayout.Button("恢复默认", buttonStyle))
                SetAvoidanceWeights(data, DEFAULT_SEPARATION_STRENGTH, DEFAULT_AVOIDANCE_STRENGTH);
            GUILayout.EndHorizontal();
        }

        // 绘制一行带实时数值的滑杆并返回结果
        private float DrawSliderRow(string label, float value, float minValue, float maxValue,
            GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(92f));
            float result = GUILayout.HorizontalSlider(value, minValue, maxValue);
            GUILayout.Label(result.ToString("0.00"), valueStyle, GUILayout.Width(42f));
            GUILayout.EndHorizontal();

            return result;
        }

        // 设置两类修正权重，归零即回到无避障的直线冲锋基线
        private void SetAvoidanceWeights(CrowdAvoidanceData data, float separationStrength, float avoidanceStrength)
        {
            data.SeparationStrength = separationStrength;
            data.AvoidanceStrength = avoidanceStrength;
        }

        // 绘制一行 Crowd ECS 诊断信息
        private void DrawInfoRow(string label, string value, GUIStyle labelStyle, GUIStyle valueStyle)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, labelStyle, GUILayout.Width(92f));
            GUILayout.Label(value, valueStyle);
            GUILayout.EndHorizontal();
        }
    }
}
