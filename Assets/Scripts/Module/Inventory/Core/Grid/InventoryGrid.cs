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

        //实心掩码, 下标与占用表一致, 长度恒为 Width*Height
        public IReadOnlyList<bool> Mask => m_mask;

        //真实存在的格子数, 异形容器比外接矩形小
        public int SolidCount { get; }

        public IReadOnlyCollection<InventoryItemPlacement> Placements => m_placements;

        public int Count => Placements.Count;

        private readonly InventoryItemPlacement[] m_cells;
        private readonly bool[] m_mask;
        private readonly HashSet<InventoryItemPlacement> m_placements;

        public InventoryGrid(int width, int height) : this(width, height, null)
        {
        }

        /// <summary>
        /// 建一张网格, 掩码留空就是满格矩形
        /// </summary>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        /// <param name="cells">实心掩码, 按行主序排列; 传 null 或长度不符时按满格处理并报错</param>
        public InventoryGrid(int width, int height, IReadOnlyList<bool> cells)
        {
            Width = width;
            Height = height;

            m_cells = new InventoryItemPlacement[width * height];
            m_mask = new bool[width * height];
            m_placements = new HashSet<InventoryItemPlacement>();

            SolidCount = FillMask(cells);
        }

        public bool Contains(InventoryItemPlacement placement) => m_placements.Contains(placement);

        #region 网格相关检测操作
        //单点检测(判断是否在外接矩形范围内)
        public bool IsInside(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Height;
        }

        //单点检测(判断这一格真实存在, 不是被挖掉的空洞)
        public bool IsSolid(Vector2Int cell)
        {
            return IsInside(cell) && m_mask[GetCellIndex(cell)];
        }

        //区域检测(判断给定区域完整落在可用空间内, 压到空洞同样算越界)
        public bool IsAreaInside(Vector2Int position, Vector2Int size)
        {
            if (position.x < 0 || position.y < 0) return false;
            if (position.x + size.x > Width || position.y + size.y > Height) return false;

            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                if (!m_mask[(position.y + y) * Width + position.x + x]) return false;
            }

            return true;
        }

        public bool IsAreaFree(Vector2Int position, Vector2Int size, InventoryItemPlacement ignore = null)
            => IsAreaFree(position, size, ignore, null);

        //区域检测(判断区域内除指定两件物品外没有其他占用, 同容器互换需同时忽略双方)
        public bool IsAreaFree(Vector2Int position, Vector2Int size, InventoryItemPlacement ignoreA,
            InventoryItemPlacement ignoreB)
        {
            if (!IsAreaInside(position, size)) return false;

            for (int y = 0; y < size.y; y++)
            for (int x = 0; x < size.x; x++)
            {
                InventoryItemPlacement occupied = m_cells[(position.y + y) * Width + position.x + x];
                //空格子本来就没主, 不能拿它去和两件忽略物品比引用, 否则双忽略的互换试算会全判占用
                if (occupied == null) continue;

                if (!ReferenceEquals(occupied, ignoreA) && !ReferenceEquals(occupied, ignoreB)) return false;
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

        //填实心表并数出真实格子数, 掩码为空就是满格矩形, 长度不对才报错兜底
        private int FillMask(IReadOnlyList<bool> cells)
        {
            if (cells == null) return FillAllSolid(null);
            if (cells.Count != m_mask.Length) return FillAllSolid($"掩码长度 {cells.Count} 与网格 {m_mask.Length} 不符");

            int solid = 0;
            for (int i = 0; i < m_mask.Length; i++)
            {
                m_mask[i] = cells[i];
                if (cells[i]) solid++;
            }

            return solid;
        }

        private int FillAllSolid(string reason)
        {
            if (reason != null) QLog.Error($"{reason}, 已按满格处理：{Width}x{Height}");

            for (int i = 0; i < m_mask.Length; i++)
                m_mask[i] = true;

            return m_mask.Length;
        }
        
        public override string ToString()
        {
            return $"[Grid {Width}x{Height}, {SolidCount} cells, {Count} placements]";
        }
    }
}