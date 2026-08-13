/*
 * ┌────────────────────────────────────────────────────┐
 * │  描    述: 游戏调试总面板入口，负责统一管理各模块调试面板
 * │  类    名: GameManager.cs
 * │  创    建: By qiqizizzz
 * └────────────────────────────────────────────────────┘
 */

using System.Collections.Generic;
using UnityEngine;

namespace Tools.GM
{
    public sealed class GameManager : MonoBehaviour
    {
        private static GameManager S_instance;
        private readonly List<IGamePanel> m_panels = new();
        private bool m_isOpen;

        // 游戏启动时创建调试总面板
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateInstance()
        {
            if (S_instance != null)
                return;

            GameObject gameObject = new GameObject(nameof(GameManager));
            S_instance = gameObject.AddComponent<GameManager>();
            DontDestroyOnLoad(gameObject);
        }

        // 初始化已注册的模块调试面板
        private void Awake()
        {
            if (S_instance != null && S_instance != this)
            {
                Destroy(gameObject);
                return;
            }

            S_instance = this;
            DontDestroyOnLoad(gameObject);
            m_panels.Add(new PlayerGamePanel());
        }

        // 监听 H 键并更新模块面板数据
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                m_isOpen = !m_isOpen;

            if (!m_isOpen)
                return;

            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].Refresh();
        }

        // 绘制调试总面板
        private void OnGUI()
        {
            if (!m_isOpen)
                return;

            GUILayout.BeginArea(new Rect(20f, 20f, 420f, Screen.height - 40f), GUI.skin.box);
            GUILayout.Label("GameManager");

            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].Draw();

            GUILayout.EndArea();
        }
    }

}
