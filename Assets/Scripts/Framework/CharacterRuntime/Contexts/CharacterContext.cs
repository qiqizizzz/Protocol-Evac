/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色运行时上下文入口，组合各领域子上下文
 * │  类    名: CharacterContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

using System;
using Framework.Character.Runtime.Skill;
using UnityEngine;

namespace Framework.Character.Runtime.Contexts
{
    public abstract class CharacterContext<TStateId, THurtAnimationId, TActionContext, TMovementContext> :
        ICharacterViewRuntimeContext, ICharacterSkillContext<TStateId>
        where TStateId : struct, Enum
        where THurtAnimationId : struct, Enum
        where TActionContext : CharacterActionContext<TStateId>, new()
        where TMovementContext : CharacterMovementContext, new()
    {
        public Transform Transform { get; }
        public CharacterInputContext Input { get; }
        public TMovementContext Movement { get; }
        public CharacterViewContext View { get; }
        public TActionContext Action { get; }
        public CharacterDamageContext<THurtAnimationId> Damage { get; }

        CharacterActionContext<TStateId> ICharacterSkillContext<TStateId>.Action => Action;
        ICharacterDamageContext ICharacterViewRuntimeContext.Damage => Damage;

        // 创建角色运行时上下文入口
        protected CharacterContext(Transform transform)
        {
            Transform = transform;
            Input = new CharacterInputContext();
            Movement = new TMovementContext();
            View = new CharacterViewContext();
            Action = new TActionContext();
            Damage = new CharacterDamageContext<THurtAnimationId>();
            ResetRunTimeData();
        }

        // 重置全部角色运行时数据
        public void ResetRunTimeData()
        {
            Input.Reset();
            Movement.Reset();
            View.Reset();
            View.CameraYaw = Transform.eulerAngles.y;
            Action.Reset();
            Damage.Reset();
        }
    }
}
