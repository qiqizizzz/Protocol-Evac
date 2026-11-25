/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色闪避配置，保存闪避动画段落与位移参数
* │  类    名: ShooterEvadeConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Framework.Character.Runtime.HFSM.Config.Common;
using TriInspector;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShooterEvadeConfig", menuName = "配置/射击玩家/闪避配置")]
    public sealed class ShooterEvadeConfigSO : CharacterStateCommonConfigSO
    {
        private const int EVADE_CLIP_INDEX = 0;

        public const int REQUIRED_STATE_CLIP_COUNT = 1;

        [Header("闪避位移")]
        [Tooltip("闪避位移的初速度")]
        [SerializeField, Min(0f)] private float EvadeSpeedValue = 8f;
        [Tooltip("动画归一化进度达到该值后停止位移")]
        [SerializeField, Range(0.01f, 1f)] private float EvadeMoveEndNormalizedTimeValue = 0.6f;
        [Tooltip("无输入时闪避方向取角色正前方，该值为输入判定阈值平方")]
        [SerializeField, Min(0f)] private float EvadeInputThresholdSqrValue = 0.01f;
        [Tooltip("闪避输入的提前缓冲时间")]
        [SerializeField, Min(0f)] private float EvadeBufferTimeValue = 0.2f;

        public float EvadeSpeed => EvadeSpeedValue;

        public float EvadeMoveEndNormalizedTime => EvadeMoveEndNormalizedTimeValue;

        public float EvadeInputThresholdSqr => EvadeInputThresholdSqrValue;

        public float EvadeBufferTime => EvadeBufferTimeValue;

        public CharacterStateClipData EvadeClipData => GetStateClip(EVADE_CLIP_INDEX);

        // 闪避位移的持续时间
        public float EvadeDuration => EvadeClipData?.StateDuration ?? 0f;
    }
}
