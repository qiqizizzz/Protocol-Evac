/*
 * ┌──────────────────────────────────┐
 * │  描    述: 业务模块标记，贴在静态装配类上供 MvvmHost 反射发现
 * │  类    名: MvvmModuleAttribute.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using UnityEngine.Scripting;

namespace Framework.QTower.MVVM.View
{
    /// <summary>
    /// 业务模块标记：贴在静态装配类上，MvvmHost 启动时自动发现并调用其 Register(MvvmRoot)
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [Preserve]
    public sealed class MvvmModuleAttribute : Attribute
    {
        private readonly int m_order;

        public MvvmModuleAttribute(int order = 0)
        {
            m_order = order;
        }

        /// <summary>
        /// 装配顺序，值小的先执行；同值时按程序集名与类名定序
        /// </summary>
        public int Order => m_order;
    }
}
