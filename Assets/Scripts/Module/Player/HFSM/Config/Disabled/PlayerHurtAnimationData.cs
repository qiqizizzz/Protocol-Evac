/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 玩家受击动画段落数据，保存动画片段与烘焙时长
 * │  类    名: PlayerHurtAnimationData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using System;
using Module.Ability.Data.Animation;
using Module.Ability.Data.Window;
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

        [Group("Animation")]
        [LabelText("窗口主体配置")]
        [Tooltip("当前受击动画对应的窗口主体配置")]
        [SerializeField] private AbilityWindowConfigSO WindowConfigValue;

        public AnimationClip AnimationClip => AnimationClipValue;
        public float Duration => DurationValue;
        public AbilityWindowConfigSO WindowConfig => WindowConfigValue;

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

        // 返回移动锁定窗口约束后的受击状态持续时间
        public float GetStateDuration()
        {
            return WindowConfigValue == null
                ? DurationValue
                : WindowConfigValue.ResolveMovementLockDuration(DurationValue);
        }

        // 判断指定动画归一化时间是否处于移动锁定窗口
        public bool IsMovementLockedAt(float normalizedTime)
        {
            return WindowConfigValue.IsMovementLockedAt(normalizedTime);
        }

        // 更新当前受击动画绑定的窗口主体配置
        public void SetWindowConfig(AbilityWindowConfigSO windowConfig)
        {
            WindowConfigValue = windowConfig;
        }
    }
}
