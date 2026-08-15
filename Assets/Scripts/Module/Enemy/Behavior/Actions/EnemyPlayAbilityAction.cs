/*
 * ┌─────────────────────────────────────────────────────────┐
 * │  描    述: 敌人能力行为节点，负责打开、等待与中断能力
 * │  类    名: EnemyPlayAbilityAction.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Ability;
using Module.Enemy.Ability.Core;
using Module.Enemy.Context.Runtime;

namespace Module.Enemy.Behavior.Actions
{
    public sealed class EnemyPlayAbilityAction : ActionBase
    {
        private readonly EnemyAbilityController m_abilityController;
        private readonly EnemyActionContext m_actionContext;

        private bool m_hasOpenedAbility;

        // 创建敌人能力行为节点
        public EnemyPlayAbilityAction(EnemyAbilityController abilityController, EnemyActionContext actionContext)
        {
            m_abilityController = abilityController;
            m_actionContext = actionContext;
        }

        // 消费能力请求并尝试打开能力
        protected override void OnStart()
        {
            m_hasOpenedAbility = m_actionContext.TryConsumeAbilityRequest(out EnemyAbilityType abilityType)
                && m_abilityController.TryOpen(abilityType);
        }

        // 等待能力时间轴完成
        protected override TaskStatus OnUpdate()
        {
            if (!m_hasOpenedAbility)
                return TaskStatus.Failure;

            m_abilityController.RequestNextStep();
            if (m_abilityController.IsRunning)
                return TaskStatus.Continue;

            return m_abilityController.IsFinished ? TaskStatus.Success : TaskStatus.Failure;
        }

        // 中断节点时关闭仍在运行的能力
        protected override void OnExit()
        {
            if (m_hasOpenedAbility && m_abilityController.IsRunning)
                m_abilityController.Close();

            m_hasOpenedAbility = false;
        }
    }
}

