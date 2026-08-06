/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家控制器，负责玩家模块初始化与生命周期调度
 * │  类    名: PlayerController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.Controller;
using Module.Player.Config;
using Module.Player.Context;
using Module.Player.Core.View;
using Module.Player.HFSM.Config.Air;
using Module.Player.HFSM;
using Module.Player.HFSM.Animation;
using Module.Player.HFSM.Animation.Controllers;
using Module.Player.HFSM.Factory;
using Module.Player.HFSM.Transition;
using Module.Player.HFSM.Transition.Controllers;
using Module.Player.Input;
using Module.Player.Skill;
using Module.Player.Skill.Core;
using TriInspector;
using UnityEngine;
using Utils.Find;
using Utils.log;

namespace Module.Player.Core
{
    public class PlayerController : MonoBehaviour
    {
        #region 路径
        private const string VIEW_ROOT_PATH = "ViewRoot";
        private const string PLAYER_CAMERA_PATH = "ViewRoot/PlayerCamera";
        #endregion

        #region 配置相关
        [LabelText("玩家配置")]
        [SerializeField] private PlayerSettingsSO Settings;
        #endregion
        
        // ==================== 状态机相关 ====================
        private Transform m_transform;
        private Animator m_animator;
        private PlayerMotor m_motor;
        private PlayerViewController m_viewController;
        private PlayerStateMachine m_stateMachine;
        private CharacterController m_characterController;
        private Transform m_viewRoot;
        private Camera m_playerCamera;
        //context
        private PlayerContext m_context;
        private readonly ControllerManager m_controllerManager = new();
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
        //Skill
        private PlayerSkillController m_skillController;
        
        #region 生命周期
        private void Awake()
        {
            FindReferences();
            CheckConfigs();

            InitCore();
            InitSkill();
            InitHFSM();
            InitAnim();
        }

        private void Update()
        {
            m_inputReader.Tick();
            m_viewController.Tick(Time.deltaTime);
            m_transitionSelector.Tick();
            m_stateMachine.Tick(Time.deltaTime);
            m_skillController.Tick(Time.deltaTime);
            m_animWriter.Tick(Time.deltaTime);
        }

        private void FixedUpdate()
        {
            m_stateMachine.FixedTick(Time.fixedDeltaTime);
            m_motor.FixedTick(Time.fixedDeltaTime);
        }

        private void OnDestroy()
        {
            m_controllerManager.Destroy();

            if (m_inputReader != null)
                m_inputReader.UnInit();
        }
        #endregion
        
        #region 初始化
        // 初始化玩家核心运行依赖
        private void InitCore()
        {
            m_context = new PlayerContext(m_transform);
            m_context.IsGrounded = m_characterController.isGrounded;
            if (m_context.IsGrounded)
                m_context.LastGroundedTime = Time.time;

            m_inputReader = new PlayerInputReader();
            m_inputReader.Init(m_context, Settings.InputConfig);

            m_motor = new PlayerMotor();
            m_motor.Init(m_characterController, m_context, Settings.MoveConfig, Settings.ViewConfig);

            m_viewController = m_controllerManager.Register(
                new PlayerViewController(m_context, Settings.ViewConfig, m_viewRoot, m_playerCamera));
        }

        // 初始化玩家技能总控
        private void InitSkill()
        {
            PlayerSkillController skillController = new PlayerSkillController(m_context);
            skillController.RegisterConfig(PlayerSkillType.NormalAttack, Settings.NormalAttackConfig);
            m_skillController = m_controllerManager.Register(skillController);
        }

        // 初始化玩家状态机与状态转换
        private void InitHFSM()
        {
            m_stateMachine = PlayerStateFactory.Create(m_context, Settings, m_skillController);

            m_transitionController = m_controllerManager.Register(
                new PlayerTransitionController(m_context, Settings.AirConfig, Settings.DodgeConfig, Settings.NormalAttackConfig));

            m_transitionSelector = new PlayerTransitionSelector(m_stateMachine, m_transitionController.Rules);
        }

        // 初始化玩家动画表现层
        private void InitAnim()
        {
            m_animController = m_controllerManager.Register(new PlayerAnimController(m_context));

            m_animResolver = new PlayerAnimResolver();
            m_animResolver.Init(m_stateMachine, m_animController.Handlers);

            m_animWriter = new PlayerAnimWriter();
            m_animWriter.Init(m_animator, m_animResolver, m_context);

            m_rootMotionReceiver = m_animator.GetComponent<PlayerRootMotionReceiver>();
            if (m_rootMotionReceiver == null)
                m_rootMotionReceiver = m_animator.gameObject.AddComponent<PlayerRootMotionReceiver>();

            m_rootMotionReceiver.Init(m_animator, m_context);
        }

        #endregion

        // 查找玩家运行依赖引用
        private void FindReferences()
        {
            m_transform = transform;
            m_characterController = this.GetOwnerComponent<CharacterController>();
            m_animator = this.GetChildComponent<Animator>();
            m_viewRoot = this.FindChild(VIEW_ROOT_PATH);
            m_playerCamera = this.FindChildComponent<Camera>(PLAYER_CAMERA_PATH);
        }

        // 校验玩家配置引用，缺失时在控制台提示
        private void CheckConfigs()
        {
            if (Settings == null)
            {
                QLog.Warning("PlayerSettings 未配置，玩家模块无法正常运行");
                return;
            }

            if (Settings.AirConfig == null)
                QLog.Warning("AirConfig 未配置，空中模块可能无法正常运行");
            else if (Settings.AirConfig.StateClipCount != PlayerAirConfigSO.REQUIRED_STATE_CLIP_COUNT)
                QLog.Error("AirConfig 必须按 JumpBegin、FallLoop、FallEnd 顺序配置三段动画");

            if (Settings.MoveConfig == null)
                QLog.Warning("MoveConfig 未配置，移动模块可能无法正常运行");

            if (Settings.InputConfig == null)
                QLog.Warning("InputConfig 未配置，Shift 短按/长按将无法正常解释");

            if (Settings.DodgeConfig == null)
                QLog.Warning("DodgeConfig 未配置，闪避可能无法正常运行");
            else if (Settings.DodgeConfig.StateClipCount == 0)
                QLog.Error("DodgeConfig 未配置任何动画段落，闪避无法运行");

            if (Settings.NormalAttackConfig == null)
                QLog.Warning("NormalAttackConfig 未配置，普通攻击将无法正常进入");
            else if (Settings.NormalAttackConfig.StepCount == 0)
                QLog.Error("NormalAttackConfig 未配置任何动画段落，普通攻击无法运行");

            if (Settings.ViewConfig == null)
                QLog.Warning("ViewConfig 未配置，视角模块可能无法正常运行");
        }
    }
}
