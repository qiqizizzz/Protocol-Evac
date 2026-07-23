/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家控制器，负责玩家模块初始化与生命周期调度
 * │  类    名: PlayerController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.Move;
using Module.Player.Context;
using Module.Player.HFSM;
using Module.Player.HFSM.States.Ground;
using Module.Player.Input;
using Module.Player.Transition;
using Module.Player.Transition.Rules;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core
{
    public class PlayerController : MonoBehaviour
    {
        [Header("移动配置")]
        [Tooltip("玩家移动配置")]
        [SerializeField] private PlayerMoveConfigSO MoveConfig;
        
        // ==================== 状态机相关 ====================
        private Transform m_transform;
        private PlayerMotor m_motor;
        private PlayerAnimatorDriver m_animatorDriver;
        private PlayerStateMachine m_stateMachine;
        private StateSelector m_stateSelector;
        private CharacterController m_characterController;
        private Animator m_animator;

        private PlayerInputReader m_inputReader;
        
        private PlayerContext m_context;
        private bool m_isInited;
        
        public PlayerContext Context => m_context;

        #region 生命周期
        private void Awake()
        {
            m_transform = transform;
            m_characterController = GetComponent<CharacterController>();
            m_animator = GetComponentInChildren<Animator>();

            if (m_characterController == null || m_animator == null || MoveConfig == null)
            {
                QLog.Error("玩家初始化失败：必要引用缺失，请检查 CharacterController、Animator 与 MoveConfig");
                return;
            }
            
            m_context = new PlayerContext(m_transform);
            m_inputReader = new PlayerInputReader();
            m_inputReader.Init(m_context);
            
            m_motor = new PlayerMotor();
            m_motor.Init(m_characterController, m_context, MoveConfig);

            m_animatorDriver = new PlayerAnimatorDriver();
            m_animatorDriver.Init(m_animator, m_context);

            RegisterAllStates();
            m_stateSelector = new StateSelector(m_stateMachine, MoveRules.Create(m_context));
            m_isInited = true;
        }

        private void Update()
        {
            if (!isRuntimeReady())
                return;

            m_inputReader.Tick();
            m_stateSelector.Tick();
            m_stateMachine.Tick(Time.deltaTime);
            m_animatorDriver.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            if (!isRuntimeReady())
                return;

            m_stateMachine.FixedTick(Time.fixedDeltaTime);
            m_motor.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            if (m_inputReader != null)
                m_inputReader.UnInit();
        }

        #endregion

        // 注册玩家初始地面状态树
        private void RegisterAllStates()
        {
            m_stateMachine = new PlayerStateMachine();
            
            m_stateMachine.RegisterState(new PlayerGroundedState());
            m_stateMachine.RegisterState(new PlayerIdleState(m_context));
            m_stateMachine.RegisterState(new PlayerMoveState(m_context, MoveConfig));
            
            m_stateMachine.Init(PlayerStateId.Grounded);
        }

        // 检查运行期依赖是否仍然可用，避免 Play Mode 热重载后字段丢失
        private bool isRuntimeReady()
        {
            return m_isInited
                && m_inputReader != null
                && m_stateSelector != null
                && m_stateMachine != null
                && m_animatorDriver != null
                && m_motor != null;
        }
    }
}
