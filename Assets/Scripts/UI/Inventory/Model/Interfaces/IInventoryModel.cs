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
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using UnityEngine;

namespace UI.Inventory
{
    public interface IInventoryModel : IModel
    {
        event Action<int> OnChanged;
        
        //注册容器
        void RegisterContainer(int containerId, InventoryContainerSpec spec);
        
        //注销容器
        void UnregisterContainer(int containerId);
        
        //获取容器快照
        InventoryContainerVO GetContainerVO(int containerId);
        
        //新物品放入容器
        InventoryMoveResult TryAddItem(int containerId, int itemId, int amount, Vector2Int cell);
        
        //物品移动
        InventoryMoveResult TryMoveItem(int containerId, int instanceId, Vector2Int cell, InventoryRotation rotation);
        
        //转移物品, 目标位置被占则与原物品交换, 朝向保持当前不变
        InventoryMoveResult TryTransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell);
        
        //预判转移落点, 纯查询不改状态, 顺带告知这一落会不会触发交换
        InventoryMoveResult PeekTransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell,
            out bool willSwap);
        
        //物品旋转
        InventoryMoveResult TryRotateItem(int containerId, int instanceId);
        
        //物品实例号查询
        bool TryGetInstanceIdAt(int containerId, Vector2Int cell, out int instanceId);
    }
}