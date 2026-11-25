/*
 * ┌───────────────────────────────────────────────┐
 * │  描    述: 角色动作上下文测试，覆盖重置、根运动与动画重播
 * │  类    名: CharacterActionContextTests.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Contexts;
using Module.Ability.Data;
using NUnit.Framework;
using UnityEngine;

namespace Tests.CharacterRuntime
{
    public sealed class CharacterActionContextTests
    {
        // 验证重置会恢复全部动作运行时的初始状态
        [Test]
        public void Reset_RestoresInitialValues()
        {
            CharacterActionContext<TestStateId> context = new CharacterActionContext<TestStateId>();
            context.IsStateFinished = true;
            context.IsWeaponVisible = true;
            context.NormalAttackIndex = 3;
            context.NormalAttackPhase = AbilityStepPhase.Recovery;
            context.RequestAnimReplay(TestStateId.Attack, 0.2f);
            context.SetRootMotionMoveEnabled(true);
            context.AddRootMotionDeltaPosition(Vector3.one);

            context.Reset();

            Assert.That(context.IsStateFinished, Is.False);
            Assert.That(context.IsWeaponVisible, Is.False);
            Assert.That(context.NormalAttackIndex, Is.Zero);
            Assert.That(context.NormalAttackPhase, Is.EqualTo(AbilityStepPhase.Begin));
            Assert.That(context.AnimReplayStateId, Is.Null);
            Assert.That(context.AnimReplayBlendDuration, Is.Zero);
            Assert.That(context.IsRootMotionMoveEnabled, Is.False);
            Assert.That(context.RootMotionDeltaPosition, Is.EqualTo(Vector3.zero));
        }

        // 验证根运动位移取出后立即归零
        [Test]
        public void ConsumeRootMotionDeltaPosition_ReturnsAccumulationAndClears()
        {
            CharacterActionContext<TestStateId> context = new CharacterActionContext<TestStateId>();
            context.SetRootMotionMoveEnabled(true);
            context.AddRootMotionDeltaPosition(new Vector3(1f, 0f, 2f));
            context.AddRootMotionDeltaPosition(new Vector3(0.5f, 0f, -0.5f));

            Vector3 consumed = context.ConsumeRootMotionDeltaPosition();

            Assert.That(consumed, Is.EqualTo(new Vector3(1.5f, 0f, 1.5f)));
            Assert.That(context.RootMotionDeltaPosition, Is.EqualTo(Vector3.zero));
        }

        // 验证关闭根运动位移会清空尚未消费的累计值
        [Test]
        public void SetRootMotionMoveEnabled_WhenDisabled_ClearsDelta()
        {
            CharacterActionContext<TestStateId> context = new CharacterActionContext<TestStateId>();
            context.SetRootMotionMoveEnabled(true);
            context.AddRootMotionDeltaPosition(Vector3.one);

            context.SetRootMotionMoveEnabled(false);

            Assert.That(context.IsRootMotionMoveEnabled, Is.False);
            Assert.That(context.RootMotionDeltaPosition, Is.EqualTo(Vector3.zero));
        }

        // 验证动画重播请求只被表现层消费一次
        [Test]
        public void ConsumeAnimReplayRequest_ReturnsRequestOnce()
        {
            CharacterActionContext<TestStateId> context = new CharacterActionContext<TestStateId>();
            context.RequestAnimReplay(TestStateId.Attack, 0.15f);

            TestStateId? first = context.ConsumeAnimReplayRequest();
            TestStateId? second = context.ConsumeAnimReplayRequest();

            Assert.That(first, Is.EqualTo(TestStateId.Attack));
            Assert.That(second, Is.Null);
            Assert.That(context.AnimReplayBlendDuration, Is.EqualTo(0.15f));
        }
    }
}
