/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家视角控制器，负责视角模式与水平垂直视角更新
 * │  类    名: PlayerViewController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.View;
using Module.Player.Context;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core.View
{
    public sealed class PlayerViewController
    {
        private PlayerContext m_context;
        private PlayerViewConfigSO m_viewConfig;

        // 初始化玩家视角控制器
        public void Init(PlayerContext context, PlayerViewConfigSO viewConfig)
        {
            if (context == null || viewConfig == null)
            {
                QLog.Error("玩家视角初始化失败：Context 或 ViewConfig 为空");
                return;
            }

            m_context = context;
            m_viewConfig = viewConfig;
            m_context.ViewMode = m_viewConfig.DefaultViewMode;
            m_context.CameraYaw = m_context.Transform.eulerAngles.y;
            m_context.CameraPitch = 0f;
        }

        // 更新玩家视角数据
        public void Tick(float deltaTime)
        {
            if (m_context == null || m_viewConfig == null)
                return;

            Vector2 lookInput = m_context.LookInput;
            updateViewAngles(lookInput, deltaTime);

            if (m_context.ViewMode == PlayerViewMode.FirstPerson)
                rotateBodyByLookYaw();
        }

        // 切换玩家视角模式
        public void SetViewMode(PlayerViewMode viewMode)
        {
            if (m_context == null)
                return;

            m_context.ViewMode = viewMode;
            m_context.CameraYaw = m_context.Transform.eulerAngles.y;
        }

        // 根据输入更新视角水平角与俯仰角
        private void updateViewAngles(Vector2 lookInput, float deltaTime)
        {
            float yawSpeed = m_context.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.FirstPersonYawSpeed
                : m_viewConfig.ThirdPersonYawSpeed;
            float pitchSpeed = m_context.ViewMode == PlayerViewMode.FirstPerson
                ? m_viewConfig.FirstPersonPitchSpeed
                : m_viewConfig.ThirdPersonPitchSpeed;

            m_context.CameraYaw += lookInput.x * yawSpeed * deltaTime;
            m_context.CameraPitch -= lookInput.y * pitchSpeed * deltaTime;
            m_context.CameraPitch = Mathf.Clamp(m_context.CameraPitch, m_viewConfig.PitchMin, m_viewConfig.PitchMax);
        }

        // 第一人称模式下使用视角水平角驱动身体朝向
        private void rotateBodyByLookYaw()
        {
            m_context.Transform.rotation = Quaternion.Euler(0f, m_context.CameraYaw, 0f);
        }
    }
}
