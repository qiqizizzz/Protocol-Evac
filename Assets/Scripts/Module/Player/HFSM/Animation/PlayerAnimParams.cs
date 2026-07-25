/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家动画参数数据，保存本帧需要写入 Animator 的表现参数
 * │  类    名: PlayerAnimParams.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM.Animation
{
    public struct PlayerAnimParams
    {
        public bool IsMoving;
        public bool IsSprinting;
        public float MoveSpeed;

        // 重置玩家动画参数
        public void Reset()
        {
            IsMoving = false;
            IsSprinting = false;
            MoveSpeed = 0f;
        }
    }
}
