/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人归位行为，通过导航路径返回出生位置
 * │  类    名: EnemyReturnToSpawnAction.cs
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
    public sealed class EnemyReturnToSpawnAction : ActionBase
    {
        private readonly EnemyContext m_context;
        private readonly INavigationController m_navigationController;
        private readonly float m_retryInterval;

        private float m_retryRemainingTime;

        // 创建敌人归位行为节点并缓存导航重试间隔
        public EnemyReturnToSpawnAction(EnemyContext context, INavigationController navigationController)
        {
            m_context = context;
            m_navigationController = navigationController;
            m_retryInterval = context.BehaviorConfig.PathRefreshIntervalValue;
        }

        // 进入归位时立即请求出生点路径
        protected override void OnStart()
        {
            m_retryRemainingTime = 0f;
            RequestPath();
        }

        // 沿归位路径移动，并在失败时按间隔重试
        protected override TaskStatus OnUpdate()
        {
            if (m_navigationController.HasFailed || !m_navigationController.HasPath)
            {
                m_context.Movement.StopMove();
                m_retryRemainingTime -= Time.deltaTime;
                if (m_retryRemainingTime <= 0f)
                    RequestPath();

                return TaskStatus.Continue;
            }

            m_navigationController.Tick(m_context.Transform.position);
            if (m_navigationController.HasReachedDestination)
            {
                m_context.Movement.StopMove();
                return TaskStatus.Success;
            }

            Vector3 moveDirection = m_navigationController.NextPosition - m_context.Transform.position;
            m_context.Movement.SetMoveDirection(moveDirection);
            m_context.Movement.SetLookDirection(moveDirection);
            return TaskStatus.Continue;
        }

        // 中断归位时清理路径和移动请求
        protected override void OnExit()
        {
            m_navigationController.Reset();
            m_context.Movement.StopMove();
        }

        // 向出生位置请求新的导航路径
        private void RequestPath()
        {
            m_navigationController.SetDestination(m_context.Transform.position, m_context.SpawnPosition);
            m_retryRemainingTime = m_retryInterval;
        }
    }
}

