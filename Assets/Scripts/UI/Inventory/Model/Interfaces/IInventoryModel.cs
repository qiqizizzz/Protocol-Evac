/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包模型契约接口                      
 * │  类    名: IInventoryModel.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.MVVM.Model.Interfaces;
using Module.Inventory.Core.Config.Loot;
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
        
        //换尺寸: 网格拓扑重建但容器身份不变, 内容放不下时回滚原拓扑并返回 false
        bool TryResizeContainer(int containerId, InventoryContainerSpec spec);

        //容器是否已注册
        bool IsContainerRegistered(int containerId);
        
        //自动找第一块空位塞入新物品, 穿戴装备与造演示物品的主路径
        InventoryMoveResult TryAddItemAuto(int containerId, int itemId, int amount);
        
        //打开野外物资箱, 摇一次掉落并注册成运行时容器; 同一份配置未关闭前重复打开返回原容器号
        int OpenLootContainer(LootContainerConfigSO loot, int seed);
        
        //获取容器快照
        InventoryContainerVO GetContainerVO(int containerId);
        
        //新物品放入容器
        InventoryMoveResult TryAddItem(int containerId, int itemId, int amount, Vector2Int cell);
        
        //物品移动
        InventoryMoveResult TryMoveItem(int containerId, int instanceId, Vector2Int cell, InventoryRotation rotation);
        
        // 转移物品, 目标位置被占用则与原物品交换, 当前朝向不可放置时自动改用可用朝向
        InventoryMoveResult TryTransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell);
        
        // 预判转移落点：不修改状态, 返回是否会触发交换与实际采用的朝向
        InventoryMoveResult PeekTransferItem(int sourceContainerId, int targetContainerId, int instanceId, Vector2Int cell,
            out bool willSwap, out InventoryRotation rotation);
        
        //物品旋转
        InventoryMoveResult TryRotateItem(int containerId, int instanceId);
        
        //物品实例号查询
        bool TryGetInstanceIdAt(int containerId, Vector2Int cell, out int instanceId);
    }
}
