/*
 * ┌──────────────────────────────────────────────┐
 * │  描    述: 角色输入缓存测试，覆盖窗口、消费与清空行为
 * │  类    名: CharacterInputBufferTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────┘
 */

using Framework.Character.Runtime.Input;
using NUnit.Framework;

namespace Tests.CharacterRuntime
{
    public sealed class CharacterInputBufferTests
    {
        // 验证缓存输入在窗口内只能被成功消费一次
        [Test]
        public void Record_WithinWindow_CanBeConsumedOnce()
        {
            CharacterInputBuffer buffer = new CharacterInputBuffer();
            buffer.Record(CharacterBufferedInputType.Jump, 1f);

            Assert.That(buffer.Has(CharacterBufferedInputType.Jump, 1.05f, 0.1f), Is.True);

            buffer.Consume(CharacterBufferedInputType.Jump);

            Assert.That(buffer.Has(CharacterBufferedInputType.Jump, 1.05f, 0.1f), Is.False);
        }

        // 验证超出输入窗口的记录不能再被消费
        [Test]
        public void Has_OutsideWindow_ReturnsFalse()
        {
            CharacterInputBuffer buffer = new CharacterInputBuffer();
            buffer.Record(CharacterBufferedInputType.Dodge, 2f);

            Assert.That(buffer.Has(CharacterBufferedInputType.Dodge, 2.11f, 0.1f), Is.False);
        }

        // 验证清空单个输入不会影响其他缓存输入
        [Test]
        public void Clear_RemovesOnlySpecifiedInput()
        {
            CharacterInputBuffer buffer = new CharacterInputBuffer();
            buffer.Record(CharacterBufferedInputType.Jump, 0f);
            buffer.Record(CharacterBufferedInputType.NormalAttack, 0f);

            buffer.Clear(CharacterBufferedInputType.Jump);

            Assert.That(buffer.Has(CharacterBufferedInputType.Jump, 0f, 0f), Is.False);
            Assert.That(buffer.Has(CharacterBufferedInputType.NormalAttack, 0f, 0f), Is.True);
        }

        // 验证全部清空会移除所有离散输入记录
        [Test]
        public void ClearAll_RemovesEveryInput()
        {
            CharacterInputBuffer buffer = new CharacterInputBuffer();
            buffer.Record(CharacterBufferedInputType.Jump, 0f);
            buffer.Record(CharacterBufferedInputType.SpecialSkill, 0f);

            buffer.ClearAll();

            Assert.That(buffer.Has(CharacterBufferedInputType.Jump, 0f, 0f), Is.False);
            Assert.That(buffer.Has(CharacterBufferedInputType.SpecialSkill, 0f, 0f), Is.False);
        }
    }
}
