/*
* ┌──────────────────────────────────┐
* │  描    述: 地面垃圾堆快照, 一堆落地物对应的容器号与世界锚点
* │  类    名: GroundPileVO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using UnityEngine;

namespace UI.Inventory
{
    public sealed class GroundPileVO
    {
        //堆登记号, 由地面模型递增分配
        public string PileId { get; }

        //承载这堆物品的运行时容器号, 打开搜刮与收拢丢弃物都靠它
        public int ContainerId { get; }

        //堆中心世界坐标, 3D 表现落位与交互判距用它
        public Vector3 AnchorPosition { get; }

        //起堆时刻, 取 Time.time, 供排序与后续过期规则使用
        public float DropTime { get; }

        public GroundPileVO(string pileId, int containerId, Vector3 anchorPosition, float dropTime)
        {
            PileId = pileId;
            ContainerId = containerId;
            AnchorPosition = anchorPosition;
            DropTime = dropTime;
        }
    }
}
