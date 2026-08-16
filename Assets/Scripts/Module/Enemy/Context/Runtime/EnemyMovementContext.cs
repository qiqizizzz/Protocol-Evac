/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 敌人移动上下文，保存导航方向与移动表现事实
 * │  类    名: EnemyMovementContext.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Enemy.Context.Runtime
{
    public sealed class EnemyMovementContext
    {
        public Vector3 MoveDirection { get; private set; }
        public Vector3 LookDirection { get; private set; }
        public bool HasMoveRequest { get; private set; }
        public bool HasLookRequest { get; private set; }
        public bool IsMoving { get; private set; }
        public Vector3 ForcedMoveVelocity { get; private set; }
        public bool HasForcedMoveVelocity { get; private set; }
        public float ForcedMoveRemainingTime { get; private set; }
        public float PendingVerticalLaunchSpeed { get; private set; }
        public bool HasPendingVerticalLaunch { get; private set; }

        // 提交本帧导航移动方向
        public void SetMoveDirection(Vector3 moveDirection)
        {
            moveDirection.y = 0f;
            MoveDirection = moveDirection.normalized;
            HasMoveRequest = MoveDirection.sqrMagnitude > 0f;
        }

        // 提交本帧身体朝向方向
        public void SetLookDirection(Vector3 lookDirection)
        {
            lookDirection.y = 0f;
            LookDirection = lookDirection.normalized;
            HasLookRequest = LookDirection.sqrMagnitude > 0f;
        }

        // 更新移动执行后的表现事实
        public void SetMoving(bool isMoving)
        {
            IsMoving = isMoving;
        }

        // 写入本次受击的强制位移与竖直起飞请求
        public void SetForcedMove(Vector3 horizontalVelocity, float duration, float verticalLaunchSpeed)
        {
            horizontalVelocity.y = 0f;
            ForcedMoveVelocity = horizontalVelocity;
            ForcedMoveRemainingTime = Mathf.Max(0f, duration);
            HasForcedMoveVelocity = ForcedMoveVelocity.sqrMagnitude > 0f && ForcedMoveRemainingTime > 0f;
            PendingVerticalLaunchSpeed = Mathf.Max(0f, verticalLaunchSpeed);
            HasPendingVerticalLaunch = PendingVerticalLaunchSpeed > 0f;
        }

        // 消费一次竖直起飞请求
        public bool TryConsumeVerticalLaunch(out float verticalLaunchSpeed)
        {
            verticalLaunchSpeed = PendingVerticalLaunchSpeed;
            if (!HasPendingVerticalLaunch)
                return false;

            PendingVerticalLaunchSpeed = 0f;
            HasPendingVerticalLaunch = false;
            return true;
        }

        // 推进强制水平位移的剩余时间
        public void TickForcedMove(float deltaTime)
        {
            if (!HasForcedMoveVelocity)
                return;

            ForcedMoveRemainingTime = Mathf.Max(0f, ForcedMoveRemainingTime - deltaTime);
            if (ForcedMoveRemainingTime > 0f)
                return;

            ForcedMoveVelocity = Vector3.zero;
            HasForcedMoveVelocity = false;
        }

        // 停止导航移动并保留受击产生的强制位移
        public void StopNavigationMove()
        {
            MoveDirection = Vector3.zero;
            HasMoveRequest = false;
            IsMoving = false;
        }

        // 停止导航移动并保留当前朝向请求
        public void StopMove()
        {
            StopNavigationMove();
            ForcedMoveVelocity = Vector3.zero;
            HasForcedMoveVelocity = false;
            ForcedMoveRemainingTime = 0f;
            PendingVerticalLaunchSpeed = 0f;
            HasPendingVerticalLaunch = false;
        }

        // 清理全部移动运行时事实
        public void Reset()
        {
            MoveDirection = Vector3.zero;
            LookDirection = Vector3.zero;
            HasMoveRequest = false;
            HasLookRequest = false;
            IsMoving = false;
        }
    }
}
