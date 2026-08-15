/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: Ability 阶段推进窗口数据，保存技能阶段推进区间
 * │  类    名: AbilityStepAdvanceWindowData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

namespace Module.Ability.Data.Window.StepAdvance
{
    [System.Serializable]
    public sealed class AbilityStepAdvanceWindowData : AbilityWindowDataBase
    {
        public AbilityStepAdvanceWindowData()
        {
        }

        public AbilityStepAdvanceWindowData(float startNormalizedTime, float endNormalizedTime)
            : base(startNormalizedTime, endNormalizedTime)
        {
        }

        public AbilityStepAdvanceWindowData(string id, float startNormalizedTime, float endNormalizedTime)
            : base(id, startNormalizedTime, endNormalizedTime)
        {
        }
    }
}
