/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 动画时长配置基类，统一提供时长同步按钮与执行流程
 * │  类    名: AnimationDurationConfigSOBase.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace Module.Ability.Data.Animation
{
    public abstract class AnimationDurationConfigSOBase : ScriptableObject
    {
        // 同步配置内所有动画数据项的时长
        [PropertySpace(SpaceBefore = 10f)]
        [Button(ButtonSizes.Small, "同步全部动画时长")]
        public bool SyncAllAnimationDurations()
        {
            bool hasSynced = false;
            foreach (IAnimationDurationSyncable animationDurationItem in GetAnimationDurationItems())
            {
                if (animationDurationItem == null)
                    continue;

                hasSynced |= animationDurationItem.SyncAnimationDurations();
            }

            return hasSynced;
        }

        // 返回当前配置内所有可同步时长的动画数据项
        protected abstract IEnumerable<IAnimationDurationSyncable> GetAnimationDurationItems();
    }
}
