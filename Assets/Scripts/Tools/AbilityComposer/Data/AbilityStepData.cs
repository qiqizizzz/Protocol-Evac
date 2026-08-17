/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 通用能力段落数据，保存动画、窗口与表现参数
 * │  类    名: AbilityStepData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

using System;
using System.Reflection;
using Module.Ability.Data.Animation;
using Module.Ability.Data.Window;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.StepAdvance;
using Module.Ability.Data.Window.Vfx;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Module.Ability.Data
{
    [Serializable]
    [DeclareFoldoutGroup("BeginAnimation", Title = "攻击阶段", Expanded = true)]
    [DeclareFoldoutGroup("RecoveryAnimation", Title = "收招阶段", Expanded = true)]
    [DeclareFoldoutGroup("StepSettings", Title = "段落设置", Expanded = true)]
    [DeclareFoldoutGroup("WindowSettings", Title = "窗口", Expanded = true)]
    public sealed class AbilityStepData : IAnimationDurationSyncable
    {
        [Group("BeginAnimation")]
        [LabelText("动画片段")]
        [Tooltip("状态动画段落的攻击阶段动画片段")]
        [FormerlySerializedAs("AnimationClipValue")]
        [FormerlySerializedAs("StateAnimationClipValue")]
        [SerializeField] private AnimationClip BeginAnimationClipValue;

        [Group("BeginAnimation")]
        [LabelText("持续时间")]
        [Tooltip("状态动画段落的攻击阶段持续时间")]
        [FormerlySerializedAs("DurationValue")]
        [FormerlySerializedAs("StateDurationValue")]
        [SerializeField] private float BeginDurationValue = 0.5f;

        [Group("BeginAnimation")]
        [LabelText("使用 Root Motion")]
        [Tooltip("攻击阶段是否使用动画根运动位移")]
        [FormerlySerializedAs("UseRootMotionValue")]
        [FormerlySerializedAs("StateUseRootMotionValue")]
        [SerializeField] private bool BeginUseRootMotionValue;

        [Group("BeginAnimation")]
        [LabelText("允许提前结束")]
        [Tooltip("外部请求取消能力时，攻击阶段是否允许提前结束")]
        [SerializeField] private bool BeginCanEndEarlyValue = true;

        [Group("RecoveryAnimation")]
        [LabelText("动画片段")]
        [Tooltip("状态动画段落的收招阶段动画片段")]
        [SerializeField] private AnimationClip RecoveryAnimationClipValue;

        [Group("RecoveryAnimation")]
        [LabelText("持续时间")]
        [Tooltip("状态动画段落的收招阶段持续时间")]
        [SerializeField] private float RecoveryDurationValue = 0.5f;

        [Group("RecoveryAnimation")]
        [LabelText("使用 Root Motion")]
        [Tooltip("收招阶段是否使用动画根运动位移")]
        [SerializeField] private bool RecoveryUseRootMotionValue;

        [Group("RecoveryAnimation")]
        [LabelText("允许提前结束")]
        [Tooltip("外部请求取消能力时，收招阶段是否允许提前结束")]
        [SerializeField] private bool RecoveryCanEndEarlyValue = true;

        [Group("StepSettings")]
        [LabelText("显示武器")]
        [Tooltip("播放该状态动画段落时是否显示武器")]
        [SerializeField] private bool ShowWeaponValue;

        [Group("WindowSettings")]
        [LabelText("窗口主体配置")]
        [Tooltip("当前攻击阶段动画对应的窗口主体配置")]
        [SerializeField] private AbilityWindowConfigSO WindowConfigValue;

        [Group("WindowSettings")]
        [Button("打开动画编辑器")]
        // 使用攻击阶段动画片段打开 Ability Composer
        private void OpenAnimationEditor()
        {
            if (WindowConfigValue != null)
                AbilityComposerOpenRequest.SetWindowConfig(WindowConfigValue);
            else
                AbilityComposerOpenRequest.SetAnimationClip(BeginAnimationClipValue);
#if UNITY_EDITOR
            Type editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            MethodInfo executeMenuItemMethod = editorApplicationType?.GetMethod("ExecuteMenuItem", BindingFlags.Public | BindingFlags.Static);
            executeMenuItemMethod?.Invoke(null, new object[] { "工具/Ability/Ability Composer" });
#endif
        }

        public AnimationClip BeginAnimationClip => BeginAnimationClipValue;
        public float BeginDuration => BeginDurationValue;
        public bool BeginUseRootMotion => BeginUseRootMotionValue;
        public bool BeginCanEndEarly => BeginCanEndEarlyValue;
        public AnimationClip RecoveryAnimationClip => RecoveryAnimationClipValue;
        public float RecoveryDuration => RecoveryDurationValue;
        public bool RecoveryUseRootMotion => RecoveryUseRootMotionValue;
        public bool RecoveryCanEndEarly => RecoveryCanEndEarlyValue;
        public float TotalDuration => BeginDurationValue + RecoveryDurationValue;
        public bool ShowWeapon => ShowWeaponValue;
        public AbilityWindowConfigSO WindowConfig => WindowConfigValue;
        public bool UseStepAdvanceWindow => WindowConfigValue != null && WindowConfigValue.UseStepAdvanceWindow;
        public AbilityStepAdvanceWindowTrackData StepAdvanceWindowTrack => WindowConfigValue == null
            ? null
            : WindowConfigValue.StepAdvanceWindowTrack;
        public bool UseHitWindow => WindowConfigValue != null && WindowConfigValue.UseHitWindow;
        public AbilityHitWindowTrackData BeginHitWindowTrack => WindowConfigValue == null
            ? null
            : WindowConfigValue.HitWindowTrack;
        public bool UseVfxWindow => WindowConfigValue != null && WindowConfigValue.UseVfxWindow;
        public AbilityVfxWindowTrackData VfxWindowTrack => WindowConfigValue == null
            ? null
            : WindowConfigValue.VfxWindowTrack;

        // 更新当前攻击阶段绑定的窗口主体配置
        public void SetWindowConfig(AbilityWindowConfigSO windowConfig)
        {
            WindowConfigValue = windowConfig;
        }

        // 从攻击与收招动画片段同步两个阶段的持续时间
        public bool SyncAnimationDurations()
        {
            bool hasSynced = false;
            if (BeginAnimationClipValue != null)
            {
                BeginDurationValue = BeginAnimationClipValue.length;
                hasSynced = true;
            }

            if (RecoveryAnimationClipValue != null)
            {
                RecoveryDurationValue = RecoveryAnimationClipValue.length;
                hasSynced = true;
            }

            return hasSynced;
        }
    }
}
