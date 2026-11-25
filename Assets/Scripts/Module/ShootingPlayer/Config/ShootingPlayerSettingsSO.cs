/*
* ┌──────────────────────────────────┐
* │  描    述: 射击玩家模块配置聚合资产
* │  类    名: ShootingPlayerSettingsSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.Player.Audio;
using Module.Player.Config;
using Module.Player.Core.View.Config;
using TriInspector;
using UnityEngine;

namespace Module.ShootingPlayer.Config
{
    [CreateAssetMenu(fileName = "ShootingPlayerSettings", menuName = "配置/射击玩家/角色配置聚合")]
    [DeclareFoldoutGroup("Stats", Title = "数值配置", Expanded = true)]
    [DeclareFoldoutGroup("Move", Title = "移动配置", Expanded = true)]
    [DeclareFoldoutGroup("Aim", Title = "瞄准配置", Expanded = true)]
    [DeclareFoldoutGroup("Crouch", Title = "蹲伏配置", Expanded = true)]
    [DeclareFoldoutGroup("Action", Title = "闪避配置", Expanded = true)]
    [DeclareFoldoutGroup("Disabled", Title = "受击配置", Expanded = true)]
    [DeclareFoldoutGroup("Skill", Title = "射击技能配置", Expanded = true)]
    [DeclareFoldoutGroup("View", Title = "视角配置", Expanded = true)]
    [DeclareFoldoutGroup("Audio", Title = "音频配置", Expanded = false)]
    public sealed class ShootingPlayerSettingsSO : ScriptableObject
    {
        [Group("Stats")]
        [LabelText("数值配置")]
        [SerializeField] private PlayerStatsConfigSO StatsConfigValue;

        [Group("Move")]
        [LabelText("移动配置")]
        [SerializeField] private ShooterMoveConfigSO MoveConfigValue;

        [Group("Aim")]
        [LabelText("瞄准配置")]
        [SerializeField] private ShooterAimConfigSO AimConfigValue;

        [Group("Crouch")]
        [LabelText("蹲伏配置")]
        [SerializeField] private ShooterCrouchConfigSO CrouchConfigValue;

        [Group("Action")]
        [LabelText("闪避配置")]
        [SerializeField] private ShooterEvadeConfigSO EvadeConfigValue;

        [Group("Disabled")]
        [LabelText("伤害配置")]
        [SerializeField] private ShooterDamageConfigSO DamageConfigValue;

        [Group("Skill")]
        [LabelText("射击技能配置")]
        [SerializeField] private ShooterShootConfigSO ShootConfigValue;

        [Group("View")]
        [LabelText("视角配置")]
        [SerializeField] private PlayerViewConfigSO ViewConfigValue;

        [Group("Audio")]
        [LabelText("玩家音效配置")]
        [SerializeField] private PlayerAudioConfigSO AudioConfigValue;

        public PlayerStatsConfigSO StatsConfig => StatsConfigValue;

        public ShooterMoveConfigSO MoveConfig => MoveConfigValue;

        public ShooterAimConfigSO AimConfig => AimConfigValue;

        public ShooterCrouchConfigSO CrouchConfig => CrouchConfigValue;

        public ShooterEvadeConfigSO EvadeConfig => EvadeConfigValue;

        public ShooterDamageConfigSO DamageConfig => DamageConfigValue;

        public ShooterShootConfigSO ShootConfig => ShootConfigValue;

        public PlayerViewConfigSO ViewConfig => ViewConfigValue;

        public PlayerAudioConfigSO AudioConfig => AudioConfigValue;
    }
}
