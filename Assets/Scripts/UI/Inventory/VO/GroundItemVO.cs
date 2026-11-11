/*
* ┌──────────────────────────────────┐
* │  描    述: 地面物品快照, 一件被丢到世界里的物品及其落点信息
* │  类    名: GroundItemVO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace UI.Inventory
{
    public sealed class GroundItemVO
    {
        //地面物品登记号, 由地面模型递增分配
        public string GroundItemId { get; }

        //承载这件物品的运行时容器号, 捡回背包要靠它做转移
        public int ContainerId { get; }

        //物品本体快照, 图标数量占格朝向全部沿用背包侧的定义
        public InventoryItemVO Item { get; }

        //落点世界坐标
        public Vector3 WorldPosition { get; }

        //落地时刻, 取 Time.time, 供排序与后续过期规则使用
        public float DropTime { get; }

        public GroundItemVO(string groundItemId, int containerId, InventoryItemVO item, Vector3 worldPosition,
            float dropTime)
        {
            GroundItemId = groundItemId;
            ContainerId = containerId;
            Item = item;
            WorldPosition = worldPosition;
            DropTime = dropTime;
        }
    }
}