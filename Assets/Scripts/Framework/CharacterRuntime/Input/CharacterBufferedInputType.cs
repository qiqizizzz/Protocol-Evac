/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色可缓存输入类型枚举
 * │  类    名: CharacterBufferedInputType.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine.Scripting.APIUpdating;

namespace Framework.Character.Runtime.Input
{
    [MovedFrom(
        true,
        sourceNamespace: "Module.ChaisakiPlayer.Input.Buffer",
        sourceAssembly: "Player",
        sourceClassName: "PlayerBufferedInputType")]
    public enum CharacterBufferedInputType
    {
        Jump,
        NormalAttack,
        SpecialSkill,
        Ultimate,
        Dodge,
    }
}
