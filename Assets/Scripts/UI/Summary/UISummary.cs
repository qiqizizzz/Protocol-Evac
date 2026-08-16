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
        private const string RETRY_BUTTON_PATH = "Content/RetryButton";

        private Button m_retryButton;

        protected override void OnInit()
        {
            m_retryButton = this.FindChildComponent<Button>(RETRY_BUTTON_PATH);
            m_retryButton.onClick.AddListener(HandleRetryButtonClicked);
        }

        protected override void OnDispose()
        {
            m_retryButton.onClick.RemoveListener(HandleRetryButtonClicked);
        }

        // 发布重新挑战请求
        private void HandleRetryButtonClicked()
        {
            EventManager.PublishEvent(EventDefines.PlayerRetryRequested);
        }
    }
}

