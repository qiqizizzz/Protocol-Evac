/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 通用能力配置，保存状态动画段落数据
 * │  类    名: AbilityConfigSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using TriInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Module.Ability.Data
{
    public abstract class AbilityConfigSO : ScriptableObject
    {
        [LabelText("状态动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("能力包含的状态动画段落列表")]
        [FormerlySerializedAs("StateClipValues")]
        [SerializeField] private AbilityStepData[] StepValues;

        public IReadOnlyList<AbilityStepData> Steps => StepValues;
        public int StepCount => StepValues?.Length ?? 0;

        private bool HasNoSteps => StepCount == 0;

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

        // 同步全部状态动画段落的动画持续时间
        [InfoBox("未配置状态动画段落，无法同步动画时长", TriMessageType.Info, visibleIf: nameof(HasNoSteps))]
        [DisableIf(nameof(HasNoSteps))]
        [Button("同步全部动画时长")]
        public bool SyncAllStepDurations()
        {
            if (StepValues == null || StepValues.Length == 0)
                return false;

            bool hasSynced = false;
            for (int i = 0; i < StepValues.Length; i++)
            {
                AbilityStepData stepData = StepValues[i];
                if (stepData == null)
                    continue;

                hasSynced |= stepData.SyncDurationsFromClips();
            }

            return hasSynced;
        }
    }
}

