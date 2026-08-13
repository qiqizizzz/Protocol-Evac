/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 窗口数据，保存类型、归一化时间范围与类型参数
 * │  类    名: AbilityWindowData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using UnityEngine;

namespace Module.Player.Window
{
    [Serializable]
    public sealed class AbilityWindowData
    {
        [SerializeField] private string IdValue;
        [SerializeField] private AbilityWindowType TypeValue;
        [SerializeField, Range(0f, 1f)] private float StartNormalizedTimeValue;
        [SerializeField, Range(0f, 1f)] private float EndNormalizedTimeValue;
        [SerializeField, Min(0f)] private float DamageValue;

        public string Id => IdValue;
        public AbilityWindowType Type => TypeValue;
        public float StartNormalizedTime => StartNormalizedTimeValue;
        public float EndNormalizedTime => EndNormalizedTimeValue;
        public float Damage => DamageValue;

        // 创建一条带稳定标识的通用窗口数据
        public AbilityWindowData(AbilityWindowType type, float startNormalizedTime, float endNormalizedTime, float damage)
        {
            IdValue = Guid.NewGuid().ToString("N");
            TypeValue = type;
            StartNormalizedTimeValue = startNormalizedTime;
            EndNormalizedTimeValue = endNormalizedTime;
            DamageValue = damage;
            Normalize();
        }

        // 更新窗口全部可编辑数据
        public void SetValues(AbilityWindowType type, float startNormalizedTime, float endNormalizedTime, float damage)
        {
            TypeValue = type;
            StartNormalizedTimeValue = startNormalizedTime;
            EndNormalizedTimeValue = endNormalizedTime;
            DamageValue = damage;
            Normalize();
        }

        // 约束窗口时间范围和伤害数值
        private void Normalize()
        {
            StartNormalizedTimeValue = Mathf.Clamp01(StartNormalizedTimeValue);
            EndNormalizedTimeValue = Mathf.Clamp01(EndNormalizedTimeValue);
            if (EndNormalizedTimeValue < StartNormalizedTimeValue)
                EndNormalizedTimeValue = StartNormalizedTimeValue;

            DamageValue = Mathf.Max(0f, DamageValue);
        }
    }
}
