/*
 * ┌──────────────────────────────────┐
 * │  描    述: 控制器管理器，统一驱动控制器生命周期
 * │  类    名: ControllerManager.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;

namespace Framework.QTower.Controller
{
    public sealed class ControllerManager
    {
        private readonly List<BaseController> m_controllers = new();

        // 注册并初始化控制器
        public T Register<T>(T controller) where T : BaseController
        {
            m_controllers.Add(controller);
            controller.Init();
            return controller;
        }

        // 按注册逆序销毁控制器
        public void Destroy()
        {
            for (int i = m_controllers.Count - 1; i >= 0; i--)
                m_controllers[i].Destroy();

            m_controllers.Clear();
        }
    }
}
