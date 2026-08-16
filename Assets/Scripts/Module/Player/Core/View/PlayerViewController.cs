/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家视角控制器，负责视角模式与水平垂直视角更新
 * │  类    名: PlayerViewController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.Common.Defines;
using Framework.QTower.Controller;
using Framework.QTower.Event;
using Module.Player.Context;
using Module.Player.Core.View.Config;
using UnityEngine;

namespace Module.Player.Core.View
{
    public sealed class PlayerViewController : BaseController
    {
        private const string ENEMY_TAG = "Enemy";
        private const float MIN_CAMERA_CAST_DISTANCE = 0.0001f;

        private readonly PlayerContext m_context;
        private readonly PlayerViewConfigSO m_viewConfig;
        private readonly Transform m_viewRoot;
        private readonly Camera m_playerCamera;
        private readonly int m_defaultCameraCullingMask;
        private readonly int m_playerLayerMask;
        private readonly float m_defaultCameraNearClipPlane;

        // 创建玩家视角控制器
        public PlayerViewController(PlayerContext context, PlayerViewConfigSO viewConfig, Transform viewRoot, Camera playerCamera)
        {
            m_context = context;
            m_viewConfig = viewConfig;
            m_viewRoot = viewRoot;
            m_playerCamera = playerCamera;
            m_defaultCameraCullingMask = playerCamera.cullingMask;
            m_playerLayerMask = LayerMask.GetMask("Player");
            m_defaultCameraNearClipPlane = playerCamera.nearClipPlane;
        }

        // 初始化玩家视角控制器
        protected override void OnInit()
        {
            m_context.View.ViewMode = m_viewConfig.DefaultViewMode;
            m_context.View.CameraYaw = m_context.Transform.eulerAngles.y;
            m_context.View.CameraPitch = 0f;
            RefreshCameraTransform();
        }

        // 更新玩家视角数据
        public override void Tick(float deltaTime)
        {
            if (m_context.Damage.IsDead)
            {
                ClearLockTarget();
                m_context.Input.LookInput = Vector2.zero;
                RefreshCameraTransform();
                return;
            }

            SwitchPlayerView();
            RefreshLockTarget();
            HandleLockOnToggleRequest();
            UpdateViewAngles(deltaTime);
            RefreshCameraTransform();
        }
        
        // 处理玩家视角模式切换请求
        private void SwitchPlayerView()
        {
            if (!m_context.View.TargetViewMode.HasValue)
                return;

            //切换视角
            m_context.View.ViewMode = m_context.View.TargetViewMode.Value;
            m_context.View.CameraYaw = m_context.Transform.eulerAngles.y;
            if (m_context.View.ViewMode == PlayerViewMode.FirstPerson)
                ClearLockTarget();
            RefreshCameraTransform();
            m_context.View.TargetViewMode = null;//置空
        }

        // 刷新锁定目标有效性并处理自动解除锁定
        private void RefreshLockTarget()
        {
            if (!m_context.View.IsLockOn)
                return;

            if (m_context.View.ViewMode != PlayerViewMode.ThirdPerson || !IsLockTargetValid(m_context.View.LockTarget))
                ClearLockTarget();
        }

        // 消费锁定输入并在锁定与解除锁定之间切换
        private void HandleLockOnToggleRequest()
        {
            if (!m_context.Input.ConsumeLockOnToggleRequest())
                return;

            if (m_context.View.IsLockOn)
            {
                ClearLockTarget();
                return;
            }

            if (m_context.View.ViewMode != PlayerViewMode.ThirdPerson)
                return;

            Transform closestEnemyTarget = FindClosestEnemyTarget(m_viewConfig.LockRange);
            if (closestEnemyTarget != null)
                SetLockTarget(closestEnemyTarget);
        }

        // 设置锁定目标并通知全局 UI
        private void SetLockTarget(Transform target)
        {
            m_context.View.SetLockTarget(target);
            EventManager.PublishEvent(EventDefines.PlayerLockOnStateChanged, true);
        }

        // 清空锁定目标并通知全局 UI
        private void ClearLockTarget()
        {
            if (!m_context.View.IsLockOn)
                return;

            m_context.View.ClearLockTarget();
            EventManager.PublishEvent(EventDefines.PlayerLockOnStateChanged, false);
        }

