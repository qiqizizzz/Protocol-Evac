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
using Module.CrowdDemo;
using UnityEngine;
using Utils.log;

namespace Tools.GM
{
    internal sealed class CrowdGamePanel : IGamePanel
    {
        private readonly CrowdWorld m_world = new CrowdWorld();
        private readonly List<CrowdAgentGroup> m_groups = new List<CrowdAgentGroup>();
        private readonly HashSet<int> m_boundViewIds = new HashSet<int>();

        private string m_spawnCountInput = "10";
        private string m_status = "未发送命令";
        private int m_entityCount;
        private int m_pendingCommandCount;
        private int m_selectedGroupId;
        private int m_groupVersion = -1;
        private int m_viewVersion = -1;
        private bool m_isGroupDropdownOpen;

        // 驱动 Crowd ECS 世界执行待处理命令
        public void Tick(float deltaTime)
        {
            RefreshGroupsIfNeeded();
            BindViewsIfNeeded();
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
        }

        // 根据当前页签绘制 Crowd ECS 调试内容
        public void Draw(int activeTabIndex, GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle toggleStyle,
            GUIStyle buttonStyle)
        {
            if (activeTabIndex != 3)
                return;

            DrawCrowdControls(labelStyle, valueStyle, buttonStyle);
        }

        // 绘制群体选择、生成和停止控制
        private void DrawCrowdControls(GUIStyle labelStyle, GUIStyle valueStyle, GUIStyle buttonStyle)
        {
            DrawInfoRow("实体数量", m_entityCount.ToString(), labelStyle, valueStyle);
            DrawInfoRow("待处理命令", m_pendingCommandCount.ToString(), labelStyle, valueStyle);
            GUILayout.Space(8f);

            GUILayout.Label("当前 Agent 群体父节点", labelStyle);
            DrawGroupDropdown(valueStyle, buttonStyle);

            CrowdAgentGroup selectedGroup = GetSelectedGroup();
            if (selectedGroup == null)
            {
                GUILayout.Label("场景中没有可用的 CrowdAgentGroup 父节点", valueStyle);
                GUILayout.Label(m_status, valueStyle);
                return;
            }

            DrawInfoRow("父节点", selectedGroup.name, labelStyle, valueStyle);
            DrawInfoRow("Group ID", selectedGroup.GroupIdValue.ToString(), labelStyle, valueStyle);
            DrawInfoRow("子节点数量", selectedGroup.AgentCount.ToString(), labelStyle, valueStyle);

            GUILayout.Space(8f);
            GUILayout.Label("生成数量", labelStyle);
            GUILayout.BeginHorizontal();
            m_spawnCountInput = GUILayout.TextField(m_spawnCountInput);
            if (GUILayout.Button("生成 Agent", buttonStyle, GUILayout.Width(112f)))
                SpawnSelectedGroup();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);
            GUILayout.Label("点击地面让当前群体前往目标位置", valueStyle);
            if (GUILayout.Button("停止当前群体", buttonStyle))
                StopSelectedGroup();

            GUILayout.Label(m_status, valueStyle);
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

            GUILayout.BeginVertical();
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

            GUILayout.EndVertical();
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

            int spawnedCount = spawnController.SpawnAgents(group, count);
            m_status = $"已在 {group.name} 下生成 {spawnedCount} 个 Agent";
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
