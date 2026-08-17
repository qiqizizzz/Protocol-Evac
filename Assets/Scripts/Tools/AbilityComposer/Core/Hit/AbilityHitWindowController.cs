/*
 * ┌─────────────────────────────────────────────────────────────┐
 * │  描    述: Ability 命中窗口控制器，负责同步窗口与战斗命中盒
 * │  类    名: AbilityHitWindowController.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────┘
 */

using Module.Ability.Data.Window.Hit;
using Module.Combat.Hitbox;
using UnityEngine;

namespace Module.Ability.Hit
{
    public sealed class AbilityHitWindowController
    {
        private readonly CombatHitbox m_combatHitbox;
        private readonly GameObject m_damageSource;

        private int m_activeSegmentIndex = -1;
        private string m_activeWindowId;

        // 创建命中窗口控制器
        public AbilityHitWindowController(CombatHitbox combatHitbox, GameObject damageSource)
        {
            m_combatHitbox = combatHitbox;
            m_damageSource = damageSource;
        }

        /// <summary>
        /// 根据当前段落与归一化时间同步命中窗口
        /// </summary>
        /// <param name="windowTrack">当前动画绑定的命中窗口轨道</param>
        /// <param name="normalizedTime">当前动画归一化时间</param>
        /// <param name="segmentIndex">当前 Ability 段落索引</param>
        public void Sync(AbilityHitWindowTrackData windowTrack, float normalizedTime, int segmentIndex)
        {
            if (windowTrack == null || !windowTrack.TryGetActiveWindow(normalizedTime, out AbilityHitWindowData activeWindow))
            {
                Close();
                return;
            }

            if (m_activeSegmentIndex == segmentIndex && m_activeWindowId == activeWindow.Id)
                return;

            Close();
            m_combatHitbox.Open(activeWindow.Damage, activeWindow.ReactionType,
                activeWindow.HorizontalKnockbackSpeed, activeWindow.HorizontalKnockbackDuration,
                activeWindow.VerticalLaunchSpeed, m_damageSource);
            m_activeSegmentIndex = segmentIndex;
            m_activeWindowId = activeWindow.Id;
        }

        // 关闭当前命中窗口并清理活动窗口记录
        public void Close()
        {
            if (m_activeSegmentIndex < 0)
                return;

            m_combatHitbox.Close();
            m_activeSegmentIndex = -1;
            m_activeWindowId = null;
        }
    }
}
