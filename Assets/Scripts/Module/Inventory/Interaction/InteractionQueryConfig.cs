/*
* ┌──────────────────────────────────┐
* │  描    述: 可交互目标查询参数, 保存距离、夹角、近身兜底与评分权重
* │  类    名: InteractionQueryConfig.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Inventory.Interaction
{
    /// <summary>
    /// 交互目标查询参数：把玩法数值从扫描代码中拆出, 方便在 Inspector 中实时调优
    /// </summary>
    [Serializable]
    public struct InteractionQueryConfig
    {
        [Header("距离")]
        [SerializeField, Min(0f)] private float MaxDistanceValue;
        [SerializeField, Min(0f)] private float NearDistanceValue;

        [Header("朝向")]
        [SerializeField, Range(0f, 180f)] private float MaxAngleValue;

        [Header("评分")]
        [SerializeField, Range(0f, 1f)] private float DistanceWeightValue;
        [SerializeField, Range(0f, 1f)] private float AngleWeightValue;

        [Header("遮挡")]
        [SerializeField] private bool RequireLineOfSightValue;

        public float MaxDistance => MaxDistanceValue;
        public float NearDistance => NearDistanceValue;
        public float MaxAngle => MaxAngleValue;
        public float DistanceWeight => DistanceWeightValue;
        public float AngleWeight => AngleWeightValue;
        public bool RequireLineOfSight => RequireLineOfSightValue;

        public InteractionQueryConfig(float maxDistance, float maxAngle, float nearDistance,
            float distanceWeight, float angleWeight, bool requireLineOfSight = true)
        {
            MaxDistanceValue = maxDistance;
            NearDistanceValue = nearDistance;
            MaxAngleValue = maxAngle;
            DistanceWeightValue = distanceWeight;
            AngleWeightValue = angleWeight;
            RequireLineOfSightValue = requireLineOfSight;
        }
    }
}
