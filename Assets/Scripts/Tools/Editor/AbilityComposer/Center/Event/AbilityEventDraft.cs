/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 动画事件内存草稿，保存未应用到动画资源的编辑数据
 * │  类    名: AbilityEventDraft.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using System;

namespace Tools.Editor.AbilityComposer.Center.Event
{
    public sealed class AbilityEventDraft
    {
        public string Id { get; }
        public int Frame { get; private set; }
        public AbilityEventCategory Category { get; private set; }
        public string FunctionName { get; private set; }

        // 在指定帧创建默认分类的空事件草稿
        public AbilityEventDraft(int frame)
        {
            Id = Guid.NewGuid().ToString("N");
            Frame = frame;
            Category = AbilityEventCategory.Default;
            FunctionName = string.Empty;
        }

        // 更新事件所在帧
        public void SetFrame(int frame)
        {
            Frame = frame;
        }

        // 更新事件分类
        public void SetCategory(AbilityEventCategory category)
        {
            Category = category;
        }

        // 更新 Unity Animation Event 的 Function 名称
        public void SetFunctionName(string functionName)
        {
            FunctionName = functionName;
        }
    }
}
