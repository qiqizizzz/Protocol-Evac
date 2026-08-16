/*
 * ┌──────────────────────────────────────────────────────────────┐
 * │  描    述: 玩家伤害配置，保存生命、硬直与击飞位移设计数据
 * │  类    名: PlayerDamageConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Ability.Data.Animation;
using Module.Player.HFSM.Animation.Type;
using TriInspector;
using UnityEngine;

namespace Module.Player.HFSM.Config.Disabled
{
    [CreateAssetMenu(fileName = "PlayerDamageConfig", menuName = "配置/玩家/受控/玩家伤害配置")]
    [DeclareFoldoutGroup("StateAnimation", Title = "状态动画段落", Expanded = true)]
    public sealed class PlayerDamageConfigSO : AnimationDurationConfigSOBase
    {
        [Group("StateAnimation")]
        [LabelText("轻受击动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("按左轻受击、右轻受击的固定顺序配置")]
        [SerializeField] private PlayerHurtAnimationData[] LightHurtAnimationValues;

        [Group("StateAnimation")]
        [LabelText("重受击动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("按左重受击、右重受击的固定顺序配置")]
        [SerializeField] private PlayerHurtAnimationData[] HeavyHurtAnimationValues;

        [Group("StateAnimation")]
        [LabelText("击飞动画段落")]
        [ListDrawerSettings(Draggable = false, ShowElementLabels = true)]
        [Tooltip("按击飞起始、击飞循环、击飞落地的固定顺序配置")]
        [SerializeField] private PlayerHurtAnimationData[] KnockUpAnimationValues;

        [Header("受击设置")]
        [LabelText("受击无敌时间")]
        [Tooltip("受到一次伤害后忽略后续伤害的时长")]
        [SerializeField, Min(0f)] private float DamageInvulnerabilityDurationValue;

        [HideInInspector, SerializeField] private float MaxHealthValue;
        [HideInInspector, SerializeField] private AnimationClip LightLeftClipValue;
        [HideInInspector, SerializeField] private float LightLeftDurationValue;
        [HideInInspector, SerializeField] private AnimationClip LightRightClipValue;
        [HideInInspector, SerializeField] private float LightRightDurationValue;
        [HideInInspector, SerializeField] private AnimationClip HeavyLeftClipValue;
        [HideInInspector, SerializeField] private float HeavyLeftDurationValue;
        [HideInInspector, SerializeField] private AnimationClip HeavyRightClipValue;
        [HideInInspector, SerializeField] private float HeavyRightDurationValue;
        [HideInInspector, SerializeField] private AnimationClip KnockUpStartClipValue;
        [HideInInspector, SerializeField] private float KnockUpStartDurationValue;
        [HideInInspector, SerializeField] private AnimationClip KnockUpLoopClipValue;
        [HideInInspector, SerializeField] private AnimationClip KnockUpFallClipValue;
        [HideInInspector, SerializeField] private float KnockUpFallDurationValue;
        [HideInInspector, SerializeField] private PlayerHurtAnimationData[] HurtAnimationValues;

        public float DamageInvulnerabilityDuration => DamageInvulnerabilityDurationValue;
        // 获取指定受击动画应使用的已烘焙时长
        public float GetHurtDuration(PlayerHurtAnimationId animationId)
        {
            PlayerHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.GetStateDuration();
        }

        // 获取指定受击动画的完整动画时长
        public float GetHurtAnimationDuration(PlayerHurtAnimationId animationId)
        {
            PlayerHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.Duration;
        }

        // 判断指定受击动画时间是否处于移动锁定窗口
        public bool IsHurtMovementLocked(PlayerHurtAnimationId animationId, float normalizedTime)
        {
            PlayerHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData != null && hurtAnimationData.IsMovementLockedAt(normalizedTime);
        }

        // 获取指定受击动画的水平击退速度
        public float GetHurtHorizontalKnockbackSpeed(PlayerHurtAnimationId animationId)
        {
            PlayerHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.HorizontalKnockbackSpeed;
        }

        // 获取指定受击动画的水平击退持续时间
        public float GetHurtHorizontalKnockbackDuration(PlayerHurtAnimationId animationId)
        {
            PlayerHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.HorizontalKnockbackDuration;
        }

        // 获取指定受击动画的竖直初速度
        public float GetHurtVerticalLaunchSpeed(PlayerHurtAnimationId animationId)
        {
            PlayerHurtAnimationData hurtAnimationData = GetHurtAnimationData(animationId);
            return hurtAnimationData == null ? 0f : hurtAnimationData.VerticalLaunchSpeed;
        }

        // 返回受击配置内所有可同步时长的动画段落
        protected override IEnumerable<IAnimationDurationSyncable> GetAnimationDurationItems()
        {
            foreach (PlayerHurtAnimationData hurtAnimationData in GetHurtAnimationItems(LightHurtAnimationValues))
                yield return hurtAnimationData;

            foreach (PlayerHurtAnimationData hurtAnimationData in GetHurtAnimationItems(HeavyHurtAnimationValues))
                yield return hurtAnimationData;

            foreach (PlayerHurtAnimationData hurtAnimationData in GetHurtAnimationItems(KnockUpAnimationValues))
                yield return hurtAnimationData;
        }

        private void OnValidate()
        {
            if (LightHurtAnimationValues != null && LightHurtAnimationValues.Length > 0 &&
                HeavyHurtAnimationValues != null && HeavyHurtAnimationValues.Length > 0 &&
                KnockUpAnimationValues != null && KnockUpAnimationValues.Length > 0)
                return;

            if (HurtAnimationValues != null && HurtAnimationValues.Length == 7)
            {
                LightHurtAnimationValues = new[]
                {
                    HurtAnimationValues[0],
                    HurtAnimationValues[1]
                };

                HeavyHurtAnimationValues = new[]
                {
                    HurtAnimationValues[2],
                    HurtAnimationValues[3]
                };

                KnockUpAnimationValues = new[]
                {
                    HurtAnimationValues[4],
                    HurtAnimationValues[5],
                    HurtAnimationValues[6]
                };
                return;
            }

            LightHurtAnimationValues = new[]
            {
                new PlayerHurtAnimationData(LightLeftClipValue, LightLeftDurationValue),
                new PlayerHurtAnimationData(LightRightClipValue, LightRightDurationValue)
            };

            HeavyHurtAnimationValues = new[]
            {
                new PlayerHurtAnimationData(HeavyLeftClipValue, HeavyLeftDurationValue),
                new PlayerHurtAnimationData(HeavyRightClipValue, HeavyRightDurationValue)
            };

            KnockUpAnimationValues = new[]
            {
                new PlayerHurtAnimationData(KnockUpStartClipValue, KnockUpStartDurationValue),
                new PlayerHurtAnimationData(KnockUpLoopClipValue, 0f),
                new PlayerHurtAnimationData(KnockUpFallClipValue, KnockUpFallDurationValue)
            };
        }

        // 根据受击动画标识获取固定顺序中的动画段落数据
        private PlayerHurtAnimationData GetHurtAnimationData(PlayerHurtAnimationId animationId)
        {
            return animationId switch
            {
                PlayerHurtAnimationId.LightLeft => GetHurtAnimationData(LightHurtAnimationValues, 0),
                PlayerHurtAnimationId.LightRight => GetHurtAnimationData(LightHurtAnimationValues, 1),
                PlayerHurtAnimationId.HeavyLeft => GetHurtAnimationData(HeavyHurtAnimationValues, 0),
                PlayerHurtAnimationId.HeavyRight => GetHurtAnimationData(HeavyHurtAnimationValues, 1),
                PlayerHurtAnimationId.KnockUpStart => GetHurtAnimationData(KnockUpAnimationValues, 0),
                PlayerHurtAnimationId.KnockUpLoop => GetHurtAnimationData(KnockUpAnimationValues, 1),
                PlayerHurtAnimationId.KnockUpFall => GetHurtAnimationData(KnockUpAnimationValues, 2),
                _ => null
            };
        }

        // 返回一个受击反应动画列表内的所有动画段落
        private IEnumerable<PlayerHurtAnimationData> GetHurtAnimationItems(PlayerHurtAnimationData[] hurtAnimationValues)
        {
            if (hurtAnimationValues == null)
                yield break;

            for (int i = 0; i < hurtAnimationValues.Length; i++)
                yield return hurtAnimationValues[i];
        }

        // 从一个受击反应动画列表中获取指定索引的段落数据
        private PlayerHurtAnimationData GetHurtAnimationData(PlayerHurtAnimationData[] hurtAnimationValues, int index)
        {
            if (hurtAnimationValues == null || index < 0 || index >= hurtAnimationValues.Length)
                return null;

            return hurtAnimationValues[index];
        }
    }
}
