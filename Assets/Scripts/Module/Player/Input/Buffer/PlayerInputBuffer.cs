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
        private readonly Dictionary<PlayerBufferedInputType, PlayerBufferedInputSlot> m_slots =
            new Dictionary<PlayerBufferedInputType, PlayerBufferedInputSlot>();

        // 记录一次离散输入
        public void Record(PlayerBufferedInputType type, float pressedTime)
        {
            m_slots[type] = new PlayerBufferedInputSlot(type, pressedTime);
        }

        // 判断指定输入是否仍可被消费
        public bool Has(PlayerBufferedInputType type, float nowTime, float bufferTime)
        {
            return m_slots.TryGetValue(type, out PlayerBufferedInputSlot slot) && slot.IsValid(nowTime, bufferTime);
        }

        // 消费指定输入
        public void Consume(PlayerBufferedInputType type)
        {
            if (!m_slots.TryGetValue(type, out PlayerBufferedInputSlot slot))
                return;

            slot.Consume();
            m_slots[type] = slot;
        }

        // 清空指定输入
        public void Clear(PlayerBufferedInputType type)
        {
            m_slots.Remove(type);
        }

        // 清空全部输入缓存
        public void ClearAll()
        {
            m_slots.Clear();
        }
    }
}
