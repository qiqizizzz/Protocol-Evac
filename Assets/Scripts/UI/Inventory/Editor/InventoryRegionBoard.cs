/*
* ┌──────────────────────────────────┐
* │  描    述: 容器逻辑区域绘制板, 用 # 与 A-Z 拖刷格子并生成区域行
* │  类    名: InventoryRegionBoard.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UI.Inventory.Editor
{
    // 逻辑区域绘制板: 复用形状板的网格几何, 只允许在实心格上刷区域
    public sealed class InventoryRegionBoard
    {
        private const int MAX_REGION_ID = 26;

        private static readonly string[] S_RegionOptions = CreateRegionOptions();
        private static readonly Color[] S_RegionColors = CreateRegionColors();
        private static readonly Color S_HoleColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        private static readonly Color S_BoardColor = new Color(0.18f, 0.19f, 0.21f, 1f);

        private int[] m_regions;
        private bool[] m_mask;
        private int m_width;
        private int m_height;
        private int m_brushRegion;
        private int m_paintRegion;
        private bool m_isPainting;
        private GUIStyle m_cellLabelStyle;

        public int[] Regions => m_regions;

        // 一次绘制板操作处于哪个阶段, 拆出来是为了让刷格逻辑能脱离 Event 单测
        public enum StrokePhase
        {
            Down,
            Drag,
            Up
        }

        /// <summary>
        /// 用现有区域与形状重建画板, 空区域按默认区域 # 处理
        /// </summary>
        /// <param name="regions">行主序区域表, 空洞为 -1</param>
        /// <param name="mask">行主序实心掩码, 空或长度不符时按满格处理</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public void Load(IReadOnlyList<int> regions, IReadOnlyList<bool> mask, int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            bool[] shape = CreateMask(width, height);

            if (mask != null && mask.Count == shape.Length)
            {
                for (int i = 0; i < shape.Length; i++) shape[i] = mask[i];
            }

            int[] regionIds = new int[shape.Length];
            bool hasRegions = regions != null && regions.Count == regionIds.Length;

            for (int i = 0; i < regionIds.Length; i++)
            {
                if (!shape[i])
                {
                    regionIds[i] = -1;
                    continue;
                }

                int regionId = hasRegions ? regions[i] : 0;
                regionIds[i] = regionId >= 0 && regionId <= MAX_REGION_ID ? regionId : 0;
            }

            m_regions = regionIds;
            m_mask = shape;
            m_width = width;
            m_height = height;
            m_isPainting = false;
        }

        /// <summary>
        /// 形状板变化后同步区域画板, 新空洞清空区域, 其余实心格保留原区域
        /// </summary>
        /// <param name="mask">新的行主序实心掩码</param>
        public void SyncMask(IReadOnlyList<bool> mask)
            => SyncMask(mask, m_width, m_height);

        /// <summary>
        /// 按新尺寸同步区域画板, 左上重叠区保留原区域, 新格默认归入 #
        /// </summary>
        /// <param name="mask">新的行主序实心掩码</param>
        /// <param name="width">新的网格列数</param>
        /// <param name="height">新的网格行数</param>
        public void SyncMask(IReadOnlyList<bool> mask, int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            bool[] shape = CreateMask(width, height);

            if (mask != null && mask.Count == shape.Length)
            {
                for (int i = 0; i < shape.Length; i++) shape[i] = mask[i];
            }

            int oldWidth = m_width;
            int oldHeight = m_height;
            int[] oldRegions = m_regions;
            int[] regionIds = new int[shape.Length];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    if (!shape[index])
                    {
                        regionIds[index] = -1;
                        continue;
                    }

                    bool keep = oldRegions != null && x < oldWidth && y < oldHeight;
                    if (!keep)
                    {
                        regionIds[index] = 0;
                        continue;
                    }

                    int oldRegion = oldRegions[y * oldWidth + x];
                    regionIds[index] = oldRegion >= 0 ? oldRegion : 0;
                }
            }

            m_regions = regionIds;
            m_mask = shape;
            m_width = width;
            m_height = height;
            m_isPainting = false;
        }

        /// <summary>
        /// 画区域画笔与整块绘制板, 返回本次是否改动过区域
        /// </summary>
        /// <param name="label">绘制板标题</param>
        /// <param name="tooltip">标题悬停提示</param>
        public bool Draw(string label, string tooltip)
        {
            if (m_regions == null) Load(null, null, m_width, m_height);

            EditorGUILayout.LabelField(new GUIContent(label, tooltip), EditorStyles.boldLabel);
            m_brushRegion = EditorGUILayout.Popup(
                new GUIContent("区域画笔", "选择 # 或 A-Z 后左键拖刷; 右键始终刷回默认区域 #"),
                m_brushRegion,
                S_RegionOptions);

            Vector2 boardSize = InventoryShapeBoard.BoardSize(m_width, m_height);
            Rect board = GUILayoutUtility.GetRect(boardSize.x, boardSize.y);
            bool changed = HandlePointer(board);

            EditorGUI.DrawRect(board, S_BoardColor);
            for (int i = 0; i < m_regions.Length; i++)
            {
                Rect cell = InventoryShapeBoard.CellRect(board, i % m_width, i / m_width);
                int regionId = m_regions[i];
                EditorGUI.DrawRect(cell, regionId < 0 ? S_HoleColor : S_RegionColors[regionId]);
                if (regionId >= 0)
                    GUI.Label(cell, GetRegionMark(regionId).ToString(), GetCellLabelStyle());
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全部归 #"))
            {
                Fill(0);
                changed = true;
            }

            EditorGUILayout.LabelField("右键擦回 #", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        /// <summary>
        /// 把某一格刷成当前笔画区域, 返回是否改动
        /// </summary>
        /// <param name="index">行主序格子下标, 传负数表示没落在格子上</param>
        /// <param name="phase">本次操作阶段</param>
        /// <param name="regionId">按下时要使用的区域号</param>
        public bool ApplyStroke(int index, StrokePhase phase, int regionId)
        {
            if (phase == StrokePhase.Up)
            {
                m_isPainting = false;
                return false;
            }

            if (index < 0 || index >= m_regions.Length || !m_mask[index]) return false;
            if (phase == StrokePhase.Drag && !m_isPainting) return false;

            if (phase == StrokePhase.Down)
            {
                m_isPainting = true;
                m_paintRegion = Mathf.Clamp(regionId, 0, MAX_REGION_ID);
            }

            if (m_regions[index] == m_paintRegion) return false;

            m_regions[index] = m_paintRegion;
            return true;
        }

        /// <summary>
        /// 把一行区域号写成 # 或 A-Z 标记
        /// </summary>
        /// <param name="regions">行主序区域表</param>
        /// <param name="y">行号</param>
        /// <param name="width">网格列数</param>
        public static string ToMarkRow(IReadOnlyList<int> regions, int y, int width)
        {
            if (regions == null || width < 1) return string.Empty;

            char[] row = new char[width];
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                row[x] = index >= 0 && index < regions.Count
                    ? GetRegionMark(regions[index])
                    : '#';
            }

            return new string(row);
        }

        /// <summary>
        /// 把整张区域表写成可直接存入 RegionRowsValue 的字符串行
        /// </summary>
        /// <param name="regions">行主序区域表</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static string[] ToRegionRows(IReadOnlyList<int> regions, int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            if (regions == null || regions.Count != width * height) return Array.Empty<string>();

            string[] rows = new string[height];
            for (int y = 0; y < height; y++)
                rows[y] = ToMarkRow(regions, y, width);

            return rows;
        }

        /// <summary>
        /// 开一张满格实心的形状掩码
        /// </summary>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static bool[] CreateMask(int width, int height)
            => InventoryShapeBoard.CreateMask(width, height);

        // 把鼠标事件翻译成带区域号的笔画阶段
        private bool HandlePointer(Rect board)
        {
            Event evt = Event.current;

            StrokePhase phase;
            switch (evt.type)
            {
                case EventType.MouseDown: phase = StrokePhase.Down; break;
                case EventType.MouseDrag: phase = StrokePhase.Drag; break;
                case EventType.MouseUp: phase = StrokePhase.Up; break;
                default: return false;
            }

            int regionId = evt.button == 1 ? 0 : m_brushRegion;
            int index = InventoryShapeBoard.HitCellIndex(board, evt.mousePosition, m_width, m_height);
            bool changed = ApplyStroke(index, phase, regionId);
            if (changed || phase == StrokePhase.Up) evt.Use();

            return changed;
        }

        // 把所有实心格统一归到指定区域
        private void Fill(int regionId)
        {
            m_isPainting = false;
            for (int i = 0; i < m_regions.Length; i++)
            {
                if (m_mask[i]) m_regions[i] = regionId;
            }
        }

        // 画板格子上的区域文字样式
        private GUIStyle GetCellLabelStyle()
        {
            if (m_cellLabelStyle != null) return m_cellLabelStyle;

            m_cellLabelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10
            };
            m_cellLabelStyle.normal.textColor = Color.white;
            m_cellLabelStyle.hover.textColor = Color.white;
            m_cellLabelStyle.active.textColor = Color.white;

            return m_cellLabelStyle;
        }

        // 区域号转 # 或 A-Z
        private static char GetRegionMark(int regionId)
        {
            if (regionId < 0) return '.';
            if (regionId == 0) return '#';

            return (char)('A' + Mathf.Clamp(regionId, 1, MAX_REGION_ID) - 1);
        }

        // 生成 # 与 A-Z 的画笔下拉选项
        private static string[] CreateRegionOptions()
        {
            string[] options = new string[MAX_REGION_ID + 1];
            options[0] = "#";

            for (int i = 1; i <= MAX_REGION_ID; i++)
                options[i] = ((char)('A' + i - 1)).ToString();

            return options;
        }

        // 为默认区域与 A-Z 生成稳定且便于区分的颜色
        private static Color[] CreateRegionColors()
        {
            Color[] colors = new Color[MAX_REGION_ID + 1];
            colors[0] = new Color(0.28f, 0.38f, 0.31f, 1f);

            for (int i = 1; i <= MAX_REGION_ID; i++)
                colors[i] = Color.HSVToRGB((i - 1f) / MAX_REGION_ID, 0.52f, 0.68f);

            return colors;
        }
    }
}
