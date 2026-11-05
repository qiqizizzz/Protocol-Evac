/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品网格占位记录类                      
 * │  类    名: InventoryItemPlacement.cs   
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;
using UnityEngine;

namespace Module.Inventory.Core.Data
{
    public class InventoryItemPlacement
    {
        public InventoryItemInstance Instance { get; }

        public Vector2Int Position { get; private set; }
        
        public InventoryRotation Rotation { get; private set; }

        public Vector2Int Size => SizeAt(Rotation);

        public int CellCount => Size.x * Size.y;

        //实际占下的格子范围, 装备槽把大件压成单格时由容器写入; 未写入时沿用配置尺寸
        public Vector2Int Footprint { get; internal set; }

        public InventoryItemPlacement(InventoryItemInstance instance, Vector2Int position, InventoryRotation rotation = InventoryRotation.None)
        {
            Instance = instance;
            Position = position;
            Rotation = rotation;
        }
        
        //指定向下的占格尺寸, 容器写过足迹后一律以足迹为准
        public Vector2Int SizeAt(InventoryRotation rotation)
            => Footprint.x > 0 && Footprint.y > 0 ? Footprint : Instance.Config.GetSizeAt(rotation);

        //判断某个格子是否被占据(AABB, 轴对齐包围盒)
        public bool Contains(Vector2Int cell)
        {
            Vector2Int size = Size; //避免重复计算开销
            return cell.x >= Position.x && cell.x < Position.x + size.x
                && cell.y >= Position.y && cell.y < Position.y + size.y;
        }
        
        //枚举自己占据的所有格子(仅在放置与移除时调用)
        public IEnumerable<Vector2Int> EnumerateCells()
        {
            Vector2Int size = Size; //避免重复计算开销
            for(int y=0;y<size.y;y++)
                for(int x=0;x<size.x;x++)
                    yield return new Vector2Int(Position.x + x, Position.y + y);
        }

        #region 状态修改
        //移动位置
        public void SetPosition(Vector2Int position)
        {
            Position = position;
        }

        //设置朝向
        public void SetRotation(InventoryRotation rotation)
        {
            Rotation = rotation;
        }

        public override string ToString()
        {
            return $"[{Instance} - Position: {Position} ,Rotation: {Rotation}]";
        }
        #endregion

    }
}