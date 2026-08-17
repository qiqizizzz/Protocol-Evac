/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 音效播放目标类型，定义音效生成位置来源
 * │  类    名: AbilityAudioTargetType.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Window.Audio
{
    public enum AbilityAudioTargetType
    {
        SourceRoot,
        SourceSocket,
        HitPoint,
        HitTargetRoot,
        HitTargetSocket
    }
}
