/*
 * ┌──────────────────────────────────┐
 * │  描    述: 千咲动画参数数据，保存本帧需要写入 Animator 的表现参数
 * │  类    名: PlayerAnimParams.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.Character.Animation;

namespace Module.ChaisakiPlayer.HFSM.Animation
{
    // 千咲动画参数，通用位移字段由角色框架约定，锁定权重为千咲专属
    public struct PlayerAnimParams : ICharacterAnimParams
    {
        public float MoveSpeed { get; set; }
        public float MoveX { get; set; }
        public float MoveY { get; set; }
        public float VerticalSpeed { get; set; }
        public bool IsGrounded { get; set; }

        // 锁定视角混合权重
        public float LockOnWeight { get; set; }

        // 重置千咲动画参数
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
