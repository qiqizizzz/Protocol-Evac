/*
 * ┌──────────────────────────────────────────┐
 * │  描    述: 玩家受控复合状态，提供受击默认子状态
 * │  类    名: PlayerDisabledState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────┘
 */

namespace Module.Player.HFSM.States.Disabled
{
    public sealed class PlayerDisabledState : PlayerCompositeState
    {
        public override PlayerStateId Id => PlayerStateId.Disabled;
        public override PlayerStateId ParentId => PlayerStateId.None;

        // 返回进入受控状态时默认激活的受击子状态
        public override PlayerStateId GetInitialChildId()
        {
            return PlayerStateId.DisabledHurt;
        }
    }
}
