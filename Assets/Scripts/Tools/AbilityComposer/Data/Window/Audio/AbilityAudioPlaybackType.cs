/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 音效播放类型，定义单次、随机、顺序与循环播放
 * │  类    名: AbilityAudioPlaybackType.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Window.Audio
{
    public enum AbilityAudioPlaybackType
    {
        OneShot,
        RandomOneShot,
        SequenceOneShot,
        Loop
    }
}
