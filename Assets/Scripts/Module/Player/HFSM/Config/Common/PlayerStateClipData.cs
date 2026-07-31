/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态动画段落数据，保存动画片段与持续时间
 * │  类    名: PlayerStateClipData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Module.Player.HFSM.Config.Common
{
    [Serializable]
    public sealed class PlayerStateClipData
    {
        [Tooltip("状态动画片段")]
        [SerializeField] private AnimationClip StateClipValue;

        [Tooltip("状态持续时间")]
        [SerializeField, Min(0f)] private float StateDurationValue = 0.5f;

        public AnimationClip StateClip => StateClipValue;

        public float StateDuration => StateDurationValue;

        // 从动画片段同步状态持续时间
        public bool SyncDurationFromClip()
        {
            if (StateClipValue == null)
                return false;

            StateDurationValue = StateClipValue.length;
            return true;
        }
    }
}
