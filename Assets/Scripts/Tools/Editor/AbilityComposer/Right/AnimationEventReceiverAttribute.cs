/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 标记可接收 Unity Animation Event 的运行时组件类型
 * │  类    名: AnimationEventReceiverAttribute.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using System;

namespace Tools.Editor.AbilityComposer.Right
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class AnimationEventReceiverAttribute : Attribute
    {
    }
}
