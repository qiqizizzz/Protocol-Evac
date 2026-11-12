/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战斗 HUD 视图，负责显示玩家锁定状态
 * │  类    名: UICombatHUD.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.Common.Defines;
using Framework.QTower.View;
using UnityEngine;
using UnityEngine.UI;
using Utils.Find;
using Utils.log;

namespace UI.Combat
{
    public sealed class UICombatHUD : UIBase
    {
        private const string LOCK_ON_TOGGLE_PATH = "LockOn";
        private const string CROSSHAIR_PATH = "Crosshair";

        private Toggle m_lockOnToggle;
        private Image m_crosshair;

        [Header("准星")]
        [SerializeField] private Color CrosshairIdleColor = new Color(0.92f, 0.95f, 0.9f, 0.72f);
        [SerializeField] private Color CrosshairFocusColor = new Color(1f, 0.72f, 0.2f, 1f);

        protected override void OnInit()
        {
            m_lockOnToggle = this.FindChildComponent<Toggle>(LOCK_ON_TOGGLE_PATH);
            m_crosshair = this.FindChildComponent<Image>(CROSSHAIR_PATH);
            if (m_lockOnToggle == null)
            {
                QLog.Error("LockOn 未找到");
                return;
            }

            if (m_crosshair == null)
            {
                QLog.Error("Crosshair 未找到");
                return;
            }

            SetLockOn(false);
            SetLootFocus(false);
        }

        // 刷新锁定状态显示
        public void SetLockOn(bool isLockOn)
        {
            if (m_lockOnToggle == null)
                return;

            m_lockOnToggle.SetIsOnWithoutNotify(isLockOn);
        }

        // 刷新准星焦点颜色, 当前只区分可交互物资箱与普通瞄准
        public void SetLootFocus(bool hasFocus)
        {
            if (m_crosshair == null) return;

            m_crosshair.color = hasFocus ? CrosshairFocusColor : CrosshairIdleColor;
        }

        protected override void SubscribeViewEvents()
        {
            RegisterEvent<bool>(EventDefines.PlayerLockOnStateChanged, SetLockOn);
            RegisterEvent<bool>(EventDefines.LootFocusStateChanged, SetLootFocus);
        }
    }
}
