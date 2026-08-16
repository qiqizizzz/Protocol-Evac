/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: Ability 命中窗口数据，保存单个窗口的伤害与受击反应参数
 * │  类    名: AbilityHitWindowData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using Module.Combat.Damage;
using UnityEngine;

namespace Module.Ability.Data.Window.Hit
{
    [System.Serializable]
    public sealed class AbilityHitWindowData : AbilityWindowDataBase
    {
        [SerializeField, Min(0f)] private float DamageValue;
        [SerializeField] private DamageReactionType ReactionTypeValue;

        public float Damage => DamageValue;
        public DamageReactionType ReactionType => ReactionTypeValue;

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
            DamageReactionType reactionType)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
            DamageValue = Mathf.Max(0f, damage);
            ReactionTypeValue = reactionType;
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
    }
}
