/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 网格导航配置，保存碰撞采样、网格与寻路参数
 * │  类    名: GridNavigationConfigSO.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Navigation.Grid.Config
{
    [CreateAssetMenu(fileName = "GridNavigationConfig", menuName = "配置/导航/网格/网格导航配置")]
    public sealed class GridNavigationConfigSO : ScriptableObject
    {
        [Header("网格")]
        [Tooltip("单个导航网格的边长")]
        [SerializeField, Min(0.1f)] private float CellSize;

        [Tooltip("路径搜索区域在起点与终点外额外扩展的距离")]
        [SerializeField, Min(0f)] private float SearchPadding;

        [Tooltip("单轴允许生成的最大网格数量")]
        [SerializeField, Min(8)] private int MaxGridSize;

        [Header("角色")]
        [Tooltip("导航角色的碰撞半径")]
        [SerializeField, Min(0.01f)] private float AgentRadius;

        [Tooltip("导航角色的站立高度")]
        [SerializeField, Min(0.1f)] private float AgentHeight;

        [Tooltip("相邻网格允许跨越的最大高度差")]
        [SerializeField, Min(0f)] private float MaxStepHeight;

        [Header("碰撞采样")]
        [Tooltip("地面射线起点高于搜索参考面的距离")]
        [SerializeField, Min(0.1f)] private float GroundProbeHeight;

        [Tooltip("地面射线向下检测的最大距离")]
        [SerializeField, Min(0.1f)] private float GroundProbeDistance;

        [Tooltip("允许作为可行走地面的 Layer")]
        [SerializeField] private LayerMask GroundLayerMask;

        [Tooltip("会阻挡导航角色通行的 Layer")]
        [SerializeField] private LayerMask ObstacleLayerMask;

        [Header("路径跟随")]
        [Tooltip("抵达路径拐点时允许的距离误差")]
        [SerializeField, Min(0.01f)] private float CornerReachDistance;

        [Tooltip("随机寻找巡逻目的地时的最大尝试次数")]
        [SerializeField, Min(1)] private int RandomSampleAttempts;

        public float CellSizeValue => CellSize;
        public float SearchPaddingValue => SearchPadding;
        public int MaxGridSizeValue => MaxGridSize;
        public float AgentRadiusValue => AgentRadius;
        public float AgentHeightValue => AgentHeight;
        public float MaxStepHeightValue => MaxStepHeight;
        public float GroundProbeHeightValue => GroundProbeHeight;
        public float GroundProbeDistanceValue => GroundProbeDistance;
        public LayerMask GroundLayerMaskValue => GroundLayerMask;
        public LayerMask ObstacleLayerMaskValue => ObstacleLayerMask;
        public float CornerReachDistanceValue => CornerReachDistance;
        public int RandomSampleAttemptsValue => RandomSampleAttempts;
    }
}

