/*
 * ┌─────────────────────────────────────────────────────────────────┐
 * │  描    述: 网格路径控制器，采样场景碰撞并维护 A* 路径拐点
 * │  类    名: GridPathController.cs
 * │  创    建: By qiqizizzz
 * └─────────────────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Navigation.Core;
using Module.Navigation.Grid.Config;
using Module.Navigation.Grid.Data;
using UnityEngine;

namespace Module.Navigation.Grid
{
    public sealed class GridPathController : INavigationController
    {
        private const int GROUND_HIT_BUFFER_SIZE = 16;
        private const int OBSTACLE_HIT_BUFFER_SIZE = 32;

        private readonly GridNavigationConfigSO m_config;
        private readonly GridPathResolver m_pathResolver;
        private readonly List<Vector3> m_path;
        private readonly RaycastHit[] m_groundHits;
        private readonly Collider[] m_obstacleHits;

        private int m_pathIndex;

        public bool HasPath { get; private set; }
        public bool HasReachedDestination { get; private set; }
        public bool HasFailed { get; private set; }
        public Vector3 NextPosition => HasPath ? m_path[m_pathIndex] : Vector3.zero;

        // 创建基于场景碰撞采样的网格路径控制器
        public GridPathController(GridNavigationConfigSO config)
        {
            m_config = config;
            m_pathResolver = new GridPathResolver();
            m_path = new List<Vector3>();
            m_groundHits = new RaycastHit[GROUND_HIT_BUFFER_SIZE];
            m_obstacleHits = new Collider[OBSTACLE_HIT_BUFFER_SIZE];
            Reset();
        }

        // 为当前位置同步计算到目的地的 A* 路径
        public void SetDestination(Vector3 currentPosition, Vector3 destination)
        {
            Reset();
            if (!TryBuildNavigationData(currentPosition, destination, out GridNavigationData navigationData))
            {
                HasFailed = true;
                return;
            }

            int sourceIndex = navigationData.GetNearestIndex(currentPosition);
            int targetIndex = navigationData.GetNearestIndex(destination);
            if (!navigationData.TryGetNearestWalkableIndex(sourceIndex, out sourceIndex)
                || !navigationData.TryGetNearestWalkableIndex(targetIndex, out targetIndex)
                || !m_pathResolver.TryResolve(navigationData, sourceIndex, targetIndex, m_path))
            {
                HasFailed = true;
                return;
            }

            HasPath = m_path.Count > 0;
            m_pathIndex = m_path.Count > 1 ? 1 : 0;
            HasReachedDestination = m_path.Count == 1;
        }

        // 根据当前位置推进当前路径拐点
        public void Tick(Vector3 currentPosition)
        {
            if (!HasPath || HasReachedDestination)
                return;

            Vector3 offset = NextPosition - currentPosition;
            offset.y = 0f;
            float reachDistance = m_config.CornerReachDistanceValue;
            if (offset.sqrMagnitude > reachDistance * reachDistance)
                return;

            if (m_pathIndex < m_path.Count - 1)
            {
                m_pathIndex++;
                return;
            }

            HasReachedDestination = true;
        }

        // 在指定区域内随机采样一个可行走目的地
        public bool TryGetRandomDestination(Vector3 center, float radius, out Vector3 destination)
        {
            for (int i = 0; i < m_config.RandomSampleAttemptsValue; i++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 samplePosition = center + new Vector3(offset.x, 0f, offset.y);
                float rayOriginY = center.y + m_config.GroundProbeHeightValue;
                if (!TrySampleCell(samplePosition.x, samplePosition.z, rayOriginY,
                        out float groundHeight, out _))
                    continue;

                destination = new Vector3(samplePosition.x, groundHeight, samplePosition.z);
                return true;
            }

            destination = Vector3.zero;
            return false;
        }

        // 清理当前路径状态
        public void Reset()
        {
            m_path.Clear();
            m_pathIndex = 0;
            HasPath = false;
            HasReachedDestination = false;
            HasFailed = false;
        }

        // 根据起点与终点附近的场景碰撞构建本次搜索网格
        private bool TryBuildNavigationData(Vector3 currentPosition, Vector3 destination,
            out GridNavigationData navigationData)
        {
            float padding = m_config.SearchPaddingValue;
            float cellSize = m_config.CellSizeValue;
            float minX = Mathf.Min(currentPosition.x, destination.x) - padding;
            float maxX = Mathf.Max(currentPosition.x, destination.x) + padding;
            float minZ = Mathf.Min(currentPosition.z, destination.z) - padding;
            float maxZ = Mathf.Max(currentPosition.z, destination.z) + padding;
            int width = Mathf.CeilToInt((maxX - minX) / cellSize) + 1;
            int height = Mathf.CeilToInt((maxZ - minZ) / cellSize) + 1;
            if (width > m_config.MaxGridSizeValue || height > m_config.MaxGridSizeValue)
            {
                navigationData = null;
                return false;
            }

            bool[] walkableValues = new bool[width * height];
            float[] heightValues = new float[width * height];
            float rayOriginY = Mathf.Max(currentPosition.y, destination.y) + m_config.GroundProbeHeightValue;
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    float worldX = minX + x * cellSize;
                    float worldZ = minZ + z * cellSize;
                    walkableValues[index] = TrySampleCell(worldX, worldZ, rayOriginY,
                        out heightValues[index], out _);
                }
            }

            RemoveUnreachableSteps(width, height, walkableValues, heightValues);
            navigationData = new GridNavigationData(new Vector3(minX, 0f, minZ), width, height, cellSize,
                m_config.MaxStepHeightValue, walkableValues, heightValues);
            return true;
        }

        // 采样单个网格的地面高度与角色站立空间
        private bool TrySampleCell(float worldX, float worldZ, float rayOriginY,
            out float groundHeight, out Collider groundCollider)
        {
            Vector3 rayOrigin = new Vector3(worldX, rayOriginY, worldZ);
            float rayDistance = m_config.GroundProbeHeightValue + m_config.GroundProbeDistanceValue;
            int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, m_groundHits, rayDistance,
                m_config.GroundLayerMaskValue, QueryTriggerInteraction.Ignore);
            if (hitCount == 0)
            {
                groundHeight = 0f;
                groundCollider = null;
                return false;
            }

            int nearestHitIndex = 0;
            for (int i = 1; i < hitCount; i++)
            {
                if (m_groundHits[i].distance < m_groundHits[nearestHitIndex].distance)
                    nearestHitIndex = i;
            }

            RaycastHit groundHit = m_groundHits[nearestHitIndex];
            groundHeight = groundHit.point.y;
            groundCollider = groundHit.collider;
            return !HasBlockingObstacle(worldX, worldZ, groundHeight, groundCollider);
        }

        // 判断角色站立胶囊内是否存在高于可跨越高度的障碍
        private bool HasBlockingObstacle(float worldX, float worldZ, float groundHeight, Collider groundCollider)
        {
            float radius = m_config.AgentRadiusValue;
            float height = Mathf.Max(m_config.AgentHeightValue, radius * 2f);
            Vector3 bottom = new Vector3(worldX, groundHeight + radius, worldZ);
            Vector3 top = new Vector3(worldX, groundHeight + height - radius, worldZ);
            int hitCount = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, m_obstacleHits,
                m_config.ObstacleLayerMaskValue, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hitCount; i++)
            {
                Collider obstacle = m_obstacleHits[i];
                if (obstacle == groundCollider)
                    continue;

                if (obstacle.bounds.max.y <= groundHeight + m_config.MaxStepHeightValue)
                    continue;

                return true;
            }

            return false;
        }

        // 移除与全部相邻节点高度差都过大的孤立网格
        private void RemoveUnreachableSteps(int width, int height, bool[] walkableValues, float[] heightValues)
        {
            bool[] sourceWalkableValues = (bool[])walkableValues.Clone();
            float maxStepHeight = m_config.MaxStepHeightValue;
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = z * width + x;
                    if (!sourceWalkableValues[index])
                        continue;

                    bool hasReachableNeighbor = false;
                    for (int offsetZ = -1; offsetZ <= 1 && !hasReachableNeighbor; offsetZ++)
                    {
                        for (int offsetX = -1; offsetX <= 1; offsetX++)
                        {
                            if (offsetX == 0 && offsetZ == 0)
                                continue;

                            int neighborX = x + offsetX;
                            int neighborZ = z + offsetZ;
                            if (neighborX < 0 || neighborX >= width || neighborZ < 0 || neighborZ >= height)
                                continue;

                            int neighborIndex = neighborZ * width + neighborX;
                            if (!sourceWalkableValues[neighborIndex])
                                continue;

                            if (Mathf.Abs(heightValues[index] - heightValues[neighborIndex]) <= maxStepHeight)
                            {
                                hasReachableNeighbor = true;
                                break;
                            }
                        }
                    }

                    walkableValues[index] = hasReachableNeighbor;
                }
            }
        }
    }
}
