/*
* ┌──────────────────────────────────┐
* │  描    述: 射击玩家控制器，负责射击角色模块初始化与生命周期调度
* │  类    名: ShootingPlayerController.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using Framework.Character.HFSM;
using Framework.QTower.Common.Defines;
using Framework.QTower.Controller;
using Framework.QTower.Event;
using Module.Combat.Damage;
using Module.Combat.Hitbox;
using Module.ShootingPlayer.Context;
using Framework.Character.Runtime.Core.View;
using Framework.Character.Runtime.Damage;
using Framework.Character.Runtime.Skill;
using Framework.Character.Runtime.Skill.Core;
using Module.ShootingPlayer.Config;
using Module.ShootingPlayer.Damage;
using Module.ShootingPlayer.HFSM;
using Module.ShootingPlayer.HFSM.Animation;
using Module.ShootingPlayer.HFSM.Animation.Controllers;
using Module.ShootingPlayer.HFSM.Factory;
using Module.ShootingPlayer.HFSM.Runtime;
using Module.ShootingPlayer.HFSM.Transition;
using Module.ShootingPlayer.HFSM.Transition.Controllers;
using Module.ShootingPlayer.Input;
using TriInspector;
using UnityEngine;
using Utils.Find;
using Utils.log;

// 持枪少女复用 Character.Core 框架实现，保留原有类型名避免全量改名
using ShooterStateMachine = Framework.Character.HFSM.CharacterStateMachine<Module.ShootingPlayer.HFSM.ShooterStateId>;
using ShooterTransitionSelector = Framework.Character.Transition.CharacterTransitionSelector<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Transition.ShooterTransitionPriority>;
using ShooterAnimResolver = Framework.Character.Animation.CharacterAnimResolver<Module.ShootingPlayer.HFSM.ShooterStateId, Module.ShootingPlayer.HFSM.Animation.ShooterAnimParams>;

namespace Module.ShootingPlayer.Core
{
    // 射击玩家控制器，持枪角色的 3C 总入口，与千咲 PlayerController 平行独立
    [RequireComponent(typeof(CharacterDamageReceiver))]
    public sealed class ShootingPlayerController : MonoBehaviour
    {
        #region 路径
        private const string VIEW_ROOT_PATH = "ViewRoot";
        private const string PLAYER_CAMERA_PATH = "ViewRoot/PlayerCamera";
        #endregion

        #region 配置相关
        [LabelText("射击玩家配置")]
        [SerializeField] private ShootingPlayerSettingsSO Settings;
        #endregion

        // ==================== 字段[私有] ====================
        private Transform m_transform;
        private Animator m_animator;
        private CharacterController m_characterController;
        private Transform m_viewRoot;
        private Camera m_playerCamera;

        private ShooterContext m_context;
        private readonly ControllerManager m_controllerManager = new();

        // HFSM
        private ShooterStateMachine m_stateMachine;
        private ShooterRuntimeState m_runtimeState;
        private ShooterTransitionController m_transitionController;
        private ShooterTransitionSelector m_transitionSelector;

        // Anim
        private ShooterAnimController m_animController;
        private ShooterAnimResolver m_animResolver;
        private ShooterAnimWriter m_animWriter;

        // Input & Move
        private ShooterInputReader m_inputReader;
        private ShooterMotor m_motor;

        // View
        private CharacterViewController m_viewController;

        // Combat
        private CombatHitbox m_combatHitbox;
        private CharacterDamageReceiver m_damageReceiver;
        private ShooterDamageController m_damageController;

        // Skill
        private CharacterSkillController<ShooterStateId> m_skillController;

        // ==================== 属性 ====================
        // 对外提供状态机当前叶子状态的只读诊断入口
        public ShooterStateId CurrentStateId => m_stateMachine == null
            ? ShooterStateId.None
            : m_stateMachine.CurrentLeafStateId;

        // 对外提供当前完整状态路径
        public IReadOnlyList<ShooterStateId> ActiveStatePath => m_stateMachine == null
            ? Array.Empty<ShooterStateId>()
            : m_stateMachine.ActiveStatePath;

        // 对外提供本帧即将转换到的目标状态
        public ShooterStateId NextStateId => m_transitionSelector == null
            ? ShooterStateId.None
            : m_transitionSelector.GetNextTargetStateId();

        // 对外提供当前生命值的调试读取入口
        public float CurrentHealth => m_damageController == null ? 0f : m_damageController.CurrentHealth;

        // 对外提供 GM 无敌状态的调试读取入口
        public bool IsGmInvincible => m_damageController != null && m_damageController.IsGmInvincible;

        // ==================== 生命周期 ====================
        private void Awake()
        {
            FindReferences();
            CheckConfigs();

            InitCore();
            InitSkill();
            InitDamage();
            InitHFSM();
            InitAnim();
        }

        private void OnEnable()
        {
            EventManager.RegisterEvent(EventDefines.PlayerRetryRequested, HandlePlayerRetryRequested);
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

        private void OnDisable()
        {
            EventManager.UnregisterEvent(EventDefines.PlayerRetryRequested, HandlePlayerRetryRequested);
        }

        private void OnDestroy()
        {
            m_controllerManager.Destroy();

            if (m_inputReader != null)
                m_inputReader.UnInit();

            if (m_damageReceiver != null)
                m_damageReceiver.OnDamageReceived -= HandleDamageReceived;
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 设置 GM 专用无敌状态
        /// </summary>
        /// <param name="isEnabled">是否开启无敌</param>
        public void SetGmInvincible(bool isEnabled)
        {
            m_damageController.SetGmInvincible(isEnabled);
        }

        /// <summary>
        /// 通过 GM 恢复射击角色全部生命
        /// </summary>
        public void RestoreFullHealth()
        {
            m_damageController.RestoreFullHealth();
        }

        // ==================== Private Function ====================
        #region 初始化
        // 查找射击角色运行依赖引用
        private void FindReferences()
        {
            m_transform = transform;
            m_characterController = this.GetOwnerComponent<CharacterController>();
            m_animator = this.GetChildComponent<Animator>();
            m_damageReceiver = this.GetOwnerComponent<CharacterDamageReceiver>();
            m_combatHitbox = this.GetChildComponent<CombatHitbox>();
            m_viewRoot = this.FindChild(VIEW_ROOT_PATH);
            m_playerCamera = this.FindChildComponent<Camera>(PLAYER_CAMERA_PATH);

            // 技能控制器会直接订阅命中盒事件，缺失时补挂而不是静默跳过
            if (m_combatHitbox == null)
                m_combatHitbox = gameObject.AddComponent<CombatHitbox>();
        }

        // 校验射击角色配置引用，缺失时在编辑器控制台报错
        private void CheckConfigs()
        {
            if (Settings == null)
            {
                QLog.Error("ShootingPlayerSettings 未配置，射击玩家模块无法正常运行");
                return;
            }

            if (m_characterController == null)
                QLog.Error("CharacterController 未找到，射击角色无法移动");
            if (m_animator == null)
                QLog.Error("Animator 未找到，射击角色无法播放动画");
            if (m_damageReceiver == null)
                QLog.Error("CharacterDamageReceiver 未找到，射击角色无法接收伤害");
            if (m_viewRoot == null)
                QLog.Error($"未找到 {VIEW_ROOT_PATH} 节点，射击角色视角模块无法运行");
            if (m_playerCamera == null)
                QLog.Error($"未找到 {PLAYER_CAMERA_PATH} 相机，射击角色视角模块无法运行");

            if (Settings.StatsConfig == null)
                QLog.Error("StatsConfig 未配置，射击角色生命初始化无法正常运行");

            if (Settings.DamageConfig == null)
                QLog.Error("DamageConfig 未配置，射击角色伤害模块无法正常运行");

            if (Settings.MoveConfig == null)
                QLog.Error("MoveConfig 未配置，射击角色移动模块无法正常运行");
            else if (Settings.MoveConfig.StateClipCount != ShooterMoveConfigSO.REQUIRED_STATE_CLIP_COUNT)
                QLog.Error("MoveConfig 必须按 Idle、Walk、Run 顺序配置三段动画");

            if (Settings.AimConfig == null)
                QLog.Error("AimConfig 未配置，射击角色瞄准模块无法正常运行");
            else if (Settings.AimConfig.StateClipCount != ShooterAimConfigSO.REQUIRED_STATE_CLIP_COUNT)
                QLog.Error("AimConfig 必须按 AimIdle、AimWalk、AimJog 顺序配置三段动画");

            if (Settings.CrouchConfig == null)
                QLog.Error("CrouchConfig 未配置，射击角色蹲伏模块无法正常运行");
            else if (Settings.CrouchConfig.StateClipCount != ShooterCrouchConfigSO.REQUIRED_STATE_CLIP_COUNT)
                QLog.Error("CrouchConfig 必须按 CrouchIdle、CrouchAimIdle、CrouchAimMove 顺序配置三段动画");

            if (Settings.EvadeConfig == null)
                QLog.Error("EvadeConfig 未配置，射击角色闪避无法正常运行");
            else if (Settings.EvadeConfig.StateClipCount != ShooterEvadeConfigSO.REQUIRED_STATE_CLIP_COUNT)
                QLog.Error("EvadeConfig 必须配置一段闪避动画");

            if (Settings.ShootConfig == null)
                QLog.Error("ShootConfig 未配置，射击角色开火无法正常运行");
            else if (Settings.ShootConfig.StepCount != ShooterShootConfigSO.REQUIRED_STEP_COUNT)
                QLog.Error("ShootConfig 必须配置一段射击动画段落");

            if (Settings.ViewConfig == null)
                QLog.Error("ViewConfig 未配置，射击角色视角模块无法正常运行");

            if (Settings.AudioConfig == null)
                QLog.Warning("AudioConfig 未配置，射击角色动画事件音效将不会播放");
        }

        // 初始化射击角色核心运行依赖
        private void InitCore()
        {
            m_context = new ShooterContext(m_transform);
            m_context.Movement.IsGrounded = m_characterController.isGrounded;
            if (m_context.Movement.IsGrounded)
                m_context.Movement.LastGroundedTime = Time.time;

            m_runtimeState = new ShooterRuntimeState();

            m_inputReader = new ShooterInputReader();
            m_inputReader.Init(m_context, m_runtimeState);

            m_motor = new ShooterMotor();
            m_motor.Init(m_characterController, m_context, Settings.MoveConfig);

            m_viewController = m_controllerManager.Register(
                new CharacterViewController(m_context, Settings.ViewConfig, m_viewRoot, m_playerCamera));
        }

        // 初始化射击技能总控，开火动作以 SpecialSkill 通道承载
        private void InitSkill()
        {
            CharacterSkillController<ShooterStateId> skillController = new CharacterSkillController<ShooterStateId>(m_context, m_combatHitbox, gameObject);
            skillController.RegisterConfig(CharacterSkillType.SpecialSkill, Settings.ShootConfig);
            m_skillController = m_controllerManager.Register(skillController);
        }

        // 初始化伤害接收与生命控制模块
        private void InitDamage()
        {
            m_damageController = new ShooterDamageController(m_context, Settings.StatsConfig, Settings.DamageConfig);
            m_damageReceiver.OnDamageReceived += HandleDamageReceived;
        }

        // 初始化状态机与状态转换
        private void InitHFSM()
        {
            m_stateMachine = new ShooterStateMachine();

            List<CharacterStateBase<ShooterStateId>> states =
                ShootingPlayerStateFactory.CreateStates(m_context, m_runtimeState, Settings, m_skillController);
            for (int i = 0; i < states.Count; i++)
                m_stateMachine.RegisterState(states[i]);

            m_stateMachine.Init(ShooterStateId.GroundedIdle);

            m_transitionController = m_controllerManager.Register(new ShooterTransitionController(
                m_context, m_runtimeState, Settings.AimConfig, Settings.EvadeConfig, Settings.ShootConfig));

            m_transitionSelector = new ShooterTransitionSelector(m_stateMachine, m_transitionController.Rules);
        }

        // 初始化动画表现层
        private void InitAnim()
        {
            m_animController = m_controllerManager.Register(new ShooterAnimController(m_context));

            m_animResolver = new ShooterAnimResolver();
            m_animResolver.Init(m_stateMachine, m_animController.Handlers);

            m_animWriter = new ShooterAnimWriter();
            m_animWriter.Init(m_animator, m_stateMachine, m_animResolver, m_runtimeState);
        }
        #endregion

        // 接收伤害回调并中断当前射击技能
        private void HandleDamageReceived(DamageData damageData)
        {
            if (m_damageController == null || !m_damageController.TryTakeDamage(damageData))
                return;

            m_skillController.Close();
            if (m_context.Damage.IsDead)
                EventManager.PublishEvent(EventDefines.PlayerDied);
        }

        // 重开一局时恢复生命并允许状态机退出死亡状态
        private void HandlePlayerRetryRequested()
        {
            m_damageController.RestoreFullHealth();
        }
    }
}
