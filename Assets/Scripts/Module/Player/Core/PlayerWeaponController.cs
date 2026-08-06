/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 玩家武器表现控制器，负责根据运行时意图切换武器显隐
 * │  类    名: PlayerWeaponController.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Module.Player.Context;
using UnityEngine;

namespace Module.Player.Core
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        private PlayerContext m_context;

        // 初始化武器表现控制器
        public void Init(PlayerContext context)
        {
            m_context = context;
            RefreshVisibility();
        }

        // 刷新武器显隐状态
        public void Tick()
        {
            RefreshVisibility();
        }

        // 按玩家运行时意图切换武器根节点
        private void RefreshVisibility()
        {
            if (gameObject.activeSelf == m_context.IsWeaponVisible)
                return;

            gameObject.SetActive(m_context.IsWeaponVisible);
        }
    }
}
