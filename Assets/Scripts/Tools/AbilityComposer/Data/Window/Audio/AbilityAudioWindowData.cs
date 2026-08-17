/*
 * ┌──────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 音效窗口数据，保存音效资源、播放方式与目标参数
 * │  类    名: AbilityAudioWindowData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace Module.Ability.Data.Window.Audio
{
    [Serializable]
    [DeclareFoldoutGroup("Audio", Title = "音效", Expanded = true)]
    [DeclareFoldoutGroup("Transform", Title = "播放位置", Expanded = false)]
    public sealed class AbilityAudioWindowData : AbilityWindowDataBase
    {
        private static readonly AudioClip[] S_EmptyAudioClips = new AudioClip[0];

        [Group("Audio")]
        [LabelText("触发方式")]
        [SerializeField] private AbilityAudioTriggerType TriggerTypeValue;

        [Group("Audio")]
        [LabelText("播放类型")]
        [SerializeField] private AbilityAudioPlaybackType PlaybackTypeValue;

        [Group("Audio")]
        [LabelText("音效列表")]
        [SerializeField] private AudioClip[] AudioClipValues = new AudioClip[0];

        [HideInInspector]
        [SerializeField] private AudioClip AudioClipAValue;

        [HideInInspector]
        [SerializeField] private AudioClip AudioClipBValue;

        [HideInInspector]
        [SerializeField] private AudioClip AudioClipCValue;

        [Group("Audio")]
        [LabelText("音量")]
        [SerializeField, Range(0f, 1f)] private float VolumeValue = 1f;

        [Group("Audio")]
        [LabelText("音高")]
        [SerializeField, Range(0.1f, 3f)] private float PitchValue = 1f;

        [Group("Audio")]
        [LabelText("随机音高")]
        [SerializeField, Range(0f, 1f)] private float RandomPitchRangeValue;

        [Group("Audio")]
        [LabelText("3D 音效")]
        [SerializeField] private bool SpatialValue = true;

        [Group("Audio")]
        [LabelText("窗口结束时停止")]
        [SerializeField] private bool StopOnWindowEndValue = true;

        [Group("Transform")]
        [LabelText("播放目标")]
        [SerializeField] private AbilityAudioTargetType TargetTypeValue;

        [Group("Transform")]
        [LabelText("挂点 Id")]
        [SerializeField] private string SocketIdValue;

        [Group("Transform")]
        [LabelText("位置偏移")]
        [SerializeField] private Vector3 LocalPositionOffsetValue;

        public AbilityAudioTriggerType TriggerType => TriggerTypeValue;
        public AbilityAudioPlaybackType PlaybackType => PlaybackTypeValue;
        public IReadOnlyList<AudioClip> AudioClips => GetAudioClips();
        public int AudioClipCount => GetAudioClipSlotCount();
        public AudioClip AudioClipA => GetAudioClip(0);
        public AudioClip AudioClipB => GetAudioClip(1);
        public AudioClip AudioClipC => GetAudioClip(2);
        public float Volume => VolumeValue;
        public float Pitch => PitchValue;
        public float RandomPitchRange => RandomPitchRangeValue;
        public bool Spatial => SpatialValue;
        public bool StopOnWindowEnd => StopOnWindowEndValue;
        public AbilityAudioTargetType TargetType => TargetTypeValue;
        public string SocketId => SocketIdValue;
        public Vector3 LocalPositionOffset => LocalPositionOffsetValue;

        public AbilityAudioWindowData()
        {
        }

        public AbilityAudioWindowData(float startNormalizedTime, float endNormalizedTime)
            : base(startNormalizedTime, endNormalizedTime)
        {
        }

        public AbilityAudioWindowData(string id, float startNormalizedTime, float endNormalizedTime,
            AbilityAudioTriggerType triggerType, AbilityAudioPlaybackType playbackType,
            IReadOnlyList<AudioClip> audioClips, float volume, float pitch, float randomPitchRange, bool spatial,
            bool stopOnWindowEnd, AbilityAudioTargetType targetType, string socketId, Vector3 localPositionOffset)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
            TriggerTypeValue = triggerType;
            PlaybackTypeValue = playbackType;
            SetAudioClipValues(audioClips);
            VolumeValue = volume;
            PitchValue = pitch;
            RandomPitchRangeValue = randomPitchRange;
            SpatialValue = spatial;
            StopOnWindowEndValue = stopOnWindowEnd;
            TargetTypeValue = targetType;
            SocketIdValue = socketId;
            LocalPositionOffsetValue = localPositionOffset;
        }

        public AbilityAudioWindowData(string id, float startNormalizedTime, float endNormalizedTime,
            AbilityAudioTriggerType triggerType, AbilityAudioPlaybackType playbackType, AudioClip audioClipA,
            AudioClip audioClipB, AudioClip audioClipC, float volume, float pitch, float randomPitchRange,
            bool spatial, bool stopOnWindowEnd, AbilityAudioTargetType targetType, string socketId,
            Vector3 localPositionOffset)
            : this(id, startNormalizedTime, endNormalizedTime, triggerType, playbackType,
                new[] { audioClipA, audioClipB, audioClipC }, volume, pitch, randomPitchRange, spatial,
                stopOnWindowEnd, targetType, socketId, localPositionOffset)
        {
        }

        // 根据序号读取可用音效资源
        public AudioClip GetAudioClip(int clipIndex)
        {
            if (clipIndex < 0)
                return null;

            if (AudioClipValues != null && AudioClipValues.Length > 0)
            {
                if (clipIndex >= AudioClipValues.Length)
                    return null;

                return AudioClipValues[clipIndex];
            }

            if (!HasLegacyAudioClip())
                return null;

            return clipIndex switch
            {
                0 => AudioClipAValue,
                1 => AudioClipBValue,
                2 => AudioClipCValue,
                _ => null
            };
        }

        // 统计当前窗口配置的可用音效数量
        public int GetAudioClipCount()
        {
            return AudioClipCount;
        }

        // 写入新的数组音效数据
        private void SetAudioClipValues(IReadOnlyList<AudioClip> audioClips)
        {
            if (audioClips == null)
            {
                AudioClipValues = S_EmptyAudioClips;
                return;
            }

            AudioClipValues = new AudioClip[audioClips.Count];
            for (int clipIndex = 0; clipIndex < audioClips.Count; clipIndex++)
                AudioClipValues[clipIndex] = audioClips[clipIndex];
        }

        // 读取序列化音效数组，兼容旧版三槽资源
        private IReadOnlyList<AudioClip> GetAudioClips()
        {
            if (AudioClipValues != null && AudioClipValues.Length > 0)
                return AudioClipValues;

            if (!HasLegacyAudioClip())
                return S_EmptyAudioClips;

            return new[] { AudioClipAValue, AudioClipBValue, AudioClipCValue };
        }

        // 获取当前音效槽位数量
        private int GetAudioClipSlotCount()
        {
            if (AudioClipValues != null && AudioClipValues.Length > 0)
                return AudioClipValues.Length;

            return HasLegacyAudioClip() ? 3 : 0;
        }

        // 判断旧版三槽资源是否需要迁移读取
        private bool HasLegacyAudioClip()
        {
            return AudioClipAValue != null || AudioClipBValue != null || AudioClipCValue != null;
        }
    }
}
