/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴视图，负责刻度、播放头与横向滚动交互
 * │  类    名: AbilityTimelineView.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using Module.Player.Window;
using Tools.Editor.AbilityComposer.Center.Event;
using Framework.QTower.Editor.View;
using UnityEngine;
using UnityEngine.UIElements;

namespace Tools.Editor.AbilityComposer.Center.Timeline
{
    public sealed class AbilityTimelineView : UIBaseEditor
    {
        private const float MIN_PIXELS_PER_FRAME = 4f;
        private const float MAX_PIXELS_PER_FRAME = 48f;
        private const float DEFAULT_PIXELS_PER_FRAME = 12f;
        private const float MIN_TIMELINE_WIDTH = 620f;
        private const float TIMELINE_LEFT_PADDING = 8f;
        private const int MAJOR_TICK_FRAME_INTERVAL = 5;

        private AbilityTimelineData m_timelineData;
        private ScrollView m_scrollView;
        private VisualElement m_timelineContent;
        private VisualElement m_timelinePlayhead;
        private float m_pixelsPerFrame = DEFAULT_PIXELS_PER_FRAME;
        private bool m_isDraggingPlayhead;
        private int m_draggingPointerId;
        private AbilityEventDraft m_draggingEvent;
        private int m_draggingEventPointerId;
        private int m_draggingEventFrame;
        private AbilityWindowDraft m_draggingWindow;
        private int m_draggingWindowPointerId;
        private WindowDragMode m_windowDragMode;
        private int m_draggingWindowStartFrame;
        private int m_draggingWindowEndFrame;
        private int m_draggingWindowPointerFrame;

        public event Action<int> OnFrameRequested;
        public event Action<string> OnEventSelected;
        public event Action<EventMoveRequest> OnEventMoved;
        public event Action<string> OnWindowSelected;
        public event Action<WindowFrameRangeRequest> OnWindowFrameRangeChanged;

        public readonly struct EventMoveRequest
        {
            public readonly string EventId;
            public readonly int Frame;

            public EventMoveRequest(string eventId, int frame)
            {
                EventId = eventId;
                Frame = frame;
            }
        }

        public readonly struct WindowFrameRangeRequest
        {
            public readonly string WindowId;
            public readonly int StartFrame;
            public readonly int EndFrame;

            public WindowFrameRangeRequest(string windowId, int startFrame, int endFrame)
            {
                WindowId = windowId;
                StartFrame = startFrame;
                EndFrame = endFrame;
            }
        }

        private enum WindowDragMode
        {
            Move,
            ResizeStart,
            ResizeEnd
        }

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
            m_timelineContent.RegisterCallback<PointerMoveEvent>(UpdateDraggedEvent, TrickleDown.TrickleDown);
            m_timelineContent.RegisterCallback<PointerUpEvent>(EndDraggedEvent, TrickleDown.TrickleDown);
            m_timelineContent.RegisterCallback<PointerMoveEvent>(UpdateDraggedWindow, TrickleDown.TrickleDown);
            m_timelineContent.RegisterCallback<PointerUpEvent>(EndDraggedWindow, TrickleDown.TrickleDown);
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

