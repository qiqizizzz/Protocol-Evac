/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家可缓存输入数据，保存一次离散输入记录
 * │  类    名: PlayerBufferedInputData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.Input.Buffer
{
    public struct PlayerBufferedInputData
    {
        public float PressedTime { get; private set; }
        public bool IsConsumed { get; private set; }

        public PlayerBufferedInputData(float pressedTime)
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
