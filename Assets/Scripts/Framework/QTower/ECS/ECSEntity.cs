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
        
        // 为实体添加指定类型的组件
        public T AddComponent<T>(int comType) where T : ECSComponent, new()
        {
            if (Components.ContainsKey(comType))
            {
                return (T) Components[comType];
            }

            ECSComponent com = new T();
            com.EntityId = EntityId;
            Components.Add(comType, com);
            com.OnAdd();
            World.RefreshEntitySystems(this);
            return com as T;
        }

        // 从实体移除指定类型的组件
        public void RemoveComponent(int comType)
        {
            if (Components.ContainsKey(comType))
            {
                ECSComponent com = Components[comType];
                com.OnRemove();
                Components.Remove(comType);
                com.Recycle();
                World.RefreshEntitySystems(this);
            }
        }

        // 移除实体上的全部组件
        public void RemoveAllComponents()
        {
            List<int> keys = new List<int>(Components.Keys);
            for (int i = keys.Count - 1; i >= 0 ; i--)
            {
                RemoveComponent(keys[i]);
            }
        }

        // 判断实体是否持有指定组件
        public bool HasComponent(int comType)
        {
            return Components.ContainsKey(comType);
        }

        // 获取实体上的指定组件
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
