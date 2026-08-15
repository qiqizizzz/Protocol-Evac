/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 敌人基础动画配置，保存待机、移动与受击片段
 * │  类    名: EnemyAnimationConfigSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using TriInspector;
using UnityEngine;

namespace Module.Enemy.Animation.Config
{
    [CreateAssetMenu(fileName = "EnemyAnimationConfig", menuName = "配置/敌人/动画/敌人动画配置")]
    public sealed class EnemyAnimationConfigSO : ScriptableObject
    {
        [LabelText("待机动画")]
        [Tooltip("敌人没有执行其他行为时循环播放的动画片段")]
        [SerializeField] private AnimationClip IdleAnimationClipValue;

        [LabelText("移动动画")]
        [Tooltip("敌人移动时循环播放的动画片段")]
        [SerializeField] private AnimationClip MoveAnimationClipValue;

        [LabelText("受击动画")]
        [Tooltip("敌人受到攻击时播放的动画片段")]
        [SerializeField] private AnimationClip HitAnimationClipValue;

        public AnimationClip IdleAnimationClip => IdleAnimationClipValue;
        public AnimationClip MoveAnimationClip => MoveAnimationClipValue;
        public AnimationClip HitAnimationClip => HitAnimationClipValue;
    }
}

