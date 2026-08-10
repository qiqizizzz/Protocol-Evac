/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability Composer 工作上下文数据，保存预览资源选择
 * │  类    名: AbilityComposerData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Tools.Editor.AbilityComposer
{
    [Serializable]
    public sealed class AbilityComposerData
    {
        [SerializeField] private GameObject PreviewPrefab;
        [SerializeField] private AnimationClip AnimationClip;

        public GameObject PreviewSource => PreviewPrefab;
        public AnimationClip SelectedAnimationClip => AnimationClip;

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
    }
}
