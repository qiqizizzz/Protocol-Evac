/*
 * ┌─────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口数据基类，保存窗口通用时间范围与稳定标识
 * │  类    名: AbilityWindowDataBase.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Module.Ability.Data.Window
{
    [Serializable]
    public abstract class AbilityWindowDataBase
    {
        [SerializeField] private string IdValue;
        [SerializeField, Range(0f, 1f)] private float StartNormalizedTimeValue;
        [SerializeField, Range(0f, 1f)] private float EndNormalizedTimeValue;

        public string Id => IdValue;
        public float StartNormalizedTime => StartNormalizedTimeValue;
        public float EndNormalizedTime => EndNormalizedTimeValue;

        protected AbilityWindowDataBase()
        {
        }

        protected AbilityWindowDataBase(float startNormalizedTime, float endNormalizedTime)
        {
            IdValue = string.Empty;
            SetTimeRange(startNormalizedTime, endNormalizedTime);
        }

        protected AbilityWindowDataBase(string id, float startNormalizedTime, float endNormalizedTime)
        {
            IdValue = id ?? string.Empty;
            SetTimeRange(startNormalizedTime, endNormalizedTime);
        }

        // 更新窗口的归一化时间范围
        public void SetTimeRange(float startNormalizedTime, float endNormalizedTime)
        {
            StartNormalizedTimeValue = Mathf.Clamp01(startNormalizedTime);
            EndNormalizedTimeValue = Mathf.Clamp01(endNormalizedTime);
            if (EndNormalizedTimeValue < StartNormalizedTimeValue)
                EndNormalizedTimeValue = StartNormalizedTimeValue;
        }

        // 判断指定时间是否处于窗口范围内
        public bool IsActiveAt(float normalizedTime)
        {
            return normalizedTime >= StartNormalizedTimeValue
                && normalizedTime <= EndNormalizedTimeValue;
        }

        // 判断时间推进是否跨过窗口范围
        public bool IsCrossedBy(float previousNormalizedTime, float currentNormalizedTime)
        {
            return currentNormalizedTime >= StartNormalizedTimeValue
                && previousNormalizedTime <= EndNormalizedTimeValue;
        }
    }
}
