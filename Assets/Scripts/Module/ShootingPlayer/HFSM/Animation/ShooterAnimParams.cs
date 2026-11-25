/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画参数数据，保存本帧需要写入 Animator 的表现参数
* │  类    名: ShooterAnimParams.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace Module.ShootingPlayer.HFSM.Animation
{
    public struct ShooterAnimParams
    {
        public float MoveSpeed;
        public float MoveX;
        public float MoveY;
        public float VerticalSpeed;
        public bool IsGrounded;

        // 重置射击角色动画参数
        public void Reset()
        {
            MoveSpeed = 0f;
            MoveX = 0f;
            MoveY = 0f;
            VerticalSpeed = 0f;
            IsGrounded = false;
        }
    }
}
