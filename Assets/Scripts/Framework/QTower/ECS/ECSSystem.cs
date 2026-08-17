/*
 * ┌──────────────────────────────────┐
 * │  描    述: ECS系统类                      
 * │  类    名: ECSSystem.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.QTower.Event.ECS
{
    public class ECSSystem
    {
        public WorldBase World;

        public virtual int SysType
        {
            get { return 0; }
        }
        
        public ECSSystem(WorldBase world)
        {
            World = world;
        }
    }
}