/*
 * ┌──────────────────────────────────┐
 * │  描    述: 单次伤害的数据载体
 * │  类    名: DamageData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Combat.Damage
{
    public readonly struct DamageData
    {
        public float Damage { get; }
        public GameObject Source { get; }
        public Vector3 HitPoint { get; }
        public Vector3 HitDirection { get; }
        public DamageReactionType ReactionType { get; }

        /// <summary>
        /// 创建一次伤害数据
        /// </summary>
        /// <param name="damage">伤害数值</param>
        /// <param name="source">伤害来源对象</param>
        /// <param name="hitPoint">命中的世界坐标</param>
        /// <param name="hitDirection">从攻击方指向受击方的方向</param>
        /// <param name="reactionType">本次命中应触发的通用受击反应</param>
        public DamageData(float damage, GameObject source, Vector3 hitPoint, Vector3 hitDirection,
            DamageReactionType reactionType)
        {
            Damage = damage;
            Source = source;
            HitPoint = hitPoint;
            HitDirection = hitDirection;
            ReactionType = reactionType;
        }
    }
}
