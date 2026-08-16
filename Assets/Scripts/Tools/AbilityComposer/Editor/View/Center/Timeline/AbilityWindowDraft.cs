/*
 * ┌───────────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 时间轴窗口内存草稿，承载独立窗口轨道的编辑数据
 * │  类    名: AbilityWindowDraft.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────────┘
 */

using System;

namespace Tools.AbilityComposer.Editor.View.Center.Timeline
{
    public enum AbilityWindowDraftType
    {
        Hit,
        StepAdvance,
        MovementLock
    }

    public sealed class AbilityWindowDraft
    {
        public string Id { get; private set; }
        public AbilityWindowDraftType Type { get; private set; }
        public int StartFrame { get; private set; }
        public int EndFrame { get; private set; }
        public float Damage { get; private set; }

        // 在指定帧创建默认命中窗口草稿
        public AbilityWindowDraft(int startFrame, int endFrame)
        {
            Id = Guid.NewGuid().ToString("N");
            Type = AbilityWindowDraftType.Hit;
            StartFrame = startFrame;
            EndFrame = endFrame;
            Damage = 1f;
        }

        // 切换窗口所属的独立轨道类型
        public void SetType(AbilityWindowDraftType type)
        {
            Type = type;
        }

        // 恢复已保存窗口的稳定标识
        public void SetId(string id)
        {
            if (!string.IsNullOrEmpty(id))
                Id = id;
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
