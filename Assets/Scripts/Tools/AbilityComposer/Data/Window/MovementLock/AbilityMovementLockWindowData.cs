/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 移动锁定窗口数据，保存禁止移动的时间区间
 * │  类    名: AbilityMovementLockWindowData.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Window.MovementLock
{
    [System.Serializable]
    public sealed class AbilityMovementLockWindowData : AbilityWindowDataBase
    {
        public AbilityMovementLockWindowData()
        {
        }

        public AbilityMovementLockWindowData(float startNormalizedTime, float endNormalizedTime)
            : base(startNormalizedTime, endNormalizedTime)
        {
        }

        public AbilityMovementLockWindowData(string id, float startNormalizedTime, float endNormalizedTime)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
        }
    }
}

