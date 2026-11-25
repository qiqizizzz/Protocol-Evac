/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 角色伤害上下文测试，覆盖生命、受击、死亡与重置
 * │  类    名: CharacterDamageContextTests.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.Combat.Damage;
using NUnit.Framework;
using UnityEngine;

namespace Tests.CharacterRuntime
{
    public sealed class CharacterDamageContextTests
    {
        // 验证初始化生命会清理此前遗留的受击数据
        [Test]
        public void InitHealth_InitializesHealthAndClearsHurtState()
        {
            CharacterDamageContext<TestHurtAnimationId> context =
                new CharacterDamageContext<TestHurtAnimationId>();
            context.InitHealth(100f);
            context.ApplyDamage(10f, DamageReactionType.Heavy, Vector3.right);
            context.SetHurtCancellationEnabled(true);
            context.RequestHurtMoveCancellation();
            context.SetHurtAnimationId(TestHurtAnimationId.LightHit);

            context.InitHealth(80f);

            Assert.That(context.CurrentHealth, Is.EqualTo(80f));
            Assert.That(context.IsDead, Is.False);
            Assert.That(context.HasPendingHurt, Is.False);
            Assert.That(context.IsHurtCancellationEnabled, Is.False);
            Assert.That(context.IsHurtMoveCancellationRequested, Is.False);
            Assert.That(context.HurtAnimationId, Is.EqualTo(TestHurtAnimationId.None));
        }

        // 验证非致命伤害会更新生命并留下待处理受击反应
        [Test]
        public void ApplyDamage_NonLethal_RecordsPendingHurt()
        {
            CharacterDamageContext<TestHurtAnimationId> context =
                new CharacterDamageContext<TestHurtAnimationId>();
            context.InitHealth(100f);
            Vector3 hitDirection = new Vector3(0f, 0f, -1f);

            context.ApplyDamage(25f, DamageReactionType.KnockUp, hitDirection);

            Assert.That(context.CurrentHealth, Is.EqualTo(75f));
            Assert.That(context.IsDead, Is.False);
            Assert.That(context.HasPendingHurt, Is.True);
            Assert.That(context.PendingReactionType, Is.EqualTo(DamageReactionType.KnockUp));
            Assert.That(context.PendingHitDirection, Is.EqualTo(hitDirection));

            context.ConsumePendingHurt();

            Assert.That(context.HasPendingHurt, Is.False);
        }

        // 验证致命伤害会夹紧生命值并清除受击请求
        [Test]
        public void ApplyDamage_Lethal_ClampsHealthAndClearsPendingHurt()
        {
            CharacterDamageContext<TestHurtAnimationId> context =
                new CharacterDamageContext<TestHurtAnimationId>();
            context.InitHealth(30f);
            context.ApplyDamage(10f, DamageReactionType.Light, Vector3.forward);

            context.ApplyDamage(50f, DamageReactionType.Heavy, Vector3.back);

            Assert.That(context.CurrentHealth, Is.Zero);
            Assert.That(context.IsDead, Is.True);
            Assert.That(context.HasPendingHurt, Is.False);
            Assert.That(context.PendingReactionType, Is.EqualTo(DamageReactionType.Light));
        }

        // 验证关闭受击取消会撤销尚未消费的移动取消请求
        [Test]
        public void SetHurtCancellationEnabled_WhenDisabled_ClearsMoveCancellation()
        {
            CharacterDamageContext<TestHurtAnimationId> context =
                new CharacterDamageContext<TestHurtAnimationId>();
            context.SetHurtCancellationEnabled(true);
            context.RequestHurtMoveCancellation();

            context.SetHurtCancellationEnabled(false);

            Assert.That(context.IsHurtCancellationEnabled, Is.False);
            Assert.That(context.IsHurtMoveCancellationRequested, Is.False);
        }

        // 验证重置会清空生命、受击和动画标识
        [Test]
        public void Reset_ClearsAllDamageState()
        {
            CharacterDamageContext<TestHurtAnimationId> context =
                new CharacterDamageContext<TestHurtAnimationId>();
            context.InitHealth(100f);
            context.ApplyDamage(10f, DamageReactionType.Heavy, Vector3.right);
            context.SetHurtAnimationId(TestHurtAnimationId.LightHit);

            context.Reset();

            Assert.That(context.CurrentHealth, Is.Zero);
            Assert.That(context.IsDead, Is.False);
            Assert.That(context.HasPendingHurt, Is.False);
            Assert.That(context.PendingHitDirection, Is.EqualTo(Vector3.zero));
            Assert.That(context.HurtAnimationId, Is.EqualTo(TestHurtAnimationId.None));
        }
    }
}
