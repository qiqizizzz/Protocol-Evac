/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家控制器，负责玩家模块初始化与生命周期调度
 * │  类    名: PlayerController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.Core.View.Config;
using Module.Player.HFSM;
using Module.Player.HFSM.Animation;
using Module.Player.HFSM.Animation.Controllers;
using Module.Player.HFSM.Config.Action;
using Module.Player.HFSM.Config.Air;
using Module.Player.HFSM.Config.Move;
using Module.Player.HFSM.States.Action;
using Module.Player.HFSM.States.Air;
using Module.Player.HFSM.States.Ground;
using Module.Player.HFSM.States.Skill;
using Module.Player.HFSM.Transition;
using Module.Player.HFSM.Transition.Controllers;
using Module.Player.Input;
using Module.Player.Input.Config;
using Module.Player.Skill.Data;
using UnityEngine;
using Utils.Find;
using Utils.log;

namespace Module.Player.Core
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerContext Context => m_context;

        #region 路径
        private const string VIEW_ROOT_PATH = "ViewRoot";
        private const string PLAYER_CAMERA_PATH = "ViewRoot/PlayerCamera";
        #endregion

        [Header("移动配置")]
        [Tooltip("玩家移动配置")]
        [SerializeField] private PlayerMoveConfigSO MoveConfig;
        [Header("输入配置")]
        [Tooltip("玩家输入解释配置")]
        [SerializeField] private PlayerInputConfigSO InputConfig;
        [Header("空中配置")]
        [Tooltip("玩家空中配置")]
        [SerializeField] private PlayerAirConfigSO AirConfig;
        [Header("动作配置")]
        [Tooltip("玩家闪避配置")]
        [SerializeField] private PlayerDodgeConfigSO DodgeConfig;
        [Header("技能配置")]
        [Tooltip("玩家普通攻击配置")]
        [SerializeField] private PlayerNormalAttackConfigSO NormalAttackConfig;
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
        private Transform m_viewRoot;
        private Camera m_playerCamera;
        //Anim
        private PlayerAnimWriter m_animWriter;
        private PlayerAnimController m_animController;
        private PlayerAnimResolver m_animResolver;
        private PlayerRootMotionReceiver m_rootMotionReceiver;
        //Transition
        private PlayerTransitionController m_transitionController;
        private PlayerTransitionSelector m_transitionSelector;
        //Input
        private PlayerInputReader m_inputReader;
        private PlayerContext m_context;
        private bool m_isInited;
        
        #region 生命周期
        private void Awake()
        {
            findReferences();
            validateConfigReferences();

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
        
        #region 初始化
        // 初始化玩家核心运行依赖
        private void initCore()
        {
            m_context = new PlayerContext(m_transform);
            m_context.IsGrounded = m_characterController.isGrounded;
            if (m_context.IsGrounded)
                m_context.LastGroundedTime = Time.time;

            m_inputReader = new PlayerInputReader();
            m_inputReader.Init(m_context, InputConfig);

            m_motor = new PlayerMotor();
            m_motor.Init(m_characterController, m_context, MoveConfig, ViewConfig);

            m_viewController = new PlayerViewController();
            m_viewController.Init(m_context, ViewConfig, m_viewRoot, m_playerCamera);
        }

        // 初始化玩家状态机与状态转换
        private void initHFSM()
        {
            RegisterAllStates();

            m_transitionController = new PlayerTransitionController();
            m_transitionController.Init(m_context, AirConfig, DodgeConfig, NormalAttackConfig);

            m_transitionSelector = new PlayerTransitionSelector(m_stateMachine, m_transitionController.Rules);
        }

        // 初始化玩家动画表现层
        private void initAnim()
        {
            m_animController = new PlayerAnimController();
            m_animController.Init(m_context);

            m_animResolver = new PlayerAnimResolver();
            m_animResolver.Init(m_stateMachine, m_animController.Handlers);

            m_animWriter = new PlayerAnimWriter();
            m_animWriter.Init(m_animator, m_animResolver, m_context);

            m_rootMotionReceiver = m_animator.GetComponent<PlayerRootMotionReceiver>();
            if (m_rootMotionReceiver == null)
                m_rootMotionReceiver = m_animator.gameObject.AddComponent<PlayerRootMotionReceiver>();

            m_rootMotionReceiver.Init(m_animator, m_context);
        }

        // 注册玩家初始地面状态树
        private void RegisterAllStates()
        {
            m_stateMachine = new PlayerStateMachine();
            
            m_stateMachine.RegisterState(new PlayerGroundedState());
            m_stateMachine.RegisterState(new PlayerIdleState(m_context));
            m_stateMachine.RegisterState(new PlayerMoveState(m_context, MoveConfig));
            m_stateMachine.RegisterState(new PlayerAirborneState(m_context, AirConfig));
            m_stateMachine.RegisterState(new PlayerJumpState(m_context, AirConfig));
            m_stateMachine.RegisterState(new PlayerFallState());
            m_stateMachine.RegisterState(new PlayerActionState());
            m_stateMachine.RegisterState(new PlayerDodgeState(m_context, DodgeConfig));
            m_stateMachine.RegisterState(new PlayerSkillState());
            m_stateMachine.RegisterState(new PlayerNormalAttackState(m_context, NormalAttackConfig));
            
            m_stateMachine.Init(PlayerStateId.Grounded);
        }
        #endregion

        // 查找玩家运行依赖引用
        private void findReferences()
        {
            m_transform = transform;
            m_characterController = this.GetOwnerComponent<CharacterController>();
            m_animator = this.GetChildComponent<Animator>();
            m_viewRoot = this.FindChild(VIEW_ROOT_PATH);
            m_playerCamera = this.FindChildComponent<Camera>(PLAYER_CAMERA_PATH);
        }

        // 校验玩家配置引用，缺失时在控制台提示
        private void validateConfigReferences()
        {
            if (MoveConfig == null)
                QLog.Warning("MoveConfig 未配置，移动模块可能无法正常运行");

            if (InputConfig == null)
                QLog.Warning("InputConfig 未配置，Shift 短按/长按将无法正常解释");

            if (AirConfig == null)
                QLog.Warning("AirConfig 未配置，空中模块可能无法正常运行");

            if (DodgeConfig == null)
                QLog.Warning("DodgeConfig 未配置，闪避可能无法正常运行");
            else if (DodgeConfig.StateClipCount == 0)
                QLog.Error("DodgeConfig 未配置任何动画段落，闪避无法运行");

            if (NormalAttackConfig == null)
                QLog.Warning("NormalAttackConfig 未配置，普通攻击将无法正常进入");
            else if (NormalAttackConfig.StepCount == 0)
                QLog.Error("NormalAttackConfig 未配置任何动画段落，普通攻击无法运行");

            if (ViewConfig == null)
                QLog.Warning("ViewConfig 未配置，视角模块可能无法正常运行");
        }
        
        // 检查运行期依赖是否仍然可用，避免 Play Mode 热重载后字段丢失
        private bool isRuntimeReady()
        {
            return m_isInited
                && m_inputReader != null
                && m_transitionController != null
                && m_transitionSelector != null
                && m_stateMachine != null
                && m_animController != null
                && m_animResolver != null
                && m_animWriter != null
                && m_motor != null
                && m_viewController != null;
        }
    }
}
