/*
 * ┌──────────────────────────────────┐
 * │  描    述: MVVM验收测试装配器，运行期搭建Canvas与测试面板节点
 * │  类    名: MvvmTestBootstrap.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Utils.log;

namespace UI.MvvmTest
{
    /// <summary>
    /// 验收测试装配器：不依赖 prefab 与 Addressables，运行期搭出面板后走真实 UIBase 生命周期
    /// </summary>
    public sealed class MvvmTestBootstrap : MonoBehaviour
    {
        private const string CANVAS_NAME = "MvvmTest_Canvas";
        private const string PANEL_NAME = "UIMvvmTestPanel";
        private const string BOOTSTRAP_NAME = "MvvmTest_Bootstrap";
        private const int SORTING_ORDER = 32767;

        private static readonly Vector2 m_panelSize = new Vector2(760f, 460f);
        private static readonly Vector2 m_buttonSize = new Vector2(200f, 64f);
        private static readonly Color m_panelColor = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        private static readonly Color m_buttonColor = new Color(0.18f, 0.34f, 0.55f, 1f);

        public static MvvmTestBootstrap Instance { get; private set; }

        public MvvmTestPanelView Panel { get; private set; }
        public Canvas Canvas { get; private set; }

        private bool m_isBuilt;

        /// <summary>
        /// 取当前测试装配器，没有则新建一个，全部物体标记 DontSave 不污染场景
        /// </summary>
        public static MvvmTestBootstrap Spawn()
        {
            MvvmTestBootstrap bootstrap = Instance;
            if (bootstrap == null)
            {
                GameObject host = new GameObject(BOOTSTRAP_NAME);
                host.hideFlags = HideFlags.DontSave;
                bootstrap = host.AddComponent<MvvmTestBootstrap>();
            }

            // 搭建不放在 Awake：MonoBehaviour 回调里的异常会被 Unity 吞成一条日志，
            // 放在这里由调用方直接触发，堆栈能原样抛出来
            bootstrap.Build();
            return bootstrap;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// 搭建 Canvas、测试面板与事件系统，重复调用只生效一次
        /// </summary>
        public void Build()
        {
            if (m_isBuilt)
                return;

            BuildCanvas();
            BuildPanel();
            EnsureEventSystem();
            m_isBuilt = true;

            QLog.Info($"[{nameof(MvvmTestBootstrap)}] 测试面板已就绪：{Panel.Snapshot()}");
        }

        // 搭建独立测试 Canvas，不复用场景内已有 UI，避免影响正式界面
        private void BuildCanvas()
        {
            GameObject canvasObject = new GameObject(CANVAS_NAME, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.hideFlags = HideFlags.DontSave;
            Canvas = canvasObject.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = SORTING_ORDER;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        // 搭建面板节点并走 UIBase 的 Init 与 Open
        private void BuildPanel()
        {
            RectTransform panelRect = CreateRect(PANEL_NAME, Canvas.transform);

            Image background = panelRect.gameObject.AddComponent<Image>();
            background.color = m_panelColor;

            SetRect(panelRect, Vector2.zero, m_panelSize);

            CreateText("Txt_Title", panelRect, "未绑定", 34f, new Vector2(0f, 170f), new Vector2(700f, 52f));
            CreateText("Txt_Count", panelRect, "未绑定", 30f, new Vector2(0f, 112f), new Vector2(700f, 46f));
            CreateText("Txt_Log", panelRect, "等待回调", 24f, new Vector2(0f, -120f), new Vector2(700f, 150f));

            CreateButton("Btn_Add", "Add", panelRect, new Vector2(-250f, 40f));
            CreateButton("Btn_Ping", "Ping", panelRect, new Vector2(0f, 40f));
            CreateButton("Btn_Lock", "Lock", panelRect, new Vector2(250f, 40f));
            CreateToggle("Tgl_Lock", panelRect, new Vector2(0f, -30f));

            Panel = panelRect.gameObject.AddComponent<MvvmTestPanelView>();
            Panel.Init();
            Panel.Open();
        }

        // 保证按钮可点：场景内没有事件系统时补一个
        private void EnsureEventSystem()
        {
            if (EventSystem.current != null)
                return;

            GameObject eventObject = new GameObject("MvvmTest_EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventObject.hideFlags = HideFlags.DontSave;
        }

        // 创建 UI 节点：运行期 SetParent 不会把 Transform 升级成 RectTransform，必须显式带组件创建
        private static RectTransform CreateRect(string name, Transform parent)
        {
            GameObject node = new GameObject(name, typeof(RectTransform));
            node.hideFlags = HideFlags.DontSave;

            RectTransform rect = node.GetComponent<RectTransform>();
            if (rect == null) QLog.Error($"[{nameof(MvvmTestBootstrap)}] 节点 {name} 未创建出 RectTransform");

            rect.SetParent(parent, false);
            return rect;
        }

        // 以中心锚点定位矩形
        private static void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        // 创建文本节点
        private static TMP_Text CreateText(string name, Transform parent, string content, float fontSize, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchoredPosition, sizeDelta);

            TMP_Text label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = content;
            label.fontSize = fontSize;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.MidlineLeft;
            label.overflowMode = TextOverflowModes.Overflow;
            label.enableWordWrapping = false;
            return label;
        }

        // 创建按钮节点，子文本路径固定为 Text
        private static Button CreateButton(string name, string labelText, Transform parent, Vector2 anchoredPosition)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchoredPosition, m_buttonSize);

            Image background = rect.gameObject.AddComponent<Image>();
            background.color = m_buttonColor;

            Button button = rect.gameObject.AddComponent<Button>();
            CreateText("Text", rect, labelText, 28f, Vector2.zero, m_buttonSize);
            return button;
        }

        // 创建开关节点，勾选图形指向自身底图
        private static Toggle CreateToggle(string name, Transform parent, Vector2 anchoredPosition)
        {
            RectTransform rect = CreateRect(name, parent);
            SetRect(rect, anchoredPosition, new Vector2(36f, 36f));

            Image background = rect.gameObject.AddComponent<Image>();
            background.color = new Color(0.35f, 0.35f, 0.4f, 1f);

            Toggle toggle = rect.gameObject.AddComponent<Toggle>();
            toggle.graphic = background;
            CreateText("Label", rect, "LockOn", 24f, new Vector2(120f, 0f), new Vector2(160f, 36f));
            return toggle;
        }
    }
}
