/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 中间视图，承载 Timeline 工作区域
 * │  类    名: AbilityCenterView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using Framework.QTower.Editor.View;
using UnityEngine.UIElements;
using Utils.log;

namespace Tools.Editor.AbilityComposer.Center
{
    public sealed class AbilityCenterView : UIBaseEditor
    {
        private readonly VisualElement m_rootVisualElement;
        private ScrollView m_timelineScrollView;
        private VisualElement m_timelineContent;

        // 注入中间区域根节点
        public AbilityCenterView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }

        // 查找 Timeline 工作区域控件
        protected override void OnEditorInit()
        {
            m_timelineScrollView = m_rootVisualElement.Q<ScrollView>("timeline-scroll-view");
            m_timelineContent = m_rootVisualElement.Q<VisualElement>("timeline-content");
        }

        // 返回 Timeline 所需的滚动容器与内容容器
        public bool TryGetTimelineElements(out ScrollView timelineScrollView, out VisualElement timelineContent)
        {
            timelineScrollView = m_timelineScrollView;
            timelineContent = m_timelineContent;
            if (timelineScrollView != null && timelineContent != null)
                return true;

            QLog.Error("配置 Ability Composer 中间视图失败：缺少 Timeline 控件");
            return false;
        }
    }
}
