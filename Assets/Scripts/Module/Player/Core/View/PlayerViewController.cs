/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家视角控制器，负责视角模式与水平垂直视角更新
 * │  类    名: PlayerViewController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View.Config;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core.View
{
    public sealed class PlayerViewController
    {
        private PlayerContext m_context;
        private PlayerViewConfigSO m_viewConfig;
        private Transform m_viewRoot;
        private Camera m_playerCamera;

        // 初始化玩家视角控制器
        public void Init(PlayerContext context, PlayerViewConfigSO viewConfig, Transform viewRoot, Camera playerCamera)
        {
            m_context = context;
            m_viewConfig = viewConfig;
            m_viewRoot = viewRoot;
            m_playerCamera = playerCamera;
            m_context.ViewMode = m_viewConfig.DefaultViewMode;
            m_context.CameraYaw = m_context.Transform.eulerAngles.y;
            m_context.CameraPitch = 0f;
            refreshCameraTransform();
        }

        // 更新玩家视角数据
        public void Tick(float deltaTime)
        {
            if (m_context == null)
                return;

            switchPlayerView();
            updateViewAngles(deltaTime);
            refreshCameraTransform();
        }
        
        // 处理玩家视角模式切换请求
        private void switchPlayerView()
        {
            if (!m_context.TargetViewMode.HasValue)
                return;

            //切换视角
            m_context.ViewMode = m_context.TargetViewMode.Value;
            m_context.CameraYaw = m_context.Transform.eulerAngles.y;
            refreshCameraTransform();
            m_context.TargetViewMode = null;//置空
        }

        // 根据输入更新视角水平角与俯仰角
        private void updateViewAngles(float deltaTime)
        {
            Vector2 lookInput = m_context.LookInput;
            
            float yawSpeed = m_context.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.FirstPersonYawSpeed
                : m_viewConfig.ThirdPersonYawSpeed;
            float pitchSpeed = m_context.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.FirstPersonPitchSpeed
                : m_viewConfig.ThirdPersonPitchSpeed;

            m_context.CameraYaw += lookInput.x * yawSpeed * deltaTime;
            m_context.CameraPitch -= lookInput.y * pitchSpeed * deltaTime;
            m_context.CameraPitch = Mathf.Clamp(m_context.CameraPitch, m_viewConfig.PitchMin, m_viewConfig.PitchMax);
            
            //第一人称模式下使用视角水平角驱动身体朝向
            if (m_context.ViewMode == PlayerViewMode.FirstPerson)
                m_context.Transform.rotation = Quaternion.Euler(0f, m_context.CameraYaw, 0f);
        }
        
        // 根据当前视角模式刷新相机枢轴与相机本地位置
        private void refreshCameraTransform()
        {
            m_viewRoot.position = m_context.Transform.position;

            if (m_context.ViewMode == PlayerViewMode.FirstPerson)
            {
                m_viewRoot.rotation = Quaternion.Euler(0f, m_context.CameraYaw, 0f);
                m_playerCamera.transform.localPosition = m_viewConfig.FirstPersonCameraLocalPosition;
                m_playerCamera.transform.localRotation = Quaternion.Euler(m_context.CameraPitch, 0f, 0f);
                return;
            }

            Quaternion viewRotation = Quaternion.Euler(m_context.CameraPitch, m_context.CameraYaw, 0f);
            m_viewRoot.rotation = viewRotation;
            m_playerCamera.transform.localPosition = m_viewConfig.ThirdPersonCameraLocalPosition;
            m_playerCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
