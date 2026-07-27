/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家移动方向解析器，负责按视角生成水平移动方向
 * │  类    名: PlayerMoveDirectionResolver.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View;
using UnityEngine;

namespace Module.Player.Core
{
    public static class PlayerMoveDirectionResolver
    {
        // 根据当前视角模式计算玩家移动方向
        public static Vector3 Resolve(PlayerContext context, Vector2 moveInput)
        {
            if (context.ViewMode == PlayerViewMode.FirstPerson)
                return buildMoveDirection(context.Transform.forward, context.Transform.right, moveInput);

            Quaternion cameraYawRotation = Quaternion.Euler(0f, context.CameraYaw, 0f);
            Vector3 cameraForward = cameraYawRotation * Vector3.forward;
            Vector3 cameraRight = cameraYawRotation * Vector3.right;

            return buildMoveDirection(cameraForward, cameraRight, moveInput);
        }

        // 获取当前视角下的水平前方向
        public static Vector3 ResolveForward(PlayerContext context)
        {
            if (context.ViewMode == PlayerViewMode.FirstPerson)
                return flattenDirection(context.Transform.forward);

            Quaternion cameraYawRotation = Quaternion.Euler(0f, context.CameraYaw, 0f);
            return flattenDirection(cameraYawRotation * Vector3.forward);
        }

        // 使用水平前方向与右方向生成移动方向
        private static Vector3 buildMoveDirection(Vector3 forward, Vector3 right, Vector2 moveInput)
        {
            forward = flattenDirection(forward);
            right = flattenDirection(right);

            Vector3 moveDirection = right * moveInput.x + forward * moveInput.y;
            return moveDirection.sqrMagnitude > 1f ? moveDirection.normalized : moveDirection;
        }

        // 将方向压平到水平面
        private static Vector3 flattenDirection(Vector3 direction)
        {
            direction.y = 0f;

            if (direction.sqrMagnitude <= 0.0001f)
                return Vector3.forward;

            return direction.normalized;
        }
    }
}
