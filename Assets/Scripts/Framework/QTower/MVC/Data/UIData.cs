/*
 * ┌──────────────────────────────────┐
 * │  描    述: UI 注册数据，保存视图类型与所属控制器
 * │  类    名: UIData.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using Framework.QTower.Controller;

namespace Framework.QTower
{
    public sealed class UIData
    {
        public Type ViewClass { get; }
        public BaseController Controller { get; }

        // 创建 UI 注册数据
        public UIData(Type viewClass, BaseController controller)
        {
            ViewClass = viewClass;
            Controller = controller;
        }
    }
}
