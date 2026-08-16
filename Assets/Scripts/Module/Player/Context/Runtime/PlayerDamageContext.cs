/*
 * ┌────────────────────────────────────────────────────────────┐
 * │  描    述: 玩家伤害运行时上下文，保存生命值与待消费的受击事实
 * │  类    名: PlayerDamageContext.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────┘
 */

using Module.Combat.Damage;
using Module.Player.HFSM.Animation.Type;
using UnityEngine;

namespace Module.Player.Context.Runtime
{
    public sealed class PlayerDamageContext : IPlayerRuntimeContext
    {
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        public bool HasPendingHurt { get; private set; }
        public bool IsHurtCancellationEnabled { get; private set; }
        public bool IsHurtMoveCancellationRequested { get; private set; }
        public DamageReactionType PendingReactionType { get; private set; }
        public Vector3 PendingHitDirection { get; private set; }
        public PlayerHurtAnimationId HurtAnimationId { get; private set; }

        // 创建玩家伤害运行时上下文
        public PlayerDamageContext()
        {
            Reset();
        }

        // 用配置的最大生命值初始化玩家生命
        public void InitHealth(float maxHealth)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
            HasPendingHurt = false;
            IsHurtCancellationEnabled = false;
            IsHurtMoveCancellationRequested = false;
            PendingReactionType = DamageReactionType.Light;
            PendingHitDirection = Vector3.zero;
            HurtAnimationId = PlayerHurtAnimationId.None;
        }

        // 应用一次有效伤害并记录待消费的受击反应
        public void ApplyDamage(float damage, DamageReactionType reactionType, Vector3 hitDirection)
        {
            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
            IsDead = CurrentHealth <= 0f;
            if (IsDead)
            {
                HasPendingHurt = false;
                return;
            }

            HasPendingHurt = true;
            PendingReactionType = reactionType;
            PendingHitDirection = hitDirection;
        }

        // 消费本次待处理的受击请求
        public void ConsumePendingHurt()
        {
            HasPendingHurt = false;
        }

        // 设置受击动画是否已开放输入取消
        public void SetHurtCancellationEnabled(bool isEnabled)
        {
            IsHurtCancellationEnabled = isEnabled;
            if (!isEnabled)
                IsHurtMoveCancellationRequested = false;
        }

        // 记录锁定窗口结束后新产生的移动取消请求
        public void RequestHurtMoveCancellation()
        {
            IsHurtMoveCancellationRequested = true;
        }

        // 写入当前受击状态要播放的动画标识
        public void SetHurtAnimationId(PlayerHurtAnimationId animationId)
        {
            HurtAnimationId = animationId;
        }

        // 重置伤害运行时数据
        public void Reset()
        {
            CurrentHealth = 0f;
            IsDead = false;
            HasPendingHurt = false;
            IsHurtCancellationEnabled = false;
            IsHurtMoveCancellationRequested = false;
            PendingReactionType = DamageReactionType.Light;
            PendingHitDirection = Vector3.zero;
            HurtAnimationId = PlayerHurtAnimationId.None;
        }
    }
}
