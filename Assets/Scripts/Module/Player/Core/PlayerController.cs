/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家控制器，负责玩家模块初始化与生命周期调度
 * │  类    名: PlayerController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Module.Player.Config.Move;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.States.Ground;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core
{
    public class PlayerController : MonoBehaviour
    {
        [Header("移动配置")]
        [Tooltip("玩家移动配置")]
        [SerializeField] private PlayerMoveConfigSO MoveConfig;
        
        private Transform m_transform;
        private PlayerMotor m_motor;
        private PlayerStateMachine m_stateMachine;
        private CharacterController m_characterController;
        
        private PlayerContext m_context;
        
        public PlayerContext Context => m_context;

        #region 生命周期
        private void Awake()
        {
            m_transform = transform;
            m_characterController = GetComponent<CharacterController>();
            
            m_context = new PlayerContext(m_transform);
            m_motor = new PlayerMotor();
            m_motor.Init(m_characterController, m_context, MoveConfig);

            RegisterAllStates();
        }

        private void Update()
        {
            m_stateMachine.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            m_stateMachine.FixedTick(Time.fixedDeltaTime);
            m_motor.FixedTick(Time.fixedDeltaTime);
        }
        #endregion

        private void RegisterAllStates()
        {
            m_stateMachine = new PlayerStateMachine();
            
            m_stateMachine.RegisterState(new PlayerGroundedState());
            m_stateMachine.RegisterState(new PlayerIdleState(m_context));
            m_stateMachine.RegisterState(new PlayerMoveState(m_context, MoveConfig));
            
            m_stateMachine.Init(PlayerStateId.Grounded);
        }
    }
}