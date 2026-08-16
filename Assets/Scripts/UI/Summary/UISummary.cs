/*
 * ┌────────────────────────────────────────────┐
 * │  描    述: 死亡结算视图，负责发起重新挑战请求
 * │  类    名: UISummary.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────┘
 */

using Framework.QTower.Common.Defines;
using Framework.QTower.Event;
using Framework.QTower.View;
using UnityEngine.UI;
using Utils.Find;

namespace UI.Summary
{
    public sealed class UISummary : UIBase
    {
        private const string BUTTON_RETRY_PATH = "Content/Btn_retry";

        private Button m_retryButton;

        protected override void OnInit()
        {
            m_retryButton = this.FindChildComponent<Button>(BUTTON_RETRY_PATH);
            m_retryButton.onClick.AddListener(OnRetryButtonClicked);
        }

        protected override void OnDispose()
        {
            m_retryButton.onClick.RemoveListener(OnRetryButtonClicked);
        }

        // 发布重新挑战请求
        private void OnRetryButtonClicked()
        {
            EventManager.PublishEvent(EventDefines.PlayerRetryRequested);
        }
    }
}

