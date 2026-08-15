/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人追击行为，持续通过导航路径接近当前目标
 * │  类    名: EnemyChaseAction.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Tasks;
using CleverCrow.Fluid.BTs.Tasks.Actions;
using Module.Enemy.Context;
using Module.Navigation.Core;
using UnityEngine;

namespace Module.Enemy.Behavior.Actions.Chase
{
    public sealed class EnemyChaseAction : ActionBase
    {
        private readonly EnemyContext m_context;
        private readonly INavigationController m_navigationController;
        private readonly float m_pathRefreshInterval;

        private float m_pathRefreshRemainingTime;

        // 创建敌人追击行为节点并缓存路径刷新间隔
        public EnemyChaseAction(EnemyContext context, INavigationController navigationController)
        {
            m_context = context;
            m_navigationController = navigationController;
            m_pathRefreshInterval = context.BehaviorConfig.PathRefreshIntervalValue;
        }

        // 进入追击时立即请求一条通向目标的路径
        protected override void OnStart()
        {
            m_pathRefreshRemainingTime = 0f;
            RefreshPath();
        }

        // 持续刷新目标路径并沿当前路径移动
        protected override TaskStatus OnUpdate()
        {
            Transform target = m_context.Target.CurrentTarget;
            if (target == null)
            {
                m_navigationController.Reset();
                m_context.Movement.StopMove();
                return TaskStatus.Failure;
            }

            m_pathRefreshRemainingTime -= Time.deltaTime;
            if (m_pathRefreshRemainingTime <= 0f)
                RefreshPath();

            if (m_navigationController.HasFailed || !m_navigationController.HasPath)
            {
                m_context.Movement.StopMove();
                return TaskStatus.Continue;
            }

            m_navigationController.Tick(m_context.Transform.position);
            if (m_navigationController.HasReachedDestination)
            {
                m_context.Movement.StopMove();
                return TaskStatus.Continue;
            }

            Vector3 moveDirection = m_navigationController.NextPosition - m_context.Transform.position;
            m_context.Movement.SetMoveDirection(moveDirection);
            m_context.Movement.SetLookDirection(moveDirection);
            return TaskStatus.Continue;
        }

        // 中断追击时清理路径和移动请求
        protected override void OnExit()
        {
            m_navigationController.Reset();
            m_context.Movement.StopMove();
        }

        // 使用目标当前位置重新请求追击路径
        private void RefreshPath()
        {
            Transform target = m_context.Target.CurrentTarget;
            if (target == null)
                return;

            m_navigationController.SetDestination(m_context.Transform.position, target.position);
            m_pathRefreshRemainingTime = m_pathRefreshInterval;
        }
    }
}

