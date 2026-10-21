/*
 * ┌──────────────────────────────────┐
 * │  描    述: 模型层契约，声明数据与规则的注册与注销
 * │  类    名: IModel.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

namespace Framework.QTower.MVVM.Model.Interfaces
{
    /// <summary>
    /// 模型层契约：只负责数据与规则，不感知 View 与 ViewModel，也不依赖 Unity 对象
    /// 两个方法都要求幂等，由 ModelRegistry 在注册与注销时各调用一次
    /// </summary>
    public interface IModel
    {
        // 进入仓库后由框架调用，在此建立缓存、订阅数据源
        void OnRegister();

        // 仓库注销时由框架调用，在此解除订阅，实例本身交还创建方
        void OnUnregister();
    }
}
