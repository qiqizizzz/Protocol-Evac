/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家动作复合状态，承载闪避、攻击与技能等主动动作
 * │  类    名: PlayerActionState.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

namespace Module.Player.HFSM.States.Action
{
    public sealed class PlayerActionState : PlayerCompositeState
    {
        public override PlayerStateId Id => PlayerStateId.Action;
        public override PlayerStateId ParentId => PlayerStateId.None;

        public override PlayerStateId GetInitialChildId()
        {
            return PlayerStateId.ActionDodge;
        }
    }
}
