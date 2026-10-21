/*
 * ┌──────────────────────────────────┐
 * │  描    述: 模型仓库，按接口类型集中托管模型实例的注册与注销
 * │  类    名: ModelRegistry.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Collections.Generic;
using Framework.QTower.MVVM.Model.Interfaces;
using Utils.log;

namespace Framework.QTower.MVVM.Model
{
    /// <summary>
    /// 模型仓库：ViewModel 只面向接口取模型，实例由装配处注册，隔离构造依赖
    /// </summary>
    public sealed class ModelRegistry
    {
        private readonly Dictionary<Type, IModel> m_models = new Dictionary<Type, IModel>();
        private readonly List<Type> m_registerOrder = new List<Type>();

        private bool m_isShutdown;

        public int Count => m_models.Count;
        public bool IsShutdown => m_isShutdown;

        /// <summary>
        /// 注册模型实例并立即驱动 OnRegister，同接口重复注册视为配置错误
        /// </summary>
        /// <param name="model">待注册的模型实例</param>
        public TModel Register<TModel>(TModel model) where TModel : class, IModel
        {
            if (model == null)
            {
                QLog.Error("模型注册失败：实例为空");
                return null;
            }

            if (m_isShutdown)
            {
                QLog.Error($"模型注册失败：仓库已注销，目标 {typeof(TModel).Name}");
                return null;
            }

            Type contract = typeof(TModel);
            if (m_models.ContainsKey(contract))
            {
                QLog.Error($"模型注册失败：{contract.Name} 已存在模型，重复注册被忽略");
                return model;
            }

            m_models.Add(contract, model);
            m_registerOrder.Add(contract);
            model.OnRegister();
            return model;
        }

        /// <summary>
        /// 取模型，未注册时直接报错，不做静默兜底
        /// </summary>
        public TModel Get<TModel>() where TModel : class, IModel
        {
            if (TryGet(out TModel model))
                return model;

            QLog.Error($"模型获取失败：{typeof(TModel).Name} 未注册，请检查装配处");
            return null;
        }

        /// <summary>
        /// 尝试取模型，供可选依赖查询
        /// </summary>
        /// <param name="model">取到的模型实例</param>
        public bool TryGet<TModel>(out TModel model) where TModel : class, IModel
        {
            model = null;
            if (m_isShutdown) return false;

            if (!m_models.TryGetValue(typeof(TModel), out IModel registered)) return false;

            model = registered as TModel;
            if (model == null)
                QLog.Error($"模型类型不匹配：{typeof(TModel).Name} 注册的是 {registered.GetType().Name}");

            return model != null;
        }

        /// <summary>
        /// 按需创建模型：已存在则直接返回，避免装配处漏注册
        /// </summary>
        /// <param name="factory">首次创建模型用的工厂</param>
        public TModel GetOrCreate<TModel>(Func<TModel> factory) where TModel : class, IModel
        {
            if (TryGet(out TModel existing)) return existing;

            if (factory == null)
            {
                QLog.Error($"模型创建失败：{typeof(TModel).Name} 的工厂为空");
                return null;
            }

            return Register(factory());
        }

        /// <summary>
        /// 逆序注销全部模型，后注册的先释放，单个异常不中断其余模型
        /// </summary>
        public void Shutdown()
        {
            if (m_isShutdown) return;

            m_isShutdown = true;

            for (int i = m_registerOrder.Count - 1; i >= 0; i--)
            {
                Type contract = m_registerOrder[i];
                if (!m_models.TryGetValue(contract, out IModel model)) continue;

                try
                {
                    model.OnUnregister();
                }
                catch (Exception exception)
                {
                    QLog.Error($"注销模型异常：{contract.Name} {exception}");
                }
            }

            m_models.Clear();
            m_registerOrder.Clear();
        }
    }
}
