/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 音效触发类型，定义窗口进入、持续和命中播放时机
 * │  类    名: AbilityAudioTriggerType.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Window.Audio
{
    public enum AbilityAudioTriggerType
    {
        WindowEnter,
        WindowDuration,
        OnHit
    }
}
