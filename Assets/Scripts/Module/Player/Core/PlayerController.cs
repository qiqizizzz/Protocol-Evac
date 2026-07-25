/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家控制器，负责玩家模块初始化与生命周期调度
 * │  类    名: PlayerController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Config.Move;
using Module.Player.Config.View;
using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.HFSM;
using Module.Player.HFSM.Animation;
using Module.Player.HFSM.Animation.Binders;
using Module.Player.HFSM.Animation.Rules;
using Module.Player.HFSM.States.Ground;
using Module.Player.HFSM.Transition;
using Module.Player.HFSM.Transition.Binders;
using Module.Player.HFSM.Transition.Rules;
using Module.Player.Input;
using UnityEngine;
using Utils.log;

namespace Module.Player.Core
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerContext Context => m_context;
        
        [Header("移动配置")]
        [Tooltip("玩家移动配置")]
        [SerializeField] private PlayerMoveConfigSO MoveConfig;
        [Header("视角配置")]
        [Tooltip("玩家视角配置")]
        [SerializeField] private PlayerViewConfigSO ViewConfig;
        
        // ==================== 状态机相关 ====================
        private Transform m_transform;
        private Animator m_animator;
        private PlayerMotor m_motor;
        private PlayerViewController m_viewController;
        private PlayerStateMachine m_stateMachine;
        private CharacterController m_characterController;
        //Anim
        private PlayerAnimWriter m_animWriter;
        private PlayerAnimBinder m_animBinder;
        private PlayerAnimResolver m_animResolver;
        //Transition
        private PlayerTransitionBinder m_transitionBinder;
        private PlayerTransitionSelector m_transitionSelector;
        //Input
        private PlayerInputReader m_inputReader;
        private PlayerContext m_context;
        private bool m_isInited;
        
        #region 生命周期
        private void Awake()
        {
            m_transform = transform;
            m_characterController = GetComponent<CharacterController>();
            m_animator = GetComponentInChildren<Animator>();

            if (m_characterController == null || m_animator == null || MoveConfig == null || ViewConfig == null)
            {
                QLog.Error("玩家初始化失败：必要引用缺失，请检查 CharacterController、Animator、MoveConfig 与 ViewConfig");
                return;
            }

            initCore();
            initHFSM();
            initAnim();

            m_isInited = true;
        }

        private void Update()
        {
            if (!isRuntimeReady())
                return;

            m_inputReader.Tick();
            m_viewController.Tick(Time.deltaTime);
            m_transitionSelector.Tick();
            m_stateMachine.Tick(Time.deltaTime);
            m_animWriter.Tick(Time.deltaTime);
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

        // 切换玩家视角模式
        public void SetViewMode(PlayerViewMode viewMode)
        {
            if (!isRuntimeReady())
                return;

            m_viewController.SetViewMode(viewMode);
        }

        #region 初始化
        // 初始化玩家核心运行依赖
        private void initCore()
        {
            m_context = new PlayerContext(m_transform);
            m_inputReader = new PlayerInputReader();
            m_inputReader.Init(m_context);

            m_motor = new PlayerMotor();
            m_motor.Init(m_characterController, m_context, MoveConfig, ViewConfig);

            m_viewController = new PlayerViewController();
            m_viewController.Init(m_context, ViewConfig);
        }

        // 初始化玩家状态机与状态转换
        private void initHFSM()
        {
            RegisterAllStates();

            m_transitionBinder = new PlayerTransitionBinder();
            m_transitionBinder.Bind(PlayerMoveTransitionRules.Create(m_context));

            m_transitionSelector = new PlayerTransitionSelector(m_stateMachine, m_transitionBinder.Rules);
        }

        // 初始化玩家动画表现层
        private void initAnim()
        {
            m_animBinder = new PlayerAnimBinder();
            m_animBinder.Bind(PlayerMoveAnimRules.Create(m_context, MoveConfig));

            m_animResolver = new PlayerAnimResolver();
            m_animResolver.Init(m_stateMachine, m_animBinder.Handlers);

            m_animWriter = new PlayerAnimWriter();
            m_animWriter.Init(m_animator, m_animResolver);
        }

        // 注册玩家初始地面状态树
        private void RegisterAllStates()
        {
            m_stateMachine = new PlayerStateMachine();
            
            m_stateMachine.RegisterState(new PlayerGroundedState());
            m_stateMachine.RegisterState(new PlayerIdleState(m_context));
            m_stateMachine.RegisterState(new PlayerMoveState(m_context, MoveConfig));
            
            m_stateMachine.Init(PlayerStateId.Grounded);
        }
        #endregion

        // 检查运行期依赖是否仍然可用，避免 Play Mode 热重载后字段丢失
        private bool isRuntimeReady()
        {
            return m_isInited
                && m_inputReader != null
                && m_transitionBinder != null
                && m_transitionSelector != null
                && m_stateMachine != null
                && m_animBinder != null
                && m_animResolver != null
                && m_animWriter != null
                && m_motor != null
                && m_viewController != null;
        }
    }
}
