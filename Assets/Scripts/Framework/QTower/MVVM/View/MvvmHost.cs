/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM装配入口，场景唯一挂载点，创建运行根节点并驱动业务模块注册
 * │  类    名: MvvmHost.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    /// <summary>
    /// MVVM 装配入口：场景里只挂这一个脚本，自动发现带 [MvvmModule] 的业务装配类并驱动注册
    /// </summary>
    public sealed class MvvmHost : MonoBehaviour
    {
        private const string REGISTER_METHOD_NAME = "Register";

        // ==================== 字段[外部设置] ====================
        [Header("装配设置")]
        [Tooltip("运行根节点名称")]
        [SerializeField] private string RootName = "MvvmRoot";

        [Tooltip("是否在 Awake 自动完成装配，关闭后需自行调用 Run")]
        [SerializeField] private bool AutoRunOnAwake = true;

        // ==================== 属性 ====================
        public MvvmRoot Root => m_root;
        public bool IsRun => m_isRun;

        // ==================== 字段[私有] ====================
        private MvvmRoot m_root;
        private bool m_isRun;

        // ==================== 生命周期 ====================
        private void Awake()
        {
            if (AutoRunOnAwake)
                Run();
        }

        // ==================== Public Function ====================
        /// <summary>
        /// 执行装配：创建运行根节点并按顺序调用全部业务模块，重复调用只生效一次
        /// </summary>
        public MvvmRoot Run()
        {
            if (m_isRun) return m_root;

            m_root = MvvmRoot.Create(transform, RootName);
            if (m_root == null)
            {
                QLog.Error($"[{nameof(MvvmHost)}] 装配失败：运行根节点未创建，检查 {name} 的父节点是否处于激活状态");
                return null;
            }

            m_isRun = true;

            List<Type> moduleTypes = CollectModuleTypes();
            if (moduleTypes.Count == 0)
                QLog.Warning($"[{nameof(MvvmHost)}] 未发现任何 [MvvmModule] 业务模块，模型与视图都不会被注册");

            for (int i = 0; i < moduleTypes.Count; i++)
                InvokeModule(moduleTypes[i], m_root);

            return m_root;
        }

        /// <summary>
        /// 扫描当前域内所有贴了 [MvvmModule] 的静态装配类，按 Order 再按程序集名与类名排序
        /// </summary>
        public static List<Type> CollectModuleTypes()
        {
            List<Type> moduleTypes = new List<Type>();

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types = GetLoadableTypes(assembly);
                if (types == null) continue;

                foreach (Type type in types)
                {
                    // C# 静态类在 IL 里是 abstract + sealed, 这里不能按 IsAbstract 过滤
                    if (!type.IsClass) continue;
                    if (type.GetCustomAttributes(typeof(MvvmModuleAttribute), false).Length == 0) continue;
                    if (moduleTypes.Contains(type)) continue;

                    moduleTypes.Add(type);
                }
            }

            moduleTypes.Sort(CompareModulePriority);
            return moduleTypes;
        }

        /// <summary>
        /// 调用业务装配类的静态 Register(MvvmRoot)，方法缺失或抛异常都只报错，不影响其余模块
        /// </summary>
        /// <param name="moduleType">业务装配类</param>
        /// <param name="root">运行根节点</param>
        public static void InvokeModule(Type moduleType, MvvmRoot root)
        {
            if (moduleType == null)
            {
                QLog.Error($"[{nameof(MvvmHost)}] 模块装配失败：业务模块类型为空");
                return;
            }

            if (root == null)
            {
                QLog.Error($"[{nameof(MvvmHost)}] 模块装配失败：运行根节点为空，目标 {moduleType.Name}");
                return;
            }

            MethodInfo method = moduleType.GetMethod(REGISTER_METHOD_NAME,
                BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(MvvmRoot) }, null);

            if (method == null)
            {
                QLog.Error($"[{nameof(MvvmHost)}] 模块装配失败：{moduleType.FullName} 缺少 public static void {REGISTER_METHOD_NAME}(MvvmRoot)");
                return;
            }

            try
            {
                method.Invoke(null, new object[] { root });
            }
            catch (TargetInvocationException exception)
            {
                QLog.Error($"[{nameof(MvvmHost)}] 模块装配异常：{moduleType.FullName} {exception.InnerException?.Message ?? exception.Message}");
            }
            catch (Exception exception)
            {
                QLog.Error($"[{nameof(MvvmHost)}] 模块装配异常：{moduleType.FullName} {exception.Message}");
            }
        }

        // ==================== Private Function ====================
        // 程序集里可能有加载失败的类型, 只取能拿到的部分, 不因个别类型丢掉整个程序集
        private static Type[] GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException exception)
            {
                List<Type> loaded = new List<Type>();
                foreach (Type type in exception.Types)
                    if (type != null) loaded.Add(type);

                return loaded.Count > 0 ? loaded.ToArray() : null;
            }
            catch (Exception exception)
            {
                QLog.Warning($"[{nameof(MvvmHost)}] 跳过无法扫描的程序集：{assembly.FullName} {exception.Message}");
                return null;
            }
        }

        // Order 小的先装配，同 Order 按程序集名+类名定序，保证每次运行顺序一致
        private static int CompareModulePriority(Type left, Type right)
        {
            int order = OrderOf(left).CompareTo(OrderOf(right));
            if (order != 0) return order;

            return string.CompareOrdinal(FullKeyOf(left), FullKeyOf(right));
        }

        private static int OrderOf(Type moduleType)
        {
            object[] attributes = moduleType.GetCustomAttributes(typeof(MvvmModuleAttribute), false);
            return attributes.Length > 0 && attributes[0] is MvvmModuleAttribute attribute ? attribute.Order : 0;
        }

        private static string FullKeyOf(Type moduleType)
            => $"{moduleType.Assembly?.GetName().Name}.{moduleType.FullName}";
    }
}
