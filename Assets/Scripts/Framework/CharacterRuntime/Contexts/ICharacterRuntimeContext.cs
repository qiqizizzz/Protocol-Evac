/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色运行时子上下文生命周期契约
 * │  类    名: ICharacterRuntimeContext.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

namespace Framework.Character.Runtime.Contexts
{
    public interface ICharacterRuntimeContext
    {
        // 重置运行时数据
        void Reset();
    }
}
