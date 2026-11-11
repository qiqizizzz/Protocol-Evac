/*
* ┌──────────────────────────────────┐
* │  描    述: 物品双击判定器, 按实例号与点击时刻识别同一物品上的连续两次左键
* │  类    名: DoubleClickDetector.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

namespace UI.Inventory.View.Tools
{
    /// <summary>
    /// 双击判定器：只记上一次点击的物品与时刻, 时刻由调用方传入, 不绑定 Unity 时间以便编辑期测试
    /// </summary>
    public sealed class DoubleClickDetector
    {
        private const int NO_ITEM = -1; //当前没有待配对的物品

        //两次点击算作双击的最大间隔（秒）
        private const float DOUBLE_CLICK_INTERVAL = 0.3f;

        private int m_lastInstanceId = NO_ITEM;
        private float m_lastClickTime;

        /// <summary>
        /// 记录一次点击, 判断它是否与上一次落在同一物品上且间隔够短
        /// </summary>
        /// <param name="instanceId">被点击物品的实例号</param>
        /// <param name="time">点击时刻, 运行期传 Time.time</param>
        /// <param name="isDoubleClick">是否构成双击</param>
        /// <returns>点击是否被受理, 实例号无效时返回 false</returns>
        public bool TryClick(int instanceId, float time, out bool isDoubleClick)
        {
            isDoubleClick = false;
            if (instanceId == NO_ITEM) return false;

            // 时刻倒退说明跨了场景或时间基准变了, 旧的配对作废
            float elapsed = time - m_lastClickTime;
            if (instanceId == m_lastInstanceId && elapsed >= 0f && elapsed <= DOUBLE_CLICK_INTERVAL)
            {
                // 双击已消费, 清掉记录, 第三击重新按首次算
                Reset();
                isDoubleClick = true;
                return true;
            }

            m_lastInstanceId = instanceId;
            m_lastClickTime = time;
            return true;
        }

        // 换容器、换物品或视图复用时清空配对状态
        public void Reset()
        {
            m_lastInstanceId = NO_ITEM;
            m_lastClickTime = 0f;
        }
    }
}
