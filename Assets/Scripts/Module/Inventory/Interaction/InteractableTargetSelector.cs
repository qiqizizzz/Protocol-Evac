/*
* ┌──────────────────────────────────┐
* │  描    述: 交互候选选择器, 按最近距离或距离与相机夹角加权选择目标
* │  类    名: InteractableTargetSelector.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Inventory.Interaction
{
    /// <summary>
    /// 交互候选选择器：物理粗筛只负责圈出候选, 最近目标与评分目标都在这里统一筛选
    /// </summary>
    public static class InteractableTargetSelector
    {
        /// <summary>
        /// 从候选数组的前 count 项里选出交互体中心离 origin 最近且可交互的一只, 无结果返回 null
        /// </summary>
        /// <param name="candidates">候选数组, 由调用方复用, 可能含空槽</param>
        /// <param name="count">本轮有效候选数量</param>
        /// <param name="origin">查询原点, 一般是玩家位置</param>
        public static ILootInteractable SelectNearest(ILootInteractable[] candidates, int count, Vector3 origin)
        {
            return SelectNearest(candidates, count, origin, null);
        }

        /// <summary>
        /// 只从指定类型的候选中选出离 origin 最近且可交互的一只, 无结果返回 null
        /// </summary>
        /// <param name="candidates">候选数组, 由调用方复用, 可能含空槽</param>
        /// <param name="count">本轮有效候选数量</param>
        /// <param name="origin">查询原点, 一般是玩家位置</param>
        public static ILootInteractable SelectNearest<T>(ILootInteractable[] candidates, int count, Vector3 origin)
            where T : ILootInteractable
        {
            return SelectNearest(candidates, count, origin, typeof(T));
        }

        /// <summary>
        /// 按距离与视角夹角加权选择最符合玩家意图的目标
        /// </summary>
        /// <param name="candidates">候选数组, 由调用方复用, 可能含空槽</param>
        /// <param name="count">本轮有效候选数量</param>
        /// <param name="origin">查询原点, 一般是玩家胸口</param>
        /// <param name="forward">玩家相机水平朝向</param>
        /// <param name="config">距离、夹角与评分参数</param>
        /// <param name="hasLineOfSight">视线过滤, 返回 false 的候选会被跳过</param>
        public static ILootInteractable SelectBest(ILootInteractable[] candidates, int count, Vector3 origin,
            Vector3 forward, InteractionQueryConfig config, Func<ILootInteractable, bool> hasLineOfSight)
        {
            return SelectBest(candidates, count, origin, forward, config, hasLineOfSight, null);
        }

        /// <summary>
        /// 从指定类型的候选中按距离与视角夹角加权选择最符合玩家意图的目标
        /// </summary>
        /// <param name="candidates">候选数组, 由调用方复用, 可能含空槽</param>
        /// <param name="count">本轮有效候选数量</param>
        /// <param name="origin">查询原点, 一般是玩家胸口</param>
        /// <param name="forward">玩家相机水平朝向</param>
        /// <param name="config">距离、夹角与评分参数</param>
        /// <param name="hasLineOfSight">视线过滤, 返回 false 的候选会被跳过</param>
        public static ILootInteractable SelectBest<T>(ILootInteractable[] candidates, int count, Vector3 origin,
            Vector3 forward, InteractionQueryConfig config, Func<ILootInteractable, bool> hasLineOfSight)
            where T : ILootInteractable
        {
            return SelectBest(candidates, count, origin, forward, config, hasLineOfSight, typeof(T));
        }

        // 统一校验候选数组边界
        private static void ValidateCandidates(ILootInteractable[] candidates, int count)
        {
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (count > candidates.Length)
                throw new ArgumentException($"有效数量 {count} 超出数组长度 {candidates.Length}", nameof(count));
        }

        // 统一校验与选择, 类型为空时不限制目标种类
        private static ILootInteractable SelectNearest(ILootInteractable[] candidates, int count, Vector3 origin,
            Type targetType)
        {
            ValidateCandidates(candidates, count);

            ILootInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                ILootInteractable candidate = candidates[i];
                if (candidate == null || !candidate.CanInteract()) continue;
                if (targetType != null && !targetType.IsInstanceOfType(candidate)) continue;

                float sqrDistance = (candidate.InteractionCenter - origin).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }

            return nearest;
        }

        // 距离和角度都归一化后加权, 夹角权重更高以保证准星附近的箱子优先
        private static ILootInteractable SelectBest(ILootInteractable[] candidates, int count, Vector3 origin,
            Vector3 forward, InteractionQueryConfig config, Func<ILootInteractable, bool> hasLineOfSight,
            Type targetType)
        {
            ValidateCandidates(candidates, count);

            Vector3 flatForward = new Vector3(forward.x, 0f, forward.z);
            flatForward = flatForward.sqrMagnitude > 0.0001f ? flatForward.normalized : Vector3.zero;

            ILootInteractable best = null;
            float bestScore = float.MaxValue;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                ILootInteractable candidate = candidates[i];
                if (candidate == null || !candidate.CanInteract()) continue;
                if (targetType != null && !targetType.IsInstanceOfType(candidate)) continue;

                Vector3 toCandidate = candidate.InteractionCenter - origin;
                toCandidate.y = 0f;

                float distance = toCandidate.magnitude;
                if (distance > config.MaxDistance) continue;

                float angle = 0f;
                bool isNear = distance <= config.NearDistance;
                if (flatForward != Vector3.zero && distance > 0.0001f)
                {
                    float dot = Vector3.Dot(flatForward, toCandidate / distance);
                    angle = Mathf.Acos(Mathf.Clamp(dot, -1f, 1f)) * Mathf.Rad2Deg;
                }
                else if (!isNear)
                    continue;

                if (!isNear && angle > config.MaxAngle) continue;

                if (config.RequireLineOfSight && hasLineOfSight != null && !hasLineOfSight(candidate))
                    continue;

                float distanceRatio = config.MaxDistance > 0f ? distance / config.MaxDistance : 1f;
                float angleRatio = config.MaxAngle > 0f ? angle / config.MaxAngle : 0f;
                float score = distanceRatio * config.DistanceWeight + angleRatio * config.AngleWeight;

                if (score > bestScore || Mathf.Approximately(score, bestScore) && distance >= bestDistance) continue;

                best = candidate;
                bestScore = score;
                bestDistance = distance;
            }

            return best;
        }
    }
}
