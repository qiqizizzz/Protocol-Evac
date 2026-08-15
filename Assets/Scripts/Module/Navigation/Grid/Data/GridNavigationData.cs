/*
 * ┌───────────────────────────────────────────────────────────────┐
 * │  描    述: 网格导航数据，保存单次路径搜索的节点与高度信息
 * │  类    名: GridNavigationData.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Navigation.Grid.Data
{
    public sealed class GridNavigationData
    {
        private readonly bool[] m_walkableValues;
        private readonly float[] m_heightValues;

        public Vector3 Origin { get; }
        public int Width { get; }
        public int Height { get; }
        public int Count => Width * Height;
        public float CellSize { get; }
        public float MaxStepHeight { get; }

        // 创建单次路径搜索使用的网格数据
        public GridNavigationData(Vector3 origin, int width, int height, float cellSize, float maxStepHeight,
            bool[] walkableValues, float[] heightValues)
        {
            Origin = origin;
            Width = width;
            Height = height;
            CellSize = cellSize;
            MaxStepHeight = maxStepHeight;
            m_walkableValues = walkableValues;
            m_heightValues = heightValues;
        }

        // 将二维网格坐标转换为线性索引
        public int GetIndex(int x, int z)
        {
            return z * Width + x;
        }

        // 获取线性索引对应的 X 坐标
        public int GetX(int index)
        {
            return index % Width;
        }

        // 获取线性索引对应的 Z 坐标
        public int GetZ(int index)
        {
            return index / Width;
        }

        // 判断二维网格坐标是否位于当前数据范围内
        public bool IsInside(int x, int z)
        {
            return x >= 0 && x < Width && z >= 0 && z < Height;
        }

        // 判断指定节点是否可行走
        public bool IsWalkable(int index)
        {
            return m_walkableValues[index];
        }

        // 获取指定节点的地面高度
        public float GetHeight(int index)
        {
            return m_heightValues[index];
        }

        // 获取指定节点的世界坐标
        public Vector3 GetWorldPosition(int index)
        {
            int x = GetX(index);
            int z = GetZ(index);
            return new Vector3(Origin.x + x * CellSize, m_heightValues[index], Origin.z + z * CellSize);
        }

        // 将世界坐标转换为范围内的最近网格索引
        public int GetNearestIndex(Vector3 worldPosition)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt((worldPosition.x - Origin.x) / CellSize), 0, Width - 1);
            int z = Mathf.Clamp(Mathf.RoundToInt((worldPosition.z - Origin.z) / CellSize), 0, Height - 1);
            return GetIndex(x, z);
        }

        // 在指定节点周围寻找最近的可行走节点
        public bool TryGetNearestWalkableIndex(int sourceIndex, out int walkableIndex)
        {
            if (IsWalkable(sourceIndex))
            {
                walkableIndex = sourceIndex;
                return true;
            }

            int sourceX = GetX(sourceIndex);
            int sourceZ = GetZ(sourceIndex);
            for (int radius = 1; radius <= 2; radius++)
            {
                for (int z = sourceZ - radius; z <= sourceZ + radius; z++)
                {
                    for (int x = sourceX - radius; x <= sourceX + radius; x++)
                    {
                        if (!IsInside(x, z))
                            continue;

                        int index = GetIndex(x, z);
                        if (!IsWalkable(index))
                            continue;

                        walkableIndex = index;
                        return true;
                    }
                }
            }

            walkableIndex = -1;
            return false;
        }
    }
}
