/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 玩家运行时子上下文生命周期契约
 * │  类    名: IPlayerRuntimeContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

namespace Module.Player.Context.Runtime
{
    public interface IPlayerRuntimeContext
    {
        // 重置运行时数据
        void Reset();
    }
}
