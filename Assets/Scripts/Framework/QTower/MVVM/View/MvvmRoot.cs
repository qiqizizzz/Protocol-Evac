/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM运行根节点，持有模型仓库与窗口服务的宿主
 * │  类    名: MvvmRoot.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using Framework.QTower.MVVM.Model;
using UnityEngine;
using Utils.log;

namespace Framework.QTower.MVVM.View
{
    /// <summary>
    /// MVVM 运行根节点：只负责窗口服务与模型仓库的生死，Canvas 与事件系统由装配处准备
    /// </summary>
    public sealed class MvvmRoot : MonoBehaviour
    {
        private const string VIEW_ROOT_NAME = "ViewRoot";

        private static MvvmRoot m_instance;

        private Transform m_viewRoot;

        public static MvvmRoot Instance => m_instance;

        public Transform ViewRoot => m_viewRoot;
        public ModelRegistry Models { get; private set; }
        public WindowService Windows { get; private set; }

        /// <summary>
        /// 创建或取得运行根节点，重复调用返回已有实例
        /// </summary>
        /// <param name="parent">挂载的父节点，通常是场景里的 Canvas 或其子层</param>
        /// <param name="rootName">根节点名称</param>
        public static MvvmRoot Create(Transform parent = null, string rootName = "MvvmRoot")
        {
            if (m_instance != null) return m_instance;

            GameObject rootObject = new GameObject(rootName);
            if (parent != null)
                rootObject.transform.SetParent(parent, false);

            // 构造期间 Awake 已执行, 这里直接取实例
            if (m_instance == null)
                QLog.Error($"[{nameof(MvvmRoot)}] 根节点创建失败：{rootName}");

            return m_instance;
        }

        private void Awake()
        {
            if (m_instance != null && m_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            m_instance = this;

            GameObject viewNode = new GameObject(VIEW_ROOT_NAME, typeof(RectTransform));
            viewNode.transform.SetParent(transform, false);
            m_viewRoot = viewNode.transform;

            Models = new ModelRegistry();
            Windows = new WindowService(m_viewRoot);
        }

        private void OnDestroy()
        {
            Windows?.DestroyAll();
            Models?.Shutdown();

            if (m_instance != this) return;

            m_instance = null;
        }
    }
}
