/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品运行时实例                      
 * │  类    名: InventoryItemInstance.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config;

namespace Module.Inventory.Core.Data
{
    public class InventoryItemInstance
    {
        private static int m_nextInstanceId = 1;
        
        public InventoryItemConfigSO Config { get; }
        
        public int InstanceId { get; }
        public int StackCount { get; private set; }

        public int ItemId => Config.ItemId;
        public bool IsStackable => Config.IsStackable;
        public int MaxStackCount => Config.MaxStackCount;
        
        public float Weight => Config.UnitWeight * StackCount;

        public InventoryItemInstance(InventoryItemConfigSO config, int stackCount = 1)
        {
            Config = config;
            InstanceId = m_nextInstanceId++;
            StackCount = ClampStackCount(stackCount);
        }

        //增加堆叠数量
        public bool TryAddStack(int count, out int accepted)
        {
            accepted = 0;
            int space = MaxStackCount - StackCount;
            if (count <= 0 || !IsStackable || space <= 0) return false;

            accepted = count < space ? count : space;
            StackCount += accepted;
            return true;
        }
        
        //减少堆叠数量
        public bool TryReduceStack(int count)
        {
            if (count <= 0 || count >= StackCount) return false;

            StackCount -= count;
            return true;
        }
        
        public override string ToString()
        {
            return $"[{Config.DisplayName} - ID：{ItemId} ,StackCount: x{StackCount} ,InstanceId: #{InstanceId}]";
        }
        
        private int ClampStackCount(int stackCount)
        {
            int clamped = stackCount < 1 ? 1 : stackCount;
            return clamped > MaxStackCount ? MaxStackCount : clamped;
        }
    }
}