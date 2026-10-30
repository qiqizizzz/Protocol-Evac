/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包网格, 纯数据结构                      
 * │  类    名: InventoryGrid.cs       
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Core.Grid
{
    public class InventoryGrid
    {
        public int Width { get; }
        public int Height { get; }

        public IReadOnlyCollection<InventoryItemPlacement> Placements => m_placements;

        public int Count => Placements.Count;

        private readonly InventoryItemPlacement[] m_cells;
        private readonly HashSet<InventoryItemPlacement> m_placements;

        public InventoryGrid(int width, int height)
        {
            Width = width;
            Height = height;

            m_cells = new InventoryItemPlacement[width * height];
            m_placements = new HashSet<InventoryItemPlacement>();
        }

        public bool Contains(InventoryItemPlacement placement) => m_placements.Contains(placement);

        #region 网格相关检测操作
        //单点检测(判断是否在网格范围内)
        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }
        
        //区域检测(判断给定区域是否完整落在网格范围内)
        public bool IsAreaInside(Vector2Int position, Vector2Int size)
        {
            if (position.x < 0 || position.y < 0) return false;

            return position.x + size.x <= Width && position.y + size.y <= Height;
        }
        
        public bool IsAreaFree(Vector2Int position, Vector2Int size, InventoryItemPlacement ignore = null)
        {
            if (!IsAreaInside(position, size)) return false;

            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var occupied = m_cells[(position.y + y) * Width + position.x + x];
                if(occupied != null && !ReferenceEquals(occupied, ignore)) return false;
            }

            return true;
        }

        //获取一维下标
        public int GetCellIndex(Vector2Int cell)
        {
            return cell.y * Width + cell.x;
        }
        #endregion
        
        #region 物品放置操作
        //判断是否能放下物品(以指定位置和朝向)
        public bool CanPlace(InventoryItemPlacement itemPlacement, Vector2Int position, InventoryRotation rotation,
            InventoryItemPlacement ignore = null)
        {
            Vector2Int size = itemPlacement.SizeAt(rotation);
            return IsAreaInside(position, size) && IsAreaFree(position, size, ignore);
        }

        //判断能否放在自身当前位置上
        public bool CanPlace(InventoryItemPlacement itemPlacement)
        {
            return CanPlace(itemPlacement, itemPlacement.Position, itemPlacement.Rotation, itemPlacement);
        }
        
        //放入网格
        public void Add(InventoryItemPlacement itemPlacement)
        {
            if(!CanPlace(itemPlacement)) QLog.Error($"[{nameof(InventoryGrid)}] 非法放置，越界或占位冲突：{itemPlacement}");
            
            m_placements.Add(itemPlacement);
            foreach (var cell in itemPlacement.EnumerateCells())
            {
                m_cells[GetCellIndex(cell)] = itemPlacement;
            }
        }
        
        //移出网格
        public bool Remove(InventoryItemPlacement itemPlacement)
        {
            if (!m_placements.Remove(itemPlacement)) return false;

            foreach (var cell in itemPlacement.EnumerateCells())
            {
                int index = GetCellIndex(cell);
                
                if(ReferenceEquals(m_cells[index], itemPlacement)) 
                    m_cells[index] = null;
            }
            
            return true;
        }
        #endregion
        
        public bool TryGetPlacementAt(Vector2Int cell, out InventoryItemPlacement itemPlacement)
        {
            itemPlacement = null;
            if (!IsInside(cell)) return false;
            itemPlacement = m_cells[GetCellIndex(cell)];
            return itemPlacement != null;
        }
        
        public override string ToString()
        {
            return $"[Grid {Width}x{Height}, {Count} placements]";
        }
    }
}