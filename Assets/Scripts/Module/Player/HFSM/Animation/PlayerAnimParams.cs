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
        public float MoveSpeed;
        public float LockOnWeight;
        public float MoveX;
        public float MoveY;
        public float VerticalSpeed;
        public bool IsGrounded;

        // 重置玩家动画参数
        public void Reset()
        {
            MoveSpeed = 0f;
            LockOnWeight = 0f;
            MoveX = 0f;
            MoveY = 0f;
            VerticalSpeed = 0f;
            IsGrounded = false;
        }
    }
}
