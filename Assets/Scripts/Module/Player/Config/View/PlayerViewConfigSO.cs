/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家视角配置，保存第一人称与第三人称视角参数
 * │  类    名: PlayerViewConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.Config.View
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
        [Tooltip("第一人称相机本地偏移")]
        [SerializeField] private Vector3 FirstPersonCameraOffsetValue = new Vector3(0f, 1.65f, 0f);

        [Header("第三人称")]
        [Tooltip("第三人称水平视角旋转速度")]
        [SerializeField, Min(0f)] private float ThirdPersonYawSpeedValue = 180f;
        [Tooltip("第三人称垂直视角旋转速度")]
        [SerializeField, Min(0f)] private float ThirdPersonPitchSpeedValue = 180f;
        [Tooltip("第三人称身体朝移动方向转向速度")]
        [SerializeField, Min(0f)] private float ThirdPersonBodyTurnSpeedValue = 720f;
        [Tooltip("第三人称相机距离")]
        [SerializeField, Min(0f)] private float ThirdPersonDistanceValue = 4f;
        [Tooltip("第三人称相机围绕点偏移")]
        [SerializeField] private Vector3 ThirdPersonPivotOffsetValue = new Vector3(0f, 1.5f, 0f);

        [Header("垂直视角限制")]
        [Tooltip("最低俯仰角")]
        [SerializeField] private float PitchMinValue = -60f;
        [Tooltip("最高俯仰角")]
        [SerializeField] private float PitchMaxValue = 75f;

        public PlayerViewMode DefaultViewMode => DefaultViewModeValue;

        public float FirstPersonYawSpeed => FirstPersonYawSpeedValue;

        public float FirstPersonPitchSpeed => FirstPersonPitchSpeedValue;

        public Vector3 FirstPersonCameraOffset => FirstPersonCameraOffsetValue;

        public float ThirdPersonYawSpeed => ThirdPersonYawSpeedValue;

        public float ThirdPersonPitchSpeed => ThirdPersonPitchSpeedValue;

        public float ThirdPersonBodyTurnSpeed => ThirdPersonBodyTurnSpeedValue;

        public float ThirdPersonDistance => ThirdPersonDistanceValue;

        public Vector3 ThirdPersonPivotOffset => ThirdPersonPivotOffsetValue;

        public float PitchMin => PitchMinValue;

        public float PitchMax => PitchMaxValue;
    }
}
