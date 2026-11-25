/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 角色音效配置，保存脚步等角色音效资源
 * │  类    名: CharacterAudioConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using TriInspector;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Framework.Character.Runtime.Audio
{
    [MovedFrom(
        true,
        sourceNamespace: "Module.ChaisakiPlayer.Audio",
        sourceAssembly: "Player",
        sourceClassName: "PlayerAudioConfigSO")]
    [CreateAssetMenu(fileName = "CharacterAudioConfig", menuName = "配置/角色/音频/角色音效配置")]
    public sealed class CharacterAudioConfigSO : ScriptableObject
    {
        [LabelText("脚步音效")]
        [Tooltip("由移动动画的左右脚落地事件触发播放")]
        [SerializeField] private AudioClip[] FootstepClipValues;

        [LabelText("随机脚步")]
        [Tooltip("开启后每次脚步随机选择一个音效，关闭后按顺序轮播")]
        [SerializeField] private bool RandomFootstepValue = true;

        [LabelText("脚步音量")]
        [SerializeField, Range(0f, 1f)] private float FootstepVolumeValue = 0.75f;

        [LabelText("脚步音高")]
        [SerializeField, Range(0.1f, 3f)] private float FootstepPitchValue = 1f;

        [LabelText("脚步随机音高")]
        [SerializeField, Range(0f, 1f)] private float FootstepRandomPitchRangeValue = 0.08f;

        [LabelText("脚步 3D 音效")]
        [SerializeField] private bool FootstepSpatialValue = true;

        public int FootstepClipCount => FootstepClipValues?.Length ?? 0;
        public bool RandomFootstep => RandomFootstepValue;
        public float FootstepVolume => FootstepVolumeValue;
        public float FootstepPitch => FootstepPitchValue;
        public float FootstepRandomPitchRange => FootstepRandomPitchRangeValue;
        public bool FootstepSpatial => FootstepSpatialValue;

        // 获取指定序号的脚步音效
        public AudioClip GetFootstepClip(int clipIndex)
        {
            if (FootstepClipValues == null || clipIndex < 0 || clipIndex >= FootstepClipValues.Length)
                return null;

            return FootstepClipValues[clipIndex];
        }
    }
}
