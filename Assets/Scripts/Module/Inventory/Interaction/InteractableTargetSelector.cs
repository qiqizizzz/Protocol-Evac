/*
* ┌──────────────────────────────────┐
* │  描    述: 交互候选选择器, 按交互体中心距离取最近一只, 不依赖物理场景
* │  类    名: InteractableTargetSelector.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using UnityEngine;

namespace Module.Inventory.Interaction
{
    /// <summary>
    /// 交互候选选择器：物理粗筛只负责圈出候选, 选谁由中心平方距离决定, 可脱离场景单测
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
            if (candidates == null) throw new ArgumentNullException(nameof(candidates));
            if (count > candidates.Length)
                throw new ArgumentException($"有效数量 {count} 超出数组长度 {candidates.Length}", nameof(count));

            ILootInteractable nearest = null;
            float nearestSqrDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                ILootInteractable candidate = candidates[i];
                if (candidate == null || !candidate.CanInteract()) continue;

                float sqrDistance = (candidate.InteractionCenter - origin).sqrMagnitude;
                if (sqrDistance >= nearestSqrDistance) continue;

                nearestSqrDistance = sqrDistance;
                nearest = candidate;
            }

            return nearest;
        }
    }
}
