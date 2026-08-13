/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴窗口内存草稿，保存窗口类型、帧范围与类型参数
 * │  类    名: AbilityWindowDraft.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;
using Module.Player.Window;

namespace Tools.Editor.AbilityComposer.Center.Timeline
{
    public sealed class AbilityWindowDraft
    {
        public string Id { get; }
        public AbilityWindowType Type { get; private set; }
        public int StartFrame { get; private set; }
        public int EndFrame { get; private set; }
        public float Damage { get; private set; }

        // 在指定帧创建默认命中窗口草稿
        public AbilityWindowDraft(int startFrame, int endFrame)
        {
            Id = Guid.NewGuid().ToString("N");
            Type = AbilityWindowType.Hit;
            StartFrame = startFrame;
            EndFrame = endFrame;
            Damage = 1f;
        }

        // 更新窗口的业务类型
        public void SetType(AbilityWindowType type)
        {
            Type = type;
        }

        // 更新窗口左右边界
        public void SetFrames(int startFrame, int endFrame)
        {
            StartFrame = startFrame;
            EndFrame = endFrame;
        }

        // 更新命中窗口的伤害参数
        public void SetDamage(float damage)
        {
            Damage = damage;
        }
    }
}
