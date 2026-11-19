/*
* ┌──────────────────────────────────┐
* │  描    述: 物品浮层测试, 覆盖左键详情与右键选中的信号编排和两个浮层视图模型的数据填充
* │  类    名: QInventoryPopupTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using Framework.QTower.MVVM.ViewModel;
using Module.Inventory.Core.Config;
using Module.Inventory.Core.Config.Type;
using Module.Inventory.Core.Data;
using Module.Inventory.Core.Rule.Type;
using NUnit.Framework;
using TMPro;
using UI.Inventory;
using UI.Inventory.View;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.Inventory
{
    /// <summary>
    /// 左键详情与右键菜单测试：不实例化 View, 直接验证 InventoryViewModel 的浮层信号与浮层视图模型行为
    /// 左键仅打开详情不建立选中态, 右键仅建立选中态不打开详情, 两条链路相互独立
    /// </summary>
    public class QInventoryPopupTests
    {
        private const int NO_SELECTION = -1;
        private const string CONTAINER_NAME = "测试容器";
        private const string DETAIL_PREFAB_PATH = "Assets/Prefabs/UI/Inventory/UIItemDetailPanel.prefab";
        private const string CONTAINER_LAYOUT_PREFAB_PATH =
            "Assets/Prefabs/UI/Inventory/UIItemContainerLayoutPanel.prefab";
        private const int FOOTPRINT_CELL_COUNT = 24;

        private QInventoryTestKit m_kit;
        private InventoryModel m_model;
        private InventoryViewModel m_viewModel;
        private int m_containerId;

        private int m_detailCount;
        private int m_menuCount;
        private int m_closeCount;
        private IDisposable m_detailSubscription;
        private IDisposable m_menuSubscription;
        private IDisposable m_closeSubscription;

        // 记录用例内创建的浮层视图模型, 拆除时统一回收
        private readonly List<ViewModelBase> m_popups = new List<ViewModelBase>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_model = new InventoryModel(m_kit.CreateInventoryConfig());
            m_model.OnRegister();

            m_containerId = m_kit.NewContainerId();
            m_model.RegisterContainer(m_containerId,
                new InventoryContainerSpec(4, 4, float.MaxValue, null, CONTAINER_NAME));

            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;
            m_popups.Clear();

            m_viewModel = new InventoryViewModel(m_model, m_containerId);
            m_detailSubscription = m_viewModel.DetailRequested.Subscribe(() => m_detailCount++);
            m_menuSubscription = m_viewModel.MenuRequested.Subscribe(() => m_menuCount++);
            m_closeSubscription = m_viewModel.PopupClosed.Subscribe(() => m_closeCount++);
            m_viewModel.OnBind();
        }

        [TearDown]
        public void TearDown()
        {
            // 与 ViewBase 拆除顺序一致：先解绑停止广播, 再释放, 最后回收响应式成员
            m_detailSubscription.Dispose();
            m_menuSubscription.Dispose();
            m_closeSubscription.Dispose();

            for (int i = m_popups.Count - 1; i >= 0; i--)
                DisposeViewModel(m_popups[i]);
            m_popups.Clear();

            m_viewModel.OnUnbind();
            m_viewModel.OnDispose();
            m_viewModel.Dispose();
            m_model.OnUnregister();
            m_kit.Dispose();
        }

        #region 左键详情编排
        [Test]
        public void InteractAt_OnItem_FiresDetailRequestWithoutSelecting()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));

            m_viewModel.InteractAt(new Vector2Int(1, 2));

            Assert.AreEqual(1, m_detailCount);
            Assert.AreEqual(0, m_menuCount);
            Assert.AreEqual(0, m_closeCount, "左键开详情不该顺带收起浮层");
            Assert.AreEqual(instanceId, m_viewModel.DetailInstanceId.Value);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value, "左键不该建立选中态");
        }

        [Test]
        public void InteractAt_OnTheSameItemAgain_ReopensDetailWithoutClosing()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));
            m_viewModel.InteractAt(new Vector2Int(1, 2));

            m_viewModel.InteractAt(new Vector2Int(2, 2));

            Assert.AreEqual(2, m_detailCount, "再点一次仍重开详情, 由视图按物品完整占格重新定位");
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(instanceId, m_viewModel.DetailInstanceId.Value);
        }

        [Test]
        public void InteractAt_OnAnotherItem_ReopensDetailWithoutClosingFirst()
        {
            int first = AddItem(1, 1, new Vector2Int(0, 0));
            int second = AddItem(1, 1, new Vector2Int(3, 3));

            m_viewModel.InteractAt(new Vector2Int(0, 0));
            m_viewModel.InteractAt(new Vector2Int(3, 3));

            Assert.AreEqual(2, m_detailCount);
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(second, m_viewModel.DetailInstanceId.Value);
            Assert.AreNotEqual(first, m_viewModel.DetailInstanceId.Value);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void InteractAt_OnEmptyCellWithoutSelection_FiresPopupClosedOnly()
        {
            m_viewModel.InteractAt(new Vector2Int(2, 2));

            Assert.AreEqual(0, m_detailCount);
            Assert.AreEqual(1, m_closeCount);
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void InteractAt_OnEmptyCell_ClosesPopupAndDropsContextMenuSelection()
        {
            int instanceId = AddItem(1, 1, new Vector2Int(0, 0));
            m_viewModel.OpenMenuAt(new Vector2Int(0, 0));
            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;

            m_viewModel.InteractAt(new Vector2Int(3, 3));

            Assert.AreEqual(0, m_detailCount, "左键点空格子不该开详情");
            Assert.AreEqual(1, m_closeCount, "点空白处要收起残留浮层");
            Assert.AreEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value, "右键留下的选中高亮要一起擦掉");
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, new Vector2Int(0, 0), out int stillThereId));
            Assert.AreEqual(instanceId, stillThereId, "左键空格子不再负责放置");
        }

        [Test]
        public void BeginDragAt_OnItem_FiresPopupClosed()
        {
            AddItem(2, 1, new Vector2Int(1, 2));

            m_viewModel.BeginDragAt(new Vector2Int(1, 2));

            Assert.IsTrue(m_viewModel.IsDragging);
            Assert.AreEqual(0, m_detailCount, "拖起来不该开详情");
            Assert.AreEqual(1, m_closeCount);
        }
        #endregion

        #region 右键菜单编排
        [Test]
        public void OpenMenuAt_OnItem_FiresMenuRequestWithoutOpeningDetail()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));

            m_viewModel.OpenMenuAt(new Vector2Int(1, 2));

            Assert.AreEqual(1, m_menuCount);
            Assert.AreEqual(0, m_detailCount, "右键不该顺手开详情");
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value);
        }

        [Test]
        public void OpenMenuAt_OnTheSelectedItem_KeepsSelectionInsteadOfTogglingOff()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(1, 2));
            m_viewModel.OpenMenuAt(new Vector2Int(1, 2));
            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;

            m_viewModel.OpenMenuAt(new Vector2Int(2, 2));

            Assert.AreEqual(1, m_menuCount);
            Assert.AreEqual(instanceId, m_viewModel.SelectedInstanceId.Value, "右键已选中的物品不能把它反选掉");
            Assert.AreEqual(0, m_closeCount);
            Assert.AreEqual(0, m_detailCount, "右键不该顺手开详情");
        }

        [Test]
        public void OpenMenuAt_OnEmptyCell_IsFullySilent()
        {
            AddItem(1, 1, new Vector2Int(0, 0));
            m_viewModel.OpenMenuAt(new Vector2Int(0, 0));
            m_detailCount = 0;
            m_menuCount = 0;
            m_closeCount = 0;

            m_viewModel.OpenMenuAt(new Vector2Int(3, 3));

            Assert.AreEqual(0, m_menuCount);
            Assert.AreEqual(0, m_detailCount);
            Assert.AreEqual(0, m_closeCount);
            Assert.AreNotEqual(NO_SELECTION, m_viewModel.SelectedInstanceId.Value, "右键空白处不该动选中态");
        }

        [Test]
        public void OpenMenuAt_OnAnotherItem_SwitchesSelectionToTheRightClickedOne()
        {
            AddItem(1, 1, new Vector2Int(0, 0));
            int rightClicked = AddItem(1, 1, new Vector2Int(3, 3));
            m_viewModel.OpenMenuAt(new Vector2Int(0, 0));
            m_menuCount = 0;

            m_viewModel.OpenMenuAt(new Vector2Int(3, 3));

            Assert.AreEqual(rightClicked, m_viewModel.SelectedInstanceId.Value);
            Assert.AreEqual(1, m_menuCount);
            Assert.AreEqual(0, m_closeCount, "换选中走的是直接置选中, 不是反选");
        }
        #endregion

        #region 详情视图模型
        [Test]
        public void Detail_Open_FillsStructuredContentFromTheModel()
        {
            InventoryItemDetailEntry[] entries =
            {
                new InventoryItemDetailEntry("恢复量", "45"),
            };
            int itemId = m_kit.CreateItem(2, 1, true, unitValue: 2025921,
                unitWeight: 1.5f,
                rarity: InventoryItemRarity.Rare, itemType: InventoryItemType.Medical,
                description: "用于快速处理失血伤势", detailEntries: entries).ItemId;
            int instanceId = AddItem(itemId, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();

            detail.Open(m_containerId, instanceId);

            Assert.IsNotEmpty(detail.Title.Value);
            Assert.AreEqual("医疗物资", detail.Tags.Value);
            Assert.AreEqual("2,025,921", detail.Value.Value);
            Assert.AreEqual("1.5KG", ReadDetailString(detail, "WeightLabel"));
            AssertFootprint(detail.FootprintPreview.Value, 2, 1, 2);
            Assert.IsFalse(detail.CanShowContainerLayout.Value);
            Assert.AreEqual("用于快速处理失血伤势", detail.Description.Value);
            Assert.IsTrue(detail.HasContent.Value);
            Assert.AreNotEqual(Color.white, detail.AccentColor.Value);
            Assert.IsNull(detail.Icon.Value);
        }

        [Test]
        public void Detail_ForValuableItem_KeepsDescriptionAsTheOnlyBodyContent()
        {
            InventoryItemDetailEntry[] misplacedEntries =
            {
                new InventoryItemDetailEntry("收藏编号", "Q9-2025-0921"),
            };
            int itemId = m_kit.CreateItem(2, 1, true, itemType: InventoryItemType.Valuable,
                description: "Q9 战队夺冠纪念", detailEntries: misplacedEntries).ItemId;
            int instanceId = AddItem(itemId, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();

            detail.Open(m_containerId, instanceId);

            Assert.AreEqual("工艺藏品", detail.Tags.Value);
            Assert.AreEqual("Q9 战队夺冠纪念", detail.Description.Value);
            Assert.IsTrue(detail.HasContent.Value);
        }

        [Test]
        public void Detail_WithoutOptionalContent_ReturnsEmptySections()
        {
            int itemId = m_kit.CreateItem(1, 1).ItemId;
            int instanceId = AddItem(itemId, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();

            detail.Open(m_containerId, instanceId);

            Assert.IsEmpty(detail.Description.Value);
            Assert.IsFalse(detail.HasContent.Value);
        }

        [Test]
        public void Detail_ForContainerItem_ShowsItsPhysicalFootprintAndLayoutEntry()
        {
            InventoryContainerConfigSO container = m_kit.CreateContainer(5, 3, "背包测试");
            InventoryItemConfigSO config = m_kit.CreateContainerItem(container);
            int instanceId = AddItem(config.ItemId, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();

            detail.Open(m_containerId, instanceId);

            Assert.AreEqual("战术挂载容器", detail.Tags.Value);
            AssertFootprint(detail.FootprintPreview.Value, 3, 3, 9);
            Assert.IsTrue(detail.CanShowContainerLayout.Value,
                "容器物品需要在详情占格旁显示完整布局入口");
        }

        [Test]
        public void Detail_ForIrregularContainer_StillShowsOnlyItsPhysicalFootprint()
        {
            InventoryContainerConfigSO container = m_kit.CreateContainer(5, 3, "异形胸挂",
                new[] { "#.#..", "##...", "#####" });
            InventoryItemConfigSO config = m_kit.CreateItem(3, 2, false, container: container,
                unitWeight: 2.5f);
            int instanceId = AddItem(config.ItemId, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();

            detail.Open(m_containerId, instanceId);

            Assert.AreEqual("2.5KG", ReadDetailString(detail, "WeightLabel"));
            AssertFootprint(detail.FootprintPreview.Value, 3, 2, 6);
            Assert.IsTrue(detail.CanShowContainerLayout.Value);
        }

        [Test]
        public void Detail_Close_FiresHideRequest()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();
            int hideCount = 0;
            using IDisposable subscription = detail.HideRequested.Subscribe(() => hideCount++);

            detail.Open(m_containerId, instanceId);
            detail.Close();

            Assert.AreEqual(1, hideCount, "关闭按钮只请求收起, 不自己 Hide");
        }

        [Test]
        public void Detail_WhenTheItemRotates_RefreshesPhysicalFootprint()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();
            detail.Open(m_containerId, instanceId);
            AssertFootprint(detail.FootprintPreview.Value, 2, 1, 2);

            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryRotateItem(m_containerId, instanceId));

            AssertFootprint(detail.FootprintPreview.Value, 1, 2, 2);
        }

        [Test]
        public void Detail_WhenTheItemVanishes_FiresHideRequest()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemDetailViewModel detail = CreateDetail();
            int hideCount = 0;
            using IDisposable subscription = detail.HideRequested.Subscribe(() => hideCount++);
            detail.Open(m_containerId, instanceId);
            Assert.AreEqual(0, hideCount);

            m_model.UnregisterContainer(m_containerId);

            Assert.AreEqual(1, hideCount, "物品没了要自动收起详情");
        }

        [Test]
        public void FormalItemConfigs_AllExposeDetailContent()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:InventoryItemConfigSO", new[] { "Assets/Config/Inventory/Item" });
            Assert.AreEqual(32, guids.Length, "正式物品配置数量变化时同步检查详情覆盖");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemConfigSO config = AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(path);
                Assert.IsNotNull(config, "物品配置加载失败：" + path);
                Assert.IsFalse(string.IsNullOrWhiteSpace(config.Description),
                    "物品缺少详情描述：" + path);

                bool hasDetailEntries = config.DetailEntries.Count > 0;
                if (config.ItemType == InventoryItemType.Valuable)
                    Assert.IsFalse(hasDetailEntries, "贵重收藏品只展示长描述：" + path);
                else
                    Assert.IsTrue(hasDetailEntries, "物品缺少类型专属属性：" + path);

                foreach (InventoryItemDetailEntry entry in config.DetailEntries)
                {
                    Assert.IsNotNull(entry, "详情条目为空：" + path);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Label), "详情条目标签为空：" + path);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(entry.Value), "详情条目内容为空：" + path);
                }
            }
        }

        [Test]
        public void FormalItemConfigs_UseDisplayCategoriesAndContainerNamesWithoutLevelSuffix()
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:InventoryItemConfigSO", new[] { "Assets/Config/Inventory/Item" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemConfigSO config = AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(path);
                Assert.IsNotNull(config, "物品配置加载失败：" + path);

                string category = ReadPropertyAsString(config, "CategoryName");
                Assert.IsFalse(string.IsNullOrWhiteSpace(category), "物品缺少展示种类：" + path);

                if (!config.IsContainer) continue;
                Assert.AreEqual("战术挂载容器", category, "容器应统一显示战术挂载容器：" + path);
                Assert.That(config.DisplayName, Does.Not.Contain("Lv"),
                    "容器物品名不应带等级后缀：" + path);
            }
        }

        [Test]
        public void DetailPrefab_HasHeaderBackgroundCenteredIconAndSixByFourFootprint()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DETAIL_PREFAB_PATH);
            Assert.IsNotNull(prefab, "详情预制体加载失败：" + DETAIL_PREFAB_PATH);

            Transform card = prefab.transform.Find("Panel_Detail");
            Assert.IsNotNull(card, "详情卡片节点缺失");
            Transform header = card.Find("Layout_Header");
            Assert.IsNotNull(header, "详情 Header 节点缺失");
            Image headerBackground = header.GetComponent<Image>();
            HorizontalLayoutGroup headerLayout = header.GetComponent<HorizontalLayoutGroup>();

            Assert.IsNotNull(headerBackground, "Header 需要独立灰色背景");
            Assert.Less(headerBackground.color.maxColorComponent, 0.35f, "Header 背景应为灰色而不是透明或亮色");
            Assert.IsNotNull(headerLayout, "Header 需要横向布局");
            Assert.IsFalse(headerLayout.childForceExpandWidth,
                "品质图标不能被强制拉伸到左侧整块区域");

            Transform meta = card.Find("Layout_Meta");
            Assert.IsNotNull(meta, "详情价值行节点缺失");
            HorizontalLayoutGroup metaLayout = meta.GetComponent<HorizontalLayoutGroup>();
            Transform metaDivider = meta.Find("Img_MetaDivider");
            Assert.IsNotNull(metaLayout, "价值行需要横向布局");
            Assert.IsNotNull(metaDivider, "价值行分隔线节点缺失");
            LayoutElement dividerLayout = metaDivider.GetComponent<LayoutElement>();
            Assert.IsNotNull(dividerLayout, "价值行分隔线需要可通过 LayoutElement 调整宽度");
            Assert.IsFalse(metaLayout.childForceExpandWidth, "分隔线不能被横向布局强制拉宽");
            Assert.AreEqual(1f, dividerLayout.preferredWidth, "价值行分隔线默认宽度应为 1px");

            LayoutElement valueLayout = meta.Find("Txt_Value").GetComponent<LayoutElement>();
            Assert.IsNotNull(valueLayout, "价值文字需要 LayoutElement");
            Assert.AreEqual(-1f, valueLayout.preferredWidth,
                "价值文字宽度应跟随实际数字内容, 不能固定预留大段空白");

            Transform showcase = card.Find("Layout_Showcase");
            Assert.IsNotNull(showcase, "详情展示区节点缺失");
            Assert.IsNull(showcase.GetComponent<HorizontalLayoutGroup>(),
                "图标与右下角信息应叠放, 不能再用等宽横向布局挤偏图标");

            Transform iconArea = showcase.Find("Layout_ShowcaseIcon");
            Assert.IsNotNull(iconArea, "展示图标容器缺失");
            RectTransform iconAreaRect = iconArea.GetComponent<RectTransform>();
            Assert.AreEqual(Vector2.zero, iconAreaRect.anchorMin, "展示图标容器需要铺满展示区");
            Assert.AreEqual(Vector2.one, iconAreaRect.anchorMax, "展示图标容器需要铺满展示区");

            Transform icon = iconArea.Find("Img_Icon");
            Assert.IsNotNull(icon, "展示区图标节点缺失");
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(0.5f, 0.5f), iconRect.anchorMin, "展示区图标应水平垂直居中");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), iconRect.anchorMax, "展示区图标应水平垂直居中");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), iconRect.pivot, "展示区图标轴心应居中");

            Transform footprint = showcase.Find("Layout_Footprint");
            Assert.IsNotNull(footprint, "右下角占格信息节点缺失");
            RectTransform footprintRect = footprint.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(1f, 0f), footprintRect.anchorMin, "重量与占格需要贴右下角");
            Assert.AreEqual(new Vector2(1f, 0f), footprintRect.anchorMax, "重量与占格需要贴右下角");
            HorizontalLayoutGroup footprintLayout = footprint.GetComponent<HorizontalLayoutGroup>();
            Assert.IsNotNull(footprintLayout, "重量与占格需要在同一横向信息组内");

            Transform footprintGrid = footprint.Find("Layout_FootprintGrid");
            Assert.IsNotNull(footprintGrid, "右下角 6x4 占格图节点缺失");
            GridLayoutGroup grid = footprintGrid.GetComponent<GridLayoutGroup>();
            TMP_Text weight = footprint.Find("Txt_Weight").GetComponent<TMP_Text>();

            Assert.IsNotNull(grid, "占格图需要网格布局");
            Assert.AreEqual(6, grid.constraintCount, "占格图固定 6 列");
            Assert.AreEqual(10f, grid.cellSize.x, "占格图需要保持小尺寸显示");
            Assert.AreEqual(10f, grid.cellSize.y, "占格图需要保持小尺寸显示");
            Assert.AreEqual(FOOTPRINT_CELL_COUNT, footprintGrid.childCount, "占格图固定 6x4 共 24 格");
            Assert.IsNotNull(weight, "占格图旁需要重量文字");

            Transform layoutButton = footprint.Find("Btn_ContainerLayout");
            Assert.IsNotNull(layoutButton, "详情占格旁需要容器布局问号按钮");
            RectTransform layoutButtonRect = layoutButton.GetComponent<RectTransform>();
            LayoutElement layoutButtonElement = layoutButton.GetComponent<LayoutElement>();
            Assert.IsNotNull(layoutButton.GetComponent<Button>(), "容器布局问号按钮需要可点击");
            Assert.IsNotNull(layoutButton.GetComponent<Image>(), "容器布局问号按钮需要射线底板");
            Assert.IsTrue(layoutButton.GetComponent<Image>().raycastTarget, "问号按钮底板必须接收点击");
            Assert.AreEqual(Vector2.one, layoutButtonRect.anchorMin, "问号按钮需要贴在占格右上角");
            Assert.AreEqual(Vector2.one, layoutButtonRect.anchorMax, "问号按钮需要贴在占格右上角");
            Assert.AreEqual(new Vector2(1f, 0f), layoutButtonRect.pivot, "问号按钮需要向上展开到占格上方");
            Assert.AreEqual(new Vector2(0f, 6f), layoutButtonRect.anchoredPosition,
                "问号按钮需要贴住占格右上方并保留 6 像素间距");
            Assert.IsNotNull(layoutButtonElement, "问号按钮需要排除在占格信息横向布局之外");
            Assert.IsTrue(layoutButtonElement.ignoreLayout, "问号按钮不能被重量与占格布局挤到右侧");
            TMP_Text layoutLabel = layoutButton.Find("Txt_ContainerLayout").GetComponent<TMP_Text>();
            Assert.IsNotNull(layoutLabel, "容器布局按钮文字缺失");
            Assert.AreEqual("?", layoutLabel.text, "容器布局按钮固定显示问号");
            Assert.IsFalse(layoutButton.gameObject.activeSelf, "普通物品默认隐藏容器布局入口");

            Assert.IsNull(card.Find("Txt_Spec"), "旧规格文字应移除");
            Assert.IsNull(card.Find("Txt_DetailEntries"), "类型属性条目应从详情面板移除");
        }

        [Test]
        public void ContainerLayout_ForIrregularContainer_BuildsFullMaskAndRegionConnections()
        {
            InventoryContainerConfigSO container = m_kit.CreateContainer(5, 3, "异形胸挂",
                new[] { "#.#..", "##...", "#####" },
                new[] { "A.A..", "AA...", "BBBBB" });
            InventoryItemConfigSO config = m_kit.CreateContainerItem(container, 3, 2);
            int instanceId = AddItem(config.ItemId, new Vector2Int(0, 0));
            ItemContainerLayoutViewModel layout = new ItemContainerLayoutViewModel(m_model);
            m_popups.Add(layout);
            layout.OnBind();

            layout.Open(m_containerId, instanceId);

            ItemFootprintPreviewVO preview = layout.Preview.Value;
            Assert.IsTrue(preview.IsContainerLayout);
            Assert.AreEqual(5, preview.ColumnCount);
            Assert.AreEqual(3, preview.RowCount);
            Assert.AreEqual(9, CountSolidCells(preview));
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, preview.RightEdges[0],
                "区域内空洞需要绘制外沿");
            Assert.AreEqual(InventorySlotEdgeKind.None, preview.LeftEdges[1], "空洞格不能产生连接线");
            Assert.AreEqual(InventorySlotEdgeKind.None, preview.RightEdges[1], "空洞格不能产生连接线");
            Assert.AreEqual(InventorySlotEdgeKind.Connected, preview.BottomEdges[0]);
            Assert.AreEqual(InventorySlotEdgeKind.Connected, preview.RightEdges[5],
                "同区域相邻格必须连接");
            Assert.AreEqual(InventorySlotEdgeKind.Boundary, preview.TopEdges[10],
                "跨逻辑区域需要重新绘制外沿");
            Assert.AreEqual(InventorySlotEdgeKind.Connected, preview.RightEdges[10]);
            Assert.AreEqual("5 x 3", layout.SizeLabel.Value);
        }

        [Test]
        public void ContainerLayoutPrefab_HasIndependentPopupHierarchy()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CONTAINER_LAYOUT_PREFAB_PATH);
            Assert.IsNotNull(prefab, "容器布局预制体加载失败：" + CONTAINER_LAYOUT_PREFAB_PATH);
            Assert.IsNotNull(prefab.GetComponent<UIItemContainerLayoutPanel>(), "容器布局根节点缺少视图组件");

            Transform card = prefab.transform.Find("Panel_ContainerLayout");
            Assert.IsNotNull(card, "容器布局卡片节点缺失");
            RectTransform cardRect = card.GetComponent<RectTransform>();
            Assert.AreEqual(new Vector2(0f, 1f), cardRect.pivot, "容器布局窗口需要以左上角为定位基准");
            ContentSizeFitter fitter = card.GetComponent<ContentSizeFitter>();
            Assert.IsNotNull(fitter, "容器布局卡片缺少内容尺寸适配组件");
            Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.horizontalFit,
                "容器布局宽度需要由内容自动计算");
            Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.verticalFit,
                "容器布局高度需要由内容自动计算");
            Assert.IsNotNull(card.Find("Layout_Header/Txt_Title"), "容器布局标题节点缺失");
            Assert.IsNotNull(card.Find("Layout_Header/Txt_Size"), "容器尺寸节点缺失");
            Assert.IsNotNull(card.Find("Layout_Header/Btn_Close"), "容器布局关闭按钮缺失");
            Transform grid = card.Find("Layout_Viewport/Layout_Grid");
            Assert.IsNotNull(grid, "容器布局网格节点缺失");
            GridLayoutGroup gridLayout = grid.GetComponent<GridLayoutGroup>();
            Assert.IsNotNull(gridLayout, "容器布局网格缺少网格布局组件");
            Assert.AreEqual(TextAnchor.UpperLeft, gridLayout.childAlignment,
                "容器布局必须从左上角按行列生成, 不能居中导致格子挤在一起");
            Assert.AreEqual(Vector2.zero, gridLayout.spacing, "容器布局格子之间不能叠加额外间距");
        }

        [Test]
        public void ContainerLayoutPrefab_DrawsHolesAndConnectedEdges()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CONTAINER_LAYOUT_PREFAB_PATH);
            Assert.IsNotNull(prefab, "容器布局预制体加载失败：" + CONTAINER_LAYOUT_PREFAB_PATH);
            GameObject instance = UnityEngine.Object.Instantiate(prefab);
            try
            {
                UIItemContainerLayoutPanel view = instance.GetComponent<UIItemContainerLayoutPanel>();
                Transform gridRoot = instance.transform.Find(
                    "Panel_ContainerLayout/Layout_Viewport/Layout_Grid");
                Assert.IsNotNull(gridRoot, "容器布局网格节点缺失");
                ItemFootprintPreviewVO preview = BuildContainerLayoutPreview();
                InvokePrivate(view, "OnAwake");
                InvokePrivate(view, "RefreshGrid", preview);

                RectTransform cardRect = instance.transform.Find("Panel_ContainerLayout")
                    .GetComponent<RectTransform>();
                LayoutElement viewportElement = instance.transform.Find(
                    "Panel_ContainerLayout/Layout_Viewport").GetComponent<LayoutElement>();
                Assert.AreEqual(290f, viewportElement.preferredWidth,
                    "容器视口宽度需要与 5 列格子同步");
                Assert.AreEqual(182f, viewportElement.preferredHeight,
                    "容器视口高度需要与 3 行格子同步");
                Assert.AreEqual(248f, cardRect.rect.height, 0.01f,
                    "卡片高度需要由视口首选高度自动计算");
                Assert.AreEqual(54f, gridRoot.GetComponent<GridLayoutGroup>().cellSize.x,
                    "容器格子需要保持统一且可读的尺寸");
                Assert.AreEqual(15, gridRoot.childCount, "完整容器布局必须保留所有网格占位");
                Transform hole = gridRoot.GetChild(2);
                Assert.IsFalse(hole.GetComponent<Image>().enabled, "空洞格不能绘制底色");
                Assert.IsFalse(hole.Find("Img_Background").GetComponent<Image>().enabled,
                    "空洞格不能绘制独立底板");
                Assert.IsFalse(hole.Find("Img_BorderTop").GetComponent<Image>().enabled);
                Assert.IsFalse(hole.Find("Img_BorderRight").GetComponent<Image>().enabled);
                Assert.IsFalse(hole.Find("Img_BorderBottom").GetComponent<Image>().enabled);
                Assert.IsFalse(hole.Find("Img_BorderLeft").GetComponent<Image>().enabled);

                Image connectedRight = gridRoot.GetChild(0).Find("Img_BorderRight").GetComponent<Image>();
                Assert.IsTrue(connectedRight.enabled, "同区域相邻格必须绘制连接线");
                Assert.AreEqual(new Color(0.36f, 0.38f, 0.41f, 1f), connectedRight.color);

                float wideWidth = cardRect.rect.width;
                float wideHeight = cardRect.rect.height;
                InvokePrivate(view, "RefreshGrid", BuildRectangularLayoutPreview(2, 2));
                Assert.Less(cardRect.rect.width, wideWidth, "更少列的容器需要自动收窄卡片");
                Assert.Less(cardRect.rect.height, wideHeight, "更少行的容器需要自动压低卡片");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        [TestCase(1000, "武器类型")]
        [TestCase(2000, "穿透等级")]
        [TestCase(3000, "防护等级")]
        [TestCase(4000, "恢复量")]
        [TestCase(6000, "内部容量")]
        [TestCase(7001, "恢复效果")]
        [TestCase(8001, "工具类型")]
        [TestCase(9001, "安装槽位")]
        public void FormalItemConfigs_UseCategorySpecificEntries(int itemId, string expectedLabel)
        {
            InventoryItemConfigSO config = FindFormalItemConfig(itemId);

            bool found = false;
            foreach (InventoryItemDetailEntry entry in config.DetailEntries)
            {
                if (entry.Label != expectedLabel) continue;
                found = true;
                break;
            }

            Assert.IsTrue(found, $"物品 {itemId} 缺少 {expectedLabel} 条目");
        }
        #endregion

        #region 菜单视图模型
        [Test]
        public void Menu_RotateOnRotatableItem_TurnsItAndDismisses()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);

            menu.Open(m_containerId, instanceId);
            Assert.IsTrue(menu.CanRotate.Value);

            menu.Rotate();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(new Vector2Int(1, 2), ItemOf(instanceId).Size);
        }

        [Test]
        public void Menu_RotateOnNonRotatableItem_LeavesFacingAloneButDismisses()
        {
            int itemId = m_kit.CreateItem(2, 1, false).ItemId;
            int instanceId = AddItem(itemId, new Vector2Int(0, 0));
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);

            menu.Open(m_containerId, instanceId);
            Assert.IsFalse(menu.CanRotate.Value, "不可旋转的物品要把按钮置灰");

            menu.Rotate();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(new Vector2Int(2, 1), ItemOf(instanceId).Size);
        }

        [Test]
        public void Menu_RotateBeforeAnyOpen_DoesNothing()
        {
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);

            menu.Rotate();

            Assert.AreEqual(0, dismissCount, "没打开过菜单就没有操作对象, 不该收起");
        }

        [Test]
        public void Menu_Dismiss_FiresDismissed()
        {
            int instanceId = AddItem(2, 1, new Vector2Int(0, 0));
            ItemMenuViewModel menu = CreateMenu();
            int dismissCount = 0;
            using IDisposable subscription = menu.Dismissed.Subscribe(() => dismissCount++);
            menu.Open(m_containerId, instanceId);

            menu.Dismiss();

            Assert.AreEqual(1, dismissCount);
            Assert.AreEqual(new Vector2Int(2, 1), ItemOf(instanceId).Size, "点空白处收起不该转物品");
        }
        #endregion

        #region Helpers
        // 向模型添加一件测试物品, 返回运行时实例号
        private int AddItem(int width, int height, Vector2Int cell, bool canRotate = true)
        {
            return AddItem(m_kit.CreateItem(width, height, canRotate).ItemId, cell);
        }

        // 用指定物品号添加一件测试物品
        private int AddItem(int itemId, Vector2Int cell)
        {
            Assert.AreEqual(InventoryMoveResult.Success, m_model.TryAddItem(m_containerId, itemId, 1, cell));
            Assert.IsTrue(m_model.TryGetInstanceIdAt(m_containerId, cell, out int instanceId));
            return instanceId;
        }

        // 从模型取指定实例
        private InventoryItemVO ItemOf(int instanceId)
        {
            Assert.IsTrue(m_model.GetContainerVO(m_containerId).TryGetItem(instanceId, out InventoryItemVO item));
            return item;
        }

        // 创建已绑定的详情视图模型并登记到拆除清单
        private ItemDetailViewModel CreateDetail()
        {
            ItemDetailViewModel detail = new ItemDetailViewModel(m_model);
            m_popups.Add(detail);
            detail.OnBind();
            return detail;
        }

        // 创建已绑定的菜单视图模型并登记到拆除清单
        private ItemMenuViewModel CreateMenu()
        {
            ItemMenuViewModel menu = new ItemMenuViewModel(m_model);
            m_popups.Add(menu);
            menu.OnBind();
            return menu;
        }

        // 按正式物品号查找配置, 供详情内容形状测试使用
        private static InventoryItemConfigSO FindFormalItemConfig(int itemId)
        {
            string[] guids = AssetDatabase.FindAssets(
                "t:InventoryItemConfigSO", new[] { "Assets/Config/Inventory/Item" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                InventoryItemConfigSO config = AssetDatabase.LoadAssetAtPath<InventoryItemConfigSO>(path);
                if (config != null && config.ItemId == itemId) return config;
            }

            Assert.Fail("未找到正式物品配置：" + itemId);
            return null;
        }

        // 读取详情视图模型新增的字符串响应属性, 让测试先以行为失败而不是编译错误暴露缺口
        private static string ReadDetailString(ItemDetailViewModel detail, string propertyName)
        {
            object reactive = ReadProperty(detail, propertyName);
            Assert.IsNotNull(reactive, "详情视图模型缺少属性：" + propertyName);
            return ReadPropertyAsString(reactive, "Value");
        }

        // 按实例属性读取对象值
        private static object ReadProperty(object target, string propertyName)
        {
            PropertyInfo property = target.GetType().GetProperty(propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(property, "属性缺失：" + target.GetType().Name + "." + propertyName);
            return property.GetValue(target);
        }

        // 把反射读到的值按字符串返回
        private static string ReadPropertyAsString(object target, string propertyName)
            => ReadProperty(target, propertyName) as string;

        // 校验物品自身物理占格的尺寸与实心格数量
        private static void AssertFootprint(ItemFootprintPreviewVO preview, int columns, int rows, int solidCount)
        {
            Assert.IsNotNull(preview);
            Assert.AreEqual(columns, preview.ColumnCount);
            Assert.AreEqual(rows, preview.RowCount);
            Assert.AreEqual(solidCount, CountSolidCells(preview));
        }

        // 统计预览中的实心格数量
        private static int CountSolidCells(ItemFootprintPreviewVO preview)
        {
            int count = 0;
            for (int i = 0; i < preview.SolidCells.Count; i++)
            {
                if (preview.SolidCells[i])
                    count++;
            }

            return count;
        }

        // 构造带空洞与一条区域连接线的完整容器预览
        private static ItemFootprintPreviewVO BuildContainerLayoutPreview()
        {
            const int width = 5;
            const int height = 3;
            const int cellCount = width * height;
            bool[] solidCells = new bool[cellCount];
            InventorySlotEdgeKind[] topEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] rightEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] bottomEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] leftEdges = new InventorySlotEdgeKind[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                solidCells[i] = i != 2;
                topEdges[i] = InventorySlotEdgeKind.Boundary;
                rightEdges[i] = InventorySlotEdgeKind.Boundary;
                bottomEdges[i] = InventorySlotEdgeKind.Boundary;
                leftEdges[i] = InventorySlotEdgeKind.Boundary;
            }

            rightEdges[0] = InventorySlotEdgeKind.Connected;
            leftEdges[1] = InventorySlotEdgeKind.Connected;
            return new ItemFootprintPreviewVO(true, width, height, solidCells, topEdges, rightEdges,
                bottomEdges, leftEdges);
        }

        // 构造指定行列数的完整矩形容器预览
        private static ItemFootprintPreviewVO BuildRectangularLayoutPreview(int width, int height)
        {
            int cellCount = width * height;
            bool[] solidCells = new bool[cellCount];
            InventorySlotEdgeKind[] topEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] rightEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] bottomEdges = new InventorySlotEdgeKind[cellCount];
            InventorySlotEdgeKind[] leftEdges = new InventorySlotEdgeKind[cellCount];

            for (int i = 0; i < cellCount; i++)
            {
                solidCells[i] = true;
                topEdges[i] = InventorySlotEdgeKind.Boundary;
                rightEdges[i] = InventorySlotEdgeKind.Boundary;
                bottomEdges[i] = InventorySlotEdgeKind.Boundary;
                leftEdges[i] = InventorySlotEdgeKind.Boundary;
            }

            return new ItemFootprintPreviewVO(true, width, height, solidCells, topEdges, rightEdges,
                bottomEdges, leftEdges);
        }

        // 调用详情视图的私有初始化与刷新入口
        private static object InvokePrivate(object target, string methodName, params object[] args)
        {
            MethodInfo method = target.GetType().GetMethod(methodName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "私有方法缺失：" + target.GetType().Name + "." + methodName);
            return method.Invoke(target, args);
        }

        // 按 ViewBase 拆除顺序回收浮层视图模型
        private static void DisposeViewModel(ViewModelBase viewModel)
        {
            viewModel.OnUnbind();
            viewModel.OnDispose();
            viewModel.Dispose();
        }
        #endregion
    }
}
