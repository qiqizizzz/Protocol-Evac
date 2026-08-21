/*
 * ┌──────────────────────────────────┐
 * │  描    述: ECS世界基类                      
 * │  类    名: WorldBase.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Utils.log;

namespace Framework.QTower.Event.ECS
{
    public class WorldBase
    {
        public Dictionary<int, ECSEntity> Entities = new Dictionary<int, ECSEntity>();
        public Dictionary<int, ECSSystem> Systems = new Dictionary<int, ECSSystem>();
        public Dictionary<int, Dictionary<int, ECSComponent>> Components =
            new Dictionary<int, Dictionary<int, ECSComponent>>();
        
        public virtual void Register()
        {
            
        }

        public void AddSystem(int sysType)
        {
            
        }
        
        // 注册一个系统实例
        public void AddSystem(ECSSystem system)
        {
            if (system == null)
            {
                QLog.Error("添加 ECS 系统失败：系统为空");
                return;
            }

            if (Systems.ContainsKey(system.SysType))
            {
                QLog.Error($"添加 ECS 系统失败：系统类型重复 {system.SysType}");
                return;
            }

            Systems.Add(system.SysType, system);
            RefreshSystemEntities(system);
        }

        // 移除指定系统类型
        public void RemoveSystem(int sysType)
        {
            if (!Systems.ContainsKey(sysType))
                return;

            ECSSystem system = Systems[sysType];
            foreach (int entityId in new List<int>(system.Entities))
                system.RemoveEntity(entityId);

            Systems.Remove(sysType);
        }

        // 创建 ECS 实体
        public ECSEntity CreateEntity(int entityId)
        {
            if (Entities.ContainsKey(entityId))
                return Entities[entityId];
            
            ECSEntity entity = new ECSEntity(this);
            entity.EntityId = entityId;
            entity.IsActive = true;
            Entities.Add(entityId, entity);
            RefreshEntitySystems(entity);
            return entity;
        }
        
        // 删除 ECS 实体
        public void DeleteEntity(int entityId)
        {
            if (!Entities.ContainsKey(entityId))
                return;

            ECSEntity entity = Entities[entityId];
            entity.IsActive = false;
            entity.RemoveAllComponents();
            RefreshEntitySystems(entity);
            Entities.Remove(entityId);
        }

        // 获取指定 ECS 实体
        public ECSEntity GetEntity(int entityId)
        {
            if (Entities.ContainsKey(entityId))
                return Entities[entityId];

            return null;
        }

        // 每帧执行全部系统
        public void Tick(float deltaTime)
        {
            foreach (ECSSystem system in Systems.Values)
                system.Tick(deltaTime);
        }

        // 刷新某个实体和全部系统的匹配关系
        public void RefreshEntitySystems(ECSEntity entity)
        {
            if (entity == null)
                return;

            foreach (ECSSystem system in Systems.Values)
            {
                if (system.Match(entity))
                    system.AddEntity(entity.EntityId);
                else
                    system.RemoveEntity(entity.EntityId);
            }
        }

        // 刷新某个系统和全部实体的匹配关系
        private void RefreshSystemEntities(ECSSystem system)
        {
            if (system == null)
                return;

            foreach (ECSEntity entity in Entities.Values)
            {
                if (system.Match(entity))
                    system.AddEntity(entity.EntityId);
                else
                    system.RemoveEntity(entity.EntityId);
            }
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
