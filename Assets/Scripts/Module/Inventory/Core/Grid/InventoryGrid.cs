/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包网格                      
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

        public IReadOnlyCollection<InventoryPlacement> Placements => m_placements;

        public int Count => Placements.Count;

        private readonly InventoryPlacement[] m_cells;
        private readonly HashSet<InventoryPlacement> m_placements;

        public InventoryGrid(int width, int height)
        {
            Width = width;
            Height = height;

            m_cells = new InventoryPlacement[width * height];
            m_placements = new HashSet<InventoryPlacement>();
        }

        //判断是否在网格范围内
        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        //获取一维下标
        public int GetCellIndex(Vector2Int cell)
        {
            return cell.y * Width + cell.x;
        }

        public bool TryGetPlacementAt(Vector2Int cell, out InventoryPlacement placement)
        {
            placement = null;
            if (!IsInside(cell)) return false;
            placement = m_cells[GetCellIndex(cell)];
            return placement != null;
        }

        //判断是否能放下物品(以指定位置和朝向)
        public bool CanPlace(InventoryPlacement placement, Vector2Int position, InventoryRotation rotation,
            InventoryPlacement ignore = null)
        {
            Vector2Int size = placement.SizeAt(rotation);
            if(position.x < 0 || position.y < 0) return false;
            if(position.x + size.x > Width || position.y + size.y > Height) return false;
            
            for(int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                var occupied = m_cells[(position.y + y) * Width + position.x + x];
                if(occupied != null && !ReferenceEquals(occupied, ignore)) return false;
            }
            
            return true;
        }

        //判断能否放在自身当前位置上
        public bool CanPlace(InventoryPlacement placement)
        {
            return CanPlace(placement, placement.Position, placement.Rotation, placement);
        }
        
        //放入网格
        public void Add(InventoryPlacement placement)
        {
            if(!CanPlace(placement)) QLog.Error($"[{nameof(InventoryGrid)}] 非法放置，越界或占位冲突：{placement}");
            
            m_placements.Add(placement);
            foreach (var cell in placement.EnumerateCells())
            {
                m_cells[GetCellIndex(cell)] = placement;
            }
        }
        
        //移出网格
        public bool Remove(InventoryPlacement placement)
        {
            if (!m_placements.Remove(placement)) return false;

            for (int i = 0; i < m_cells.Length; i++)
            {
                if(ReferenceEquals(m_cells[i],placement))
                    m_cells[i] = null;
            }
            
            return true;
        }
        
        public override string ToString()
        {
            return $"[Grid {Width}x{Height}, {Count} placements]";
        }
    }
}