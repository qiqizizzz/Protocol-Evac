/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 敌人基础动画配置，保存待机、移动、受击与死亡片段
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

        [LabelText("首段受击动画")]
        [Tooltip("敌人在连续受击的第一次命中时播放的短受击片段")]
        [SerializeField] private AnimationClip HitAnimationClipValue;

        [LabelText("击倒动画")]
        [Tooltip("敌人在连续受击达到击倒次数后播放的倒地片段")]
        [SerializeField] private AnimationClip KnockdownAnimationClipValue;

        [LabelText("起身动画")]
        [Tooltip("敌人完成击倒硬直后播放的起身片段")]
        [SerializeField] private AnimationClip GetUpAnimationClipValue;

        [LabelText("死亡动画")]
        [Tooltip("敌人生命归零后播放并停留的死亡片段")]
        [SerializeField] private AnimationClip DeathAnimationClipValue;

        [LabelText("最短受击硬直")]
        [Tooltip("普通受击至少保持受击控制的时长，击退和击飞会按各自位移时长延长")]
        [SerializeField, Min(0f)] private float MinimumHurtDurationValue;

        [LabelText("连续命中判定间隔")]
        [Tooltip("两次命中间隔不超过该时长时，视为同一轮连续受击")]
        [SerializeField, Min(0f)] private float ConsecutiveHitIntervalValue;

        [LabelText("触发击倒所需命中次数")]
        [Tooltip("同一轮连续受击达到该次数后，播放击倒动画")]
        [SerializeField, Min(2)] private int KnockdownHitCountValue;

        public AnimationClip IdleAnimationClip => IdleAnimationClipValue;
        public AnimationClip MoveAnimationClip => MoveAnimationClipValue;
        public AnimationClip LightHitAnimationClip => HitAnimationClipValue;
        public AnimationClip KnockdownAnimationClip => KnockdownAnimationClipValue;
        public AnimationClip GetUpAnimationClip => GetUpAnimationClipValue;
        public AnimationClip DeathAnimationClip => DeathAnimationClipValue;
        public float MinimumHurtDuration => MinimumHurtDurationValue;
        public float ConsecutiveHitInterval => ConsecutiveHitIntervalValue;
        public int KnockdownHitCount => KnockdownHitCountValue;
    }
}
