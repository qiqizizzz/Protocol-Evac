/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态动画段落数据，保存动画片段、持续时间与武器表现
 * │  类    名: PlayerStateClipData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using System.Reflection;
using Module.Ability.Data;
using Module.Ability.Data.Animation;
using Module.Ability.Data.Window.StepAdvance;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Common
{
    [Serializable]
    [DeclareFoldoutGroup("Animation", Title = "动画与段落", Expanded = true)]
    [DeclareFoldoutGroup("WindowSettings", Title = "窗口", Expanded = true)]
    public sealed class PlayerStateClipData : IAnimationDurationSyncable
    {
        [Group("Animation")]
        [LabelText("动画片段")]
        [Tooltip("状态动画片段")]
        [SerializeField] private AnimationClip StateClipValue;

        [Group("Animation")]
        [LabelText("持续时间")]
        [Tooltip("状态持续时间")]
        [SerializeField, Min(0f)] private float StateDurationValue = 0.5f;

        [Group("Animation")]
        [LabelText("显示武器")]
        [Tooltip("播放该动画段落时是否显示武器")]
        [SerializeField] private bool ShowWeaponValue;

        [Group("WindowSettings")]
        [Button("打开技能编辑器")]
        // 打开 Ability Composer 编辑窗口
        private void OpenSkillEditor()
        {
            AbilityComposerOpenRequest.SetAnimationClip(StateClipValue);
#if UNITY_EDITOR
            Type editorApplicationType = Type.GetType("UnityEditor.EditorApplication, UnityEditor");
            MethodInfo executeMenuItemMethod = editorApplicationType?.GetMethod("ExecuteMenuItem", BindingFlags.Public | BindingFlags.Static);
            executeMenuItemMethod?.Invoke(null, new object[] { "工具/Ability/Ability Composer" });
#endif
        }

        [Group("WindowSettings")]
        [LabelText("启用连段窗口")]
        [Tooltip("是否启用连段窗口")]
        [SerializeField] private bool UseComboWindowValue;

        [Group("WindowSettings")]
        [ShowIf(nameof(UseComboWindowValue))]
        [LabelText("连招窗口配置")]
        [Tooltip("当前动画段对应的阶段推进窗口轨道")]
        [SerializeField] private AbilityStepAdvanceWindowTrackSO ComboWindowTrackValue;

        public AnimationClip StateClip => StateClipValue;

        public float StateDuration => StateDurationValue;

        public bool ShowWeapon => ShowWeaponValue;

        public bool UseComboWindow => UseComboWindowValue;

        public AbilityStepAdvanceWindowTrackSO ComboWindowTrack => ComboWindowTrackValue;

        // 从动画片段同步状态持续时间
        public bool SyncAnimationDurations()
        {
            if (StateClipValue == null)
                return false;

            StateDurationValue = StateClipValue.length;
            return true;
        }

    }
}
