/*
 * ┌──────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 动画窗口主体配置，聚合动画与三类窗口轨道
 * │  类    名: AbilityWindowConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────────┘
 */

using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.MovementLock;
using Module.Ability.Data.Window.StepAdvance;
using TriInspector;
using UnityEngine;

namespace Module.Ability.Data.Window
{
    [CreateAssetMenu(fileName = "AbilityWindowConfig", menuName = "配置/Ability/窗口/窗口主体配置")]
    [DeclareFoldoutGroup("Animation", Title = "动画", Expanded = true)]
    [DeclareFoldoutGroup("WindowSettings", Title = "窗口", Expanded = true)]
    public sealed class AbilityWindowConfigSO : ScriptableObject
    {
        [Group("Animation")]
        [LabelText("动画片段")]
        [Tooltip("当前主体配置唯一绑定的动画片段")]
        [SerializeField] private AnimationClip AnimationClipValue;

        [Group("WindowSettings")]
        [LabelText("启用命中窗口")]
        [Tooltip("是否启用当前动画的命中窗口")]
        [SerializeField] private bool UseHitWindowValue;

        [Group("WindowSettings")]
        [ShowIf(nameof(UseHitWindowValue))]
        [LabelText("命中窗口数据")]
        [Tooltip("当前动画的命中窗口轨道数据")]
        [SerializeField] private AbilityHitWindowTrackData HitWindowTrackValue = new AbilityHitWindowTrackData();

        [Group("WindowSettings")]
        [LabelText("启用阶段推进窗口")]
        [Tooltip("是否启用当前动画的阶段推进窗口")]
        [SerializeField] private bool UseStepAdvanceWindowValue;

        [Group("WindowSettings")]
        [ShowIf(nameof(UseStepAdvanceWindowValue))]
        [LabelText("阶段推进窗口数据")]
        [Tooltip("当前动画的阶段推进窗口轨道数据")]
        [SerializeField] private AbilityStepAdvanceWindowTrackData StepAdvanceWindowTrackValue = new AbilityStepAdvanceWindowTrackData();

        [Group("WindowSettings")]
        [LabelText("启用移动锁定窗口")]
        [Tooltip("是否启用当前动画的移动锁定窗口")]
        [SerializeField] private bool UseMovementLockWindowValue;

        [Group("WindowSettings")]
        [ShowIf(nameof(UseMovementLockWindowValue))]
        [LabelText("移动锁定窗口数据")]
        [Tooltip("当前动画的移动锁定窗口轨道数据")]
        [SerializeField] private AbilityMovementLockWindowTrackData MovementLockWindowTrackValue = new AbilityMovementLockWindowTrackData();

        public AnimationClip AnimationClip => AnimationClipValue;
        public bool UseHitWindow => UseHitWindowValue;
        public AbilityHitWindowTrackData HitWindowTrack => HitWindowTrackValue;
        public bool UseStepAdvanceWindow => UseStepAdvanceWindowValue;
        public AbilityStepAdvanceWindowTrackData StepAdvanceWindowTrack => StepAdvanceWindowTrackValue;
        public bool UseMovementLockWindow => UseMovementLockWindowValue;
        public AbilityMovementLockWindowTrackData MovementLockWindowTrack => MovementLockWindowTrackValue;

        // 更新主体配置唯一绑定的动画片段
        public void SetAnimationClip(AnimationClip animationClip)
        {
            AnimationClipValue = animationClip;
        }

        // 更新命中窗口启用状态与轨道数据
        public void SetHitWindow(bool isEnabled, AbilityHitWindowTrackData windowTrack)
        {
            UseHitWindowValue = isEnabled;
            HitWindowTrackValue = windowTrack;
        }

        // 更新阶段推进窗口启用状态与轨道数据
        public void SetStepAdvanceWindow(bool isEnabled, AbilityStepAdvanceWindowTrackData windowTrack)
        {
            UseStepAdvanceWindowValue = isEnabled;
            StepAdvanceWindowTrackValue = windowTrack;
        }

        // 更新移动锁定窗口启用状态与轨道数据
        public void SetMovementLockWindow(bool isEnabled, AbilityMovementLockWindowTrackData windowTrack)
        {
            UseMovementLockWindowValue = isEnabled;
            MovementLockWindowTrackValue = windowTrack;
        }

        // 根据移动锁定窗口末端计算当前动画的控制时长
        public float ResolveMovementLockDuration(float animationDuration)
        {
            if (!UseMovementLockWindowValue || MovementLockWindowTrackValue == null
                || MovementLockWindowTrackValue.WindowCount == 0)
                return animationDuration;

            return animationDuration * MovementLockWindowTrackValue.GetLatestEndNormalizedTime();
        }
    }
}
