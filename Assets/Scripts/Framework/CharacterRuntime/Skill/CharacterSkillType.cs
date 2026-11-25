/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色技能类型枚举
 * │  类    名: CharacterSkillType.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine.Scripting.APIUpdating;

namespace Framework.Character.Runtime.Skill
{
    [MovedFrom(
        true,
        sourceNamespace: "Module.ChaisakiPlayer.Skill",
        sourceAssembly: "Player",
        sourceClassName: "PlayerSkillType")]
    public enum CharacterSkillType
    {
        NormalAttack, //普攻
        SpecialSkill, //特殊技能
        Ultimate      //大招
    }
}

