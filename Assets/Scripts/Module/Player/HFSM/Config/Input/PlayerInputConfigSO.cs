/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家输入配置，保存输入解释相关参数
 * │  类    名: PlayerInputConfigSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Player.HFSM.Config.Input
{
    [CreateAssetMenu(fileName = "PlayerInputConfig", menuName = "配置/玩家/输入/玩家输入配置")]
    public sealed class PlayerInputConfigSO : ScriptableObject
    {
        [Header("Shift 输入")]
        [Tooltip("按住 Shift 超过该时间后判定为疾跑，短于该时间松开则判定为闪避")]
        [SerializeField, Min(0f)] private float SprintHoldTimeValue = 0.2f;

        public float SprintHoldTime => SprintHoldTimeValue;
    }
}
