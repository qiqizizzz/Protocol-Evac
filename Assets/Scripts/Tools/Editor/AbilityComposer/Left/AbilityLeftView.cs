/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 左侧视图，管理播放控制与事件操作入口
 * │  类    名: AbilityLeftView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using Framework.QTower.Editor.View;
using Tools.Editor.AbilityComposer.Center.Timeline;
using UnityEngine.UIElements;
using Utils.log;

namespace Tools.Editor.AbilityComposer.Left
{
    public sealed class AbilityLeftView : UIBaseEditor
    {
        private readonly VisualElement m_rootVisualElement;
        private Button m_jumpFirstFrameButton;
        private Button m_previousFrameButton;
        private Button m_playToggleButton;
        private Button m_nextFrameButton;
        private Button m_jumpLastFrameButton;
        private Label m_playToggleLabel;
        private TextField m_currentFrameField;
        private Label m_lastFrameLabel;
        private Button m_addEventButton;
        private Button m_deleteEventButton;
        private bool m_isControlsReady;

        public event Action OnJumpFirstFrameRequested;
        public event Action OnPreviousFrameRequested;
        public event Action OnPlaybackToggled;
        public event Action OnNextFrameRequested;
        public event Action OnJumpLastFrameRequested;
        public event Action<int> OnCurrentFrameChanged;
        public event Action OnAddEventRequested;
        public event Action OnDeleteSelectedEventRequested;

        // 注入左侧区域根节点
        public AbilityLeftView(VisualElement rootVisualElement)
        {
            m_rootVisualElement = rootVisualElement;
        }

        // 查找左侧播放控件
        protected override void OnEditorInit()
        {
            m_jumpFirstFrameButton = m_rootVisualElement.Q<Button>("jump-first-frame-button");
            m_previousFrameButton = m_rootVisualElement.Q<Button>("previous-frame-button");
            m_playToggleButton = m_rootVisualElement.Q<Button>("play-toggle-button");
            m_nextFrameButton = m_rootVisualElement.Q<Button>("next-frame-button");
            m_jumpLastFrameButton = m_rootVisualElement.Q<Button>("jump-last-frame-button");
            m_playToggleLabel = m_rootVisualElement.Q<Label>("play-toggle-label");
            m_currentFrameField = m_rootVisualElement.Q<TextField>("preview-current-frame-field");
            m_lastFrameLabel = m_rootVisualElement.Q<Label>("preview-last-frame-label");
            m_addEventButton = m_rootVisualElement.Q<Button>("add-event-button");
            m_deleteEventButton = m_rootVisualElement.Q<Button>("delete-event-button");
            m_isControlsReady = m_jumpFirstFrameButton != null && m_previousFrameButton != null
                && m_playToggleButton != null && m_nextFrameButton != null && m_jumpLastFrameButton != null
                && m_playToggleLabel != null && m_currentFrameField != null && m_lastFrameLabel != null
                && m_addEventButton != null && m_deleteEventButton != null;
            if (!m_isControlsReady)
                QLog.Error("配置 Ability Composer 左侧视图失败：缺少必要的 UXML 控件");
        }

        // 使用当前数据刷新播放状态
        public void Refresh(AbilityTimelineData timelineData, bool hasPreview)
        {
            bool hasAnimationClip = timelineData.HasClip;
            m_playToggleButton.SetEnabled(hasAnimationClip);
            m_jumpFirstFrameButton.SetEnabled(hasAnimationClip);
            m_previousFrameButton.SetEnabled(hasAnimationClip);
            m_nextFrameButton.SetEnabled(hasAnimationClip);
            m_jumpLastFrameButton.SetEnabled(hasAnimationClip);
            m_addEventButton.SetEnabled(hasAnimationClip);
            m_deleteEventButton.SetEnabled(timelineData.SelectedEvent != null);

            if (!hasAnimationClip)
            {
                m_currentFrameField.SetValueWithoutNotify("--");
                m_lastFrameLabel.text = "--";
                m_playToggleLabel.text = "▶";
                return;
            }

            m_currentFrameField.SetValueWithoutNotify(timelineData.CurrentFrame.ToString());
            m_lastFrameLabel.text = timelineData.LastFrame.ToString();
            m_playToggleLabel.text = timelineData.IsPlaying ? "Ⅱ" : "▶";
        }

        protected override void SubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_currentFrameField.RegisterValueChangedCallback(HandleCurrentFrameChanged);
            m_jumpFirstFrameButton.clicked += RequestJumpFirstFrame;
            m_previousFrameButton.clicked += RequestPreviousFrame;
            m_playToggleButton.clicked += RequestPlaybackToggle;
            m_nextFrameButton.clicked += RequestNextFrame;
            m_jumpLastFrameButton.clicked += RequestJumpLastFrame;
            m_addEventButton.clicked += RequestAddEvent;
            m_deleteEventButton.clicked += RequestDeleteSelectedEvent;
        }

        protected override void UnsubscribeViewEvents()
        {
            if (!m_isControlsReady)
                return;

            m_currentFrameField.UnregisterValueChangedCallback(HandleCurrentFrameChanged);
            m_jumpFirstFrameButton.clicked -= RequestJumpFirstFrame;
            m_previousFrameButton.clicked -= RequestPreviousFrame;
            m_playToggleButton.clicked -= RequestPlaybackToggle;
            m_nextFrameButton.clicked -= RequestNextFrame;
            m_jumpLastFrameButton.clicked -= RequestJumpLastFrame;
            m_addEventButton.clicked -= RequestAddEvent;
            m_deleteEventButton.clicked -= RequestDeleteSelectedEvent;
            m_isControlsReady = false;
        }

        // 将帧输入转换为跳转请求
        private void HandleCurrentFrameChanged(ChangeEvent<string> changeEvent)
        {
            if (int.TryParse(changeEvent.newValue, out int frame))
                OnCurrentFrameChanged?.Invoke(frame);
        }

        // 请求跳转到第一帧
        private void RequestJumpFirstFrame() => OnJumpFirstFrameRequested?.Invoke();

        // 请求显示前一帧
        private void RequestPreviousFrame() => OnPreviousFrameRequested?.Invoke();

        // 请求切换播放状态
        private void RequestPlaybackToggle() => OnPlaybackToggled?.Invoke();

        // 请求显示后一帧
        private void RequestNextFrame() => OnNextFrameRequested?.Invoke();

        // 请求跳转到最后一帧
        private void RequestJumpLastFrame() => OnJumpLastFrameRequested?.Invoke();

        // 请求在当前帧添加事件
        private void RequestAddEvent() => OnAddEventRequested?.Invoke();

        // 请求删除选中的事件
        private void RequestDeleteSelectedEvent() => OnDeleteSelectedEventRequested?.Invoke();
    }
}
