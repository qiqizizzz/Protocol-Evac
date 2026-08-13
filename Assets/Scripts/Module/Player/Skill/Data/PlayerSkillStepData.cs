/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能段落数据，保存动画、窗口与伤害参数
 * │  类    名: PlayerSkillStepData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using System.Reflection;
using Module.Ability.Window;
using Module.Ability.Window.Hit;
using Module.Ability.Window.StepAdvance;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Module.Player.Skill.Data
{
    [Serializable]
    [DeclareFoldoutGroup("BeginAnimation", Title = "攻击阶段", Expanded = true)]
    [DeclareFoldoutGroup("RecoveryAnimation", Title = "收招阶段", Expanded = true)]
    [DeclareFoldoutGroup("StepSettings", Title = "段落设置", Expanded = true)]
    [DeclareFoldoutGroup("WindowSettings", Title = "窗口", Expanded = true)]
    public sealed class PlayerSkillStepData
    {
        [Group("BeginAnimation")]
        [LabelText("动画片段")]
        [Tooltip("技能段落的攻击阶段动画片段")]
        [FormerlySerializedAs("AnimationClipValue")]
        [SerializeField] private AnimationClip BeginAnimationClipValue;

        [Group("BeginAnimation")]
        [LabelText("持续时间")]
        [Tooltip("技能段落的攻击阶段持续时间")]
        [FormerlySerializedAs("DurationValue")]
        [SerializeField] private float BeginDurationValue = 0.5f;

        [Group("BeginAnimation")]
        [LabelText("使用 Root Motion")]
        [Tooltip("攻击阶段是否使用动画根运动位移")]
        [FormerlySerializedAs("UseRootMotionValue")]
        [SerializeField] private bool BeginUseRootMotionValue;

        [Group("BeginAnimation")]
        [LabelText("允许提前结束")]
        [Tooltip("收到移动或冲刺取消意图时，攻击阶段是否允许提前结束")]
        [SerializeField] private bool BeginCanEndEarlyValue = true;

        [Group("RecoveryAnimation")]
        [LabelText("动画片段")]
        [Tooltip("技能段落的收招阶段动画片段")]
        [SerializeField] private AnimationClip RecoveryAnimationClipValue;

        [Group("RecoveryAnimation")]
        [LabelText("持续时间")]
        [Tooltip("技能段落的收招阶段持续时间")]
        [SerializeField] private float RecoveryDurationValue = 0.5f;

        [Group("RecoveryAnimation")]
        [LabelText("使用 Root Motion")]
        [Tooltip("收招阶段是否使用动画根运动位移")]
        [SerializeField] private bool RecoveryUseRootMotionValue;

        [Group("RecoveryAnimation")]
        [LabelText("允许提前结束")]
        [Tooltip("收到移动或冲刺取消意图时，收招阶段是否允许提前结束")]
        [SerializeField] private bool RecoveryCanEndEarlyValue = true;

        [Group("StepSettings")]
        [LabelText("显示武器")]
        [Tooltip("播放该技能段落的攻击与收招动画时是否显示武器")]
        [SerializeField] private bool ShowWeaponValue;

        [Group("WindowSettings")]
        [Button("打开技能编辑器")]
        // 打开 Ability Composer 编辑窗口
        private void OpenSkillEditor()
        {
            AbilityComposerOpenRequest.SetAnimationClip(BeginAnimationClipValue);
#if UNITY_EDITOR
            Type editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            MethodInfo executeMenuItemMethod = editorApplicationType?.GetMethod("ExecuteMenuItem", BindingFlags.Public | BindingFlags.Static);
            executeMenuItemMethod?.Invoke(null, new object[] { "工具/Ability/Ability Composer" });
#endif
        }

        [Group("WindowSettings")]
        [LabelText("启用连招窗口")]
        [Tooltip("是否启用下一段推进窗口")]
        [SerializeField] private bool UseStepAdvanceWindowValue;

        [Group("WindowSettings")]
        [ShowIf(nameof(UseStepAdvanceWindowValue))]
        [LabelText("连招窗口配置")]
        [Tooltip("当前技能段对应的阶段推进窗口轨道")]
        [SerializeField] private AbilityStepAdvanceWindowTrackSO StepAdvanceWindowTrackValue;

        [Group("WindowSettings")]
        [LabelText("启用命中窗口")]
        [Tooltip("是否启用命中窗口")]
        [SerializeField] private bool UseHitWindowValue;

        [Group("WindowSettings")]
        [ShowIf(nameof(UseHitWindowValue))]
        [LabelText("命中窗口配置")]
        [Tooltip("当前攻击阶段对应的命中窗口轨道")]
        [SerializeField] private AbilityHitWindowTrackSO BeginHitWindowTrackValue;

        public AnimationClip BeginAnimationClip => BeginAnimationClipValue;

        public AbilityHitWindowTrackSO BeginHitWindowTrack => BeginHitWindowTrackValue;

        public AbilityStepAdvanceWindowTrackSO StepAdvanceWindowTrack => StepAdvanceWindowTrackValue;

        public float BeginDuration => BeginDurationValue;

        public bool BeginUseRootMotion => BeginUseRootMotionValue;

        public bool BeginCanEndEarly => BeginCanEndEarlyValue;

        public AnimationClip RecoveryAnimationClip => RecoveryAnimationClipValue;

        public float RecoveryDuration => RecoveryDurationValue;

        public bool RecoveryUseRootMotion => RecoveryUseRootMotionValue;

        public bool RecoveryCanEndEarly => RecoveryCanEndEarlyValue;

        public float TotalDuration => BeginDurationValue + RecoveryDurationValue;

        public bool ShowWeapon => ShowWeaponValue;

        public bool UseStepAdvanceWindow => UseStepAdvanceWindowValue;

        public bool UseHitWindow => UseHitWindowValue;

        // 从攻击与收招动画片段同步两个阶段的持续时间
        public bool SyncDurationsFromClips()
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
