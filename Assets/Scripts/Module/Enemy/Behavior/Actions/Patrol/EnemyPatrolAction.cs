/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人巡逻行为，在出生区域内选择并跟随导航路径
 * │  类    名: EnemyPatrolAction.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Context;
using Module.Navigation.Core;
using UnityEngine;

namespace Module.Enemy.Behavior.Actions.Patrol
{
    public sealed class EnemyPatrolAction : ActionBase
    {
        private readonly EnemyContext m_context;
        private readonly INavigationController m_navigationController;
        private readonly Vector3 m_patrolCenter;
        private readonly float m_patrolRadius;
        private readonly float m_patrolWaitDuration;
        private readonly float m_retryInterval;

        private float m_waitRemainingTime;
        private float m_retryRemainingTime;

        // 创建敌人巡逻行为节点并固定出生区域中心
        public EnemyPatrolAction(EnemyContext context, INavigationController navigationController)
        {
            m_context = context;
            m_navigationController = navigationController;
            m_patrolCenter = context.Transform.position;
            m_patrolRadius = context.BehaviorConfig.PatrolRadiusValue;
            m_patrolWaitDuration = context.BehaviorConfig.PatrolWaitDurationValue;
            m_retryInterval = context.BehaviorConfig.PathRefreshIntervalValue;
        }

        // 进入巡逻时准备第一个随机目的地
        protected override void OnStart()
        {
            m_waitRemainingTime = 0f;
            m_retryRemainingTime = 0f;
            TrySelectDestination();
        }

        // 跟随当前路径，并在抵达后等待再选择新目的地
        protected override TaskStatus OnUpdate()
        {
            if (m_waitRemainingTime > 0f)
            {
                m_waitRemainingTime -= Time.deltaTime;
                m_context.Movement.StopMove();
                if (m_waitRemainingTime <= 0f)
                    TrySelectDestination();

                return TaskStatus.Continue;
            }

            if (m_navigationController.HasFailed || !m_navigationController.HasPath)
            {
                m_context.Movement.StopMove();
                m_retryRemainingTime -= Time.deltaTime;
                if (m_retryRemainingTime <= 0f)
                    TrySelectDestination();

                return TaskStatus.Continue;
            }

            m_navigationController.Tick(m_context.Transform.position);
            if (m_navigationController.HasReachedDestination)
            {
                m_context.Movement.StopMove();
                m_waitRemainingTime = m_patrolWaitDuration;
                return TaskStatus.Continue;
            }

            Vector3 moveDirection = m_navigationController.NextPosition - m_context.Transform.position;
            m_context.Movement.SetMoveDirection(moveDirection);
            m_context.Movement.SetLookDirection(moveDirection);
            return TaskStatus.Continue;
        }

        // 中断巡逻时清理路径和移动请求
        protected override void OnExit()
        {
            m_navigationController.Reset();
            m_context.Movement.StopMove();
        }

        // 尝试选择新的随机巡逻目的地并请求路径
        private void TrySelectDestination()
        {
            m_navigationController.Reset();
            if (m_navigationController.TryGetRandomDestination(m_patrolCenter, m_patrolRadius,
                    out Vector3 destination))
            {
                m_navigationController.SetDestination(m_context.Transform.position, destination);
            }

            m_retryRemainingTime = m_retryInterval;
        }
    }
}

