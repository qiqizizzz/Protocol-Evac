/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人伤害运行时上下文，保存生命值与受击持续状态
 * │  类    名: EnemyDamageContext.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Context.Runtime
{
    public sealed class EnemyDamageContext
    {
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsHurt { get; private set; }
        public float HurtRemainingTime { get; private set; }

        // 创建敌人伤害运行时数据
        public EnemyDamageContext()
        {
            Reset();
        }

        // 初始化敌人生命值
        public void InitHealth(float maxHealth)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
            IsHurt = false;
            HurtRemainingTime = 0f;
        }

        // 扣除生命并写入受击持续时间
        public void ApplyDamage(float damage, float hurtDuration)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            IsDead = CurrentHealth <= 0f;
            IsHurt = !IsDead;
            HurtRemainingTime = IsHurt ? Mathf.Max(0f, hurtDuration) : 0f;
        }

        // 进入不扣血的起身硬直阶段
        public void BeginHurt(float hurtDuration)
        {
            IsHurt = true;
            HurtRemainingTime = Mathf.Max(0f, hurtDuration);
        }

        // 推进受击持续时间并返回是否刚结束
        public bool TickHurt(float deltaTime)
        {
            if (!IsHurt)
                return false;

            HurtRemainingTime = Mathf.Max(0f, HurtRemainingTime - deltaTime);
            if (HurtRemainingTime > 0f)
                return false;

            IsHurt = false;
            return true;
        }

        // 重置敌人伤害运行时数据
        public void Reset()
        {
            CurrentHealth = 0f;
            IsDead = false;
            IsHurt = false;
            HurtRemainingTime = 0f;
        }
    }
}
