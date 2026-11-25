/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色动画参数契约，约束各角色共享的位移类参数
 * │  类    名: ICharacterAnimParams.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.Character.Animation
{
    /// <summary>
    /// 角色动画参数契约，框架只负责这一批通用参数，角色可继续扩展自己的字段
    /// </summary>
    public interface ICharacterAnimParams
    {
        // 位移速度，用于驱动位移混合树
        float MoveSpeed { get; set; }

        // 本地空间水平移动分量
        float MoveX { get; set; }

        // 本地空间纵深移动分量
        float MoveY { get; set; }

        // 垂直速度，用于跳跃与下落
        float VerticalSpeed { get; set; }

        // 是否处于 grounded 状态
        bool IsGrounded { get; set; }

        /// <summary>
        /// 复位为默认参数，每帧解析前由框架调用
        /// </summary>
        void Reset();
    }
}
