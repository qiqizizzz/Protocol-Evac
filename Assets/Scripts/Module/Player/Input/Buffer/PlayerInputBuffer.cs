/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家输入缓存，负责记录与消费离散输入
 * │  类    名: PlayerInputBuffer.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Module.Player.Input.Buffer
{
    public sealed class PlayerInputBuffer
    {
        private readonly Dictionary<PlayerBufferedInputType, PlayerBufferedInputData> m_bufferedInputs =
            new Dictionary<PlayerBufferedInputType, PlayerBufferedInputData>();

        // 记录一次离散输入
        public void Record(PlayerBufferedInputType type, float pressedTime)
        {
            m_bufferedInputs[type] = new PlayerBufferedInputData(pressedTime);
        }

        // 判断指定输入是否仍可被消费
        public bool Has(PlayerBufferedInputType type, float nowTime, float bufferTime)
        {
            return m_bufferedInputs.TryGetValue(type, out PlayerBufferedInputData inputData)
                && inputData.IsValid(nowTime, bufferTime);
        }

        // 消费指定输入
        public void Consume(PlayerBufferedInputType type)
        {
            if (!m_bufferedInputs.TryGetValue(type, out PlayerBufferedInputData inputData))
                return;

            inputData.Consume();
            m_bufferedInputs[type] = inputData;
        }

        // 清空指定输入
        public void Clear(PlayerBufferedInputType type)
        {
            m_bufferedInputs.Remove(type);
        }

        // 清空全部输入缓存
        public void ClearAll()
        {
            m_bufferedInputs.Clear();
        }
    }
}
