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
        private static readonly string[] S_tabNames = { "概览", "玩家", "状态机", "群体" };

        private static GameManager S_instance;
        private readonly List<IGamePanel> m_panels = new();
        private CrowdGamePanel m_crowdGamePanel;
        private bool m_isOpen;
        private int m_activeTabIndex;
        private Vector2 m_contentScrollPosition;
        private GUIStyle m_panelStyle;
        private GUIStyle m_headerStyle;
        private GUIStyle m_tabStyle;
        private GUIStyle m_contentStyle;
        private GUIStyle m_labelStyle;
        private GUIStyle m_valueStyle;
        private GUIStyle m_toggleStyle;
        private GUIStyle m_buttonStyle;
        private GUIStyle m_sectionStyle;
        private GUIStyle m_sectionHeaderStyle;
        private GUIStyle m_inputStyle;
        private GUIStyle m_statusStyle;
        private Texture2D m_panelTexture;
        private Texture2D m_headerTexture;
        private Texture2D m_tabTexture;
        private Texture2D m_activeTabTexture;
        private Texture2D m_buttonTexture;
        private Texture2D m_sectionTexture;

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
            m_crowdGamePanel = new CrowdGamePanel();
            m_panels.Add(m_crowdGamePanel);
        }

        // 释放运行时创建的 IMGUI 纹理
        private void OnDestroy()
        {
            Destroy(m_panelTexture);
            Destroy(m_headerTexture);
            Destroy(m_tabTexture);
            Destroy(m_activeTabTexture);
            Destroy(m_buttonTexture);
            Destroy(m_sectionTexture);
        }

        // 监听 H 键并更新模块面板数据
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.H))
                m_isOpen = !m_isOpen;

            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].Tick(Time.deltaTime);

            if (!m_isOpen)
                return;

            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].Refresh();
        }

        // 驱动调试面板关联模块的固定步长逻辑
        private void FixedUpdate()
        {
            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].FixedTick(Time.fixedDeltaTime);
        }

        // 在模拟完成后驱动调试面板关联模块同步表现
        private void LateUpdate()
        {
            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].LateTick(Time.deltaTime);
        }

        // 绘制调试总面板
        private void OnGUI()
        {
            if (!m_isOpen)
                return;

            CreateStyles();

            float panelWidth = Mathf.Min(480f, Screen.width - 40f);
            float panelHeight = Mathf.Min(700f, Screen.height - 40f);
            GUILayout.BeginArea(new Rect(20f, 20f, panelWidth, panelHeight), m_panelStyle);
            GUILayout.Label("GM Console", m_headerStyle);
            m_activeTabIndex = GUILayout.Toolbar(m_activeTabIndex, S_tabNames, m_tabStyle);
            GUILayout.Space(12f);
            m_contentScrollPosition = GUILayout.BeginScrollView(m_contentScrollPosition, false, true,
                GUILayout.ExpandHeight(true));
            GUILayout.BeginVertical(m_contentStyle);

            for (int i = 0; i < m_panels.Count; i++)
                m_panels[i].Draw(m_activeTabIndex, m_labelStyle, m_valueStyle, m_toggleStyle, m_buttonStyle,
                    m_sectionStyle, m_sectionHeaderStyle, m_inputStyle, m_statusStyle);

            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        // 创建 GM 面板所需的 IMGUI 样式
        private void CreateStyles()
        {
            if (m_panelStyle != null)
                return;

            m_panelTexture = CreateColorTexture(new Color(0.035f, 0.055f, 0.08f, 0.96f));
            m_headerTexture = CreateColorTexture(new Color(0.055f, 0.18f, 0.24f, 1f));
            m_tabTexture = CreateColorTexture(new Color(0.075f, 0.1f, 0.14f, 1f));
            m_activeTabTexture = CreateColorTexture(new Color(0.08f, 0.46f, 0.58f, 1f));
            m_buttonTexture = CreateColorTexture(new Color(0.1f, 0.31f, 0.38f, 1f));
            m_sectionTexture = CreateColorTexture(new Color(0.055f, 0.075f, 0.105f, 1f));

            m_panelStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_panelTexture },
                padding = new RectOffset(16, 16, 16, 16)
            };
            m_headerStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { background = m_headerTexture, textColor = Color.white },
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                padding = new RectOffset(12, 12, 10, 10),
                margin = new RectOffset(0, 0, 0, 10)
            };
            m_tabStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = m_tabTexture, textColor = new Color(0.7f, 0.8f, 0.85f) },
                onNormal = { background = m_activeTabTexture, textColor = Color.white },
                hover = { background = m_activeTabTexture, textColor = Color.white },
                onHover = { background = m_activeTabTexture, textColor = Color.white },
                fixedHeight = 32f,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            m_contentStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_tabTexture },
                padding = new RectOffset(14, 14, 14, 14)
            };
            m_labelStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.58f, 0.7f, 0.76f) },
                fontSize = 13,
                alignment = TextAnchor.MiddleLeft
            };
            m_valueStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.9f, 0.97f, 1f) },
                fontSize = 13,
                wordWrap = true,
                alignment = TextAnchor.MiddleLeft
            };
            m_toggleStyle = new GUIStyle(GUI.skin.toggle)
            {
                normal = { textColor = Color.white },
                onNormal = { textColor = new Color(0.38f, 0.9f, 0.78f) },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                margin = new RectOffset(0, 0, 8, 12)
            };
            m_buttonStyle = new GUIStyle(GUI.skin.button)
            {
                normal = { background = m_buttonTexture, textColor = Color.white },
                hover = { background = m_activeTabTexture, textColor = Color.white },
                fixedHeight = 32f,
                fontSize = 13,
                fontStyle = FontStyle.Bold
            };
            m_sectionStyle = new GUIStyle(GUI.skin.box)
            {
                normal = { background = m_sectionTexture },
                padding = new RectOffset(12, 12, 10, 12),
                margin = new RectOffset(0, 0, 0, 10)
            };
            m_sectionHeaderStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = Color.white },
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(0, 0, 0, 8)
            };
            m_inputStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 13,
                fixedHeight = 30f,
                padding = new RectOffset(8, 8, 5, 5)
            };
            m_statusStyle = new GUIStyle(GUI.skin.label)
            {
                normal = { textColor = new Color(0.4f, 0.9f, 0.78f) },
                fontSize = 12,
                wordWrap = true,
                padding = new RectOffset(0, 0, 6, 0)
            };
        }

        // 创建单色 IMGUI 背景纹理
        private Texture2D CreateColorTexture(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }

}
