/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 网格路径解析器，使用 A* 计算并简化路径拐点
 * │  类    名: GridPathResolver.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Navigation.Grid.Data;
using UnityEngine;

namespace Module.Navigation.Grid
{
    public sealed class GridPathResolver
    {
        private const float STRAIGHT_COST = 1f;
        private const float DIAGONAL_COST = 1.41421356f;

        private readonly List<int> m_openHeap = new List<int>();
        private readonly List<int> m_reversePath = new List<int>();

        private float[] m_gCosts;
        private float[] m_fCosts;
        private int[] m_parentIndices;
        private byte[] m_nodeStates;

        // 使用 A* 解析起点到终点的路径拐点
        public bool TryResolve(GridNavigationData navigationData, int startIndex, int targetIndex,
            List<Vector3> path)
        {
            path.Clear();
            PrepareBuffers(navigationData.Count);

            m_gCosts[startIndex] = 0f;
            m_fCosts[startIndex] = GetHeuristic(navigationData, startIndex, targetIndex);
            m_nodeStates[startIndex] = 1;
            PushOpen(startIndex);

            while (m_openHeap.Count > 0)
            {
                int currentIndex = PopOpen();
                if (currentIndex == targetIndex)
                {
                    BuildPath(navigationData, startIndex, targetIndex, path);
                    return true;
                }

                m_nodeStates[currentIndex] = 2;
                ExpandNeighbors(navigationData, currentIndex, targetIndex);
            }

            return false;
        }

        // 清理并准备本次搜索需要的缓存
        private void PrepareBuffers(int nodeCount)
        {
            if (m_gCosts == null || m_gCosts.Length < nodeCount)
            {
                m_gCosts = new float[nodeCount];
                m_fCosts = new float[nodeCount];
                m_parentIndices = new int[nodeCount];
                m_nodeStates = new byte[nodeCount];
            }

            for (int i = 0; i < nodeCount; i++)
            {
                m_gCosts[i] = float.PositiveInfinity;
                m_fCosts[i] = float.PositiveInfinity;
                m_parentIndices[i] = -1;
                m_nodeStates[i] = 0;
            }

            m_openHeap.Clear();
            m_reversePath.Clear();
        }

        // 扩展当前节点周围的八方向相邻节点
        private void ExpandNeighbors(GridNavigationData navigationData, int currentIndex, int targetIndex)
        {
            int currentX = navigationData.GetX(currentIndex);
            int currentZ = navigationData.GetZ(currentIndex);
            for (int offsetZ = -1; offsetZ <= 1; offsetZ++)
            {
                for (int offsetX = -1; offsetX <= 1; offsetX++)
                {
                    if (offsetX == 0 && offsetZ == 0)
                        continue;

                    int neighborX = currentX + offsetX;
                    int neighborZ = currentZ + offsetZ;
                    if (!navigationData.IsInside(neighborX, neighborZ))
                        continue;

                    int neighborIndex = navigationData.GetIndex(neighborX, neighborZ);
                    if (!CanTraverse(navigationData, currentIndex, neighborIndex, offsetX, offsetZ))
                        continue;

                    float moveCost = offsetX != 0 && offsetZ != 0 ? DIAGONAL_COST : STRAIGHT_COST;
                    float tentativeCost = m_gCosts[currentIndex] + moveCost;
                    if (tentativeCost >= m_gCosts[neighborIndex])
                        continue;

                    m_parentIndices[neighborIndex] = currentIndex;
                    m_gCosts[neighborIndex] = tentativeCost;
                    m_fCosts[neighborIndex] = tentativeCost
                        + GetHeuristic(navigationData, neighborIndex, targetIndex);

                    if (m_nodeStates[neighborIndex] == 1)
                    {
                        RebuildOpenHeap();
                        continue;
                    }

                    m_nodeStates[neighborIndex] = 1;
                    PushOpen(neighborIndex);
                }
            }
        }

        // 判断相邻节点之间是否可以通行
        private bool CanTraverse(GridNavigationData navigationData, int currentIndex, int neighborIndex,
            int offsetX, int offsetZ)
        {
            if (!navigationData.IsWalkable(neighborIndex))
                return false;

            if (Mathf.Abs(navigationData.GetHeight(currentIndex) - navigationData.GetHeight(neighborIndex))
                > navigationData.MaxStepHeight)
                return false;

            if (offsetX == 0 || offsetZ == 0)
                return true;

            int currentX = navigationData.GetX(currentIndex);
            int currentZ = navigationData.GetZ(currentIndex);
            int horizontalIndex = navigationData.GetIndex(currentX + offsetX, currentZ);
            int verticalIndex = navigationData.GetIndex(currentX, currentZ + offsetZ);
            return navigationData.IsWalkable(horizontalIndex)
                && navigationData.IsWalkable(verticalIndex)
                && Mathf.Abs(navigationData.GetHeight(currentIndex) - navigationData.GetHeight(horizontalIndex))
                <= navigationData.MaxStepHeight
                && Mathf.Abs(navigationData.GetHeight(currentIndex) - navigationData.GetHeight(verticalIndex))
                <= navigationData.MaxStepHeight;
        }

