/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品配置数据类                      
 * │  类    名: InvItemConfigSO.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "InvItemConfig", menuName = "配置/背包/物品定义配置")]
    public sealed class InvItemConfigSO : ScriptableObject
    {
        [SerializeField] private string ConfigIdValue = string.Empty;
        
        [SerializeField, Min(1)] private int GridWidthValue = 1;
        [SerializeField, Min(1)] private int GridHeightValue = 1;
        
        public string ConfigId => ConfigIdValue;
        public int GridWidth => GridWidthValue;
        public int GridHeight => GridHeightValue;
    }
}