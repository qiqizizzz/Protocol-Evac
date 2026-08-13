/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口数据基类，保存窗口通用时间范围与稳定标识 │
 * │  类    名: AbilityWindowDataBase.cs                         │
 * │  创    建: By qiqizizzz                                    │
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Module.Ability.Window
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
            IdValue = Guid.NewGuid().ToString("N");
            SetTimeRange(startNormalizedTime, endNormalizedTime);
        }

        protected AbilityWindowDataBase(string id, float startNormalizedTime, float endNormalizedTime)
        {
            IdValue = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N") : id;
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
    }
}
