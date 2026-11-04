/*
* ┌──────────────────────────────────┐
* │  描    述: 容器定义配置, 描述一块网格自身的尺寸、形状与名称
* │  类    名: InventoryContainerConfigSO.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using Utils.log;

namespace Module.Inventory.Core.Config
{
    [CreateAssetMenu(fileName = "ContainerConfig", menuName = "配置/背包/容器定义配置")]
    public sealed class InventoryContainerConfigSO : ScriptableObject
    {
        private const char SOLID_MARK = '#';
        private const char VOID_MARK = '.';

        [Header("标识")]
        [Tooltip("容器显示名, 用于载体表头的容器标题")]
        [SerializeField] private string DisplayNameValue = string.Empty;

        [Header("网格")]
        [Tooltip("网格列数")]
        [SerializeField, Min(1)] private int WidthValue = 1;

        [Tooltip("网格行数")]
        [SerializeField, Min(1)] private int HeightValue = 1;

        [Tooltip("异形形状行, 一行一串, # 是格子 . 是空洞; 留空代表满格矩形")]
        [SerializeField] private string[] RowsValue = Array.Empty<string>();

        public string DisplayName => DisplayNameValue;
        public int Width => WidthValue;
        public int Height => HeightValue;

        //行主序实心掩码, 长度恒为 Width*Height
        public IReadOnlyList<bool> Mask => ParseShape(RowsValue, WidthValue, HeightValue);

        /// <summary>
        /// 把形状行解析成行主序掩码, 第一个元素是最上面一行的最左格
        /// </summary>
        /// <param name="rows">形状行, 空或 null 代表满格</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static bool[] ParseShape(string[] rows, int width, int height)
        {
            bool[] mask = new bool[width * height];

            if (rows == null || rows.Length == 0)
            {
                for (int i = 0; i < mask.Length; i++) mask[i] = true;
                return mask;
            }

            if (rows.Length != height)
            {
                QLog.Error($"形状行数 {rows.Length} 与网格行数 {height} 不符, 已按满格处理");
                for (int i = 0; i < mask.Length; i++) mask[i] = true;
                return mask;
            }

            for (int y = 0; y < height; y++)
            {
                string row = rows[y] ?? string.Empty;
                if (row.Length != width)
                {
                    QLog.Error($"第 {y} 行形状长度 {row.Length} 与网格列数 {width} 不符, 已按满格处理");
                    for (int i = 0; i < mask.Length; i++) mask[i] = true;
                    return mask;
                }

                for (int x = 0; x < width; x++)
                {
                    char mark = row[x];
                    if (mark != SOLID_MARK && mark != VOID_MARK)
                        QLog.Error($"第 {y} 行第 {x} 列是非法字符 '{mark}', 只允许 {SOLID_MARK} 与 {VOID_MARK}");

                    mask[y * width + x] = mark == SOLID_MARK;
                }
            }

            return mask;
        }

        /// <summary>
        /// 把实心掩码写回形状行文本, 满格矩形不需要形状行, 返回空数组
        /// </summary>
        /// <param name="mask">行主序实心掩码</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        public static string[] ToShapeRows(IReadOnlyList<bool> mask, int width, int height)
        {
            if (mask == null || mask.Count != width * height) return Array.Empty<string>();

            for (int i = 0; i < mask.Count; i++)
            {
                if (!mask[i]) return WriteShapeRows(mask, width, height);
            }

            return Array.Empty<string>();
        }

        //按 # 与 . 逐行拼形状文本
        private static string[] WriteShapeRows(IReadOnlyList<bool> mask, int width, int height)
        {
            string[] rows = new string[height];

            for (int y = 0; y < height; y++)
            {
                char[] row = new char[width];
                for (int x = 0; x < width; x++)
                    row[x] = mask[y * width + x] ? SOLID_MARK : VOID_MARK;

                rows[y] = new string(row);
            }

            return rows;
        }
    }
}