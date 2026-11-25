/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色动画参数数据，保存本帧需要写入 Animator 的表现参数
* │  类    名: ShooterAnimParams.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.Character.Animation;

namespace Module.ShootingPlayer.HFSM.Animation
{
    // 射击角色动画参数，字段全部落在角色框架的通用位移约定内
    public struct ShooterAnimParams : ICharacterAnimParams
    {
        public float MoveSpeed { get; set; }
        public float MoveX { get; set; }
        public float MoveY { get; set; }
        public float VerticalSpeed { get; set; }
        public bool IsGrounded { get; set; }

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
