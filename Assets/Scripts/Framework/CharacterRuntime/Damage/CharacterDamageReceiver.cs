/*
 * ┌────────────────────────────────────────────────────────────┐
 * │  描    述: 角色伤害回调接收器，将通用伤害事件转交给角色模块
 * │  类    名: CharacterDamageReceiver.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────────────┘
 */

using System;
using Module.Combat.Damage;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Framework.Character.Runtime.Damage
{
    [MovedFrom(
        true,
        sourceNamespace: "Module.ChaisakiPlayer.Damage",
        sourceAssembly: "Player",
        sourceClassName: "PlayerDamageReceiver")]
    public sealed class CharacterDamageReceiver : MonoBehaviour, IDamageable
    {
        public event Action<DamageData> OnDamageReceived;

        /// <summary>
        /// 接收通用 Combat 模块提交的一次伤害
        /// </summary>
        /// <param name="damageData">本次命中的伤害数据</param>
        public void TakeDamage(DamageData damageData)
        {
            OnDamageReceived?.Invoke(damageData);
        }
    }
}
