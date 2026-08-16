/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 工作上下文数据，保存预览资源选择
 * │  类    名: AbilityComposerData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System;
using Module.Ability.Data.Window.Hit;
using Module.Ability.Data.Window.MovementLock;
using Module.Ability.Data.Window.StepAdvance;
using UnityEngine;

namespace Tools.AbilityComposer.Editor
{
    [Serializable]
    public sealed class AbilityComposerData
    {
        [SerializeField] private GameObject PreviewPrefab;
        [SerializeField] private AnimationClip AnimationClip;
        [SerializeField] private bool ShowGlobalAnimations;
        [SerializeField] private AbilityHitWindowTrackSO HitWindowTrack;
        [SerializeField] private AbilityStepAdvanceWindowTrackSO StepAdvanceWindowTrack;
        [SerializeField] private AbilityMovementLockWindowTrackSO MovementLockWindowTrack;

        public GameObject PreviewSource => PreviewPrefab;
        public AnimationClip SelectedAnimationClip => AnimationClip;
        public bool IsShowingGlobalAnimations => ShowGlobalAnimations;
        public AbilityHitWindowTrackSO SelectedHitWindowTrack => HitWindowTrack;
        public AbilityStepAdvanceWindowTrackSO SelectedStepAdvanceWindowTrack => StepAdvanceWindowTrack;
        public AbilityMovementLockWindowTrackSO SelectedMovementLockWindowTrack => MovementLockWindowTrack;

        // 更新当前选择的预览 Prefab
        public void SetPreviewSource(GameObject previewPrefab)
        {
            PreviewPrefab = previewPrefab;
        }

        // 更新当前选择的 Animation Clip
        public void SetAnimationClip(AnimationClip animationClip)
        {
            AnimationClip = animationClip;
        }

        // 更新动画选择器是否显示全局动画
        public void SetShowGlobalAnimations(bool showGlobalAnimations)
        {
            ShowGlobalAnimations = showGlobalAnimations;
        }

        // 更新当前编辑的窗口轨道资产
        public void SetWindowTracks(AbilityHitWindowTrackSO hitWindowTrack,
            AbilityStepAdvanceWindowTrackSO stepAdvanceWindowTrack,
            AbilityMovementLockWindowTrackSO movementLockWindowTrack)
        {
            HitWindowTrack = hitWindowTrack;
            StepAdvanceWindowTrack = stepAdvanceWindowTrack;
            MovementLockWindowTrack = movementLockWindowTrack;
        }

    }
}
