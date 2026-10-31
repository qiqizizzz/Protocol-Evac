/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包模型契约接口                      
 * │  类    名: IInventoryModel.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.MVVM.Model.Interfaces;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace UI.Inventory
{
    public interface IInventoryModel : IModel
    {
        event Action<int> OnChanged;
        
        //注册容器
        void RegisterContainer(int containerId, int width, int height, float weightLimit);
        
        //注销容器
        void UnregisterContainer(int containerId);
        
        //获取容器快照
        InventoryContainerVO GetContainerVO(int containerId);
        
        //新物品放入容器
        InventoryMoveResult TryAddItem(int containerId, int itemId, int amount, Vector2Int cell);
        
        //物品移动
        InventoryMoveResult TryMoveItem(int containerId, int instanceId, Vector2Int cell, InventoryRotation rotation);
        
        //物品旋转
        InventoryMoveResult TryRotateItem(int containerId, int instanceId);
        
        //物品实例号查询
        bool TryGetInstanceIdAt(int containerId, Vector2Int cell, out int instanceId);
    }
}