/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: 可同步动画时长的数据项约定
 * │  类    名: IAnimationDurationSyncable.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Animation
{
    public interface IAnimationDurationSyncable
    {
        // 从关联动画片段同步自身维护的时长
        bool SyncAnimationDurations();
    }
}
