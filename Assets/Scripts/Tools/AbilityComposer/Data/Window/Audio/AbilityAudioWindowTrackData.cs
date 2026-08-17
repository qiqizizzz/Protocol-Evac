/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 音效窗口轨道数据，保存一个动画的音效窗口集合
 * │  类    名: AbilityAudioWindowTrackData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEngine;

namespace Module.Ability.Data.Window.Audio
{
    [Serializable]
    public sealed class AbilityAudioWindowTrackData
    {
        [SerializeField] private List<AbilityAudioWindowData> WindowValues = new List<AbilityAudioWindowData>();

        public IReadOnlyList<AbilityAudioWindowData> Windows => WindowValues;
        public int WindowCount => WindowValues.Count;

        // 使用编辑器提交的音效窗口替换轨道数据
        public void SetWindows(IReadOnlyList<AbilityAudioWindowData> windows)
        {
            WindowValues.Clear();
            for (int windowIndex = 0; windowIndex < windows.Count; windowIndex++)
            {
                AbilityAudioWindowData window = windows[windowIndex];
                WindowValues.Add(new AbilityAudioWindowData(window.Id, window.StartNormalizedTime,
                    window.EndNormalizedTime, window.TriggerType, window.PlaybackType, window.AudioClips,
                    window.Volume, window.Pitch, window.RandomPitchRange,
                    window.Spatial, window.StopOnWindowEnd, window.TargetType, window.SocketId,
                    window.LocalPositionOffset));
            }
        }

        // 查找指定时间处于活动状态的全部音效窗口
        public void GetActiveWindows(float normalizedTime, List<AbilityAudioWindowData> results)
        {
            results.Clear();
            for (int windowIndex = 0; windowIndex < WindowValues.Count; windowIndex++)
            {
                AbilityAudioWindowData window = WindowValues[windowIndex];
                if (window.IsActiveAt(normalizedTime))
                    results.Add(window);
            }
        }
    }
}
