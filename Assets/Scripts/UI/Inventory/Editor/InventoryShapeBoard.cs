/*
* ┌──────────────────────────────────┐
* │  描    述: 容器形状绘制板, 一格一格刷异形网格, 配置工具与 Inspector 共用
* │  类    名: InventoryShapeBoard.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UI.Inventory.Editor
{
    // 形状绘制板: 几何换算与掩码改动全是静态纯函数, 只有画和刷依赖 EditorGUI
    public sealed class InventoryShapeBoard
    {
        // ==================== 常量与静态字段 ====================
        // 绘制板上单格边长与格间距
        public const float CELL_SIZE = 20f;
        public const float CELL_GAP = 1f;

        private const float STRIDE = CELL_SIZE + CELL_GAP;

        private static readonly Color m_solidColor = new Color(0.32f, 0.62f, 0.34f, 1f);
        private static readonly Color m_voidColor = new Color(0.06f, 0.06f, 0.08f, 1f);
        private static readonly Color m_boardColor = new Color(0.24f, 0.24f, 0.26f, 1f);

        // ==================== 属性 ====================
        // 行主序实心掩码, 可直接交给配置层序列化
        public bool[] Shape => m_shape;
        public int Width => m_width;
        public int Height => m_height;
        public int SolidCount => CountSolid(m_shape);

        // ==================== 字段[私有] ====================
        private bool[] m_shape;
        private int m_width;
        private int m_height;
        private bool m_isPainting;
        private bool m_paintValue;

        // 一次绘制板操作处于哪个阶段, 拆出来是为了让刷格逻辑能脱离 Event 单测
        public enum StrokePhase
        {
            Down,
            Drag,
            Up
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 用配置里的掩码重建画板, 掩码为空或长度不符时按满格矩形开一张新表
        /// </summary>
        /// <param name="mask">行主序实心掩码</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public void Load(IReadOnlyList<bool> mask, int width, int height)
        {
            bool[] shape = CreateMask(width, height);

            if (mask != null && mask.Count == shape.Length)
            {
                for (int i = 0; i < shape.Length; i++) shape[i] = mask[i];
            }

            m_shape = shape;
            m_width = Mathf.Max(1, width);
            m_height = Mathf.Max(1, height);
            m_isPainting = false;
        }

        /// <summary>
        /// 按新的行列调整画板, 尺寸没变时什么都不做
        /// </summary>
        /// <param name="width">新的列数</param>
        /// <param name="height">新的行数</param>
        public void Resize(int width, int height)
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            if (m_shape != null && width == m_width && height == m_height) return;

            m_shape = ResizeMask(m_shape, m_width, width, height);
            m_width = width;
            m_height = height;
            m_isPainting = false;
        }

        /// <summary>
        /// 画一整块绘制板并处理鼠标, 返回本次是否改动过形状, 调用方据此重绘窗口
        /// </summary>
        /// <param name="label">绘制板标题</param>
        /// <param name="tooltip">标题悬停提示</param>
        public bool Draw(string label, string tooltip)
        {
            if (m_shape == null) Load(null, m_width, m_height);

            EditorGUILayout.LabelField(new GUIContent(label, tooltip), EditorStyles.boldLabel);

            Vector2 boardSize = BoardSize(m_width, m_height);
            Rect board = GUILayoutUtility.GetRect(boardSize.x, boardSize.y);
            bool changed = HandlePointer(board);

            EditorGUI.DrawRect(board, m_boardColor);
            for (int i = 0; i < m_shape.Length; i++)
                EditorGUI.DrawRect(CellRect(board, i % m_width, i / m_width), m_shape[i] ? m_solidColor : m_voidColor);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全填充")) { Fill(m_shape, true); changed = true; }
            if (GUILayout.Button("全挖空")) { Fill(m_shape, false); changed = true; }
            if (GUILayout.Button("反选")) { Invert(m_shape); changed = true; }
            EditorGUILayout.EndHorizontal();

            return changed;
        }

        /// <summary>
        /// 把某一格刷成当前笔画的目标值, 返回是否改动
        /// </summary>
        /// <param name="index">行主序格子下标, 传负数表示没落在格子上</param>
        /// <param name="phase">本次操作阶段</param>
        public bool ApplyStroke(int index, StrokePhase phase)
        {
            if (phase == StrokePhase.Up)
            {
                m_isPainting = false;
                return false;
            }

            if (index < 0 || index >= m_shape.Length) return false;
            if (phase == StrokePhase.Drag && !m_isPainting) return false;

            if (phase == StrokePhase.Down)
            {
                m_isPainting = true;
                // 按下先定调: 实心就往下挖, 空洞就往上调, 之后拖着刷一律沿用
                m_paintValue = !m_shape[index];
            }

            m_shape[index] = m_paintValue;
            return true;
        }

        // ==================== Static Function ====================
        /// <summary>
        /// 开一张满格实心的掩码
        /// </summary>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static bool[] CreateMask(int width, int height)
        {
            bool[] mask = new bool[Mathf.Max(1, width) * Mathf.Max(1, height)];
            for (int i = 0; i < mask.Length; i++) mask[i] = true;

            return mask;
        }

        /// <summary>
        /// 换尺寸时保住左上重叠区, 新增的格一律按实心算
        /// </summary>
        /// <param name="oldMask">旧掩码, 可为空</param>
        /// <param name="oldWidth">旧列数</param>
        /// <param name="newWidth">新列数</param>
        /// <param name="newHeight">新行数</param>
        public static bool[] ResizeMask(bool[] oldMask, int oldWidth, int newWidth, int newHeight)
        {
            newWidth = Mathf.Max(1, newWidth);
            newHeight = Mathf.Max(1, newHeight);

            bool[] mask = CreateMask(newWidth, newHeight);
            if (oldMask == null || oldWidth < 1) return mask;

            int oldHeight = oldMask.Length / oldWidth;
            int keepWidth = Mathf.Min(newWidth, oldWidth);
            int keepHeight = Mathf.Min(newHeight, oldHeight);

            for (int y = 0; y < keepHeight; y++)
            {
                for (int x = 0; x < keepWidth; x++)
                    mask[y * newWidth + x] = oldMask[y * oldWidth + x];
            }

            return mask;
        }

        /// <summary>
        /// 整块填成实心或空洞
        /// </summary>
        /// <param name="mask">行主序掩码</param>
        /// <param name="solid">true 全实心, false 全空洞</param>
        public static void Fill(bool[] mask, bool solid)
        {
            if (mask == null) return;
            for (int i = 0; i < mask.Length; i++) mask[i] = solid;
        }

        /// <summary>
        /// 逐格取反
        /// </summary>
        /// <param name="mask">行主序掩码</param>
        public static void Invert(bool[] mask)
        {
            if (mask == null) return;
            for (int i = 0; i < mask.Length; i++) mask[i] = !mask[i];
        }

        /// <summary>
        /// 数实心格
        /// </summary>
        /// <param name="mask">行主序掩码, 可为空</param>
        public static int CountSolid(IReadOnlyList<bool> mask)
        {
            if (mask == null) return 0;

            int solid = 0;
            for (int i = 0; i < mask.Count; i++)
            {
                if (mask[i]) solid++;
            }

            return solid;
        }

        /// <summary>
        /// 某一格在板子里的位置
        /// </summary>
        /// <param name="board">绘制板整体矩形</param>
        /// <param name="x">列号</param>
        /// <param name="y">行号</param>
        public static Rect CellRect(Rect board, int x, int y)
            => new Rect(board.x + CELL_GAP + x * STRIDE, board.y + CELL_GAP + y * STRIDE, CELL_SIZE, CELL_SIZE);

        /// <summary>
        /// 整块绘制板需要占多大, 四周与格间都留一条缝
        /// </summary>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static Vector2 BoardSize(int width, int height)
            => new Vector2(Mathf.Max(1, width) * STRIDE + CELL_GAP, Mathf.Max(1, height) * STRIDE + CELL_GAP);

        /// <summary>
        /// 落在第几格, 板外与左上留白返回 -1; 格间缝隙算给右边那一格, 点起来更宽容
        /// </summary>
        /// <param name="board">绘制板整体矩形</param>
        /// <param name="point">鼠标位置</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static int HitCellIndex(Rect board, Vector2 point, int width, int height)
        {
            if (!board.Contains(point)) return -1;

            int x = Mathf.FloorToInt((point.x - board.x - CELL_GAP) / STRIDE);
            int y = Mathf.FloorToInt((point.y - board.y - CELL_GAP) / STRIDE);

            if (x < 0 || x >= width || y < 0 || y >= height) return -1;

            return y * width + x;
        }

        // ==================== Private Function ====================
        // 把鼠标事件翻译成一次笔画阶段
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

            bool changed = ApplyStroke(HitCellIndex(board, evt.mousePosition, m_width, m_height), phase);
            if (changed) evt.Use();

            return changed;
        }
    }
}
