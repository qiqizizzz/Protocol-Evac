/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴视图，负责刻度、播放头与横向滚动交互
 * │  类    名: AbilityTimelineView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using Tools.Editor.AbilityComposer.Preview;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.Editor.AbilityComposer.Timeline
{
    public sealed class AbilityTimelineView
    {
        private const float TIMELINE_PIXELS_PER_FRAME = 12f;
        private const float MIN_TIMELINE_WIDTH = 620f;
        private const int MAJOR_TICK_FRAME_INTERVAL = 5;

        private AbilityPreviewData m_previewData;
        private ScrollView m_scrollView;
        private VisualElement m_timelineContent;
        private VisualElement m_timelinePlayhead;
        private bool m_isDraggingPlayhead;
        private int m_draggingPointerId;

        public event Action<int> OnFrameRequested;

        // 初始化时间轴的 UI 元素与鼠标交互
        public void Initialize(ScrollView scrollView, VisualElement timelineContent)
        {
            m_scrollView = scrollView;
            m_timelineContent = timelineContent;
            m_scrollView.mode = ScrollViewMode.Horizontal;
            m_scrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            m_scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_timelineContent.RegisterCallback<PointerDownEvent>(BeginPlayheadDrag);
            m_timelineContent.RegisterCallback<PointerMoveEvent>(UpdatePlayheadDrag);
            m_timelineContent.RegisterCallback<PointerUpEvent>(EndPlayheadDrag);
            m_timelineContent.RegisterCallback<PointerCaptureOutEvent>(CancelPlayheadDrag);
        }

        // 重建当前动画片段对应的刻度、轨道和播放头
        public void SetPreviewData(AbilityPreviewData previewData)
        {
            m_previewData = previewData;
            BuildTimeline();
        }

        // 刷新播放头到当前帧的统一像素坐标
        public void RefreshCurrentFrame()
        {
            if (m_timelinePlayhead == null || m_previewData == null)
                return;

            m_timelinePlayhead.style.left = m_previewData.CurrentFrame * TIMELINE_PIXELS_PER_FRAME;
        }

        // 将指定帧滚动到当前可见区域
        public void ScrollFrameIntoView(int frame)
        {
            float framePosition = frame * TIMELINE_PIXELS_PER_FRAME;
            float viewportWidth = m_scrollView.contentViewport.resolvedStyle.width;
            float scrollPosition = m_scrollView.scrollOffset.x;
            if (framePosition < scrollPosition)
                m_scrollView.scrollOffset = new Vector2(framePosition, 0f);
            else if (framePosition > scrollPosition + viewportWidth - TIMELINE_PIXELS_PER_FRAME)
                m_scrollView.scrollOffset = new Vector2(framePosition - viewportWidth + TIMELINE_PIXELS_PER_FRAME * 2f, 0f);
        }

        // 解除重建窗口前注册的 UI 回调
        public void Dispose()
        {
            if (m_timelineContent == null)
                return;

            if (m_isDraggingPlayhead)
                m_timelineContent.ReleasePointer(m_draggingPointerId);

            m_timelineContent.UnregisterCallback<PointerDownEvent>(BeginPlayheadDrag);
            m_timelineContent.UnregisterCallback<PointerMoveEvent>(UpdatePlayheadDrag);
            m_timelineContent.UnregisterCallback<PointerUpEvent>(EndPlayheadDrag);
            m_timelineContent.UnregisterCallback<PointerCaptureOutEvent>(CancelPlayheadDrag);
            m_isDraggingPlayhead = false;
        }

        // 根据动画帧数据构建时间轴内容
        private void BuildTimeline()
        {
            m_timelineContent.Clear();
            m_timelinePlayhead = null;
            if (m_previewData == null || !m_previewData.HasClip)
            {
                m_timelineContent.style.width = MIN_TIMELINE_WIDTH;
                CreateEmptyState();
                return;
            }

            float timelineWidth = Mathf.Max(MIN_TIMELINE_WIDTH, m_previewData.LastFrame * TIMELINE_PIXELS_PER_FRAME + 1f);
            m_timelineContent.style.width = timelineWidth;
            CreateTimelineTrack();
            CreateTimelineRuler(timelineWidth);
            CreateTimelinePlayhead();
        }

        // 创建未选择动画时的时间轴提示
        private void CreateEmptyState()
        {
            VisualElement emptyState = new VisualElement();
            emptyState.AddToClassList("ac-timeline-empty-state");
            Label emptyStateLabel = new Label("选择 Animation Clip 后，在此处编辑动画事件");
            emptyStateLabel.AddToClassList("ac-empty-state-label");
            emptyState.Add(emptyStateLabel);
            m_timelineContent.Add(emptyState);
        }

        // 创建 Animation Events 轨道背景
        private void CreateTimelineTrack()
        {
            VisualElement track = new VisualElement();
            track.AddToClassList("ac-timeline-track");
            Label trackLabel = new Label("Animation Events");
            trackLabel.AddToClassList("ac-timeline-track-label");
            track.Add(trackLabel);
            m_timelineContent.Add(track);
        }

        // 创建动画软件风格的帧刻度与数字
        private void CreateTimelineRuler(float timelineWidth)
        {
            VisualElement ruler = new VisualElement();
            ruler.AddToClassList("ac-timeline-ruler");
            ruler.style.width = timelineWidth;

            for (int frame = 0; frame <= m_previewData.LastFrame; frame++)
            {
                float framePosition = frame * TIMELINE_PIXELS_PER_FRAME;
                VisualElement tick = new VisualElement();
                tick.AddToClassList(frame % MAJOR_TICK_FRAME_INTERVAL == 0 ? "ac-ruler-tick-major" : "ac-ruler-tick");
                tick.style.left = framePosition;
                ruler.Add(tick);

                if (frame % MAJOR_TICK_FRAME_INTERVAL != 0)
                    continue;

                Label frameLabel = new Label(frame.ToString());
                frameLabel.AddToClassList("ac-ruler-frame-label");
                frameLabel.style.left = frame == 0 ? 0f : framePosition - 12f;
                if (frame == 0)
                    frameLabel.AddToClassList("ac-ruler-frame-label-first");

                ruler.Add(frameLabel);
            }

            m_timelineContent.Add(ruler);
        }

        // 创建贯穿时间轴的长播放头
        private void CreateTimelinePlayhead()
        {
            m_timelinePlayhead = new VisualElement();
            m_timelinePlayhead.AddToClassList("ac-timeline-playhead");
            m_timelineContent.Add(m_timelinePlayhead);
            RefreshCurrentFrame();
        }

        // 开始拖动播放头并请求跳转到鼠标所在帧
        private void BeginPlayheadDrag(PointerDownEvent pointerEvent)
        {
            if (m_previewData == null || !m_previewData.HasClip || pointerEvent.button != 0)
                return;

            m_isDraggingPlayhead = true;
            m_draggingPointerId = pointerEvent.pointerId;
            m_timelineContent.CapturePointer(m_draggingPointerId);
            RequestFrameFromPointerPosition(pointerEvent.position);
            pointerEvent.StopPropagation();
        }

        // 拖动期间持续请求更新到最近的整数帧
        private void UpdatePlayheadDrag(PointerMoveEvent pointerEvent)
        {
            if (!m_isDraggingPlayhead || pointerEvent.pointerId != m_draggingPointerId)
                return;

            RequestFrameFromPointerPosition(pointerEvent.position);
            pointerEvent.StopPropagation();
        }

        // 松开鼠标后结束播放头拖动
        private void EndPlayheadDrag(PointerUpEvent pointerEvent)
        {
            if (!m_isDraggingPlayhead || pointerEvent.pointerId != m_draggingPointerId)
                return;

            m_timelineContent.ReleasePointer(m_draggingPointerId);
            m_isDraggingPlayhead = false;
            pointerEvent.StopPropagation();
        }

        // 鼠标捕获丢失时清理拖动状态
        private void CancelPlayheadDrag(PointerCaptureOutEvent pointerEvent)
        {
            if (pointerEvent.pointerId != m_draggingPointerId)
                return;

            m_isDraggingPlayhead = false;
        }

        // 将鼠标位置换算为最近的有效动画帧并通知窗口
        private void RequestFrameFromPointerPosition(Vector3 pointerPosition)
        {
            Vector2 panelPosition = new Vector2(pointerPosition.x, pointerPosition.y);
            float localPositionX = m_timelineContent.WorldToLocal(panelPosition).x;
            int targetFrame = Mathf.RoundToInt(localPositionX / TIMELINE_PIXELS_PER_FRAME);
            OnFrameRequested?.Invoke(Mathf.Clamp(targetFrame, 0, m_previewData.LastFrame));
        }
    }
}
