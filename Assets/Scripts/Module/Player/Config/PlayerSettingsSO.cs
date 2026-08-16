/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家模块配置聚合资产
 * │  类    名: PlayerSettingsSO.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Core.View.Config;
using Module.Player.HFSM.Config.Action;
using Module.Player.HFSM.Config.Air;
using Module.Player.HFSM.Config.Disabled;
using Module.Player.HFSM.Config.Move;
using Module.Player.Input.Config;
using Module.Player.Skill.Data;
using TriInspector;
using UnityEngine;

namespace Module.Player.Config
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "配置/玩家/玩家配置聚合")]
    [DeclareFoldoutGroup("Stats", Title = "数值配置", Expanded = true)]
    [DeclareFoldoutGroup("Movement", Title = "移动配置", Expanded = true)]
    [DeclareFoldoutGroup("Input", Title = "输入配置", Expanded = true)]
    [DeclareFoldoutGroup("Air", Title = "空中配置", Expanded = true)]
    [DeclareFoldoutGroup("Action", Title = "动作配置", Expanded = true)]
    [DeclareFoldoutGroup("Disabled", Title = "受控配置", Expanded = true)]
    [DeclareFoldoutGroup("Skill", Title = "技能配置", Expanded = true)]
    [DeclareFoldoutGroup("View", Title = "视角配置", Expanded = true)]
    public sealed class PlayerSettingsSO : ScriptableObject
    {
        [Group("Stats")]
        [LabelText("数值配置")]
        [SerializeField] private PlayerStatsConfigSO StatsConfigValue;

        [Group("Movement")]
        [LabelText("移动配置")]
        [SerializeField] private PlayerMoveConfigSO MoveConfigValue;

        [Group("Input")]
        [LabelText("输入配置")]
        [SerializeField] private PlayerInputConfigSO InputConfigValue;

        [Group("Air")]
        [LabelText("空中配置")]
        [SerializeField] private PlayerAirConfigSO AirConfigValue;

        [Group("Action")]
        [LabelText("闪避配置")]
        [SerializeField] private PlayerDodgeConfigSO DodgeConfigValue;

        [Group("Disabled")]
        [LabelText("伤害配置")]
        [SerializeField] private PlayerDamageConfigSO DamageConfigValue;

        [Group("Skill")]
        [LabelText("普通攻击配置")]
        [SerializeField] private PlayerNormalAttackConfigSO NormalAttackConfigValue;

        [Group("View")]
        [LabelText("视角配置")]
        [SerializeField] private PlayerViewConfigSO ViewConfigValue;

        public PlayerStatsConfigSO StatsConfig => StatsConfigValue;
        public PlayerMoveConfigSO MoveConfig => MoveConfigValue;
        public PlayerInputConfigSO InputConfig => InputConfigValue;
        public PlayerAirConfigSO AirConfig => AirConfigValue;
        public PlayerDodgeConfigSO DodgeConfig => DodgeConfigValue;
        public PlayerDamageConfigSO DamageConfig => DamageConfigValue;
        public PlayerNormalAttackConfigSO NormalAttackConfig => NormalAttackConfigValue;
        public PlayerViewConfigSO ViewConfig => ViewConfigValue;
    }
}
