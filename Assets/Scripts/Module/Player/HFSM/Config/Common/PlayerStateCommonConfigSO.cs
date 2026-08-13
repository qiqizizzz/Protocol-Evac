/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家状态通用配置基类，保存动画段落数据
 * │  类    名: PlayerStateCommonConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Common
{
    public abstract class PlayerStateCommonConfigSO : ScriptableObject
    {
        [LabelText("状态动画段落")]
        [ListDrawerSettings(Draggable = true, ShowElementLabels = true)]
        [Tooltip("状态动画段落列表")]
        [SerializeField] private PlayerStateClipData[] StateClipValues;

        public IReadOnlyList<PlayerStateClipData> StateClips => StateClipValues;

        public int StateClipCount => StateClipValues?.Length ?? 0;

        private bool HasNoStateClips => StateClipCount == 0;

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

        // 同步全部动画段落的持续时间
        [InfoBox("未配置状态动画段落，无法同步动画时长", TriMessageType.Info, visibleIf: nameof(HasNoStateClips))]
        [DisableIf(nameof(HasNoStateClips))]
        [Button("同步全部动画时长")]
        public bool SyncAllClipDurations()
        {
            if (StateClipValues == null || StateClipValues.Length == 0)
                return SyncAdditionalClipDurations();

            bool hasSynced = false;
            for (int i = 0; i < StateClipValues.Length; i++)
            {
                PlayerStateClipData clipData = StateClipValues[i];
                if (clipData == null)
                    continue;

                hasSynced |= clipData.SyncDurationFromClip();
            }

            return hasSynced | SyncAdditionalClipDurations();
        }

        // 同步派生配置中额外维护的动画时长
        protected virtual bool SyncAdditionalClipDurations()
        {
            return false;
        }
    }
}
