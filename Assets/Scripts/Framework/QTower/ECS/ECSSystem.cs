/*
 * ┌──────────────────────────────────┐
 * │  描    述: ECS系统类                      
 * │  类    名: ECSSystem.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Framework.QTower.Event.ECS
{
    public class ECSSystem
    {
        public WorldBase World;
        public List<int> Entities = new List<int>();
        public Dictionary<int,bool> EntityDict = new Dictionary<int,bool>();    

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