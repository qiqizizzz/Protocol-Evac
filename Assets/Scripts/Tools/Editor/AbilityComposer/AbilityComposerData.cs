/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 工作上下文数据，保存预览资源选择
 * │  类    名: AbilityComposerData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using Module.Player.Window;
using UnityEngine;

namespace Tools.Editor.AbilityComposer
{
    [Serializable]
    public sealed class AbilityComposerData
    {
        [SerializeField] private GameObject PreviewPrefab;
        [SerializeField] private AnimationClip AnimationClip;
        [SerializeField] private AbilityWindowTrackSO WindowTrack;

        public GameObject PreviewSource => PreviewPrefab;
        public AnimationClip SelectedAnimationClip => AnimationClip;
        public AbilityWindowTrackSO SelectedWindowTrack => WindowTrack;

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

        // 更新当前编辑的窗口轨道资产
        public void SetWindowTrack(AbilityWindowTrackSO windowTrack)
        {
            WindowTrack = windowTrack;
        }

    }
}
