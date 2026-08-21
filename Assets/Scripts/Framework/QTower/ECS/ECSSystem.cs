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
        
        public virtual int[] RequireComponents
        {
            get { return null; }
        }
        
        public ECSSystem(WorldBase world)
        {
            World = world;
        }

        // 判断实体是否满足系统组件需求
        public bool Match(ECSEntity entity)
        {
            if (entity == null || !entity.IsActive)
                return false;

            int[] requireComponents = RequireComponents;
            if (requireComponents == null || requireComponents.Length == 0)
                return true;

            for (int i = 0; i < requireComponents.Length; i++)
            {
                if (!entity.HasComponent(requireComponents[i]))
                    return false;
            }
            
            return true;
        }

        // 将实体加入系统实体列表
        public void AddEntity(int entityId)
        {
            if (EntityDict.ContainsKey(entityId))
                return;

            EntityDict.Add(entityId, true);
            Entities.Add(entityId);
        }

        // 从系统实体列表移除实体
        public void RemoveEntity(int entityId)
        {
            if (!EntityDict.ContainsKey(entityId))
                return;

            EntityDict.Remove(entityId);
            Entities.Remove(entityId);
        }

        // 执行系统逻辑
        public virtual void Tick(float deltaTime)
        {
        }
    }
}
