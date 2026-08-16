/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态通用配置基类，保存动画段落数据
 * │  类    名: PlayerStateCommonConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data.Animation;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Common
{
    public abstract class PlayerStateCommonConfigSO : AnimationDurationConfigSOBase
    {
        [LabelText("状态动画段落")]
        [ListDrawerSettings(Draggable = true, ShowElementLabels = true)]
        [Tooltip("状态动画段落列表")]
        [SerializeField] private PlayerStateClipData[] StateClipValues;

        public IReadOnlyList<PlayerStateClipData> StateClips => StateClipValues;

        public int StateClipCount => StateClipValues?.Length ?? 0;

        // 获取指定索引的状态动画段落
        public PlayerStateClipData GetStateClip(int index)
        {
            if (StateClipValues == null || index < 0 || index >= StateClipValues.Length)
                return null;

            return StateClipValues[index];
        }

        // 获取指定索引的状态持续时间
        public float GetStateDuration(int index)
        {
            PlayerStateClipData clipData = GetStateClip(index);
            return clipData.StateDuration;
        }

        // 返回玩家状态配置内所有可同步时长的动画段落
        protected override IEnumerable<IAnimationDurationSyncable> GetAnimationDurationItems()
        {
            if (StateClipValues != null)
            {
                for (int i = 0; i < StateClipValues.Length; i++)
                    yield return StateClipValues[i];
            }

            foreach (IAnimationDurationSyncable animationDurationItem in GetAdditionalAnimationDurationItems())
                yield return animationDurationItem;
        }

        // 返回派生配置中额外维护的动画数据项
        protected virtual IEnumerable<IAnimationDurationSyncable> GetAdditionalAnimationDurationItems()
        {
            yield break;
        }
    }
}
