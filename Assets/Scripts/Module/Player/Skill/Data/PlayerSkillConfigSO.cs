/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能通用配置，保存技能段落数据
 * │  类    名: PlayerSkillConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace Module.Player.Skill.Data
{
    public class PlayerSkillConfigSO : ScriptableObject
    {
        [LabelText("技能段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("技能段落列表")]
        [SerializeField] private PlayerSkillStepData[] StepValues;

        public IReadOnlyList<PlayerSkillStepData> Steps => StepValues;

        public int StepCount => StepValues?.Length ?? 0;

        private bool HasNoSteps => StepCount == 0;

        // 获取指定索引的技能段落
        public PlayerSkillStepData GetStep(int index)
        {
            if (StepValues == null || index < 0 || index >= StepValues.Length)
                return null;

            return StepValues[index];
        }

        // 获取指定索引的技能段落持续时间
        public float GetStepDuration(int index)
        {
            PlayerSkillStepData stepData = GetStep(index);
            return stepData.TotalDuration;
        }

        // 同步全部技能段落的动画持续时间
        [InfoBox("未配置技能段落，无法同步动画时长", TriMessageType.Info, visibleIf: nameof(HasNoSteps))]
        [DisableIf(nameof(HasNoSteps))]
        [Button("同步全部动画时长")]
        public bool SyncAllStepDurations()
        {
            if (StepValues == null || StepValues.Length == 0)
                return false;

            bool hasSynced = false;
            for (int i = 0; i < StepValues.Length; i++)
            {
                PlayerSkillStepData stepData = StepValues[i];
                if (stepData == null)
                    continue;

                hasSynced |= stepData.SyncDurationsFromClips();
            }

            return hasSynced;
        }
    }
}
