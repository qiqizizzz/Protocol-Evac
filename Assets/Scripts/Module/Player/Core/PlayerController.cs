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
        private CharacterController m_characterController;
        private PlayerContext m_context;
        
        public PlayerContext Context => m_context;

        #region 生命周期
        private void Awake()
        {
            m_transform = transform;
            m_characterController = GetComponent<CharacterController>();
            validateReferences();
            
            m_context = new PlayerContext(m_transform);
        }
        #endregion

        // 校验玩家运行所需引用
        private void validateReferences()
        {
            if (m_characterController == null)
                QLog.Throw(new MissingComponentException($"{nameof(PlayerController)} 缺少 CharacterController"));

            if (MoveConfig == null)
                QLog.Throw(new MissingReferenceException($"{nameof(PlayerController)} 缺少 PlayerMoveConfigSO"));
        }
    }
}