/*
 * ┌──────────────────────────────────┐
 * │  描    述: 用于验证伤害流程的测试受击对象
 * │  类    名: CombatTargetDummy.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Combat.Damage;
using UnityEngine;
using Utils.log;

namespace Module.Combat.Testing
{
    public class CombatTargetDummy : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float MaxHealth = 100f;

        public float CurrentHealth { get; private set; }

        private void Awake()
        {
            CurrentHealth = MaxHealth;
        }

        /// <summary>
        /// 接收伤害并扣除测试目标生命值
        /// </summary>
        /// <param name="damageData">本次伤害数据</param>
        public void TakeDamage(DamageData damageData)
        {
            if (damageData.Damage <= 0f)
            {
                QLog.Error($"测试目标收到非法伤害值：{damageData.Damage}");
                return;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damageData.Damage);
            QLog.Info($"测试目标受到伤害：{damageData.Damage}，剩余生命值：{CurrentHealth}");
        }
    }
}