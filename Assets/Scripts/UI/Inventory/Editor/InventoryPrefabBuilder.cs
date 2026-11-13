/*
 * ┌──────────────────────────────────┐
* │  描    述: 背包预制体生成器，产出载体与格子、物品三张模板，另可单独重出物品模板、战备面板与三张物品弹窗
 * │  类    名: InventoryPrefabBuilder.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using Framework.QTower.Common.Defines;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Utils.log;

namespace UI.Inventory.Editor
{
    // 7a 装配工具: 只搭节点与 Unity 组件, 业务脚本按完整类型名反射挂, 脚本没敲完也能先出树
    public static class InventoryPrefabBuilder
    {
        // ==================== 常量与静态字段 ====================
        private const string LOG = "[InventoryPrefabBuilder] ";
        private const string PREFAB_DIR = "Assets/Prefabs/UI/Inventory";
        private const string CARRIER_PATH = PREFAB_DIR + "/UIInventoryCarrier.prefab";
        private const string SLOT_PATH = PREFAB_DIR + "/UIInventorySlot.prefab";
        private const string ITEM_PATH = PREFAB_DIR + "/UIInventoryItem.prefab";
        private const string LEGACY_PATH = PREFAB_DIR + "/UIInventoryPanel.prefab";
        private const string SILHOUETTE_MATERIAL_PATH =
            "Assets/Shaders/UI/Mat_LootSearchSilhouette.mat";
        private const float SILHOUETTE_INSET = 2f;

        private const string CARRIER_SCRIPT = "UI.Inventory.View.UIInventoryCarrier";
        private const string SLOT_SCRIPT = "UI.Inventory.View.UIInventorySlot";
        private const string ITEM_SCRIPT = "UI.Inventory.View.UIInventoryItem";

        private const string EQUIPMENT_SCRIPT = "UI.Inventory.View.UIEquipmentPanel";
        private const string EQUIPMENT_PATH = PREFAB_DIR + "/UIEquipmentPanel.prefab";
        private const string ADDRESSABLE_GROUP_NAME = "UI";

        private const string DETAIL_SCRIPT = "UI.Inventory.View.UIItemDetailPanel";
        private const string DETAIL_PATH = PREFAB_DIR + "/UIItemDetailPanel.prefab";
        private const string MENU_SCRIPT = "UI.Inventory.View.UIItemMenuPanel";
        private const string MENU_PATH = PREFAB_DIR + "/UIItemMenuPanel.prefab";
        private const string SPLIT_SCRIPT = "UI.Inventory.View.UIItemSplitPanel";
        private const string SPLIT_PATH = PREFAB_DIR + "/UIItemSplitPanel.prefab";

        private const int CELL_SIZE = 80;
        private const int CELL_SPACING = 4;
        private const int DEFAULT_COLUMNS = 8;
        private const int DEFAULT_ROWS = 5;
        private const int PADDING = 12;
        private const int HEADER_HEIGHT = 32;
        private const int HINT_HEIGHT = 28;
        private const int ROTATE_WIDTH = 96;
        private const int GAP = 8;
        private const string SELECTED_FRAME_NAME = "Img_Selected";
        private const int SELECTED_BORDER_WIDTH = 3;

        private const int PANEL_PADDING = 16;
        private const int CONTAINER_SPACING = 16;
        private const int PANEL_COLUMN_COUNT = 2;

        private static readonly Color m_itemDefaultColor = new Color(0.16f, 0.18f, 0.21f, 0.95f);
        private static readonly Color m_selectedBorderColor = new Color(0.31f, 0.76f, 1f, 1f);
        private const string CJK_FONT_PATH = "Assets/Fonts/miSans/MiSans-Regular SDF.asset";

        // 弹窗卡片与遮罩：详情卡片不接收射线, 菜单卡片接收射线, 全屏遮罩用于点击空白隐藏
        private static readonly Color m_popupBgColor = new Color(0.086f, 0.098f, 0.118f, 0.97f);
        private static readonly Color m_popupDimColor = new Color(0f, 0f, 0f, 0.25f);
        private static readonly Color m_popupButtonColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color m_textPrimaryColor = new Color(0.9f, 0.93f, 0.98f, 1f);
        private static readonly Color m_textSecondColor = new Color(0.62f, 0.68f, 0.75f, 1f);

        private static readonly Vector2 m_centerAnchor = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 m_topLeftAnchor = new Vector2(0f, 1f);
        private static readonly Vector2 m_topRightAnchor = new Vector2(1f, 1f);
        private static readonly Vector2 m_topCenterAnchor = new Vector2(0.5f, 1f);

        private const int DETAIL_CARD_WIDTH = 320;
        private const int DETAIL_CARD_HEIGHT = 372;
        private const int DETAIL_ICON_SIZE = 120;
        private const int DETAIL_TEXT_WIDTH = 288;
        private const int DETAIL_EDGE = 16;
        private const int DETAIL_NAME_TOP = 148;
        private const int DETAIL_TAGS_TOP = 182;
        private const int DETAIL_SPEC_TOP = 214;
        private const int DETAIL_SPEC_HEIGHT = 140;
        private const int POPUP_EDGE = 8;
        private const int MENU_CARD_WIDTH = 168;
        private const int MENU_BUTTON_WIDTH = 152;
        private const int MENU_BUTTON_HEIGHT = 36;
        private const int CLOSE_BUTTON_WIDTH = 56;
        private const int CLOSE_BUTTON_HEIGHT = 28;

        // 拆分弹窗：卡片两行定宽布局, 滑条行与操作行均由 HorizontalLayoutGroup 排布
        private const int SPLIT_CARD_WIDTH = 288;
        private const int SPLIT_ROW_WIDTH = 272;
        private const int SPLIT_COUNT_HEIGHT = 28;
        private const int SPLIT_ROW_HEIGHT = 32;
        private const int SPLIT_STEP_BUTTON_SIZE = 32;
        private const int SPLIT_SLIDER_WIDTH = 192;
        private const int SPLIT_ACTION_BUTTON_WIDTH = 132;
        private const int SLIDER_HANDLE_WIDTH = 20;
        private const int SLIDER_FILL_INSET = 13;

        private static readonly Color m_sliderBgColor = new Color(1f, 1f, 1f, 0.18f);
        private static readonly Color m_sliderFillColor = new Color(0.62f, 0.68f, 0.75f, 0.9f);
        private static readonly Color m_sliderHandleColor = new Color(0.9f, 0.93f, 0.98f, 1f);

        // 载体节点后缀， 顺序与 InventoryContainerIds.ROLE_SLOT_IDS 一致
        private static readonly string[] m_carrierNames = { "Backpack", "Rig", "Pockets", "SafeBox" };

        private static readonly List<string> m_missingScripts = new List<string>();

        // ==================== Public Function ====================
        [MenuItem("Tools/背包7a/0.生成背包载体预制体")]
        public static void BuildAll()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError(LOG + "当前处于 Prefab 编辑模式，请先退出再运行");
                return;
            }

            m_missingScripts.Clear();
            DeleteLegacy();

            // 临时空场景承载节点, 存完就关, 不碰你正在编辑的场景
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SavePrefab(BuildSlot(), SLOT_PATH);
                SavePrefab(BuildItem(), ITEM_PATH);
                SavePrefab(BuildCarrier(), CARRIER_PATH);
            }
            finally
            {
                EditorSceneManager.CloseScene(tempScene, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Report();
            }
        }

        // 只产出战备面板, 三张模板与载体预制体一概不动, 避免覆盖手工调过的 UIInventoryCarrier
        [MenuItem("Tools/背包7a/7.生成战备面板预制体")]
        public static void BuildEquipmentPanel()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError(LOG + "当前处于 Prefab 编辑模式，请先退出再运行");
                return;
            }

            GameObject carrierAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CARRIER_PATH);
            if (carrierAsset == null)
            {
                Debug.LogError(LOG + "找不到载体预制体 " + CARRIER_PATH + "，请先点 0.生成背包载体预制体");
                return;
            }

            m_missingScripts.Clear();

            // 临时空场景承载节点, 存完就关, 不碰正在编辑的场景
            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                GameObject root = BuildEquipment(carrierAsset);
                if (root == null) return;

                SavePrefab(root, EQUIPMENT_PATH);
                RegisterAddressable(EQUIPMENT_PATH, UIDefines.UIEquipmentPanel);
            }
            finally
            {
                EditorSceneManager.CloseScene(tempScene, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (m_missingScripts.Count > 0)
                    Debug.LogWarning(LOG + "以下脚本还没敲，本次未挂上，敲完再点一次本菜单即可: " + string.Join(", ", m_missingScripts));
            }
        }
        // 只重出物品模板, 格子与载体预制体一概不动, 避免覆盖手工调过的 UIInventoryCarrier
        [MenuItem("Tools/背包7a/8.仅生成物品模板")]
        public static void BuildItemTemplate()
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError(LOG + "当前处于 Prefab 编辑模式，请先退出再运行");
                return;
            }

            m_missingScripts.Clear();

            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SavePrefab(BuildItem(), ITEM_PATH);
            }
            finally
            {
                EditorSceneManager.CloseScene(tempScene, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (m_missingScripts.Count > 0)
                    Debug.LogWarning(LOG + "脚本还没敲，本次未挂上，敲完再点一次本菜单即可: " + string.Join(", ", m_missingScripts));
            }
        }

        // 仅生成物品详情弹窗, 不修改战备面板与三张模板
        [MenuItem("Tools/背包7a/9.生成物品详情弹窗预制体")]
        public static void BuildDetailPopup() => BuildPopup(BuildDetailCard, DETAIL_PATH, UIDefines.UIItemDetailPanel);

        // 仅生成物品右键菜单弹窗
        [MenuItem("Tools/背包7a/10.生成物品右键菜单预制体")]
        public static void BuildMenuPopup() => BuildPopup(BuildMenuCard, MENU_PATH, UIDefines.UIItemMenuPanel);

        // 仅生成物品拆分弹窗
        [MenuItem("Tools/背包7a/11.生成物品拆分弹窗预制体")]
        public static void BuildSplitPopup() => BuildPopup(BuildSplitCard, SPLIT_PATH, UIDefines.UIItemSplitPanel);

        // ==================== Private Function ====================
        // 在临时空场景构建弹窗节点, 保存为预制体并登记 Addressables, 不影响当前场景
        private static void BuildPopup(Func<GameObject> builder, string path, string address)
        {
            if (PrefabStageUtility.GetCurrentPrefabStage() != null)
            {
                Debug.LogError(LOG + "当前处于 Prefab 编辑模式，请先退出再运行");
                return;
            }

            m_missingScripts.Clear();

            Scene tempScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SavePrefab(builder(), path);
                RegisterAddressable(path, address);
            }
            finally
            {
                EditorSceneManager.CloseScene(tempScene, true);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                if (m_missingScripts.Count > 0)
                    Debug.LogWarning(LOG + "脚本还没敲，本次未挂上，敲完再点一次本菜单即可: " + string.Join(", ", m_missingScripts));
            }
        }

        // 详情弹窗：全屏根节点与左上角轴心卡片, 卡片位置由 View 依据鼠标设置
        private static GameObject BuildDetailCard()
        {
            GameObject root = NewNode("UIItemDetailPanel", null);
            SetStretch(root.GetComponent<RectTransform>(), 0f);

            GameObject card = NewNode("Panel_Detail", root.transform);
            SetRect(card.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_topLeftAnchor,
                new Vector2(DETAIL_CARD_WIDTH, DETAIL_CARD_HEIGHT), Vector2.zero);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = m_popupBgColor;
            // 卡片不接收射线, 卡片外的点击落到下层载体, 由交互逻辑决定隐藏或切换物品
            cardImage.raycastTarget = false;

            GameObject icon = NewNode("Img_Icon", card.transform);
            SetRect(icon.GetComponent<RectTransform>(), m_topCenterAnchor, m_topCenterAnchor, m_topCenterAnchor,
                new Vector2(DETAIL_ICON_SIZE, DETAIL_ICON_SIZE), new Vector2(0f, -POPUP_EDGE * 2f));
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            // 无 Sprite 的 Image 渲染为纯白, 图标组件在 View 绑定 Sprite 后启用
            iconImage.enabled = false;

            GameObject nameNode = NewNode("Txt_Name", card.transform);
            SetRect(nameNode.GetComponent<RectTransform>(), m_topLeftAnchor, m_topLeftAnchor, m_topLeftAnchor,
                new Vector2(DETAIL_TEXT_WIDTH, 30f), new Vector2(DETAIL_EDGE, -DETAIL_NAME_TOP));
            AddTmp(nameNode, "物品名称", 24, TextAlignmentOptions.MidlineLeft, m_textPrimaryColor);

            GameObject tags = NewNode("Txt_Tags", card.transform);
            SetRect(tags.GetComponent<RectTransform>(), m_topLeftAnchor, m_topLeftAnchor, m_topLeftAnchor,
                new Vector2(DETAIL_TEXT_WIDTH, 24f), new Vector2(DETAIL_EDGE, -DETAIL_TAGS_TOP));
            AddTmp(tags, "类型 · 品质 · 容器", 18, TextAlignmentOptions.MidlineLeft, m_textSecondColor);

            GameObject spec = NewNode("Txt_Spec", card.transform);
            SetRect(spec.GetComponent<RectTransform>(), m_topLeftAnchor, m_topLeftAnchor, m_topLeftAnchor,
                new Vector2(DETAIL_TEXT_WIDTH, DETAIL_SPEC_HEIGHT), new Vector2(DETAIL_EDGE, -DETAIL_SPEC_TOP));
            AddTmp(spec, "占格  0x0", 18, TextAlignmentOptions.TopLeft, m_textSecondColor);

            GameObject close = NewNode("Btn_Close", card.transform);
            SetRect(close.GetComponent<RectTransform>(), m_topRightAnchor, m_topRightAnchor, m_topRightAnchor,
                new Vector2(CLOSE_BUTTON_WIDTH, CLOSE_BUTTON_HEIGHT), new Vector2(-POPUP_EDGE, -POPUP_EDGE));
            AddButton(close, m_popupButtonColor);
            GameObject closeText = NewNode("Txt_Close", close.transform);
            SetStretch(closeText.GetComponent<RectTransform>(), 0f);
            AddTmp(closeText, "关闭", 16, TextAlignmentOptions.Midline, m_textPrimaryColor);

            AttachScript(root, DETAIL_SCRIPT);
            return root;
        }

        // 右键菜单弹窗：全屏透明遮罩用于点击空白隐藏, 卡片竖排按钮执行操作
        private static GameObject BuildMenuCard()
        {
            GameObject root = NewNode("UIItemMenuPanel", null);
            SetStretch(root.GetComponent<RectTransform>(), 0f);

            GameObject dismiss = NewNode("Btn_Dismiss", root.transform);
            SetStretch(dismiss.GetComponent<RectTransform>(), 0f);
            AddButton(dismiss, m_popupDimColor);

            GameObject card = NewNode("Panel_Menu", root.transform);
            // 高度仅为初始值, 实际尺寸由 ContentSizeFitter 按按钮数量计算
            SetRect(card.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_topLeftAnchor,
                new Vector2(MENU_CARD_WIDTH, MENU_BUTTON_HEIGHT + POPUP_EDGE * 2), Vector2.zero);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = m_popupBgColor;
            // 卡片需遮挡遮罩射线, 否则点击按钮会同时触发隐藏
            cardImage.raycastTarget = true;

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(POPUP_EDGE, POPUP_EDGE, POPUP_EDGE, POPUP_EDGE);
            cardLayout.spacing = 4;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            // 布局仅负责竖向排列, 按钮尺寸由自身参数决定, 后续新增项无需调整
            cardLayout.childControlWidth = false;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            ContentSizeFitter cardFitter = card.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject rotate = NewNode("Btn_Rotate", card.transform);
            rotate.GetComponent<RectTransform>().sizeDelta = new Vector2(MENU_BUTTON_WIDTH, MENU_BUTTON_HEIGHT);
            AddButton(rotate, m_popupButtonColor);
            GameObject rotateText = NewNode("Txt_Rotate", rotate.transform);
            SetStretch(rotateText.GetComponent<RectTransform>(), 0f);
            AddTmp(rotateText, "旋转", 20, TextAlignmentOptions.Midline, m_textPrimaryColor);

            GameObject split = NewNode("Btn_Split", card.transform);
            split.GetComponent<RectTransform>().sizeDelta = new Vector2(MENU_BUTTON_WIDTH, MENU_BUTTON_HEIGHT);
            AddButton(split, m_popupButtonColor);
            GameObject splitText = NewNode("Txt_Split", split.transform);
            SetStretch(splitText.GetComponent<RectTransform>(), 0f);
            AddTmp(splitText, "拆分", 20, TextAlignmentOptions.Midline, m_textPrimaryColor);

            AttachScript(root, MENU_SCRIPT);
            return root;
        }

        // 拆分弹窗：全屏遮罩用于点击空白隐藏, 卡片竖排数量文本、滑条行与操作行
        private static GameObject BuildSplitCard()
        {
            GameObject root = NewNode("UIItemSplitPanel", null);
            SetStretch(root.GetComponent<RectTransform>(), 0f);

            GameObject dismiss = NewNode("Btn_Dismiss", root.transform);
            SetStretch(dismiss.GetComponent<RectTransform>(), 0f);
            AddButton(dismiss, m_popupDimColor);

            GameObject card = NewNode("Panel_Split", root.transform);
            // 高度仅为初始值, 实际尺寸由 ContentSizeFitter 按三行内容计算
            SetRect(card.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_topLeftAnchor,
                new Vector2(SPLIT_CARD_WIDTH, SPLIT_COUNT_HEIGHT + SPLIT_ROW_HEIGHT * 2 + POPUP_EDGE * 4), Vector2.zero);
            Image cardImage = card.AddComponent<Image>();
            cardImage.color = m_popupBgColor;
            // 卡片需遮挡遮罩射线, 否则拖动滑条会同时触发隐藏
            cardImage.raycastTarget = true;

            VerticalLayoutGroup cardLayout = card.AddComponent<VerticalLayoutGroup>();
            cardLayout.padding = new RectOffset(POPUP_EDGE, POPUP_EDGE, POPUP_EDGE, POPUP_EDGE);
            cardLayout.spacing = POPUP_EDGE;
            cardLayout.childAlignment = TextAnchor.UpperCenter;
            // 布局仅负责竖向排列, 行尺寸由自身参数决定
            cardLayout.childControlWidth = false;
            cardLayout.childControlHeight = false;
            cardLayout.childForceExpandWidth = false;
            cardLayout.childForceExpandHeight = false;

            ContentSizeFitter cardFitter = card.AddComponent<ContentSizeFitter>();
            cardFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            cardFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            GameObject count = NewNode("Txt_Count", card.transform);
            count.GetComponent<RectTransform>().sizeDelta = new Vector2(SPLIT_ROW_WIDTH, SPLIT_COUNT_HEIGHT);
            AddTmp(count, "数量：0/0", 20, TextAlignmentOptions.MidlineLeft, m_textPrimaryColor);

            GameObject sliderRow = NewNode("Layout_Slider", card.transform);
            sliderRow.GetComponent<RectTransform>().sizeDelta = new Vector2(SPLIT_ROW_WIDTH, SPLIT_ROW_HEIGHT);
            AddHBoxLayout(sliderRow);

            GameObject decrease = NewNode("Btn_Decrease", sliderRow.transform);
            SetRect(decrease.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_centerAnchor,
                new Vector2(SPLIT_STEP_BUTTON_SIZE, SPLIT_STEP_BUTTON_SIZE), Vector2.zero);
            AddButton(decrease, m_popupButtonColor);
            GameObject decreaseText = NewNode("Txt_Decrease", decrease.transform);
            SetStretch(decreaseText.GetComponent<RectTransform>(), 0f);
            AddTmp(decreaseText, "-", 20, TextAlignmentOptions.Midline, m_textPrimaryColor);

            GameObject slider = NewNode("Slider_Count", sliderRow.transform);
            SetRect(slider.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_centerAnchor,
                new Vector2(SPLIT_SLIDER_WIDTH, SPLIT_ROW_HEIGHT), Vector2.zero);
            AddSlider(slider, m_sliderBgColor, m_sliderFillColor, m_sliderHandleColor);

            GameObject increase = NewNode("Btn_Increase", sliderRow.transform);
            SetRect(increase.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_centerAnchor,
                new Vector2(SPLIT_STEP_BUTTON_SIZE, SPLIT_STEP_BUTTON_SIZE), Vector2.zero);
            AddButton(increase, m_popupButtonColor);
            GameObject increaseText = NewNode("Txt_Increase", increase.transform);
            SetStretch(increaseText.GetComponent<RectTransform>(), 0f);
            AddTmp(increaseText, "+", 20, TextAlignmentOptions.Midline, m_textPrimaryColor);

            GameObject actionRow = NewNode("Layout_Actions", card.transform);
            actionRow.GetComponent<RectTransform>().sizeDelta = new Vector2(SPLIT_ROW_WIDTH, SPLIT_ROW_HEIGHT);
            AddHBoxLayout(actionRow);

            AddStepButton(actionRow.transform, "Btn_Cancel", "取消", SPLIT_ACTION_BUTTON_WIDTH);
            AddStepButton(actionRow.transform, "Btn_Confirm", "拆分", SPLIT_ACTION_BUTTON_WIDTH);

            AttachScript(root, SPLIT_SCRIPT);
            return root;
        }

        // 战备面板: 背景 + 横向排开的四个容器载体, 载体本体是既有预制体实例
        private static GameObject BuildEquipment(GameObject carrierAsset)
        {
            GameObject root = NewNode("UIEquipmentPanel", null);
            SetStretch(root.GetComponent<RectTransform>(), 0f);

            GameObject background = NewNode("Img_Bg", root.transform);
            SetStretch(background.GetComponent<RectTransform>(), 0f);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0.06f, 0.07f, 0.09f, 0.92f);
            backgroundImage.raycastTarget = true;

            GameObject layout = NewNode("Layout_Containers", root.transform);
            SetStretch(layout.GetComponent<RectTransform>(), PANEL_PADDING);
            // 四张载体同屏在 1920x1080 放不下横排, 这里摆成两列两行
            GridLayoutGroup layoutGroup = layout.AddComponent<GridLayoutGroup>();
            layoutGroup.cellSize = carrierAsset.GetComponent<RectTransform>().sizeDelta;
            layoutGroup.spacing = new Vector2(CONTAINER_SPACING, CONTAINER_SPACING);
            layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layoutGroup.constraintCount = PANEL_COLUMN_COUNT;
            layoutGroup.startCorner = GridLayoutGroup.Corner.UpperLeft;
            // 载体行优先摆放, 与面板 Containers 数组顺序一致
            layoutGroup.startAxis = GridLayoutGroup.Axis.Horizontal;
            layoutGroup.childAlignment = TextAnchor.UpperCenter;

            Type carrierType = FindType(CARRIER_SCRIPT);
            if (carrierType == null)
            {
                Debug.LogError(LOG + "取不到载体脚本类型 " + CARRIER_SCRIPT + "，请先点 0 生成载体预制体");
                return null;
            }

            List<Component> carriers = new List<Component>();
            for (int i = 0; i < m_carrierNames.Length; i++)
            {
                GameObject carrier = PrefabUtility.InstantiatePrefab(carrierAsset) as GameObject;
                carrier.transform.SetParent(layout.transform, false);
                carrier.name = "Carrier_" + m_carrierNames[i];

                Component carrierComponent = carrier.GetComponent(carrierType);
                if (carrierComponent == null)
                {
                    Debug.LogError(LOG + "载体实例缺少组件 " + CARRIER_SCRIPT + "，请先点 0 重新生成载体");
                    return null;
                }

                carriers.Add(carrierComponent);
            }

            Component panelScript = AttachScript(root, EQUIPMENT_SCRIPT);
            if (panelScript != null) AssignCarriers(panelScript, carriers);
            return root;
        }

        // 把四个载体组件写进面板的 Containers 数组, 字段找不到只提示不抛
        private static void AssignCarriers(Component panelScript, List<Component> carriers)
        {
            SerializedObject serialized = new SerializedObject(panelScript);
            SerializedProperty property = serialized.FindProperty("Containers");
            if (property == null)
            {
                Debug.LogWarning(LOG + "UIEquipmentPanel 找不到 [SerializeField] UIInventoryCarrier[] Containers，请在 Inspector 手动拖引用");
                return;
            }

            property.arraySize = carriers.Count;
            for (int i = 0; i < carriers.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = carriers[i];

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 把生成的面板登记进 Addressables 的 UI 组, 地址与 UIDefines 常量保持一致
        private static void RegisterAddressable(string assetPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError(LOG + "Addressables 尚未初始化，请手动把 " + assetPath + " 拖入 Group \"UI\" 且 Address 填 " + address);
                return;
            }

            AddressableAssetGroup group = settings.DefaultGroup;
            foreach (AddressableAssetGroup candidate in settings.groups)
            {
                if (candidate != null && candidate.Name == ADDRESSABLE_GROUP_NAME)
                    group = candidate;
            }

            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError(LOG + "预制体还没导入，登记失败: " + assetPath);
                return;
            }

            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, true);
            entry.address = address;
            settings.SetDirty(AddressableAssetSettings.ModificationEvent.EntryModified, entry, true, true);
            Debug.Log(LOG + "已登记 Addressables: " + address + " -> " + assetPath + "，Group " + group.Name);
        }
        // 删除被命名规范淘汰的空壳面板, 已确认没有被场景/预制体/Addressables 引用
        private static void DeleteLegacy()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(LEGACY_PATH) == null) return;

            if (AssetDatabase.DeleteAsset(LEGACY_PATH))
                Debug.Log(LOG + "已删除空壳预制体 " + LEGACY_PATH);
        }

        // 格子模板: 整格一张底板, 点击与高亮都由 UIInventorySlot 自己管
        private static GameObject BuildSlot()
        {
            GameObject node = NewNode("UIInventorySlot", null);
            SetRect(node.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(CELL_SIZE, CELL_SIZE), Vector2.zero);

            Image image = node.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.16f);
            image.raycastTarget = true;

            AttachScript(node, SLOT_SCRIPT);
            return node;
        }

        // 物品模板: 底板 + 搜索阴影 + 图标 + 堆叠数, 尺寸由 View 按占地换算
        private static GameObject BuildItem()
        {
            GameObject node = NewNode("UIInventoryItem", null);
            SetRect(node.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(CELL_SIZE, CELL_SIZE), Vector2.zero);

            Image background = node.AddComponent<Image>();
            // 模板默认底色, 运行时按稀有度覆盖
            background.color = m_itemDefaultColor;
            // 物品自身接收点击与拖拽, 拖影期间再由 UIInventoryItem 关掉射线
            background.raycastTarget = true;

            GameObject silhouette = NewNode("Img_Silhouette", node.transform);
            SetStretch(silhouette.GetComponent<RectTransform>(), SILHOUETTE_INSET);
            Image silhouetteImage = silhouette.AddComponent<Image>();
            silhouetteImage.material =
                AssetDatabase.LoadAssetAtPath<Material>(SILHOUETTE_MATERIAL_PATH);
            silhouetteImage.color = Color.white;
            silhouetteImage.raycastTarget = false;
            silhouetteImage.enabled = false;

            if (silhouetteImage.material == null)
                QLog.Error(LOG + "搜索阴影材质加载失败: " + SILHOUETTE_MATERIAL_PATH);

            GameObject icon = NewNode("Img_Icon", node.transform);
            SetStretch(icon.GetComponent<RectTransform>(), 6f);
            Image iconImage = icon.AddComponent<Image>();
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            // 无 Sprite 的 Image 会画成一块纯白, 物品图标改由 View 绑到 Sprite 时才启用
            iconImage.enabled = false;

            GameObject count = NewNode("Txt_Count", node.transform);
            SetRect(count.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(56f, 20f), new Vector2(-6f, 6f));
            AddTmp(count, "", 16, TextAlignmentOptions.BottomRight, new Color(1f, 0.92f, 0.65f, 1f));

            // 选中态描边框: 只画四条边, 不碰物品底色
            AddSelectedFrame(node.transform);

            AttachScript(node, ITEM_SCRIPT);
            return node;
        }

        // 载体面板: 表头 + 网格层 + 物品层, 尺寸按默认 8x5 落盘, 运行时由 VO 重算
        private static GameObject BuildCarrier()
        {
            int gridWidth = DEFAULT_COLUMNS * (CELL_SIZE + CELL_SPACING) - CELL_SPACING;
            int gridHeight = DEFAULT_ROWS * (CELL_SIZE + CELL_SPACING) - CELL_SPACING;
            Vector2 gridPosition = new Vector2(PADDING, -(PADDING + HEADER_HEIGHT + GAP));

            GameObject node = NewNode("UIInventoryCarrier", null);
            SetRect(node.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                new Vector2(gridWidth + PADDING * 2, PADDING + HEADER_HEIGHT + GAP + gridHeight + GAP + HINT_HEIGHT + PADDING),
                Vector2.zero);

            GameObject bg = NewNode("Img_Bg", node.transform);
            SetStretch(bg.GetComponent<RectTransform>(), 0f);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.086f, 0.098f, 0.118f, 0.94f);
            bgImage.raycastTarget = true;

            GameObject header = NewNode("Layout_Header", node.transform);
            SetRect(header.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(gridWidth, HEADER_HEIGHT), new Vector2(PADDING, -PADDING));
            HorizontalLayoutGroup headerLayout = header.AddComponent<HorizontalLayoutGroup>();
            headerLayout.spacing = 8;
            headerLayout.childAlignment = TextAnchor.MiddleLeft;
            headerLayout.childControlWidth = false;
            headerLayout.childControlHeight = false;
            headerLayout.childForceExpandWidth = false;
            headerLayout.childForceExpandHeight = false;

            GameObject nameNode = NewNode("Txt_Name", header.transform);
            nameNode.GetComponent<RectTransform>().sizeDelta = new Vector2(150f, HEADER_HEIGHT);
            AddTmp(nameNode, "背包", 20, TextAlignmentOptions.MidlineLeft, new Color(0.86f, 0.9f, 0.96f, 1f));

            GameObject capacityNode = NewNode("Txt_Capacity", header.transform);
            capacityNode.GetComponent<RectTransform>().sizeDelta = new Vector2(90f, HEADER_HEIGHT);
            AddTmp(capacityNode, "0/40", 20, TextAlignmentOptions.MidlineLeft, new Color(0.62f, 0.68f, 0.75f, 1f));

            GameObject infoButton = NewNode("Btn_Info", header.transform);
            infoButton.GetComponent<RectTransform>().sizeDelta = new Vector2(26f, 26f);
            AddButton(infoButton, new Color(1f, 1f, 1f, 0.12f));
            GameObject infoText = NewNode("Txt_Info", infoButton.transform);
            SetStretch(infoText.GetComponent<RectTransform>(), 0f);
            AddTmp(infoText, "?", 16, TextAlignmentOptions.Midline, new Color(0.8f, 0.85f, 0.9f, 1f));

            GameObject grid = NewNode("Layout_Grid", node.transform);
            SetRect(grid.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(gridWidth, gridHeight), gridPosition);
            GridLayoutGroup gridLayout = grid.AddComponent<GridLayoutGroup>();
            gridLayout.cellSize = new Vector2(CELL_SIZE, CELL_SIZE);
            gridLayout.spacing = new Vector2(CELL_SPACING, CELL_SPACING);
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = DEFAULT_COLUMNS;
            gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            // 格子行优先填充, 与视图模型 index -> (x, y) 的行优先映射一致
            gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            gridLayout.childAlignment = TextAnchor.UpperLeft;

            // 物品层必须排在网格层之后, 同尺寸同位置才能盖在格子上
            GameObject itemLayer = NewNode("Layout_Item", node.transform);
            SetRect(itemLayer.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(gridWidth, gridHeight), gridPosition);

            GameObject hint = NewNode("Txt_Hint", node.transform);
            SetRect(hint.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(gridWidth - ROTATE_WIDTH, HINT_HEIGHT), new Vector2(PADDING, PADDING));
            AddTmp(hint, "", 18, TextAlignmentOptions.MidlineLeft, new Color(0.72f, 0.8f, 0.9f, 1f));

            GameObject rotate = NewNode("Btn_Rotate", node.transform);
            SetRect(rotate.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f),
                new Vector2(1f, 0f), new Vector2(ROTATE_WIDTH, 32f), new Vector2(-PADDING, PADDING));
            AddButton(rotate, new Color(1f, 1f, 1f, 0.12f));
            GameObject rotateText = NewNode("Txt_Rotate", rotate.transform);
            SetStretch(rotateText.GetComponent<RectTransform>(), 0f);
            AddTmp(rotateText, "R", 18, TextAlignmentOptions.Midline, new Color(0.9f, 0.94f, 1f, 1f));

            AttachScript(node, CARRIER_SCRIPT);
            BindTemplateFields(node);
            return node;
        }

        // 模板引用走 [SerializeField], 字段名固定为 SlotPrefab 与 ItemPrefab
        private static void BindTemplateFields(GameObject root)
        {
            SerializedObject serialized = new SerializedObject(root);
            AssignPrefab(serialized, "SlotPrefab", SLOT_PATH);
            AssignPrefab(serialized, "ItemPrefab", ITEM_PATH);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        // 字段不存在只提示不报错, 因为脚本可能还没敲
        private static void AssignPrefab(SerializedObject serialized, string fieldName, string path)
        {
            SerializedProperty property = serialized.FindProperty(fieldName);
            if (property == null)
            {
                Debug.LogWarning(LOG + "字段未找到 " + fieldName + "，请在 UIInventoryCarrier 声明 [SerializeField] GameObject " + fieldName);
                return;
            }

            property.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }

        // 按完整类型名反射挂载脚本, 取不到时记录缺失, 不阻塞节点生成
        private static Component AttachScript(GameObject node, string fullTypeName)
        {
            Type type = FindType(fullTypeName);
            if (type == null)
            {
                m_missingScripts.Add(fullTypeName);
                return null;
            }

            return node.AddComponent(type);
        }

        private static Type FindType(string fullName)
        {
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null) return type;
            }

            return null;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
            if (!success || saved == null)
                Debug.LogError(LOG + "预制体保存失败 " + path);
            else
                Debug.Log(LOG + "已生成 " + path);

            UnityEngine.Object.DestroyImmediate(root);
        }

        private static void Report()
        {
            Debug.Log(LOG + "三张预制体已生成于 " + PREFAB_DIR + "，重跑会整体覆盖手工改动");

            if (m_missingScripts.Count > 0)
                Debug.LogWarning(LOG + "以下脚本还没敲，本次未挂上，敲完再点一次本菜单即可: " + string.Join(", ", m_missingScripts));

            Debug.Log(LOG + "Addressables 手动登记: 把 UIInventoryCarrier.prefab 拖入 Group \"UI\" 且 Address 填 UIInventoryCarrier，两张模板不登记");
        }

        // ==================== 节点工具 ====================
        private static GameObject NewNode(string name, Transform parent)
        {
            GameObject node = new GameObject(name, typeof(RectTransform));
            if (parent != null) node.transform.SetParent(parent, false);
            return node;
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
            Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPosition;
        }

        // 铺满父节点, inset 为四边内缩量
        private static void SetStretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static TextMeshProUGUI AddTmp(GameObject node, string text, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            TextMeshProUGUI tmp = node.AddComponent<TextMeshProUGUI>();
            tmp.font = ResolveFont();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = color;
            tmp.enableWordWrapping = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            tmp.raycastTarget = false;
            return tmp;
        }

        // 工程默认 TMP 字体是 LiberationSans, 不含中文字形, 表头与提示的中文会画不出来, 这里锁定中文 SDF 字体
        private static TMP_FontAsset ResolveFont()
        {
            TMP_FontAsset cjkFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CJK_FONT_PATH);
            if (cjkFont != null) return cjkFont;

            Debug.LogWarning(LOG + "未找到中文字体资产 " + CJK_FONT_PATH + ", 回退到 TMP 默认字体");
            return TMP_Settings.defaultFontAsset;
        }

        // Button 需要一张可射线检测的底板, 统一用纯色 Image
        private static Button AddButton(GameObject node, Color backgroundColor)
        {
            Image image = node.AddComponent<Image>();
            image.color = backgroundColor;
            image.raycastTarget = true;
            return node.AddComponent<Button>();
        }

        // 横向排布行: 子节点尺寸由自身参数决定, 行内只负责等距摆放
        private static void AddHBoxLayout(GameObject node)
        {
            HorizontalLayoutGroup group = node.AddComponent<HorizontalLayoutGroup>();
            group.spacing = POPUP_EDGE;
            group.childAlignment = TextAnchor.MiddleCenter;
            group.childControlWidth = false;
            group.childControlHeight = false;
            group.childForceExpandWidth = false;
            group.childForceExpandHeight = false;
        }

        // 拆分弹窗的按钮项: 定宽底板加居中文字, 子文字不接收射线
        private static void AddStepButton(Transform parent, string name, string label, int width)
        {
            GameObject button = NewNode(name, parent);
            SetRect(button.GetComponent<RectTransform>(), m_centerAnchor, m_centerAnchor, m_centerAnchor,
                new Vector2(width, SPLIT_ROW_HEIGHT), Vector2.zero);
            AddButton(button, m_popupButtonColor);

            GameObject labelNode = NewNode(name.Replace("Btn_", "Txt_"), button.transform);
            SetStretch(labelNode.GetComponent<RectTransform>(), 0f);
            AddTmp(labelNode, label, 20, TextAlignmentOptions.Midline, m_textPrimaryColor);
        }

        // 滑条: 按 Unity 默认结构补出轨道、填充区与手柄, 轨道与手柄接收射线以便点击与拖动
        private static Slider AddSlider(GameObject node, Color trackColor, Color fillColor, Color handleColor)
        {
            Vector2 trackMin = new Vector2(0f, 0.325f);
            Vector2 trackMax = new Vector2(1f, 0.675f);
            Vector2 stretchMin = Vector2.zero;
            Vector2 stretchMax = Vector2.one;

            GameObject track = NewNode("Background", node.transform);
            SetRect(track.GetComponent<RectTransform>(), trackMin, trackMax, m_centerAnchor, Vector2.zero, Vector2.zero);
            Image trackImage = track.AddComponent<Image>();
            trackImage.color = trackColor;
            trackImage.raycastTarget = true;

            // 填充区左右各内缩, 与 Unity 默认滑条一致, 使填充末端略微压在手柄之下
            GameObject fillArea = NewNode("Fill Area", node.transform);
            SetRect(fillArea.GetComponent<RectTransform>(), trackMin, trackMax, m_centerAnchor,
                new Vector2(-SLIDER_FILL_INSET, 0f), Vector2.zero);
            GameObject fill = NewNode("Fill", fillArea.transform);
            SetRect(fill.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            GameObject handleArea = NewNode("Handle Slide Area", node.transform);
            SetRect(handleArea.GetComponent<RectTransform>(), stretchMin, stretchMax, m_centerAnchor,
                new Vector2(-SLIDER_HANDLE_WIDTH, 0f), Vector2.zero);
            GameObject handle = NewNode("Handle", handleArea.transform);
            SetRect(handle.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), m_centerAnchor,
                new Vector2(SLIDER_HANDLE_WIDTH, 0f), Vector2.zero);
            Image handleImage = handle.AddComponent<Image>();
            handleImage.color = handleColor;
            handleImage.raycastTarget = true;

            Slider slider = node.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 0.5f;
            slider.fillRect = fill.GetComponent<RectTransform>();
            slider.handleRect = handle.GetComponent<RectTransform>();
            slider.targetGraphic = handleImage;
            return slider;
        }

        // 选中描边框: 一个父节点带四条边, 未选中时整体隐藏
        private static void AddSelectedFrame(Transform parent)
        {
            GameObject frame = NewNode(SELECTED_FRAME_NAME, parent);
            SetStretch(frame.GetComponent<RectTransform>(), 0f);

            AddBorderBar(frame.transform, "Img_Bar_Top", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, SELECTED_BORDER_WIDTH), Vector2.zero);
            AddBorderBar(frame.transform, "Img_Bar_Bottom", new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0.5f, 0f), new Vector2(0f, SELECTED_BORDER_WIDTH), Vector2.zero);
            AddBorderBar(frame.transform, "Img_Bar_Left", new Vector2(0f, 0f), new Vector2(0f, 1f),
                new Vector2(0f, 0.5f), new Vector2(SELECTED_BORDER_WIDTH, 0f), Vector2.zero);
            AddBorderBar(frame.transform, "Img_Bar_Right", new Vector2(1f, 0f), new Vector2(1f, 1f),
                new Vector2(1f, 0.5f), new Vector2(SELECTED_BORDER_WIDTH, 0f), Vector2.zero);

            frame.SetActive(false);
        }

        // 生成一条描边边条, 射线留给格子
        private static void AddBorderBar(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPosition)
        {
            GameObject bar = NewNode(name, parent);
            SetRect(bar.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition);
            Image image = bar.AddComponent<Image>();
            image.color = m_selectedBorderColor;
            image.raycastTarget = false;
        }
    }
}
