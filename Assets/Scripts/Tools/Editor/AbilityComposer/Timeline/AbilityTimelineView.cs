/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴视图，负责刻度、播放头与横向滚动交互
 * │  类    名: AbilityTimelineView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using Framework.QTower.Editor.View;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.Editor.AbilityComposer.Timeline
{
    public sealed class AbilityTimelineView : UIBaseEditor
    {
        private const float MIN_PIXELS_PER_FRAME = 4f;
        private const float MAX_PIXELS_PER_FRAME = 48f;
        private const float DEFAULT_PIXELS_PER_FRAME = 12f;
        private const float MIN_TIMELINE_WIDTH = 620f;
        private const int MAJOR_TICK_FRAME_INTERVAL = 5;

        private AbilityTimelineData m_timelineData;
        private ScrollView m_scrollView;
        private VisualElement m_timelineContent;
        private VisualElement m_timelinePlayhead;
        private float m_pixelsPerFrame = DEFAULT_PIXELS_PER_FRAME;
        private bool m_isDraggingPlayhead;
        private int m_draggingPointerId;

        public event Action<int> OnFrameRequested;

        public float PixelsPerFrame => m_pixelsPerFrame;

        // 注入时间轴视图需要的 UI 元素
        public AbilityTimelineView(ScrollView scrollView, VisualElement timelineContent)
        {
            m_scrollView = scrollView;
            m_timelineContent = timelineContent;
        }

        // 初始化时间轴的 UI 元素
        protected override void OnEditorInit()
        {
            m_scrollView.mode = ScrollViewMode.Horizontal;
            m_scrollView.horizontalScrollerVisibility = ScrollerVisibility.Auto;
            m_scrollView.verticalScrollerVisibility = ScrollerVisibility.Hidden;
            m_timelineContent.RegisterCallback<WheelEvent>(HandleTimelineWheel);
        }

        protected override void SubscribeViewEvents()
        {
            m_timelineContent.RegisterCallback<PointerDownEvent>(BeginPlayheadDrag);
            m_timelineContent.RegisterCallback<PointerMoveEvent>(UpdatePlayheadDrag);
            m_timelineContent.RegisterCallback<PointerUpEvent>(EndPlayheadDrag);
            m_timelineContent.RegisterCallback<PointerCaptureOutEvent>(CancelPlayheadDrag);
        }

        // 设置当前时间轴数据并重建刻度、轨道和播放头
        public void SetTimelineData(AbilityTimelineData timelineData)
        {
            m_timelineData = timelineData;
            BuildTimeline();
        }

        // 刷新播放头到当前帧的统一像素坐标
        public void RefreshCurrentFrame()
        {
            if (m_timelinePlayhead == null || m_timelineData == null)
                return;

            m_timelinePlayhead.style.left = FrameToPixel(m_timelineData.CurrentFrame);
        }

        // 设置时间轴显示缩放并保持当前帧位置
        public void SetPixelsPerFrame(float pixelsPerFrame)
        {
            SetPixelsPerFrame(pixelsPerFrame, null);
        }

        // 将指定帧滚动到当前可见区域
        public void ScrollFrameIntoView(int frame)
        {
            float framePosition = FrameToPixel(frame);
            float viewportWidth = m_scrollView.contentViewport.resolvedStyle.width;
            float scrollPosition = m_scrollView.scrollOffset.x;
            if (framePosition < scrollPosition)
                m_scrollView.scrollOffset = new Vector2(framePosition, 0f);
            else if (framePosition > scrollPosition + viewportWidth - m_pixelsPerFrame)
                m_scrollView.scrollOffset = new Vector2(framePosition - viewportWidth + m_pixelsPerFrame * 2f, 0f);
        }

        protected override void UnsubscribeViewEvents()
        {
            if (m_isDraggingPlayhead)
                m_timelineContent.ReleasePointer(m_draggingPointerId);

            m_timelineContent.UnregisterCallback<PointerDownEvent>(BeginPlayheadDrag);
            m_timelineContent.UnregisterCallback<PointerMoveEvent>(UpdatePlayheadDrag);
            m_timelineContent.UnregisterCallback<PointerUpEvent>(EndPlayheadDrag);
            m_timelineContent.UnregisterCallback<PointerCaptureOutEvent>(CancelPlayheadDrag);
            m_timelineContent.UnregisterCallback<WheelEvent>(HandleTimelineWheel);
            m_isDraggingPlayhead = false;
        }

        // 根据动画帧数据构建时间轴内容
        private void BuildTimeline()
        {
            m_timelineContent.Clear();
            m_timelinePlayhead = null;
            if (m_timelineData == null || !m_timelineData.HasClip)
            {
                m_timelineContent.style.width = MIN_TIMELINE_WIDTH;
                CreateEmptyState();
                return;
            }

            float timelineWidth = CalculateTimelineWidth();
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

            for (int frame = 0; frame <= m_timelineData.LastFrame; frame++)
            {
                float framePosition = FrameToPixel(frame);
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
            if (m_timelineData == null || !m_timelineData.HasClip || pointerEvent.button != 0)
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
            int targetFrame = PixelToFrame(localPositionX);
            OnFrameRequested?.Invoke(Mathf.Clamp(targetFrame, 0, m_timelineData.LastFrame));
        }

        // 使用 Ctrl 加滚轮调整时间轴缩放，并以鼠标位置为锚点
        private void HandleTimelineWheel(WheelEvent wheelEvent)
        {
            if (!wheelEvent.ctrlKey || m_timelineData == null || !m_timelineData.HasClip)
                return;

            float viewportX = m_scrollView.contentViewport.WorldToLocal(wheelEvent.mousePosition).x;
            float localX = m_timelineContent.WorldToLocal(wheelEvent.mousePosition).x;
            float anchorFrame = localX / m_pixelsPerFrame;
            float zoomFactor = wheelEvent.delta.y < 0f ? 1.15f : 1f / 1.15f;
            SetPixelsPerFrame(m_pixelsPerFrame * zoomFactor, new ZoomAnchor(anchorFrame, viewportX));
            wheelEvent.StopPropagation();
        }

        // 应用缩放比例并重建时间轴布局
        private void SetPixelsPerFrame(float pixelsPerFrame, ZoomAnchor? anchor)
        {
            float clampedPixelsPerFrame = Mathf.Clamp(pixelsPerFrame, MIN_PIXELS_PER_FRAME, MAX_PIXELS_PER_FRAME);
            if (Mathf.Approximately(clampedPixelsPerFrame, m_pixelsPerFrame))
                return;

            m_pixelsPerFrame = clampedPixelsPerFrame;
            float targetScrollX = anchor.HasValue
                ? anchor.Value.Frame * m_pixelsPerFrame - anchor.Value.ViewportX
                : m_scrollView.scrollOffset.x;
            BuildTimeline();
            m_scrollView.schedule.Execute(() =>
            {
                float maxScrollX = Mathf.Max(0f, m_timelineContent.resolvedStyle.width - m_scrollView.contentViewport.resolvedStyle.width);
                m_scrollView.scrollOffset = new Vector2(Mathf.Clamp(targetScrollX, 0f, maxScrollX), 0f);
            });
        }

        // 计算当前缩放比例下的时间轴宽度
        private float CalculateTimelineWidth()
        {
            if (m_timelineData == null || !m_timelineData.HasClip)
                return MIN_TIMELINE_WIDTH;

            return Mathf.Max(MIN_TIMELINE_WIDTH, FrameToPixel(m_timelineData.LastFrame) + 1f);
        }

        // 将帧号换算为时间轴像素位置
        private float FrameToPixel(int frame)
        {
            return frame * m_pixelsPerFrame;
        }

        // 将时间轴像素位置换算为最近帧号
        private int PixelToFrame(float pixel)
        {
            return Mathf.RoundToInt(pixel / m_pixelsPerFrame);
        }

        private readonly struct ZoomAnchor
        {
            public readonly float Frame;
            public readonly float ViewportX;

            public ZoomAnchor(float frame, float viewportX)
            {
                Frame = frame;
                ViewportX = viewportX;
            }
        }
    }
}
