/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 音效窗口控制器，负责按时间与命中事件播放音效
 * │  类    名: AbilityAudioWindowController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Framework.QTower.Common.Sound;
using Module.Ability.Data.Window.Audio;
using Module.Ability.Vfx;
using Module.Combat.Damage;
using UnityEngine;

namespace Module.Ability.Audio
{
    public sealed class AbilityAudioWindowController
    {
        private readonly GameObject m_sourceRoot;
        private readonly VfxSocketBinder m_sourceSocketBinder;
        private readonly List<AbilityAudioWindowData> m_activeWindows = new List<AbilityAudioWindowData>();
        private readonly List<AudioClip> m_availableClips = new List<AudioClip>();
        private readonly List<AudioSource> m_durationSources = new List<AudioSource>();
        private readonly List<string> m_durationWindowIds = new List<string>();
        private readonly Dictionary<string, int> m_sequenceClipIndices = new Dictionary<string, int>();
        private readonly HashSet<string> m_enteredWindowIds = new HashSet<string>();

        private AbilityAudioWindowTrackData m_currentTrack;
        private int m_currentSegmentIndex = -1;
        private float m_currentNormalizedTime;

        // 创建音效窗口控制器
        public AbilityAudioWindowController(GameObject sourceRoot)
        {
            m_sourceRoot = sourceRoot;
            m_sourceSocketBinder = sourceRoot == null ? null : sourceRoot.GetComponentInChildren<VfxSocketBinder>();
        }

        // 根据当前段落与时间同步进入类和持续类音效
        public void Sync(AbilityAudioWindowTrackData windowTrack, float normalizedTime, int segmentIndex)
        {
            if (m_currentSegmentIndex != segmentIndex || m_currentTrack != windowTrack)
            {
                Close();
                m_currentSegmentIndex = segmentIndex;
                m_currentTrack = windowTrack;
            }

            m_currentNormalizedTime = normalizedTime;
            if (windowTrack == null)
            {
                Close();
                return;
            }

            windowTrack.GetActiveWindows(normalizedTime, m_activeWindows);
            SyncWindowEnterAudio();
            SyncWindowDurationAudio();
        }

        // 在真实命中时播放当前时间可用的 OnHit 音效
        public void PlayHitAudio(DamageData damageData, Component hitTarget)
        {
            if (m_currentTrack == null)
                return;

            m_currentTrack.GetActiveWindows(m_currentNormalizedTime, m_activeWindows);
            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityAudioWindowData window = m_activeWindows[windowIndex];
                if (window.TriggerType != AbilityAudioTriggerType.OnHit)
                    continue;

                PlayAudio(window, damageData, hitTarget);
            }
        }

        // 关闭当前所有持续音效
        public void Close()
        {
            for (int sourceIndex = 0; sourceIndex < m_durationSources.Count; sourceIndex++)
                SoundManager.Stop(m_durationSources[sourceIndex]);

            m_durationSources.Clear();
            m_durationWindowIds.Clear();
            m_enteredWindowIds.Clear();
            m_sequenceClipIndices.Clear();
            m_currentSegmentIndex = -1;
            m_currentTrack = null;
            m_currentNormalizedTime = 0f;
        }

