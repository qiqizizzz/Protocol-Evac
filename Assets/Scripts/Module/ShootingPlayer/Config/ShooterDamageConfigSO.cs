/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色伤害配置，保存受击动画段落与无敌时间
* │  类    名: ShooterDamageConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using Module.Ability.Data.Animation;
using Module.ShootingPlayer.HFSM.Animation;
using TriInspector;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShooterDamageConfig", menuName = "配置/射击玩家/受击配置")]
    [DeclareFoldoutGroup("HurtAnimation", Title = "受击动画段落", Expanded = true)]
    [DeclareFoldoutGroup("DeadAnimation", Title = "死亡动画段落", Expanded = true)]
    public sealed class ShooterDamageConfigSO : AnimationDurationConfigSOBase
    {
        private const int HIT_ONE_INDEX = 0;
        private const int HIT_TWO_INDEX = 1;
        private const int STUN_INDEX = 2;
        private const int DIE_ONE_INDEX = 0;
        private const int DIE_TWO_INDEX = 1;

        [Group("HurtAnimation")]
        [LabelText("受击动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("按 Hit1、Hit2、Stun 的固定顺序配置")]
        [SerializeField] private ShooterHurtAnimationData[] HurtAnimationValues;

        [Group("DeadAnimation")]
        [LabelText("死亡动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("按 Die1、Die2 的固定顺序配置")]
        [SerializeField] private ShooterHurtAnimationData[] DeadAnimationValues;

        [Header("受击设置")]
        [LabelText("受击无敌时间")]
        [Tooltip("受到一次伤害后忽略后续伤害的时长")]
        [SerializeField, Min(0f)] private float DamageInvulnerabilityDurationValue = 0.5f;

        public float DamageInvulnerabilityDuration => DamageInvulnerabilityDurationValue;

        // 获取指定受击动画的完整动画时长
        public float GetHurtAnimationDuration(ShooterHurtAnimationId animationId)
        {
            ShooterHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.Duration;
        }

        // 判断指定受击动画时间是否处于移动锁定窗口
        public bool IsHurtMovementLocked(ShooterHurtAnimationId animationId, float normalizedTime)
        {
            ShooterHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData != null && hurtAnimationData.IsMovementLockedAt(normalizedTime);
        }

        // 获取指定受击动画的水平击退速度
        public float GetHurtHorizontalKnockbackSpeed(ShooterHurtAnimationId animationId)
        {
            ShooterHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.HorizontalKnockbackSpeed;
        }

        // 获取指定受击动画的水平击退持续时间
        public float GetHurtHorizontalKnockbackDuration(ShooterHurtAnimationId animationId)
        {
            ShooterHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.HorizontalKnockbackDuration;
        }

        // 返回受击配置内所有可同步时长的动画段落
        protected override IEnumerable<IAnimationDurationSyncable> GetAnimationDurationItems()
        {
            foreach (ShooterHurtAnimationData hurtAnimationData in GetItems(HurtAnimationValues))
                yield return hurtAnimationData;

            foreach (ShooterHurtAnimationData hurtAnimationData in GetItems(DeadAnimationValues))
                yield return hurtAnimationData;
        }

        // 根据受击动画标识获取固定顺序中的动画段落数据
        private ShooterHurtAnimationData GetHurtAnimationData(ShooterHurtAnimationId animationId)
        {
            return animationId switch
            {
                ShooterHurtAnimationId.Hit1 => GetHurtAnimationData(HurtAnimationValues, HIT_ONE_INDEX),
                ShooterHurtAnimationId.Hit2 => GetHurtAnimationData(HurtAnimationValues, HIT_TWO_INDEX),
                ShooterHurtAnimationId.Stun => GetHurtAnimationData(HurtAnimationValues, STUN_INDEX),
                ShooterHurtAnimationId.Die1 => GetHurtAnimationData(DeadAnimationValues, DIE_ONE_INDEX),
                ShooterHurtAnimationId.Die2 => GetHurtAnimationData(DeadAnimationValues, DIE_TWO_INDEX),
                _ => null
            };
        }

        // 返回一个动画列表内的所有段落数据
        private IEnumerable<ShooterHurtAnimationData> GetItems(ShooterHurtAnimationData[] values)
        {
            if (values == null)
                yield break;

            for (int i = 0; i < values.Length; i++)
                yield return values[i];
        }

        // 从一个动画列表中获取指定索引的段落数据
        private ShooterHurtAnimationData GetHurtAnimationData(ShooterHurtAnimationData[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
                return null;

            return values[index];
        }
    }
}
