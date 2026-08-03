/*
 * ┌──────────────────────────────────┐
 * │  描    述: 可接收伤害对象的通用接口
 * │  类    名: IDamageable.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Combat.Damage
{
    public interface IDamageable
    {
        /// <summary>
        /// 接收一次伤害
        /// </summary>
        /// <param name="damageData">本次伤害数据</param>
        void TakeDamage(DamageData damageData);
    }
}