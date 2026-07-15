/*
 * ┌──────────────────────────────────┐
 * │  描    述: 玩家状态基类
 * │  类    名: BasePlayerState.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Player.HFSM
{
    public abstract class BasePlayerState
    {
        public abstract PlayerStateId Id { get; }
        
        public abstract PlayerStateId ParentId { get; }
        
        public virtual void Enter()
        {
            
        }
        
        public virtual void Exit()
        {
            
        }
        
        public virtual void Tick(float deltaTime)
        {
            
        }
        
        public virtual void FixedTick(float fixedDeltaTime)
        {
            
        }
    }
}