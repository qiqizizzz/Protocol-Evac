/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 敌人移动配置，保存移动速度与身体转向速度
 * │  类    名: EnemyMoveConfigSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Movement.Config
{
    [CreateAssetMenu(fileName = "EnemyMoveConfig", menuName = "配置/敌人/移动/敌人移动配置")]
    public sealed class EnemyMoveConfigSO : ScriptableObject
    {
        [Tooltip("敌人沿导航路径移动的速度")]
        [SerializeField, Min(0f)] private float MoveSpeed;

        [Tooltip("敌人身体每秒允许旋转的最大角度")]
        [SerializeField, Min(0f)] private float TurnSpeed;

        public float MoveSpeedValue => MoveSpeed;
        public float TurnSpeedValue => TurnSpeed;
    }
}

