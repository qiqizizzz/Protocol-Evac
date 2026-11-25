/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色视角模式枚举
 * │  类    名: CharacterViewMode.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine.Scripting.APIUpdating;

namespace Framework.Character.Runtime.Core.View
{
    [MovedFrom(
        true,
        sourceNamespace: "Module.ChaisakiPlayer.Core.View",
        sourceAssembly: "Player",
        sourceClassName: "PlayerViewMode")]
    public enum CharacterViewMode
    {
        FirstPerson, //第一人称
        ThirdPerson  //第三人称
    }
}
