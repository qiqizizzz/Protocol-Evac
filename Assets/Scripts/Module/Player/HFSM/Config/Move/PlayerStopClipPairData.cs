/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家急停动画对数据，保存左右脚动作与各自结束进度
 * │  类    名: PlayerStopClipPairData.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Move
{
    [Serializable]
    public sealed class PlayerStopClipPairData
    {
        [LabelText("左脚动作")]
        [Tooltip("左脚落地时使用的急停动画")]
        [SerializeField] private AnimationClip LeftClipValue;

        [LabelText("左脚持续时间")]
        [Tooltip("左脚急停动画持续时间，点击同步按钮可按动画片段长度更新")]
        [SerializeField, Min(0f)] private float LeftDurationValue;

        [LabelText("左脚播放结束进度")]
        [Tooltip("左脚动作播放到该归一化进度后退出急停，1 表示完整播放")]
        [SerializeField, Range(0f, 1f)] private float LeftEndNormalizedTimeValue = 1f;

        [LabelText("右脚动作")]
        [Tooltip("右脚落地时使用的急停动画")]
        [SerializeField] private AnimationClip RightClipValue;

        [LabelText("右脚持续时间")]
        [Tooltip("右脚急停动画持续时间，点击同步按钮可按动画片段长度更新")]
        [SerializeField, Min(0f)] private float RightDurationValue;

        [LabelText("右脚播放结束进度")]
        [Tooltip("右脚动作播放到该归一化进度后退出急停，1 表示完整播放")]
        [SerializeField, Range(0f, 1f)] private float RightEndNormalizedTimeValue = 1f;

        // 获取指定落脚对应的急停动画
        public AnimationClip GetClip(bool useLeftFoot)
        {
            return useLeftFoot ? LeftClipValue : RightClipValue;
        }

        // 获取指定落脚对应的急停播放时长
        public float GetDuration(bool useLeftFoot)
        {
            AnimationClip clip = GetClip(useLeftFoot);
            float endNormalizedTime = useLeftFoot ? LeftEndNormalizedTimeValue : RightEndNormalizedTimeValue;
            float configuredDuration = useLeftFoot ? LeftDurationValue : RightDurationValue;
            float clipLength = clip == null ? 0f : clip.length;
            float duration = configuredDuration > 0f ? configuredDuration : clipLength;
            if (clipLength > 0f)
                duration = Mathf.Min(duration, clipLength);

            return duration * endNormalizedTime;
        }

        // 同步急停动画时长显示并校验左右动作配置
        public bool SyncDurationsFromClips()
        {
            bool hasSynced = false;
            if (LeftClipValue != null && !Mathf.Approximately(LeftDurationValue, LeftClipValue.length))
            {
                LeftDurationValue = LeftClipValue.length;
                hasSynced = true;
            }

            if (RightClipValue != null && !Mathf.Approximately(RightDurationValue, RightClipValue.length))
            {
                RightDurationValue = RightClipValue.length;
                hasSynced = true;
            }

            return hasSynced;
        }
    }
}
