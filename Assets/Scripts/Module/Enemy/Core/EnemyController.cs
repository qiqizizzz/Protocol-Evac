/*
 * ┌──────────────────────────────────┐
 * │  描    述: 敌人模块的装配入口与生命周期调度器
 * │  类    名: EnemyController.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Enemy.Config;
using Module.Enemy.Behavior.Config;
using Module.Enemy.Context;
using UnityEngine;
using Utils.log;

namespace Module.Enemy.Core
{
    public sealed class EnemyController : MonoBehaviour
    {
        [Header("Enemy 配置")]
        [SerializeField] private EnemySettingsSO Settings;
        
        // ==================== 行为树相关 ====================
        private Transform m_transform;
        //content
        private EnemyContext m_context;

        public EnemyContext Context => m_context;

        #region 生命周期
        private void Awake()
        {
            FindReferences();
            CheckConfigs();
            InitCore();
        }

        private void OnEnable()
        {
            if (m_context == null) return;

            m_context.SetActive(true);
        }

        private void OnDisable()
        {
            if (m_context == null) return;

            m_context.SetActive(false);
        }

        private void OnDestroy()
        {
            if (m_context == null) return;

            m_context.SetActive(false);
            m_context = null;
        }
        #endregion

        #region 初始化
        private void InitCore()
        {
            m_context = new EnemyContext(m_transform, Settings.StatsConfig, Settings.BehaviorConfig);
        }
        
        private void FindReferences()
        {
            m_transform = transform;
        }
        #endregion

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
        }
        
        
    }
}
