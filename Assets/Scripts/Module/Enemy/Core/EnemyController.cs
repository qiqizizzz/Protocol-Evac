/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人模块的装配入口与生命周期调度器
 * │  类    名: EnemyController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using CleverCrow.Fluid.BTs.Trees;
using Module.Combat.Hitbox;
using Module.Enemy.Animation;
using Module.Enemy.Behavior.Config;
using Module.Enemy.Behavior.Core;
using Module.Enemy.Behavior.Readers;
using Module.Enemy.Behavior.Trees.Wanderer;
using Module.Enemy.Config;
using Module.Enemy.Context;
using Module.Enemy.Skill;
using Module.Enemy.Skill.Core;
using TriInspector;
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
        [LabelText("运行时行为树")]
        [Tooltip("Play Mode 下点击 View Tree 查看当前行为树节点状态")]
        [SerializeField] private BehaviorTree EnemyBehaviorTree;
        
        private Transform m_transform;
        private Animator m_animator;
        private CombatHitbox m_combatHitbox;
        private EnemyContext m_context;
        private EnemySkillController m_skillController;
        private EnemyBehaviorController m_behaviorController;
        private EnemyTargetReader m_targetReader;
        private EnemyAnimWriter m_animWriter;

        public EnemyContext Context => m_context;
        public bool IsNormalAttacking => m_skillController.IsRunning
            && m_skillController.CurrentSkillType == EnemySkillType.NormalAttack;
        public bool IsNormalAttackCoolingDown => m_skillController.IsCoolingDown(EnemySkillType.NormalAttack);
        public bool IsNormalAttackMovementLocked => m_skillController.IsMovementLocked;
        public bool CanRotateDuringNormalAttack => m_skillController.CanRotate;

        #region 生命周期
        private void Awake()
        {
            FindReferences();
            CheckConfigs();
            InitCore();
            InitSkill();
            InitAnim();
            InitBehavior();
            InitTarget();
        }

        private void OnEnable()
        {
            m_context.SetActive(true);
        }

        private void Update()
        {
            if (m_targetReader.Tick(Time.deltaTime))
                m_behaviorController.Reset();

            m_skillController.Tick(Time.deltaTime);
            m_behaviorController.Tick();
            m_animWriter.Tick(Time.deltaTime);
        }

        private void OnDisable()
        {
            m_behaviorController.Reset();
            m_skillController.Close();
            m_animWriter.Close();
            m_context.Target.Reset();
            m_context.SetActive(false);
        }

        private void OnDestroy()
        {
            m_behaviorController.Reset();
            m_skillController.Close();
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

        // 初始化敌人技能模块
        private void InitSkill()
        {
            m_skillController = new EnemySkillController(m_context.Action, m_combatHitbox, gameObject);
            m_skillController.RegisterConfig(EnemySkillType.NormalAttack, Settings.NormalAttackConfig);
        }

        // 初始化敌人行为树模块
        private void InitBehavior()
        {
            WandererBehaviorTree wandererBehaviorTree = new WandererBehaviorTree(gameObject, m_context,
                m_skillController);
            m_behaviorController = new EnemyBehaviorController(wandererBehaviorTree.Tree);
            EnemyBehaviorTree = m_behaviorController.Tree;
        }

        // 初始化敌人动画表现层
        private void InitAnim()
        {
            m_animWriter = new EnemyAnimWriter();
            m_animWriter.Init(m_animator, m_context, Settings.AnimationConfig);
        }

        // 初始化敌人目标事实读取器
        private void InitTarget()
        {
            m_targetReader = new EnemyTargetReader(m_context);
        }

        // 查找敌人运行依赖引用
        private void FindReferences()
        {
            m_transform = transform;
            m_animator = this.GetChildComponent<Animator>();
            m_combatHitbox = this.GetChildComponent<CombatHitbox>();
        }
        #endregion

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

            if (Settings.AnimationConfig == null)
            {
                QLog.Warning("EnemyAnimationConfig 未配置，敌人动画模块可能无法正常运行");
            }
            else
            {
                if (Settings.AnimationConfig.IdleAnimationClip == null)
                    QLog.Error("EnemyAnimationConfig 未配置待机动画");

                if (Settings.AnimationConfig.MoveAnimationClip == null)
                    QLog.Error("EnemyAnimationConfig 未配置移动动画");

                if (Settings.AnimationConfig.HitAnimationClip == null)
                    QLog.Error("EnemyAnimationConfig 未配置受击动画");
            }

            if (Settings.NormalAttackConfig == null)
                QLog.Warning("NormalAttackConfig 未配置，敌人普通攻击模块可能无法正常运行");
            else if (Settings.NormalAttackConfig.StepCount == 0)
                QLog.Error("NormalAttackConfig 未配置任何状态动画段落，敌人普通攻击无法运行");
        }
    }
}