        // 搜索指定范围内距离玩家最近的 Enemy Tag 目标
        private Transform FindClosestEnemyTarget(float range)
        {
            GameObject[] enemyObjects = GameObject.FindGameObjectsWithTag(ENEMY_TAG);
            Transform closestTarget = null;
            float closestSqrDistance = range * range;

            foreach (GameObject enemyObject in enemyObjects)
            {
                float sqrDistance = GetHorizontalSqrDistance(enemyObject.transform.position);
                if (sqrDistance > closestSqrDistance)
                    continue;

                closestTarget = enemyObject.transform;
                closestSqrDistance = sqrDistance;
            }

            return closestTarget;
        }

        // 判断当前锁定目标是否仍可继续锁定
        private bool IsLockTargetValid(Transform lockTarget)
        {
            if (lockTarget == null || !lockTarget.gameObject.activeInHierarchy)
                return false;

            float lockReleaseRangeSqr = m_viewConfig.LockReleaseRange * m_viewConfig.LockReleaseRange;
            return GetHorizontalSqrDistance(lockTarget.position) <= lockReleaseRangeSqr;
        }

        // 获取玩家与目标位置的水平距离平方
        private float GetHorizontalSqrDistance(Vector3 targetPosition)
        {
            Vector3 direction = targetPosition - m_context.Transform.position;
            direction.y = 0f;
            return direction.sqrMagnitude;
        }

        // 根据输入更新视角水平角与俯仰角
        private void UpdateViewAngles(float deltaTime)
        {
            Vector2 lookInput = m_context.Input.LookInput;
            
            float yawSpeed = m_context.View.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.FirstPersonYawSpeed
                : m_viewConfig.ThirdPersonYawSpeed;
            float pitchSpeed = m_context.View.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.FirstPersonPitchSpeed
                : m_viewConfig.ThirdPersonPitchSpeed;

            if (m_context.View.IsLockOn)
                FollowLockTargetYaw(deltaTime);
            else
                m_context.View.CameraYaw += lookInput.x * yawSpeed * deltaTime;
            m_context.View.CameraPitch -= lookInput.y * pitchSpeed * deltaTime;
            float pitchMax = m_context.View.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.PitchMax
                : m_viewConfig.ThirdPersonPitchMax;
            m_context.View.CameraPitch = Mathf.Clamp(m_context.View.CameraPitch, m_viewConfig.PitchMin, pitchMax);
            
            //第一人称模式下使用视角水平角驱动身体朝向
            if (m_context.View.ViewMode == PlayerViewMode.FirstPerson)
                m_context.Transform.rotation = Quaternion.Euler(0f, m_context.View.CameraYaw, 0f);
        }

        // 锁定时平滑修正相机水平朝向目标
        private void FollowLockTargetYaw(float deltaTime)
        {
            Vector3 targetDirection = m_context.View.LockTarget.position - m_context.Transform.position;
            targetDirection.y = 0f;
            if (targetDirection.sqrMagnitude <= 0.0001f)
                return;

            float targetYaw = Quaternion.LookRotation(targetDirection.normalized).eulerAngles.y;
            m_context.View.CameraYaw = Mathf.MoveTowardsAngle(
                m_context.View.CameraYaw,
                targetYaw,
                m_viewConfig.LockCameraYawSpeed * deltaTime);
        }
        
        // 根据当前视角模式刷新相机枢轴与相机本地位置
        private void RefreshCameraTransform()
        {
            m_viewRoot.position = m_context.Transform.position;

            if (m_context.View.ViewMode == PlayerViewMode.FirstPerson)
            {
                m_viewRoot.rotation = Quaternion.Euler(0f, m_context.View.CameraYaw, 0f);
                m_playerCamera.cullingMask = m_defaultCameraCullingMask & ~m_playerLayerMask;
                m_playerCamera.nearClipPlane = m_viewConfig.FirstPersonCameraNearClipPlane;
                m_playerCamera.transform.localPosition = ResolveFirstPersonCameraLocalPosition();
                m_playerCamera.transform.localRotation = Quaternion.Euler(m_context.View.CameraPitch, 0f, 0f);
                return;
            }

            Quaternion viewRotation = Quaternion.Euler(m_context.View.CameraPitch, m_context.View.CameraYaw, 0f);
            m_viewRoot.rotation = viewRotation;
            m_playerCamera.cullingMask = m_defaultCameraCullingMask;
            m_playerCamera.nearClipPlane = m_defaultCameraNearClipPlane;
            m_playerCamera.transform.localPosition = ResolveThirdPersonCameraLocalPosition();
            m_playerCamera.transform.localRotation = Quaternion.identity;
        }