        // 使用八方向距离估算剩余路径成本
        private float GetHeuristic(GridNavigationData navigationData, int fromIndex, int toIndex)
        {
            int deltaX = Mathf.Abs(navigationData.GetX(fromIndex) - navigationData.GetX(toIndex));
            int deltaZ = Mathf.Abs(navigationData.GetZ(fromIndex) - navigationData.GetZ(toIndex));
            int diagonalSteps = Mathf.Min(deltaX, deltaZ);
            int straightSteps = Mathf.Max(deltaX, deltaZ) - diagonalSteps;
            return diagonalSteps * DIAGONAL_COST + straightSteps * STRAIGHT_COST;
        }

        // 从父节点链构建并压缩最终路径
        private void BuildPath(GridNavigationData navigationData, int startIndex, int targetIndex,
            List<Vector3> path)
        {
            int currentIndex = targetIndex;
            while (currentIndex >= 0)
            {
                m_reversePath.Add(currentIndex);
                if (currentIndex == startIndex)
                    break;

                currentIndex = m_parentIndices[currentIndex];
            }

            m_reversePath.Reverse();
            path.Add(navigationData.GetWorldPosition(m_reversePath[0]));
            Vector2Int previousDirection = Vector2Int.zero;
            for (int i = 1; i < m_reversePath.Count; i++)
            {
                int previousIndex = m_reversePath[i - 1];
                int pathIndex = m_reversePath[i];
                Vector2Int direction = new Vector2Int(
                    navigationData.GetX(pathIndex) - navigationData.GetX(previousIndex),
                    navigationData.GetZ(pathIndex) - navigationData.GetZ(previousIndex));

                if (i > 1 && direction != previousDirection)
                    path.Add(navigationData.GetWorldPosition(previousIndex));

                previousDirection = direction;
            }

            Vector3 targetPosition = navigationData.GetWorldPosition(targetIndex);
            if (path[path.Count - 1] != targetPosition)
                path.Add(targetPosition);
        }

        // 将节点加入开放列表最小堆
        private void PushOpen(int index)
        {
            m_openHeap.Add(index);
            int heapIndex = m_openHeap.Count - 1;
            while (heapIndex > 0)
            {
                int parentIndex = (heapIndex - 1) / 2;
                if (m_fCosts[m_openHeap[parentIndex]] <= m_fCosts[index])
                    break;

                m_openHeap[heapIndex] = m_openHeap[parentIndex];
                heapIndex = parentIndex;
            }

            m_openHeap[heapIndex] = index;
        }

        // 取出开放列表中成本最低的节点
        private int PopOpen()
        {
            int result = m_openHeap[0];
            int lastIndex = m_openHeap[m_openHeap.Count - 1];
            m_openHeap.RemoveAt(m_openHeap.Count - 1);
            if (m_openHeap.Count == 0)
                return result;

            m_openHeap[0] = lastIndex;
            int heapIndex = 0;
            while (true)
            {
                int leftIndex = heapIndex * 2 + 1;
                if (leftIndex >= m_openHeap.Count)
                    break;

                int rightIndex = leftIndex + 1;
                int childIndex = rightIndex < m_openHeap.Count
                    && m_fCosts[m_openHeap[rightIndex]] < m_fCosts[m_openHeap[leftIndex]]
                    ? rightIndex
                    : leftIndex;
                if (m_fCosts[m_openHeap[heapIndex]] <= m_fCosts[m_openHeap[childIndex]])
                    break;

                (m_openHeap[heapIndex], m_openHeap[childIndex]) =
                    (m_openHeap[childIndex], m_openHeap[heapIndex]);
                heapIndex = childIndex;
            }

            return result;
        }

        // 在已有节点成本降低后重新构建开放列表堆
        private void RebuildOpenHeap()
        {
            for (int i = m_openHeap.Count / 2 - 1; i >= 0; i--)
            {
                int heapIndex = i;
                while (true)
                {
                    int leftIndex = heapIndex * 2 + 1;
                    if (leftIndex >= m_openHeap.Count)
                        break;

                    int rightIndex = leftIndex + 1;
                    int childIndex = rightIndex < m_openHeap.Count
                        && m_fCosts[m_openHeap[rightIndex]] < m_fCosts[m_openHeap[leftIndex]]
                        ? rightIndex
                        : leftIndex;
                    if (m_fCosts[m_openHeap[heapIndex]] <= m_fCosts[m_openHeap[childIndex]])
                        break;

                    (m_openHeap[heapIndex], m_openHeap[childIndex]) =
                        (m_openHeap[childIndex], m_openHeap[heapIndex]);
                    heapIndex = childIndex;
                }
            }
        }
    }
}
