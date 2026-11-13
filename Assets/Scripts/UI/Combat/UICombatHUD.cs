/*
 * ┌──────────────────────────────────┐
 * │  描    述: 战斗 HUD 视图，负责显示玩家锁定状态、准星反馈与交互提示
 * │  类    名: UICombatHUD.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections;
using Framework.QTower.Common.Defines;
using Framework.QTower.View;
using TMPro;
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
        private const string LOOT_SEARCH_ICON_PATH = "LootSearchIcon";
        private const string INTERACTION_HINT_PATH = "InteractionHint";
        private const float INTERACTION_HINT_DURATION = 1.25f;

        private Toggle m_lockOnToggle;
        private Image m_crosshair;
        private Image m_lootSearchIcon;
        private TMP_Text m_interactionHint;
        private Coroutine m_interactionHintRoutine;

        [Header("准星")]
        [SerializeField] private Color CrosshairIdleColor = new Color(0.92f, 0.95f, 0.9f, 0.72f);
        [SerializeField] private Color CrosshairFocusColor = new Color(1f, 0.72f, 0.2f, 1f);

        protected override void OnInit()
        {
            m_lockOnToggle = this.FindChildComponent<Toggle>(LOCK_ON_TOGGLE_PATH);
            m_crosshair = this.FindChildComponent<Image>(CROSSHAIR_PATH);
            m_lootSearchIcon = this.FindChildComponent<Image>(LOOT_SEARCH_ICON_PATH);
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

            if (m_lootSearchIcon == null)
            {
                QLog.Error($"{LOOT_SEARCH_ICON_PATH} 未找到");
                return;
            }

            m_interactionHint = this.FindChildComponent<TMP_Text>(INTERACTION_HINT_PATH);
            if (m_interactionHint == null)
            {
                QLog.Error($"{INTERACTION_HINT_PATH} 未找到");
                return;
            }

            SetLockOn(false);
            SetLootFocus(false);
            ClearInteractionHint();
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
            m_lootSearchIcon.color = CrosshairFocusColor;
            m_lootSearchIcon.gameObject.SetActive(hasFocus);
        }

        // 显示短暂的交互结果提示, 新提示会覆盖旧提示并重新计时
        public void ShowInteractionHint(string message)
        {
            if (m_interactionHint == null || string.IsNullOrEmpty(message)) return;

            if (m_interactionHintRoutine != null) StopCoroutine(m_interactionHintRoutine);

            m_interactionHint.text = message;
            m_interactionHint.gameObject.SetActive(true);
            m_interactionHintRoutine = StartCoroutine(HideInteractionHintRoutine());
        }

        protected override void OnDispose()
        {
            if (m_interactionHintRoutine != null)
            {
                StopCoroutine(m_interactionHintRoutine);
                m_interactionHintRoutine = null;
            }
        }

        protected override void SubscribeViewEvents()
        {
            RegisterEvent<bool>(EventDefines.PlayerLockOnStateChanged, SetLockOn);
            RegisterEvent<bool>(EventDefines.LootFocusStateChanged, SetLootFocus);
            RegisterEvent<string>(EventDefines.InteractionHintRequested, ShowInteractionHint);
        }

        // 提示到时后清空文本并收起节点
        private IEnumerator HideInteractionHintRoutine()
        {
            yield return new WaitForSecondsRealtime(INTERACTION_HINT_DURATION);

            m_interactionHintRoutine = null;
            ClearInteractionHint();
        }

        // 清空提示内容并隐藏节点
        private void ClearInteractionHint()
        {
            if (m_interactionHint == null) return;

            m_interactionHint.text = string.Empty;
            m_interactionHint.gameObject.SetActive(false);
        }
    }
}