        // 计算第一人称镜头在建筑前的安全本地位置
        private Vector3 ResolveFirstPersonCameraLocalPosition()
        {
            Vector3 requestedLocalPosition = m_viewConfig.FirstPersonCameraLocalPosition;
            Vector3 headLocalPosition = new Vector3(0f, requestedLocalPosition.y, 0f);
            Vector3 horizontalOffset = requestedLocalPosition - headLocalPosition;
            float castDistance = horizontalOffset.magnitude;
            if (castDistance <= MIN_CAMERA_CAST_DISTANCE)
                return requestedLocalPosition;

            Vector3 castOrigin = m_viewRoot.TransformPoint(headLocalPosition);
            Vector3 castDirection = m_viewRoot.TransformDirection(horizontalOffset / castDistance);
            Vector3 requestedWorldPosition = m_viewRoot.TransformPoint(requestedLocalPosition);
            if (IsCameraPositionBlocked(requestedWorldPosition, m_viewConfig.FirstPersonCameraCollisionRadius))
                return headLocalPosition;

            if (!Physics.SphereCast(castOrigin, m_viewConfig.FirstPersonCameraCollisionRadius, castDirection,
                    out RaycastHit hit, castDistance + m_viewConfig.CameraCollisionPadding,
                    m_viewConfig.EnvironmentLayerMask, QueryTriggerInteraction.Ignore))
                return requestedLocalPosition;

            float safeDistance = Mathf.Max(0f, hit.distance - m_viewConfig.CameraCollisionPadding);
            Vector3 safeWorldPosition = castOrigin + castDirection * safeDistance;
            return m_viewRoot.InverseTransformPoint(safeWorldPosition);
        }

        // 计算第三人称镜头在建筑前的安全本地位置
        private Vector3 ResolveThirdPersonCameraLocalPosition()
        {
            Vector3 requestedLocalPosition = m_viewConfig.ThirdPersonCameraLocalPosition;
            Vector3 pivotLocalPosition = new Vector3(0f, requestedLocalPosition.y, 0f);
            Vector3 cameraOffsetLocalPosition = requestedLocalPosition - pivotLocalPosition;
            float castDistance = cameraOffsetLocalPosition.magnitude;
            if (castDistance <= MIN_CAMERA_CAST_DISTANCE)
                return requestedLocalPosition;

            Vector3 castOrigin = m_viewRoot.TransformPoint(pivotLocalPosition);
            Vector3 castDirection = m_viewRoot.TransformDirection(cameraOffsetLocalPosition / castDistance);

            if (!Physics.SphereCast(castOrigin, m_viewConfig.ThirdPersonCameraCollisionRadius, castDirection,
                    out RaycastHit hit, castDistance + m_viewConfig.CameraCollisionPadding,
                    m_viewConfig.EnvironmentLayerMask, QueryTriggerInteraction.Ignore))
                return requestedLocalPosition;

            float safeDistance = Mathf.Max(0f, hit.distance - m_viewConfig.CameraCollisionPadding);
            Vector3 safeWorldPosition = castOrigin + castDirection * safeDistance;
            return m_viewRoot.InverseTransformPoint(safeWorldPosition);
        }

        // 判断期望镜头位置是否已与环境碰撞体重叠
        private bool IsCameraPositionBlocked(Vector3 position, float radius)
        {
            return Physics.CheckSphere(position, radius, m_viewConfig.EnvironmentLayerMask, QueryTriggerInteraction.Ignore);
        }
    }
}
