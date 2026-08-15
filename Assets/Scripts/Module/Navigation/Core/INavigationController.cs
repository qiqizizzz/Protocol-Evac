/*
 * ┌───────────────────────────────────────────────────────────┐
 * │  描    述: 导航控制契约，隔离行为层与具体路径搜索后端
 * │  类    名: INavigationController.cs
 * │  创    建: By qiqizizzz
 * └───────────────────────────────────────────────────────────┘
 */

using UnityEngine;

namespace Module.Navigation.Core
{
    public interface INavigationController
    {
        bool HasPath { get; }
        bool HasReachedDestination { get; }
        bool HasFailed { get; }
        Vector3 NextPosition { get; }

        void SetDestination(Vector3 currentPosition, Vector3 destination);
        void Tick(Vector3 currentPosition);
        bool TryGetRandomDestination(Vector3 center, float radius, out Vector3 destination);
        void Reset();
    }
}

