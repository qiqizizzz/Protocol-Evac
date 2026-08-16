/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: Ability 命中窗口数据，保存单个窗口的伤害与受击反应参数
 * │  类    名: AbilityHitWindowData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using Module.Combat.Damage;
using TriInspector;
using UnityEngine;

namespace Module.Ability.Data.Window.Hit
{
    [System.Serializable]
    [DeclareFoldoutGroup("Impact", Title = "击退与浮空", Expanded = false)]
    public sealed class AbilityHitWindowData : AbilityWindowDataBase
    {
        [SerializeField, Min(0f)] private float DamageValue;
        [SerializeField] private DamageReactionType ReactionTypeValue;

        [Group("Impact")]
        [LabelText("水平击退速度")]
        [Tooltip("命中后施加给受击方的水平速度")]
        [SerializeField, Min(0f)] private float HorizontalKnockbackSpeedValue;

        [Group("Impact")]
        [LabelText("水平击退持续时间")]
        [Tooltip("命中后保持水平击退速度的时长")]
        [SerializeField, Min(0f)] private float HorizontalKnockbackDurationValue;

        [Group("Impact")]
        [LabelText("竖直起飞初速度")]
        [Tooltip("命中后施加给受击方的竖直初速度")]
        [SerializeField, Min(0f)] private float VerticalLaunchSpeedValue;

        public float Damage => DamageValue;
        public DamageReactionType ReactionType => ReactionTypeValue;
        public float HorizontalKnockbackSpeed => HorizontalKnockbackSpeedValue;
        public float HorizontalKnockbackDuration => HorizontalKnockbackDurationValue;
        public float VerticalLaunchSpeed => VerticalLaunchSpeedValue;

        public AbilityHitWindowData()
        {
        }

        public AbilityHitWindowData(float startNormalizedTime, float endNormalizedTime, float damage)
            : base(startNormalizedTime, endNormalizedTime)
        {
            DamageValue = Mathf.Max(0f, damage);
            ReactionTypeValue = DamageReactionType.Light;
        }

        public AbilityHitWindowData(string id, float startNormalizedTime, float endNormalizedTime, float damage)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
            DamageValue = Mathf.Max(0f, damage);
            ReactionTypeValue = DamageReactionType.Light;
        }

        public AbilityHitWindowData(string id, float startNormalizedTime, float endNormalizedTime, float damage,
            DamageReactionType reactionType, float horizontalKnockbackSpeed = 0f,
            float horizontalKnockbackDuration = 0f, float verticalLaunchSpeed = 0f)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
            DamageValue = Mathf.Max(0f, damage);
            ReactionTypeValue = reactionType;
            HorizontalKnockbackSpeedValue = Mathf.Max(0f, horizontalKnockbackSpeed);
            HorizontalKnockbackDurationValue = Mathf.Max(0f, horizontalKnockbackDuration);
            VerticalLaunchSpeedValue = Mathf.Max(0f, verticalLaunchSpeed);
        }

        // 更新命中窗口的伤害参数
        public void SetDamage(float damage)
        {
            DamageValue = Mathf.Max(0f, damage);
        }

        // 更新命中窗口的通用受击反应类型
        public void SetReactionType(DamageReactionType reactionType)
        {
            ReactionTypeValue = reactionType;
        }

        // 更新命中窗口的击退与浮空参数
        public void SetImpact(float horizontalKnockbackSpeed, float horizontalKnockbackDuration,
            float verticalLaunchSpeed)
        {
            HorizontalKnockbackSpeedValue = Mathf.Max(0f, horizontalKnockbackSpeed);
            HorizontalKnockbackDurationValue = Mathf.Max(0f, horizontalKnockbackDuration);
            VerticalLaunchSpeedValue = Mathf.Max(0f, verticalLaunchSpeed);
        }
    }
}
