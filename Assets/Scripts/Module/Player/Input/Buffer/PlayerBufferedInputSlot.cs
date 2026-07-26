/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家可缓存输入槽，保存一次离散输入记录
 * │  类    名: PlayerBufferedInputSlot.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.Input.Buffer
{
    public struct PlayerBufferedInputSlot
    {
        public PlayerBufferedInputType Type { get; private set; }
        public float PressedTime { get; private set; }
        public bool IsConsumed { get; private set; }
        public bool HasValue { get; private set; }

        public PlayerBufferedInputSlot(PlayerBufferedInputType type, float pressedTime)
        {
            Type = type;
            PressedTime = pressedTime;
            IsConsumed = false;
            HasValue = true;
        }

        // 判断输入槽是否仍处于可消费窗口
        public bool IsValid(float nowTime, float bufferTime)
        {
            return HasValue && !IsConsumed && nowTime - PressedTime <= bufferTime;
        }

        // 标记输入槽已经被消费
        public void Consume()
        {
            IsConsumed = true;
        }

        // 清空输入槽
        public void Clear()
        {
            Type = default;
            PressedTime = 0f;
            IsConsumed = false;
            HasValue = false;
        }
    }
}
