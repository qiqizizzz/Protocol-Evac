/*
 * ┌──────────────────────────────────┐
 * │  描    述: 角色可缓存输入数据，保存一次离散输入记录
 * │  类    名: CharacterBufferedInputData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.Character.Runtime.Input
{
    public struct CharacterBufferedInputData
    {
        public float PressedTime { get; private set; }
        public bool IsConsumed { get; private set; }

        public CharacterBufferedInputData(float pressedTime)
        {
            PressedTime = pressedTime;
            IsConsumed = false;
        }

        // 判断输入数据是否仍处于可消费窗口
        public bool IsValid(float nowTime, float bufferTime)
        {
            return !IsConsumed && nowTime - PressedTime <= bufferTime;
        }

        // 标记输入数据已经被消费
        public void Consume()
        {
            IsConsumed = true;
        }
    }
}
