/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战斗 HUD 视图，负责显示玩家锁定状态
 * │  类    名: UICombatHUD.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.Common.Defines;
using Framework.QTower.View;
using UnityEngine.UI;
using Utils.Find;

namespace UI.Combat
{
    public sealed class UICombatHUD : UIBase
    {
        private const string LOCK_ON_TOGGLE_PATH = "LockOn";

        private Toggle m_lockOnToggle;

        protected override void OnInit()
        {
            m_lockOnToggle = this.FindChildComponent<Toggle>(LOCK_ON_TOGGLE_PATH);
            if (m_lockOnToggle == null)
                return;

            SetLockOn(false);
        }

        // 刷新锁定状态显示
        public void SetLockOn(bool isLockOn)
        {
            if (m_lockOnToggle == null)
                return;

            m_lockOnToggle.SetIsOnWithoutNotify(isLockOn);
        }

        protected override void SubscribeViewEvents()
        {
            RegisterEvent<bool>(EventDefines.PlayerLockOnStateChanged, SetLockOn);
        }
    }
}
