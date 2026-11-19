/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品悬停高亮测试, 覆盖多格、选中、搜索、拖拽与禁用生命周期
 * │  类    名: QInventoryItemHoverTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Reflection;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using TMPro;
using UI.Inventory;
using UI.Inventory.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tests.Inventory
{
    /// <summary>
    /// 物品悬停高亮测试：复用现有选中边框, 不改变选中业务与搜索隔离
    /// </summary>
    public class QInventoryItemHoverTests
    {
        private const int NO_SELECTION = -1;
        private const string ITEM_PREFAB_PATH = "Assets/Prefabs/UI/Inventory/UIInventoryItem.prefab";
        private const string CARRIER_PREFAB_PATH = "Assets/Prefabs/UI/Inventory/UIInventoryCarrier.prefab";

        private static readonly Vector2 m_cellSize = new Vector2(80f, 80f);
        private static readonly Vector2 m_cellSpacing = new Vector2(4f, 4f);

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private InventoryViewModel m_viewModel;
        private int m_containerId;
        private GameObject m_itemObject;
        private UIInventoryItem m_itemView;
        private GameObject m_selectedFrame;

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId, new InventoryContainerSpec(4, 4, float.MaxValue));

            m_viewModel = new InventoryViewModel(m_model, m_containerId);
            m_viewModel.OnBind();

            m_itemObject = new GameObject("UIInventoryItem", typeof(RectTransform));
            m_itemObject.AddComponent<Image>();
            CreateImage("Img_Background");
            CreateImage("Img_Icon");
            CreateImage("Img_Silhouette");
            CreateImage("Img_Search");
            CreateText("Txt_Count");

            GameObject selectedObject = new GameObject("Img_Selected", typeof(RectTransform));
            selectedObject.transform.SetParent(m_itemObject.transform, false);
            RectTransform selectedRect = selectedObject.GetComponent<RectTransform>();
            selectedRect.anchorMin = Vector2.zero;
            selectedRect.anchorMax = Vector2.one;
            selectedRect.pivot = new Vector2(0.5f, 0.5f);
            selectedRect.anchoredPosition = Vector2.zero;
            selectedRect.sizeDelta = Vector2.zero;
            m_selectedFrame = selectedObject;

            m_itemView = m_itemObject.AddComponent<UIInventoryItem>();
            InvokePrivate(m_itemView, "Awake");
            SetPrivateField(m_itemView, "m_viewModel", m_viewModel);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_itemObject);

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        // 鼠标进入多格物品时必须点亮完整占格边框
        [Test]
        public void OnPointerEnter_ShowsSelectionFrameForWholeFootprint()
        {
            SetupItem(new Vector2Int(2, 2));

            m_itemView.OnPointerEnter(CreatePointerEvent());

            RectTransform itemRect = m_itemObject.GetComponent<RectTransform>();
            RectTransform selectedRect = m_selectedFrame.GetComponent<RectTransform>();
            Assert.IsTrue(m_selectedFrame.activeSelf, "鼠标进入必须显示整件物品的高亮边框");
            Assert.AreEqual(new Vector2(164f, 164f), itemRect.rect.size,
                "2x2 物品根节点必须覆盖完整占格");
            Assert.AreEqual(Vector2.zero, selectedRect.anchorMin,
                "高亮节点必须从物品左上角开始覆盖");
            Assert.AreEqual(Vector2.one, selectedRect.anchorMax,
                "高亮节点必须覆盖到物品右下角");
        }

        // 未选中物品移出鼠标后必须收起悬停边框
        [Test]
        public void OnPointerExit_ClearsHoverWhenItemIsNotSelected()
        {
            SetupItem(Vector2Int.one);
            m_itemView.OnPointerEnter(CreatePointerEvent());

            m_itemView.OnPointerExit(CreatePointerEvent());

            Assert.IsFalse(m_selectedFrame.activeSelf, "未选中物品离开鼠标后必须恢复无高亮");
        }

        // 已选中物品移出鼠标后仍要保留选中边框
        [Test]
        public void OnPointerExit_KeepsFrameWhenItemRemainsSelected()
        {
            SetupItem(Vector2Int.one);
            m_itemView.OnPointerEnter(CreatePointerEvent());
            m_viewModel.OpenMenuAt(Vector2Int.zero);
            Assert.AreNotEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);

            m_itemView.OnPointerExit(CreatePointerEvent());

            Assert.IsTrue(m_selectedFrame.activeSelf, "已选中物品离开鼠标后仍要保留选中边框");
        }

        // 搜索态下鼠标进入不能提前暴露物品边框
        [Test]
        public void OnPointerEnter_WhileSearching_DoesNotRevealHoverFrame()
        {
            SetupItem(Vector2Int.one);
            m_viewModel.OpenMenuAt(Vector2Int.zero);
            m_itemView.SetSearching(true);

            m_itemView.OnPointerEnter(CreatePointerEvent());

            Assert.IsFalse(m_selectedFrame.activeSelf, "搜索态不能因为鼠标经过而暴露物品");
        }

        // 悬停中的物品切到搜索态后必须清除悬停, 搜索结束也不能重新亮起
        [Test]
        public void SetSearching_AfterHover_ClearsHoverState()
        {
            SetupItem(Vector2Int.one);
            m_itemView.OnPointerEnter(CreatePointerEvent());

            m_itemView.SetSearching(true);
            m_itemView.SetSearching(false);

            Assert.IsFalse(m_selectedFrame.activeSelf, "搜索结束后不能残留搜索前的悬停高亮");
        }

        // 开始拖拽时必须立即清除悬停边框
        [Test]
        public void OnBeginDrag_AfterHover_ClearsHoverFrame()
        {
            SetupItem(Vector2Int.one);
            m_itemView.OnPointerEnter(CreatePointerEvent());

            PointerEventData eventData = CreatePointerEvent();
            eventData.button = PointerEventData.InputButton.Left;
            m_itemView.OnBeginDrag(eventData);

            Assert.IsFalse(m_selectedFrame.activeSelf, "开始拖拽后不能残留悬停边框");
        }

        // 物品禁用或面板关闭时必须清除悬停边框
        [Test]
        public void OnDisable_AfterHover_ClearsHoverFrame()
        {
            SetupItem(Vector2Int.one);
            m_itemView.OnPointerEnter(CreatePointerEvent());

            InvokePrivate(m_itemView, "OnDisable");

            Assert.IsFalse(m_selectedFrame.activeSelf, "对象禁用或面板关闭后不能残留悬停边框");
        }

        // 物品预制体高亮节点必须覆盖完整占格, 且载体继续复用同一个物品预制体
        [Test]
        public void ItemPrefab_SelectedFrameCoversTheFullItemAndCarrierUsesIt()
        {
            GameObject itemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ITEM_PREFAB_PATH);
            Assert.IsNotNull(itemPrefab, "物品预制体加载失败");
            Transform frame = itemPrefab.transform.Find("Img_Selected");
            Assert.IsNotNull(frame, "物品预制体必须保留 Img_Selected");

            RectTransform frameRect = frame.GetComponent<RectTransform>();
            Assert.AreEqual(Vector2.zero, frameRect.anchorMin);
            Assert.AreEqual(Vector2.one, frameRect.anchorMax);
            Assert.AreEqual(Vector2.zero, frameRect.sizeDelta);
            Assert.IsFalse(frame.gameObject.activeSelf, "预制体默认不能显示高亮");

            GameObject carrierPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CARRIER_PREFAB_PATH);
            Assert.IsNotNull(carrierPrefab, "载体预制体加载失败");
            UIInventoryCarrier carrier = carrierPrefab.GetComponent<UIInventoryCarrier>();
            Assert.IsNotNull(carrier, "载体预制体必须挂 UIInventoryCarrier");

            FieldInfo itemPrefabField = typeof(UIInventoryCarrier).GetField("ItemPrefab",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(itemPrefabField, "UIInventoryCarrier 缺少 ItemPrefab 字段");
            GameObject carrierItemPrefab = (GameObject)itemPrefabField.GetValue(carrier);
            Assert.AreEqual(ITEM_PREFAB_PATH, AssetDatabase.GetAssetPath(carrierItemPrefab),
                "普通背包和动态补给箱必须共用同一个 UIInventoryItem 预制体");
        }

        // 往测试容器放入指定占格的物品并刷新物品视图
        private void SetupItem(Vector2Int footprint)
        {
            var config = m_kit.CreateItem(footprint.x, footprint.y);
            Assert.AreEqual(InventoryMoveResult.Success,
                m_model.TryAddItem(m_containerId, config.ItemId, 1, Vector2Int.zero));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, Vector2Int.zero, out int instanceId));

            InventoryContainerVO container = m_model.GetContainerVO(m_containerId);
            Assert.IsTrue(container.TryGetItem(instanceId, out InventoryItemVO item));
            m_itemView.Setup(item, container, m_cellSize, m_cellSpacing);
        }

        // 造一个最小指针事件, 悬停路径不依赖真实射线命中
        private static PointerEventData CreatePointerEvent()
        {
            return new PointerEventData(EventSystem.current)
            {
                button = PointerEventData.InputButton.Left,
                position = Vector2.zero,
            };
        }

        // 建一个图像子节点
        private Image CreateImage(string objectName)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(m_itemObject.transform, false);
            return child.AddComponent<Image>();
        }

        // 建一个文本子节点
        private void CreateText(string objectName)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(m_itemObject.transform, false);
            child.AddComponent<TextMeshProUGUI>();
        }

        // 调用物品视图的私有生命周期或表现方法
        private static object InvokePrivate(object target, string methodName, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "私有方法缺失: " + methodName);
            return method.Invoke(target, arguments);
        }

        // 注入物品视图的运行时依赖
        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "私有字段缺失: " + fieldName);
            field.SetValue(target, value);
        }
    }
}
