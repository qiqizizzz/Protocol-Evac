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

        [Tooltip("逻辑区域行, 一行一串, # 为默认区域 0, A-Z 为区域 1-26, . 为空洞; 留空代表所有实心格同属区域 0")]
        [SerializeField] private string[] RegionRowsValue = Array.Empty<string>();

        public string DisplayName => DisplayNameValue;
        public int Width => WidthValue;
        public int Height => HeightValue;

        //行主序实心掩码, 长度恒为 Width*Height
        public IReadOnlyList<bool> Mask => ParseShape(RowsValue, WidthValue, HeightValue);

        //行主序区域表, 实心格为非负区域号, 空洞与围栏外统一为 -1
        public IReadOnlyList<int> RegionIds => ParseRegions(RegionRowsValue, WidthValue, HeightValue, Mask);

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
        /// 把逻辑区域行解析成行主序区域表, 区域必须与形状掩码的实心格和空洞完全一致
        /// </summary>
        /// <param name="rows">区域行, 空或 null 代表所有实心格同属区域 0</param>
        /// <param name="width">网格列数</param>
        /// <param name="height">网格行数</param>
        /// <param name="mask">已解析的实心掩码</param>
        public static int[] ParseRegions(string[] rows, int width, int height, IReadOnlyList<bool> mask)
        {
            int[] regions = new int[width * height];
            bool defaultRegions = rows == null || rows.Length == 0;
            bool maskUsable = mask != null && mask.Count == regions.Length;

            if (!maskUsable)
            {
                QLog.Error("区域表缺少有效的形状掩码, 已按全部实心格区域 0 处理");
                defaultRegions = true;
            }

            if (!defaultRegions)
            {
                if (rows.Length != height)
                {
                    QLog.Error($"区域行数 {rows.Length} 与网格行数 {height} 不符, 已按全部实心格区域 0 处理");
                    defaultRegions = true;
                }

                for (int y = 0; !defaultRegions && y < height; y++)
                {
                    string row = rows[y] ?? string.Empty;
                    if (row.Length != width)
                    {
                        QLog.Error($"第 {y} 行区域长度 {row.Length} 与网格列数 {width} 不符, 已按全部实心格区域 0 处理");
                        defaultRegions = true;
                    }
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    bool solid = !maskUsable || mask[index];

                    if (defaultRegions)
                    {
                        regions[index] = solid ? 0 : -1;
                        continue;
                    }

                    char mark = rows[y][x];
                    bool regionSolid = mark != VOID_MARK;
                    int regionId = ResolveRegionId(mark);

                    if (regionSolid && regionId < 0)
                    {
                        QLog.Error($"第 {y} 行第 {x} 列是非法区域字符 '{mark}', 只允许 ., # 与 A-Z");
                        SetDefaultRegions(regions, mask);
                        return regions;
                    }

                    if (regionSolid != solid)
                    {
                        QLog.Error($"第 {y} 行第 {x} 列区域字符 '{mark}' 与形状掩码不一致, 已按全部实心格区域 0 处理");
                        SetDefaultRegions(regions, mask);
                        return regions;
                    }

                    regions[index] = solid ? regionId : -1;
                }
            }

            return regions;
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

        //区域字符转区域号, . 是空洞, # 是默认区域 0, A-Z 是区域 1-26
        private static int ResolveRegionId(char mark)
        {
            if (mark == VOID_MARK) return -1;
            if (mark == SOLID_MARK) return 0;
            if (mark >= 'A' && mark <= 'Z') return mark - 'A' + 1;
            return -1;
        }

        //把实心格全部归入默认区域, 空洞保持 -1
        private static void SetDefaultRegions(int[] regions, IReadOnlyList<bool> mask)
        {
            for (int i = 0; i < regions.Length; i++)
                regions[i] = mask == null || mask[i] ? 0 : -1;
        }
    }
}
