/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态通用配置基类，保存动画段落数据
 * │  类    名: PlayerStateCommonConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Module.Player.HFSM.Config.Common
{
    public abstract class PlayerStateCommonConfigSO : ScriptableObject
    {
        [Header("状态动画段落")]
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
        public float GetStateDuration(int index, float defaultDuration = 0f)
        {
            PlayerStateClipData clipData = GetStateClip(index);
            return clipData != null ? clipData.StateDuration : defaultDuration;
        }

        // 同步全部动画段落的持续时间
        public bool SyncAllClipDurations()
        {
            if (StateClipValues == null || StateClipValues.Length == 0)
                return false;

            bool hasSynced = false;
            for (int i = 0; i < StateClipValues.Length; i++)
            {
                PlayerStateClipData clipData = StateClipValues[i];
                if (clipData == null)
                    continue;

                hasSynced |= clipData.SyncDurationFromClip();
            }

            return hasSynced;
        }
    }
}
