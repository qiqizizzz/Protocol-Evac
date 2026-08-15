/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家视角配置，保存第一人称与第三人称视角参数
 * │  类    名: PlayerViewConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Player.Core.View.Config
{
    [CreateAssetMenu(fileName = "PlayerViewConfig", menuName = "配置/玩家/视角/玩家视角配置")]
    public sealed class PlayerViewConfigSO : ScriptableObject
    {
        [Header("视角模式")]
        [Tooltip("默认玩家视角模式")]
        [SerializeField] private PlayerViewMode DefaultViewModeValue = PlayerViewMode.FirstPerson;

        [Header("第一人称")]
        [Tooltip("第一人称水平视角旋转速度")]
        [SerializeField, Min(0f)] private float FirstPersonYawSpeedValue = 180f;
        [Tooltip("第一人称垂直视角旋转速度")]
        [SerializeField, Min(0f)] private float FirstPersonPitchSpeedValue = 180f;
        [Tooltip("第一人称相机本地位置")]
        [SerializeField] private Vector3 FirstPersonCameraLocalPositionValue = new Vector3(0f, 1.55f, 0.15f);
        [Tooltip("第一人称相机近裁面距离")]
        [SerializeField, Min(0.01f)] private float FirstPersonCameraNearClipPlaneValue = 0.03f;
        [Tooltip("第一人称镜头检测球体半径")]
        [SerializeField, Min(0.01f)] private float FirstPersonCameraCollisionRadiusValue = 0.15f;

        [Header("第三人称")]
        [Tooltip("第三人称水平视角旋转速度")]
        [SerializeField, Min(0f)] private float ThirdPersonYawSpeedValue = 180f;
        [Tooltip("第三人称垂直视角旋转速度")]
        [SerializeField, Min(0f)] private float ThirdPersonPitchSpeedValue = 180f;
        [Tooltip("第三人称身体朝移动方向转向速度")]
        [SerializeField, Min(0f)] private float ThirdPersonBodyTurnSpeedValue = 720f;
        [Tooltip("第三人称相机本地位置")]
        [SerializeField] private Vector3 ThirdPersonCameraLocalPositionValue = new Vector3(0f, 1.45f, -4f);
        [Tooltip("第三人称镜头检测球体半径")]
        [SerializeField, Min(0.01f)] private float ThirdPersonCameraCollisionRadiusValue = 0.25f;

        [Header("建筑碰撞")]
        [Tooltip("相机防穿模检测的建筑与地面 Layer")]
        [SerializeField] private LayerMask EnvironmentLayerMaskValue;
        [Tooltip("镜头与建筑保持的最小间距")]
        [SerializeField, Min(0f)] private float CameraCollisionPaddingValue = 0.03f;

        [Header("锁定目标")]
        [Tooltip("按下锁定输入时搜索 Enemy Tag 目标的最大水平距离")]
        [SerializeField, Min(0f)] private float LockRangeValue = 8f;
        [Tooltip("锁定目标超过该水平距离时自动解除锁定")]
        [SerializeField, Min(0f)] private float LockReleaseRangeValue = 12f;
        [Tooltip("锁定时相机水平朝向目标的最大旋转速度")]
        [SerializeField, Min(0f)] private float LockCameraYawSpeedValue = 360f;

        [Header("垂直视角限制")]
        [Tooltip("最低俯仰角")]
        [SerializeField] private float PitchMinValue = -60f;
        [Tooltip("最高俯仰角")]
        [SerializeField] private float PitchMaxValue = 75f;

        public PlayerViewMode DefaultViewMode => DefaultViewModeValue;

        public float FirstPersonYawSpeed => FirstPersonYawSpeedValue;

        public float FirstPersonPitchSpeed => FirstPersonPitchSpeedValue;

        public Vector3 FirstPersonCameraLocalPosition => FirstPersonCameraLocalPositionValue;

        public float FirstPersonCameraNearClipPlane => FirstPersonCameraNearClipPlaneValue;

        public float FirstPersonCameraCollisionRadius => FirstPersonCameraCollisionRadiusValue;

        public float ThirdPersonYawSpeed => ThirdPersonYawSpeedValue;

        public float ThirdPersonPitchSpeed => ThirdPersonPitchSpeedValue;

        public float ThirdPersonBodyTurnSpeed => ThirdPersonBodyTurnSpeedValue;

        public Vector3 ThirdPersonCameraLocalPosition => ThirdPersonCameraLocalPositionValue;

        public float ThirdPersonCameraCollisionRadius => ThirdPersonCameraCollisionRadiusValue;

        public LayerMask EnvironmentLayerMask => EnvironmentLayerMaskValue;

        public float CameraCollisionPadding => CameraCollisionPaddingValue;

        public float LockRange => LockRangeValue;

        public float LockReleaseRange => LockReleaseRangeValue;

        public float LockCameraYawSpeed => LockCameraYawSpeedValue;

        public float PitchMin => PitchMinValue;

        public float PitchMax => PitchMaxValue;
    }
}
