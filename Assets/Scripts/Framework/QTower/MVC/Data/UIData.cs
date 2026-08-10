/*
 * ┌──────────────────────────────────┐
 * │  描    述: UI 注册数据，保存视图预制体、挂载节点与所属控制器
 * │  类    名: UIData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.Controller;
using UnityEngine;

namespace Framework.QTower
{
    public sealed class UIData
    {
        public Type ViewType { get; }
        public GameObject Prefab { get; }
        public Transform Parent { get; }
        public BaseController Controller { get; }

        public UIData(Type viewType, GameObject prefab, Transform parent, BaseController controller)
        {
            ViewType = viewType;
            Prefab = prefab;
            Parent = parent;
            Controller = controller;
        }
    }
}
