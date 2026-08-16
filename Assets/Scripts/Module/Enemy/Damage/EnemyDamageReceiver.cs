/*
 * ┌────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人伤害回调接收器，将通用伤害事件转交给敌人模块
 * │  类    名: EnemyDamageReceiver.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────┘
 */

using System;
using Module.Combat.Damage;
using UnityEngine;

namespace Module.Enemy.Damage
{
    public sealed class EnemyDamageReceiver : MonoBehaviour, IDamageable
    {
        public event Action<DamageData> OnDamageReceived;

        /// <summary>
        /// 接收通用 Combat 模块提交的一次伤害
        /// </summary>
        /// <param name="damageData">本次命中的伤害数据</param>
        public void TakeDamage(DamageData damageData)
        {
            OnDamageReceived?.Invoke(damageData);
        }
    }
}
