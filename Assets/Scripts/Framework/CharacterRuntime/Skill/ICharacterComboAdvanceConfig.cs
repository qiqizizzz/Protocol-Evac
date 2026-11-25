/*
 * ┌─────────────────────────────────────────────────────┐
 * │  描    述: 角色连段推进配置契约
 * │  类    名: ICharacterComboAdvanceConfig.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────┘
 */

namespace Framework.Character.Runtime.Skill
{
    public interface ICharacterComboAdvanceConfig
    {
        float ComboRecoveryBufferTime { get; }
    }
}
