/*
 * ┌──────────────────────────────────┐
 * │  描    述: ECS世界基类                      
 * │  类    名: WorldBase.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.QTower.Event.ECS
{
    public class WorldBase
    {
        public virtual void Register()
        {
            
        }

        public void AddSystem(int sysType)
        {
            
        }
        
        public void AddComponent(ECSComponent com)
        {
            
        }

        public void RemoveComponent(ECSComponent com)
        {
            
        }

        public ECSEntity CreateEntity(int entityId)
        {
            return null;
        }
        
        public void DeleteEntity(int entityId)
        {
            
        }

        public virtual void RestoreState(byte[] data)
        {
            
        }

        public virtual byte[] CaptureState()
        {
            return new byte[0];
        }
    }
}