/*
 * ┌──────────────────────────────────┐
 * │  描    述: 层级查找工具，提供组件与Transform的子节点查找扩展
 * │  类    名: HierarchyFindTool.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using UnityEngine;
using Utils.log;

namespace Utils.Find
{
    public static class HierarchyFindTool
    {
        // 从组件所在节点下查找子节点
        public static Transform FindChild(this Component owner, string path)
        {
            if (owner == null)
            {
                QLog.Error($"查找子节点失败：组件为空，路径 {path}");
                return null;
            }

            return owner.transform.Find(path);
        }

        // 从组件所在节点下查找子节点GameObject
        public static GameObject FindChildGo(this Component owner, string path)
        {
            Transform target = owner.FindChild(path);
            return target != null ? target.gameObject : null;
        }

        // 从组件所在节点下查找子节点组件
        public static T FindChildComponent<T>(this Component owner, string path) where T : Component
        {
            Transform target = owner.FindChild(path);
            return getComponent<T>(target, path);
        }

        // 从指定根节点下查找子节点
        public static Transform FindChild(this Transform root, string path)
        {
            if (root == null)
            {
                QLog.Error($"查找子节点失败：根节点为空，路径 {path}");
                return null;
            }

            if (string.IsNullOrEmpty(path))
            {
                QLog.Error($"查找子节点失败：路径为空，根节点 {root.name}");
                return null;
            }

            Transform target = root.Find(path);
            if (target == null)
            {
                QLog.Error($"查找子节点失败：根节点 {root.name} 下未找到 {path}");
                return null;
            }

            return target;
        }

        // 从指定根节点下查找子节点GameObject
        public static GameObject FindChildGo(this Transform root, string path)
        {
            Transform target = root.Find(path);
            return target != null ? target.gameObject : null;
        }

        // 从指定根节点下查找子节点组件
        public static T FindChildComponent<T>(this Transform root, string path) where T : Component
        {
            Transform target = root.Find(path);
            return getComponent<T>(target, path);
        }

        // 获取目标节点上的组件
        private static T getComponent<T>(Transform target, string path) where T : Component
        {
            if (target == null)
                return null;

            T component = target.GetComponent<T>();
            if (component == null)
                QLog.Error($"查找组件失败：节点 {path} 上未找到 {typeof(T).Name}");

            return component;
        }
    }
}
