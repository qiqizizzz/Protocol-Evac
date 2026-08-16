/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家输入读取器，负责读取当前帧移动输入                      
 * │  类    名: PlayerInputReader.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.Input.Buffer;
using Module.Player.Input.Config;
using Module.Player.Input.Interpreter;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Module.Player.Input
{
    public class PlayerInputReader
    {
        private const float MOVE_INPUT_THRESHOLD_SQR = 0.01f;

        private PlayerContext m_context;
        private PlayerInputActions m_inputActions;
        private PlayerSprintInterpreter m_sprintInterpreter;
        private bool m_wasMoveInputActive;

        // 初始化玩家输入读取器
        public void Init(PlayerContext context, PlayerInputConfigSO inputConfig)
        {
            m_context = context;
            m_inputActions = new PlayerInputActions();
            m_sprintInterpreter = new PlayerSprintInterpreter();
            m_sprintInterpreter.Init(m_context, inputConfig);
            RegisterDiscreteInputCallbacks();
            m_inputActions.Player.Enable();
        }

        // 释放玩家输入读取器
        public void UnInit()
        {
            if (m_inputActions == null)
                return;

            UnregisterDiscreteInputCallbacks();
            m_inputActions.Player.Disable();
            m_inputActions.Dispose();
        }

        // 读取当前帧玩家输入
        public void Tick()
        {
            //WASD: 移动
            Vector2 moveInput = m_inputActions.Player.Move.ReadValue<Vector2>();
            RecordHurtMoveCancellation(moveInput);
            m_context.Input.MoveInput = moveInput;
            //Shift: 短按闪避，长按疾跑
            m_sprintInterpreter.Tick(
                m_inputActions.Player.Sprint.WasPressedThisFrame(),
                m_inputActions.Player.Sprint.IsPressed(),
                m_inputActions.Player.Sprint.WasReleasedThisFrame());
            //鼠标移动: 视角
            m_context.Input.LookInput = m_inputActions.Player.Look.ReadValue<Vector2>();
        }

        // 注册一次性离散输入回调
        private void RegisterDiscreteInputCallbacks()
        {
            m_inputActions.Player.Jump.performed += OnJump;
            m_inputActions.Player.Attack.performed += OnAttack;
            m_inputActions.Player.LockOn.performed += OnLockOn;
            m_inputActions.Player.ToggleWalk.performed += OnToggleWalk;
            m_inputActions.Player.SwitchToFirstPerson.performed += OnSwitchToFirstPerson;
            m_inputActions.Player.SwitchToThirdPerson.performed += OnSwitchToThirdPerson;
        }

        // 解除一次性离散输入回调
        private void UnregisterDiscreteInputCallbacks()
        {
            m_inputActions.Player.Jump.performed -= OnJump;
            m_inputActions.Player.Attack.performed -= OnAttack;
            m_inputActions.Player.LockOn.performed -= OnLockOn;
            m_inputActions.Player.ToggleWalk.performed -= OnToggleWalk;
            m_inputActions.Player.SwitchToFirstPerson.performed -= OnSwitchToFirstPerson;
            m_inputActions.Player.SwitchToThirdPerson.performed -= OnSwitchToThirdPerson;
        }

        #region 输入回调
        // 写入跳跃输入缓存
        private void OnJump(InputAction.CallbackContext context)
        {
            RecordBufferedInput(PlayerBufferedInputType.Jump);
        }

        // 写入普通攻击输入缓存
        private void OnAttack(InputAction.CallbackContext context)
        {
            RecordBufferedInput(PlayerBufferedInputType.NormalAttack);
        }

        // 请求切换锁定目标状态
        private void OnLockOn(InputAction.CallbackContext context)
        {
            m_context.Input.RequestLockOnToggle();
        }

        // 切换步行与奔跑模式
        private void OnToggleWalk(InputAction.CallbackContext context)
        {
            m_context.Input.ToggleWalkMode();
        }

        // 请求切换至第一人称视角
        private void OnSwitchToFirstPerson(InputAction.CallbackContext context)
        {
            m_context.View.TargetViewMode = PlayerViewMode.FirstPerson;
        }

        // 请求切换至第三人称视角
        private void OnSwitchToThirdPerson(InputAction.CallbackContext context)
        {
            m_context.View.TargetViewMode = PlayerViewMode.ThirdPerson;
        }

        // 写入一次性操作的输入缓存
        private void RecordBufferedInput(PlayerBufferedInputType inputType)
        {
            m_context.Input.Buffer.Record(inputType, Time.time);
        }

        // 仅记录受击锁定窗口结束后新开始的移动输入
        private void RecordHurtMoveCancellation(Vector2 moveInput)
        {
            bool hasMoveInput = moveInput.sqrMagnitude > MOVE_INPUT_THRESHOLD_SQR;
            if (m_context.Damage.IsHurtCancellationEnabled && hasMoveInput && !m_wasMoveInputActive)
                m_context.Damage.RequestHurtMoveCancellation();

            m_wasMoveInputActive = hasMoveInput;
        }
        #endregion
    }
}
