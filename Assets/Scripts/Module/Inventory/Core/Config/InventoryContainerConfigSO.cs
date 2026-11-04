/*
* ┌──────────────────────────────────┐
* │  描    述: 容器定义配置, 描述一块网格自身的尺寸与名称
* │  类    名: InventoryContainerConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "ContainerConfig", menuName = "配置/背包/容器定义配置")]
    public sealed class InventoryContainerConfigSO : ScriptableObject
    {
        [Header("标识")]
        [Tooltip("容器显示名, 用于载体表头的容器标题")]
        [SerializeField] private string DisplayNameValue = string.Empty;

        [Header("网格")]
        [Tooltip("网格列数")]
        [SerializeField, Min(1)] private int WidthValue = 1;

        [Tooltip("网格行数")]
        [SerializeField, Min(1)] private int HeightValue = 1;

        public string DisplayName => DisplayNameValue;
        public int Width => WidthValue;
        public int Height => HeightValue;
    }
}
