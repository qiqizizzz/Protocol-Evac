/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 动画候选解析器，收集预制体依赖的动画片段
 * │  类    名: AbilityAnimationClipResolver.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Tools.AbilityComposer.Editor.Selection
{
    public sealed class AbilityAnimationClipResolver
    {
        private readonly List<AnimationClip> m_animationClips = new List<AnimationClip>();
        private readonly HashSet<AnimationClip> m_uniqueAnimationClips = new HashSet<AnimationClip>();

        // 收集当前预制体通过 Animator 与配置资产依赖的全部动画片段
        public IReadOnlyList<AnimationClip> Resolve(GameObject previewPrefab)
        {
            m_animationClips.Clear();
            m_uniqueAnimationClips.Clear();
            if (previewPrefab == null)
                return m_animationClips;

            CollectAnimatorClips(previewPrefab);
            CollectDependencyClips(previewPrefab);
            m_animationClips.Sort(CompareAnimationClips);
            return m_animationClips;
        }

        // 收集预制体层级中 AnimatorController 挂载的动画片段
        private void CollectAnimatorClips(GameObject previewPrefab)
        {
            Animator[] animators = previewPrefab.GetComponentsInChildren<Animator>(true);
            for (int animatorIndex = 0; animatorIndex < animators.Length; animatorIndex++)
            {
                RuntimeAnimatorController animatorController = animators[animatorIndex].runtimeAnimatorController;
                if (animatorController == null)
                    continue;

                AnimationClip[] animationClips = animatorController.animationClips;
                for (int clipIndex = 0; clipIndex < animationClips.Length; clipIndex++)
                    AddAnimationClip(animationClips[clipIndex]);
            }
        }

        // 收集预制体通过组件和 ScriptableObject 间接引用的动画片段
        private void CollectDependencyClips(GameObject previewPrefab)
        {
            UnityEngine.Object[] dependencies = EditorUtility.CollectDependencies(new UnityEngine.Object[] { previewPrefab });
            for (int dependencyIndex = 0; dependencyIndex < dependencies.Length; dependencyIndex++)
            {
                if (dependencies[dependencyIndex] is AnimationClip animationClip)
                    AddAnimationClip(animationClip);
            }
        }

        // 添加尚未收录的动画片段
        private void AddAnimationClip(AnimationClip animationClip)
        {
            if (animationClip != null && m_uniqueAnimationClips.Add(animationClip))
                m_animationClips.Add(animationClip);
        }

        // 按动画名称与资源路径稳定排序
        private static int CompareAnimationClips(AnimationClip left, AnimationClip right)
        {
            int nameComparison = string.Compare(left.name, right.name, StringComparison.OrdinalIgnoreCase);
            if (nameComparison != 0)
                return nameComparison;

            return string.Compare(AssetDatabase.GetAssetPath(left), AssetDatabase.GetAssetPath(right),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
