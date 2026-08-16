/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 通用能力配置，保存状态动画段落数据
 * │  类    名: AbilityConfigSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data.Animation;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Module.Ability.Data
{
    public abstract class AbilityConfigSO : AnimationDurationConfigSOBase
    {
        [LabelText("状态动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("能力包含的状态动画段落列表")]
        [FormerlySerializedAs("StateClipValues")]
        [SerializeField] private AbilityStepData[] StepValues;

        public IReadOnlyList<AbilityStepData> Steps => StepValues;
        public int StepCount => StepValues?.Length ?? 0;

        // 获取指定索引的状态动画段落
        public AbilityStepData GetStep(int index)
        {
            if (StepValues == null || index < 0 || index >= StepValues.Length)
                return null;

            return StepValues[index];
        }

        // 获取指定索引的状态动画段落持续时间
        public float GetStepDuration(int index)
        {
            AbilityStepData stepData = GetStep(index);
            return stepData.TotalDuration;
        }

        // 返回能力配置内所有可同步时长的动画段落
        protected override IEnumerable<IAnimationDurationSyncable> GetAnimationDurationItems()
        {
            if (StepValues == null)
                yield break;

            for (int i = 0; i < StepValues.Length; i++)
                yield return StepValues[i];
        }
    }
}
