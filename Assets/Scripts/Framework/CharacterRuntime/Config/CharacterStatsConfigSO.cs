/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色固有数值配置资产
 * │  类    名: CharacterStatsConfigSO.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Framework.Character.Runtime.Config
{
    [MovedFrom(
        true,
        sourceNamespace: "Module.ChaisakiPlayer.Config",
        sourceAssembly: "Player",
        sourceClassName: "PlayerStatsConfigSO")]
    [CreateAssetMenu(fileName = "CharacterStatsConfig", menuName = "配置/角色/数值/角色数值配置")]
    public sealed class CharacterStatsConfigSO : ScriptableObject
    {
        [Tooltip("角色的最大生命值")]
        [SerializeField, Min(1f)] private float MaxHealth;

        public float MaxHealthValue => MaxHealth;
    }
}
