/*
* ┌──────────────────────────────────┐
* │  描    述: 可搜刮目标契约, 物资箱与地面垃圾堆共用同一套判距与准入
* │  类    名: ILootInteractable.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace Module.Inventory.Interaction
{
    /// <summary>
    /// 可搜刮的世界目标：只暴露判距中心与准入, 不牵扯容器与模型
    /// </summary>
    public interface ILootInteractable
    {
        // 选择器用的判距中心, 交互体缺失时退回自身位置
        Vector3 InteractionCenter { get; }

        // 遮挡采样用的世界包围盒
        Bounds InteractionBounds { get; }

        // 配置齐备且还值得交互才返回 true
        bool CanInteract();
    }
}
