/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色输入缓存，负责记录与消费离散输入
 * │  类    名: CharacterInputBuffer.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Framework.Character.Runtime.Input
{
    public sealed class CharacterInputBuffer
    {
        private readonly Dictionary<CharacterBufferedInputType, CharacterBufferedInputData> m_bufferedInputs =
            new Dictionary<CharacterBufferedInputType, CharacterBufferedInputData>();

        // 记录一次离散输入
        public void Record(CharacterBufferedInputType type, float pressedTime)
        {
            m_bufferedInputs[type] = new CharacterBufferedInputData(pressedTime);
        }

        // 判断指定输入是否仍可被消费
        public bool Has(CharacterBufferedInputType type, float nowTime, float bufferTime)
        {
            return m_bufferedInputs.TryGetValue(type, out CharacterBufferedInputData inputData)
                && inputData.IsValid(nowTime, bufferTime);
        }

        // 消费指定输入
        public void Consume(CharacterBufferedInputType type)
        {
            if (!m_bufferedInputs.TryGetValue(type, out CharacterBufferedInputData inputData))
                return;

            inputData.Consume();
            m_bufferedInputs[type] = inputData;
        }

        // 清空指定输入
        public void Clear(CharacterBufferedInputType type)
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