        // 同步只在窗口进入时播放一次的音效
        private void SyncWindowEnterAudio()
        {
            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityAudioWindowData window = m_activeWindows[windowIndex];
                if (window.TriggerType != AbilityAudioTriggerType.WindowEnter || m_enteredWindowIds.Contains(window.Id))
                    continue;

                AudioSource source = PlayAudio(window, default, null);
                if (source != null && window.StopOnWindowEnd)
                    TrackDurationSource(window.Id, source);

                m_enteredWindowIds.Add(window.Id);
            }
        }

        // 同步窗口持续期间的循环音效或可截断音效
        private void SyncWindowDurationAudio()
        {
            for (int sourceIndex = m_durationSources.Count - 1; sourceIndex >= 0; sourceIndex--)
            {
                if (ContainsActiveDurationWindow(m_durationWindowIds[sourceIndex]))
                    continue;

                SoundManager.Stop(m_durationSources[sourceIndex]);
                m_durationSources.RemoveAt(sourceIndex);
                m_durationWindowIds.RemoveAt(sourceIndex);
            }

            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityAudioWindowData window = m_activeWindows[windowIndex];
                if (window.TriggerType != AbilityAudioTriggerType.WindowDuration
                    || m_durationWindowIds.Contains(window.Id))
                    continue;

                AudioSource source = PlayAudio(window, default, null);
                if (source != null && (window.PlaybackType == AbilityAudioPlaybackType.Loop || window.StopOnWindowEnd))
                    TrackDurationSource(window.Id, source);
            }
        }

        // 判断指定持续窗口当前是否仍处于活动状态
        private bool ContainsActiveDurationWindow(string windowId)
        {
            for (int windowIndex = 0; windowIndex < m_activeWindows.Count; windowIndex++)
            {
                AbilityAudioWindowData window = m_activeWindows[windowIndex];
                if (window.Id == windowId && window.TriggerType != AbilityAudioTriggerType.OnHit)
                    return true;
            }

            return false;
        }

        // 记录需要在窗口结束时停止的播放源
        private void TrackDurationSource(string windowId, AudioSource audioSource)
        {
            m_durationSources.Add(audioSource);
            m_durationWindowIds.Add(windowId);
        }

        // 播放单个音效窗口
        private AudioSource PlayAudio(AbilityAudioWindowData window, DamageData damageData, Component hitTarget)
        {
            AudioClip clip = SelectAudioClip(window);
            if (clip == null || !TryResolveAudioTarget(window, damageData, hitTarget,
                    out Transform parent, out Vector3 position))
                return null;

            float pitch = ResolvePitch(window);
            if (window.PlaybackType == AbilityAudioPlaybackType.Loop)
                return SoundManager.PlayLoop(clip, parent, parent == null ? position : window.LocalPositionOffset,
                    window.Volume, pitch, window.Spatial);

            return SoundManager.PlayEffect(clip, position, window.Volume, pitch, window.Spatial);
        }

        // 按窗口播放类型选择本次音效资源
        private AudioClip SelectAudioClip(AbilityAudioWindowData window)
        {
            m_availableClips.Clear();
            for (int clipIndex = 0; clipIndex < window.AudioClipCount; clipIndex++)
            {
                AudioClip clip = window.GetAudioClip(clipIndex);
                if (clip != null)
                    m_availableClips.Add(clip);
            }

            if (m_availableClips.Count == 0)
                return null;

            if (window.PlaybackType == AbilityAudioPlaybackType.RandomOneShot)
                return m_availableClips[Random.Range(0, m_availableClips.Count)];

            if (window.PlaybackType == AbilityAudioPlaybackType.SequenceOneShot)
                return SelectSequenceAudioClip(window.Id);

            return m_availableClips[0];
        }

        // 顺序轮播读取音效资源
        private AudioClip SelectSequenceAudioClip(string windowId)
        {
            if (!m_sequenceClipIndices.TryGetValue(windowId, out int clipIndex))
                clipIndex = 0;

            AudioClip clip = m_availableClips[clipIndex % m_availableClips.Count];
            m_sequenceClipIndices[windowId] = clipIndex + 1;
            return clip;
        }

        // 计算本次播放音高
        private float ResolvePitch(AbilityAudioWindowData window)
        {
            if (window.RandomPitchRange <= 0f)
                return window.Pitch;

            return Mathf.Max(0.01f, window.Pitch + Random.Range(-window.RandomPitchRange, window.RandomPitchRange));
        }

        // 解析本次音效的播放位置
        private bool TryResolveAudioTarget(AbilityAudioWindowData window, DamageData damageData, Component hitTarget,
            out Transform parent, out Vector3 position)
        {
            parent = null;
            position = Vector3.zero;
            switch (window.TargetType)
            {
                case AbilityAudioTargetType.SourceRoot:
                    parent = m_sourceRoot == null ? null : m_sourceRoot.transform;
                    break;
                case AbilityAudioTargetType.SourceSocket:
                    if (!TryResolveSourceSocket(window.SocketId, out parent))
                        return false;

                    break;
                case AbilityAudioTargetType.HitPoint:
                    if (damageData.Source == null)
                        return false;

                    position = damageData.HitPoint + window.LocalPositionOffset;
                    return true;
                case AbilityAudioTargetType.HitTargetRoot:
                    parent = hitTarget == null ? null : hitTarget.transform;
                    break;
                case AbilityAudioTargetType.HitTargetSocket:
                    if (!TryResolveHitTargetSocket(hitTarget, window.SocketId, out parent))
                        return false;

                    break;
            }

            if (parent == null)
                return false;

            position = parent.TransformPoint(window.LocalPositionOffset);
            return true;
        }

        // 查询攻击者音效挂点
        private bool TryResolveSourceSocket(string socketId, out Transform socket)
        {
            socket = null;
            return m_sourceSocketBinder != null && m_sourceSocketBinder.TryGetSocket(socketId, out socket);
        }

        // 查询受击目标音效挂点
        private static bool TryResolveHitTargetSocket(Component hitTarget, string socketId, out Transform socket)
        {
            socket = null;
            if (hitTarget == null)
                return false;

            VfxSocketBinder socketBinder = hitTarget.GetComponentInChildren<VfxSocketBinder>();
            return socketBinder != null && socketBinder.TryGetSocket(socketId, out socket);
        }
    }
}
