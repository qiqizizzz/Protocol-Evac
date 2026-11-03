/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包载体预制体生成器，一键产出载体与格子、物品三张预制体
 * │  类    名: InventoryPrefabBuilder.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System;
using System.Reflection;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

        private const string CARRIER_SCRIPT = "UI.Inventory.View.UIInventoryCarrier";
        private const string SLOT_SCRIPT = "UI.Inventory.View.UIInventorySlot";
        private const string ITEM_SCRIPT = "UI.Inventory.View.UIInventoryItem";

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

        private static readonly Color m_itemDefaultColor = new Color(0.16f, 0.18f, 0.21f, 0.95f);
        private static readonly Color m_selectedBorderColor = new Color(0.31f, 0.76f, 1f, 1f);
        private const string CJK_FONT_PATH = "Assets/Fonts/miSans/MiSans-Regular SDF.asset";

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

        // ==================== Private Function ====================
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

        // 物品模板: 底板 + 图标 + 堆叠数, 尺寸由 View 按占地换算
        private static GameObject BuildItem()
        {
            GameObject node = NewNode("UIInventoryItem", null);
            SetRect(node.GetComponent<RectTransform>(), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(CELL_SIZE, CELL_SIZE), Vector2.zero);

            Image background = node.AddComponent<Image>();
            // 模板默认底色, 运行时由 InventoryItemVO.BaseColor 覆盖
            background.color = m_itemDefaultColor;
            // 射线留给底下的格子, 物品只是载荷, 点击统一由 UIInventorySlot 接收
            background.raycastTarget = false;

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
            gridLayout.startAxis = GridLayoutGroup.Axis.Vertical;
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

        // 按完整类型名反射挂脚本, 取不到就先记账, 不阻塞出树
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