        // 刷新事件标记外观
        public void RefreshEventMarkers()
        {
            BuildTimeline();
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
            m_timelineContent.UnregisterCallback<PointerMoveEvent>(UpdateDraggedEvent, TrickleDown.TrickleDown);
            m_timelineContent.UnregisterCallback<PointerUpEvent>(EndDraggedEvent, TrickleDown.TrickleDown);
            m_timelineContent.UnregisterCallback<PointerMoveEvent>(UpdateDraggedWindow, TrickleDown.TrickleDown);
            m_timelineContent.UnregisterCallback<PointerUpEvent>(EndDraggedWindow, TrickleDown.TrickleDown);
            m_timelineContent.UnregisterCallback<WheelEvent>(HandleTimelineWheel);
            m_isDraggingPlayhead = false;
            m_draggingEvent = null;
            m_draggingWindow = null;
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
            CreateEventMarkers();
            CreateWindowTrackDivider();
            CreateWindowMarkers();
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
                frameLabel.style.left = frame == 0 ? TIMELINE_LEFT_PADDING : framePosition - 12f;
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

        // 创建按分类显示颜色的动画事件标记
        private void CreateEventMarkers()
        {
            foreach (AbilityEventDraft eventDraft in m_timelineData.EventDraftValues)
            {
                Button eventMarker = new Button();
                eventMarker.name = eventDraft.Id;
                eventMarker.AddToClassList("ac-timeline-event-marker");
                eventMarker.AddToClassList($"ac-event-{eventDraft.Category.ToString().ToLowerInvariant()}");
                if (m_timelineData.SelectedEvent == eventDraft)
                {
                    eventMarker.AddToClassList("ac-timeline-event-marker-selected");
                    ApplySelectedInnerBorder(eventMarker);
                }

                eventMarker.style.left = FrameToPixel(eventDraft.Frame) - 5f;
                eventMarker.clicked += () => OnEventSelected?.Invoke(eventDraft.Id);
                eventMarker.RegisterCallback<PointerDownEvent>(pointerEvent => BeginEventDrag(eventMarker, eventDraft, pointerEvent),
                    TrickleDown.TrickleDown);
                m_timelineContent.Add(eventMarker);
            }
        }

        // 创建事件轨道与窗口轨道之间的分隔线
        private void CreateWindowTrackDivider()
        {
            VisualElement divider = new VisualElement();
            divider.AddToClassList("ac-timeline-window-divider");
            m_timelineContent.Add(divider);
        }

        // 创建按窗口类型显示颜色的时间区间标记
        private void CreateWindowMarkers()
        {
            foreach (AbilityWindowDraft windowDraft in m_timelineData.WindowDraftValues)
            {
                Button windowMarker = new Button();
                windowMarker.name = windowDraft.Id;
                windowMarker.AddToClassList("ac-timeline-window-marker");
                windowMarker.AddToClassList(windowDraft.Type == AbilityWindowType.Hit ? "ac-window-hit" : "ac-window-invincible");
                if (m_timelineData.SelectedWindow == windowDraft)
                {
                    windowMarker.AddToClassList("ac-timeline-window-marker-selected");
                    ApplySelectedInnerBorder(windowMarker);
                }

                windowMarker.style.left = FrameToPixel(windowDraft.StartFrame);
                windowMarker.style.width = Mathf.Max(m_pixelsPerFrame, FrameToPixel(windowDraft.EndFrame) - FrameToPixel(windowDraft.StartFrame) + m_pixelsPerFrame);
                Label windowLabel = new Label(windowDraft.Type == AbilityWindowType.Hit ? "命中窗口" : "无敌帧窗口");
                windowLabel.AddToClassList("ac-timeline-window-label");
                windowMarker.Add(windowLabel);
                windowMarker.RegisterCallback<PointerDownEvent>(pointerEvent => BeginWindowDrag(windowMarker, windowDraft, pointerEvent), TrickleDown.TrickleDown);
                m_timelineContent.Add(windowMarker);
            }
        }

        // 开始移动或调整窗口边界并阻止事件冒泡到播放头
        private void BeginWindowDrag(Button windowMarker, AbilityWindowDraft windowDraft, PointerDownEvent pointerEvent)
        {
            if (pointerEvent.button != 0 || m_timelineData == null || !m_timelineData.HasClip)
                return;

            float localPointerX = windowMarker.WorldToLocal(pointerEvent.position).x;
            float markerWidth = windowMarker.resolvedStyle.width;
            m_draggingWindow = windowDraft;
            m_draggingWindowPointerId = pointerEvent.pointerId;
            m_draggingWindowStartFrame = windowDraft.StartFrame;
            m_draggingWindowEndFrame = windowDraft.EndFrame;
            m_draggingWindowPointerFrame = GetFrameFromPointerPosition(pointerEvent.position);
            m_windowDragMode = localPointerX <= 7f
                ? WindowDragMode.ResizeStart
                : localPointerX >= markerWidth - 7f
                    ? WindowDragMode.ResizeEnd
                    : WindowDragMode.Move;
            windowMarker.CapturePointer(m_draggingWindowPointerId);
            OnWindowSelected?.Invoke(windowDraft.Id);
            pointerEvent.StopPropagation();
        }

        // 根据鼠标位置实时预览窗口平移或缩放结果
        private void UpdateDraggedWindow(PointerMoveEvent pointerEvent)
        {
            if (m_draggingWindow == null || pointerEvent.pointerId != m_draggingWindowPointerId)
                return;

            int pointerFrame = GetFrameFromPointerPosition(pointerEvent.position);
            int startFrame = m_draggingWindowStartFrame;
            int endFrame = m_draggingWindowEndFrame;
            if (m_windowDragMode == WindowDragMode.ResizeStart)
            {
                startFrame = Mathf.Clamp(pointerFrame, 0, endFrame);
            }
            else if (m_windowDragMode == WindowDragMode.ResizeEnd)
            {
                endFrame = Mathf.Clamp(pointerFrame, startFrame, m_timelineData.LastFrame);
            }
            else
            {
                int windowLength = endFrame - startFrame;
                int frameOffset = pointerFrame - m_draggingWindowPointerFrame;
                startFrame = Mathf.Clamp(m_draggingWindowStartFrame + frameOffset, 0, m_timelineData.LastFrame - windowLength);
                endFrame = startFrame + windowLength;
            }

            UpdateWindowMarker(m_draggingWindow.Id, startFrame, endFrame);
            pointerEvent.StopPropagation();
        }

        // 结束当前窗口拖动并提交吸附后的帧范围
        private void EndDraggedWindow(PointerUpEvent pointerEvent)
        {
            if (m_draggingWindow == null || pointerEvent.pointerId != m_draggingWindowPointerId)
                return;

            int pointerFrame = GetFrameFromPointerPosition(pointerEvent.position);
            int startFrame = m_draggingWindowStartFrame;
            int endFrame = m_draggingWindowEndFrame;
            if (m_windowDragMode == WindowDragMode.ResizeStart)
                startFrame = Mathf.Clamp(pointerFrame, 0, endFrame);
            else if (m_windowDragMode == WindowDragMode.ResizeEnd)
                endFrame = Mathf.Clamp(pointerFrame, startFrame, m_timelineData.LastFrame);
            else
            {
                int windowLength = endFrame - startFrame;
                int frameOffset = pointerFrame - m_draggingWindowPointerFrame;
                startFrame = Mathf.Clamp(m_draggingWindowStartFrame + frameOffset, 0, m_timelineData.LastFrame - windowLength);
                endFrame = startFrame + windowLength;
            }

            Button windowMarker = m_timelineContent.Q<Button>(m_draggingWindow.Id);
            if (windowMarker != null && windowMarker.HasPointerCapture(m_draggingWindowPointerId))
                windowMarker.ReleasePointer(m_draggingWindowPointerId);

            OnWindowFrameRangeChanged?.Invoke(new WindowFrameRangeRequest(m_draggingWindow.Id, startFrame, endFrame));
            m_draggingWindow = null;
            pointerEvent.StopPropagation();
        }

        // 按帧范围更新当前窗口条的预览位置和宽度
        private void UpdateWindowMarker(string windowId, int startFrame, int endFrame)
        {
            Button windowMarker = m_timelineContent.Q<Button>(windowId);
            if (windowMarker == null)
                return;

            windowMarker.style.left = FrameToPixel(startFrame);
            windowMarker.style.width = Mathf.Max(m_pixelsPerFrame, FrameToPixel(endFrame) - FrameToPixel(startFrame) + m_pixelsPerFrame);
        }

        // 为选中标记设置不受 Button 悬停样式覆盖的白色内边框
        private void ApplySelectedInnerBorder(VisualElement marker)
        {
            marker.style.borderTopWidth = 2f;
            marker.style.borderRightWidth = 2f;
            marker.style.borderBottomWidth = 2f;
            marker.style.borderLeftWidth = 2f;
            marker.style.borderTopColor = Color.white;
            marker.style.borderRightColor = Color.white;
            marker.style.borderBottomColor = Color.white;
            marker.style.borderLeftColor = Color.white;
        }

        // 开始拖动动画事件并阻止事件冒泡到播放头
        private void BeginEventDrag(Button eventMarker, AbilityEventDraft eventDraft, PointerDownEvent pointerEvent)
        {
            if (m_timelineData == null || !m_timelineData.HasClip || pointerEvent.button != 0)
                return;

            m_draggingEvent = eventDraft;
            m_draggingEventPointerId = pointerEvent.pointerId;
            m_draggingEventFrame = eventDraft.Frame;
            eventMarker.CapturePointer(m_draggingEventPointerId);
            OnEventSelected?.Invoke(eventDraft.Id);
            pointerEvent.StopPropagation();
        }

        // 按鼠标位置实时更新当前拖动事件所在帧
        private void UpdateDraggedEvent(PointerMoveEvent pointerEvent)
        {
            if (m_draggingEvent == null || pointerEvent.pointerId != m_draggingEventPointerId)
                return;

            int targetFrame = GetFrameFromPointerPosition(pointerEvent.position);
            m_draggingEventFrame = targetFrame;
            Button eventMarker = m_timelineContent.Q<Button>(m_draggingEvent.Id);
            if (eventMarker != null)
                eventMarker.style.left = FrameToPixel(targetFrame) - 5f;
            pointerEvent.StopPropagation();
        }

        // 结束当前事件拖动并提交新的帧位置
        private void EndDraggedEvent(PointerUpEvent pointerEvent)
        {
            if (m_draggingEvent == null || pointerEvent.pointerId != m_draggingEventPointerId)
                return;

            Button eventMarker = m_timelineContent.Q<Button>(m_draggingEvent.Id);
            if (eventMarker != null && eventMarker.HasPointerCapture(m_draggingEventPointerId))
                eventMarker.ReleasePointer(m_draggingEventPointerId);

            OnEventMoved?.Invoke(new EventMoveRequest(m_draggingEvent.Id, m_draggingEventFrame));
            m_draggingEvent = null;
            pointerEvent.StopPropagation();
        }

        // 开始拖动播放头并请求跳转到鼠标所在帧
        private void BeginPlayheadDrag(PointerDownEvent pointerEvent)
        {
            if (m_timelineData == null || !m_timelineData.HasClip || pointerEvent.button != 0
                || m_draggingEvent != null || m_draggingWindow != null)
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
            int targetFrame = GetFrameFromPointerPosition(pointerPosition);
            OnFrameRequested?.Invoke(Mathf.Clamp(targetFrame, 0, m_timelineData.LastFrame));
        }

        // 将屏幕坐标转换为时间轴帧号
        private int GetFrameFromPointerPosition(Vector3 pointerPosition)
        {
            Vector2 panelPosition = new Vector2(pointerPosition.x, pointerPosition.y);
            float localPositionX = m_timelineContent.WorldToLocal(panelPosition).x;
            return Mathf.Clamp(PixelToFrame(localPositionX), 0, m_timelineData.LastFrame);
        }

        // 使用 Ctrl 加滚轮调整时间轴缩放，并以鼠标位置为锚点
        private void HandleTimelineWheel(WheelEvent wheelEvent)
        {
            if (!wheelEvent.ctrlKey || m_timelineData == null || !m_timelineData.HasClip)
                return;

            float viewportX = m_scrollView.contentViewport.WorldToLocal(wheelEvent.mousePosition).x;
            float localX = m_timelineContent.WorldToLocal(wheelEvent.mousePosition).x;
            float anchorFrame = PixelToFrame(localX);
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
                ? FrameToPixel(anchor.Value.Frame) - anchor.Value.ViewportX
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
            return FrameToPixel((float)frame);
        }

        // 将浮点帧位置换算为时间轴像素位置
        private float FrameToPixel(float frame)
        {
            return TIMELINE_LEFT_PADDING + frame * m_pixelsPerFrame;
        }

        // 将时间轴像素位置换算为最近帧号
        private int PixelToFrame(float pixel)
        {
            return Mathf.RoundToInt((pixel - TIMELINE_LEFT_PADDING) / m_pixelsPerFrame);
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
