/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 玩家受击动画段落数据，保存动画片段与烘焙时长
 * │  类    名: PlayerHurtAnimationData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using System;
using Module.Ability.Data.Animation;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Disabled
{
    [Serializable]
    [DeclareFoldoutGroup("Animation", Title = "动画与段落", Expanded = true)]
    public sealed class PlayerHurtAnimationData : IAnimationDurationSyncable
    {
        [Group("Animation")]
        [LabelText("动画片段")]
        [Tooltip("受击状态使用的动画片段")]
        [SerializeField] private AnimationClip AnimationClipValue;

        [Group("Animation")]
        [LabelText("持续时间")]
        [Tooltip("从受击动画片段烘焙的状态持续时间")]
        [SerializeField, Min(0f)] private float DurationValue;

        public AnimationClip AnimationClip => AnimationClipValue;
        public float Duration => DurationValue;

        // 创建空的受击动画段落数据
        public PlayerHurtAnimationData()
        {
        }

        // 创建受击动画段落数据
        public PlayerHurtAnimationData(AnimationClip animationClip, float duration)
        {
            AnimationClipValue = animationClip;
            DurationValue = duration;
        }

        // 从动画片段同步受击段落持续时间
        public bool SyncAnimationDurations()
        {
            if (AnimationClipValue == null)
                return false;

            DurationValue = AnimationClipValue.length;
            return true;
        }
    }
}
