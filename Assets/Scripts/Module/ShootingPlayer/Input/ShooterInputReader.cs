/*
* ┌──────────────────────────────────┐
* │  描    述: 射击角色输入读取器，只启用 Shooter Action Map 并写入运行时输入
* │  类    名: ShooterInputReader.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using Module.ShootingPlayer.Context;
using Framework.Character.Runtime.Input;
using Module.ShootingPlayer.HFSM.Runtime;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Module.ShootingPlayer.Input
{
    // 射击角色输入读取器，与千咲输入读取器完全独立
    public sealed class ShooterInputReader
    {
        private ShootingPlayerInputActions m_inputActions;
        private ShooterContext m_context;
        private ShooterRuntimeState m_runtimeState;
        private bool m_isInited;

        // 初始化输入读取器，只启用 Shooter Action Map
        public void Init(ShooterContext context, ShooterRuntimeState runtimeState)
        {
            m_context = context;
            m_runtimeState = runtimeState;
            m_inputActions = new ShootingPlayerInputActions();
            RegisterDiscreteInputCallbacks();

            // 只启用 Shooter map，避免抢占 UI map 的右键与 R 键绑定
            m_inputActions.Shooter.Enable();
            m_isInited = true;
        }

        // 释放输入读取器
        public void UnInit()
        {
            if (!m_isInited)
                return;

            UnregisterDiscreteInputCallbacks();
            m_inputActions.Shooter.Disable();
            m_inputActions.Dispose();
            m_inputActions = null;
            m_isInited = false;
        }

        // 读取当前帧射击角色输入
        public void Tick()
        {
            if (!m_isInited)
                return;

            ShootingPlayerInputActions.ShooterActions shooter = m_inputActions.Shooter;

            m_context.Input.MoveInput = shooter.Move.ReadValue<Vector2>();
            m_context.Input.LookInput = shooter.Look.ReadValue<Vector2>();

            // 右键瞄准或左键腰射都进入瞄准姿态
            m_runtimeState.IsAiming = shooter.Aim.IsPressed() || shooter.Fire.IsPressed();
        }

        // 注册一次性离散输入回调
        private void RegisterDiscreteInputCallbacks()
        {
            m_inputActions.Shooter.ToggleWalk.performed += OnToggleWalk;
            m_inputActions.Shooter.ToggleCrouch.performed += OnToggleCrouch;
            m_inputActions.Shooter.Evade.performed += OnEvade;
            m_inputActions.Shooter.Fire.performed += OnFire;
        }

        // 解除一次性离散输入回调
        private void UnregisterDiscreteInputCallbacks()
        {
            m_inputActions.Shooter.ToggleWalk.performed -= OnToggleWalk;
            m_inputActions.Shooter.ToggleCrouch.performed -= OnToggleCrouch;
            m_inputActions.Shooter.Evade.performed -= OnEvade;
            m_inputActions.Shooter.Fire.performed -= OnFire;
        }

        #region 输入回调
        // 切换步行与奔跑档位
        private void OnToggleWalk(InputAction.CallbackContext callbackContext)
        {
            m_context.Input.ToggleWalkMode();
        }

        // 切换蹲伏姿态
        private void OnToggleCrouch(InputAction.CallbackContext callbackContext)
        {
            m_runtimeState.IsCrouching = !m_runtimeState.IsCrouching;
        }

        // 写入闪避输入缓存
        private void OnEvade(InputAction.CallbackContext callbackContext)
        {
            m_context.Input.Buffer.Record(CharacterBufferedInputType.Dodge, Time.time);
        }

        // 写入开火输入缓存
        private void OnFire(InputAction.CallbackContext callbackContext)
        {
            m_context.Input.Buffer.Record(CharacterBufferedInputType.SpecialSkill, Time.time);
        }
        #endregion
    }
}
