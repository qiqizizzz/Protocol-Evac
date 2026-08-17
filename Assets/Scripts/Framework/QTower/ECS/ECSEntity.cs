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

        public void AddComponent<T>(int comType) where T : ECSComponent
        {
            
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