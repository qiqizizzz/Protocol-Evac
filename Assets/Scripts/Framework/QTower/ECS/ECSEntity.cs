/*
 * ┌──────────────────────────────────┐
 * │  描    述: ECS实体类                      
 * │  类    名: ECSEntity.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Framework.QTower.Event.ECS
{
    public class ECSEntity
    {
        public int EntityId;
        public bool IsActive = true;
        public Dictionary<int, ECSComponent> Components = new Dictionary<int, ECSComponent>();
        public WorldBase World;

        public ECSEntity(WorldBase world)
        {
            World = world;
        }
        
        public T AddComponent<T>(int comType) where T : ECSComponent, new()
        {
            if (Components.ContainsKey(comType))
            {
                return (T) Components[comType];
            }

            ECSComponent com = new T();
            com.EntityId = EntityId;
            Components.Add(comType, com);
            World.AddComponent(com);
            return com as T;
        }

        public void RemoveComponent(int comType)
        {
            
        }

        public void RemoveAllComponents()
        {
            
        }

        public bool HasComponent(int comType)
        {
            return Components.ContainsKey(comType);
        }

        public T GetComponent<T>(int comType) where T : ECSComponent
        {
            if (Components.ContainsKey(comType))
            {
                return Components[comType] as T;
            }

            return null;
        }
    }
}