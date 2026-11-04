/*
 * ┌──────────────────────────────────┐
 * │  描    述: 容器规格定义                      
 * │  类    名: InventoryContainerSpec.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Module.Inventory.Core.Data
{
    public readonly struct InventoryContainerSpec
    {
        public int Width { get; }
        public int Height { get; }
        
        public float WeightLimit { get; }
        
        public int CellCount =>  Width * Height;

        public InventoryContainerSpec(int width, int height, float weightLimit)
        {
            Width = width;
            Height = height;
            WeightLimit = weightLimit;
        }
    }
}