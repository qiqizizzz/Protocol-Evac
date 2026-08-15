/*
 * ┌───────────────────────────────────────────────────────┐
 * │  描    述: Ability 无敌窗口数据，保存单个无敌时间区间
 * │  类    名: AbilityInvincibleWindowData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Window.Invincible
{
    [System.Serializable]
    public sealed class AbilityInvincibleWindowData : AbilityWindowDataBase
    {
        public AbilityInvincibleWindowData()
        {
        }

        public AbilityInvincibleWindowData(float startNormalizedTime, float endNormalizedTime)
            : base(startNormalizedTime, endNormalizedTime)
        {
        }

        public AbilityInvincibleWindowData(string id, float startNormalizedTime, float endNormalizedTime)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
        }
    }
}
