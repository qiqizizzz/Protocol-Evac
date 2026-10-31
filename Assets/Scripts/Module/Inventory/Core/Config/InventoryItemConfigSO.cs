/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品配置数据类                      
 * │  类    名: InventoryItemConfigSO.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "ItemConfig", menuName = "配置/背包/物品定义配置")]
    public sealed class InventoryItemConfigSO : ScriptableObject
    {
        [Header("基础属性")]
        [Tooltip("全局唯一编号")] 
        [SerializeField, Min(1)] private int ItemIdValue;
        
        [Tooltip("物品显示名")]
        [SerializeField] private string DisplayNameValue = string.Empty;
        
        [Tooltip("物品类型")]
        [SerializeField] private InventoryItemType ItemTypeValue = InventoryItemType.None;
        
        [Tooltip("是否允许 90 度旋转")]
        [SerializeField] private bool CanRotateValue = true;
        
        [Header("堆叠与重量")]
        [Tooltip("最大堆叠数")]
        [SerializeField,Min(1)] private int MaxStackCountValue = 1;
        
        [Tooltip("单件重量/kg")]
        [SerializeField,Min(0f)] private float UnitWeightValue;
        
        [Header("占格")]
        [SerializeField, Min(1)] private int GridWidthValue = 1;
        [SerializeField, Min(1)] private int GridHeightValue = 1;

        [Header("UI表现")] 
        [SerializeField] private Sprite IconValue;
        
        public int ItemId => ItemIdValue;
        public string DisplayName => DisplayNameValue;
        public InventoryItemType ItemType => ItemTypeValue;
        public bool CanRotate => CanRotateValue;
        public int MaxStackCount => MaxStackCountValue;
        public float UnitWeight => UnitWeightValue;
        public Vector2Int Size => new Vector2Int(GridWidthValue, GridHeightValue);
        public Sprite Icon => IconValue;
        public bool IsStackable => MaxStackCountValue > 1;
        
        //指定朝下的占格尺寸(用于试算)
        public Vector2Int GetSizeAt(InventoryRotation rotation)
        {
            Vector2Int size = Size;
            return (int)rotation % 2 == 1 ? new Vector2Int(size.y, size.x) : size;
        }
    }
}