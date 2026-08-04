/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家技能通用配置，保存技能段落数据
 * │  类    名: PlayerSkillConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Player.Skill.Data
{
    [CreateAssetMenu(fileName = "PlayerSkillConfig", menuName = "配置/玩家/技能/玩家技能配置")]
    public class PlayerSkillConfigSO : ScriptableObject
    {
        [Header("技能段落")]
        [Tooltip("技能段落列表")]
        [SerializeField] private PlayerSkillStepData[] StepValues;

        public IReadOnlyList<PlayerSkillStepData> Steps => StepValues;

        public int StepCount => StepValues?.Length ?? 0;

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
            return stepData.Duration;
        }

        // 同步全部技能段落的动画持续时间
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

                hasSynced |= stepData.SyncDurationFromClip();
            }

            return hasSynced;
        }
    }
}

