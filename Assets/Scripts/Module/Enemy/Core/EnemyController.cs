/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人模块的装配入口与生命周期调度器
 * │  类    名: EnemyController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Trees;
using Module.Combat.Hitbox;
using Module.Enemy.Ability;
using Module.Enemy.Ability.Core;
using Module.Enemy.Animation;
using Module.Enemy.Behavior.Config;
using Module.Enemy.Behavior.Core;
using Module.Enemy.Config;
using Module.Enemy.Context;
using UnityEngine;
using Utils.Find;
using Utils.log;

namespace Module.Enemy.Core
{
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Enemy 配置")]
        [SerializeField] private EnemySettingsSO Settings;

        [Header("行为树调试")]
        [SerializeField] private BehaviorTree EnemyBehaviorTree;
        
        private Transform m_transform;
        private Animator m_animator;
        private CombatHitbox m_combatHitbox;
        private EnemyContext m_context;
        private EnemyAbilityController m_abilityController;
        private EnemyBehaviorController m_behaviorController;
        private EnemyAnimWriter m_animWriter;

        public EnemyContext Context => m_context;
        public bool IsNormalAttacking => m_abilityController.IsRunning
            && m_abilityController.CurrentAbilityType == EnemyAbilityType.NormalAttack;
        public bool IsNormalAttackCoolingDown => m_abilityController.IsCoolingDown(EnemyAbilityType.NormalAttack);
        public bool IsNormalAttackMovementLocked => m_abilityController.IsMovementLocked;
        public bool CanRotateDuringNormalAttack => m_abilityController.CanRotate;

        #region 生命周期
        private void Awake()
        {
            FindReferences();
            CheckConfigs();
            InitCore();
            InitAbility();
            InitBehavior();
            InitAnim();
        }

        private void OnEnable()
        {
            m_context.SetActive(true);
        }

        private void Update()
        {
            m_abilityController.Tick(Time.deltaTime);
            m_behaviorController.Tick();
            m_animWriter.Tick();
        }

        private void OnDisable()
        {
            m_behaviorController.Reset();
            m_abilityController.Close();
            m_animWriter.Close();
            m_context.SetActive(false);
        }

        private void OnDestroy()
        {
            m_behaviorController.Reset();
            m_abilityController.Close();
            m_animWriter.UnInit();
            m_context.SetActive(false);
            m_context = null;
        }
        #endregion

        #region 初始化
        // 初始化敌人核心运行时上下文
        private void InitCore()
        {
            m_context = new EnemyContext(m_transform, Settings.StatsConfig, Settings.BehaviorConfig);
        }

        // 初始化敌人能力模块
        private void InitAbility()
        {
            m_abilityController = new EnemyAbilityController(m_context.Action, m_combatHitbox, gameObject);
            m_abilityController.RegisterConfig(EnemyAbilityType.NormalAttack, Settings.NormalAttackConfig);
        }

        // 初始化敌人行为树模块
        private void InitBehavior()
        {
            m_behaviorController = new EnemyBehaviorController(gameObject, m_context, m_abilityController);
            EnemyBehaviorTree = m_behaviorController.Tree;
        }

        // 初始化敌人动画表现层
        private void InitAnim()
        {
            m_animWriter = new EnemyAnimWriter();
            m_animWriter.Init(m_animator, m_context, Settings.NormalAttackConfig);
        }

        // 查找敌人运行依赖引用
        private void FindReferences()
        {
            m_transform = transform;
            m_animator = this.GetChildComponent<Animator>();
            m_combatHitbox = this.GetChildComponent<CombatHitbox>();
        }
        #endregion

        // 尝试开始敌人普通攻击
        public bool TryStartNormalAttack()
        {
            if (!m_abilityController.CanOpen(EnemyAbilityType.NormalAttack))
                return false;

            m_context.Action.RequestAbility(EnemyAbilityType.NormalAttack);
            m_behaviorController.Reset();
            return true;
        }

        // 校验敌人配置引用，缺失时在控制台提示
        private void CheckConfigs()
        {
            if (Settings == null)
            {
                QLog.Warning("EnemySettings 未配置，Enemy 模块无法正常运行");
                return;
            }

            if (Settings.StatsConfig == null)
                QLog.Warning("StatsConfig 未配置，敌人数值模块可能无法正常运行");

            if (Settings.BehaviorConfig == null)
                QLog.Warning("EnemyBehaviorConfig 未配置，敌人行为模块可能无法正常运行");

            if (Settings.NormalAttackConfig == null)
                QLog.Warning("NormalAttackConfig 未配置，敌人普通攻击模块可能无法正常运行");
            else if (Settings.NormalAttackConfig.StepCount == 0)
                QLog.Error("NormalAttackConfig 未配置任何状态动画段落，敌人普通攻击无法运行");
        }
    }
}
