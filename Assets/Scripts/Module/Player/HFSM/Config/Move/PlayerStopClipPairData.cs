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

        [LabelText("左脚播放结束进度")]
        [Tooltip("左脚动作播放到该归一化进度后退出急停，1 表示完整播放")]
        [SerializeField, Range(0f, 1f)] private float LeftEndNormalizedTimeValue = 1f;

        [LabelText("右脚动作")]
        [Tooltip("右脚落地时使用的急停动画")]
        [SerializeField] private AnimationClip RightClipValue;

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
            return clip.length * endNormalizedTime;
        }
    }
}
