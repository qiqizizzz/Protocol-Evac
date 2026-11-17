/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包物品搜索态测试, 覆盖未搜索阴影的点击接收与揭晓行为
 * │  类    名: QInventoryItemSearchTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Reflection;
using Module.Inventory.Core.Config.Type;
using NUnit.Framework;
using TMPro;
using UI.Inventory;
using UI.Inventory.View;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tests.Inventory
{
    /// <summary>
    /// 物品搜索态测试：阴影显示期间仍要保留可点击底板, 点击不能抢在搜索节奏前揭晓
    /// </summary>
    public class QInventoryItemSearchTests
    {
        private GameObject m_itemObject;
        private UIInventoryItem m_itemView;
        private Image m_clickArea;
        private Image m_background;
        private Image m_icon;
        private Image m_silhouette;
        private Image m_searchIndicator;

        [SetUp]
        public void SetUp()
        {
            m_itemObject = new GameObject("UIInventoryItem", typeof(RectTransform));
            m_clickArea = m_itemObject.AddComponent<Image>();
            m_background = CreateImage("Img_Background");
            StretchToItem(m_background.rectTransform, 0f);

            m_icon = CreateImage("Img_Icon");
            m_silhouette = CreateImage("Img_Silhouette");
            StretchToItem(m_silhouette.rectTransform, 2f);
            m_searchIndicator = CreateImage("Img_Search");
            CreateText("Txt_Count");
            new GameObject("Img_Selected", typeof(RectTransform)).transform.SetParent(m_itemObject.transform, false);

            m_itemView = m_itemObject.AddComponent<UIInventoryItem>();
            InvokeAwake();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_itemObject);
        }

        [Test]
        public void SetSearching_HidesTheItemButKeepsTheBackgroundRaycastable()
        {
            m_itemView.SetSearching(true);

            Assert.IsTrue(m_background.enabled, "搜索态仍需底板接收点击");
            Assert.IsTrue(m_clickArea.raycastTarget, "搜索态点击不能穿透到背包空白");
        }

        [Test]
        public void OnPointerClick_WhileSearching_LeavesTheItemHidden()
        {
            m_itemView.Setup(new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, Vector2Int.one, InventoryRotation.None),
                CreateUniformContainer(1, 1),
                new Vector2(80f, 80f), new Vector2(4f, 4f));
            m_itemView.SetSearching(true);

            m_itemView.OnPointerClick(new PointerEventData(null) { button = PointerEventData.InputButton.Left });

            Assert.IsTrue(m_silhouette.enabled, "搜索中点击不能提前揭晓物品");
            Assert.IsTrue(m_background.enabled, "搜索中仍要保留底板接收点击");
            Assert.IsTrue(m_clickArea.raycastTarget, "搜索中点击不能穿透到背包空白");
            Assert.AreEqual(Color.clear, m_background.color, "搜索中底板必须保持透明");
            Assert.IsFalse(m_icon.enabled, "搜索中不能显示物品图标");
        }

        [Test]
        public void OnDrag_WhileSearching_DoesNotStartDraggingOrHideTheSilhouette()
        {
            m_itemView.SetSearching(true);
            PointerEventData down = new PointerEventData(null) { position = Vector2.zero };
            PointerEventData moved = new PointerEventData(null) { position = new Vector2(100f, 0f) };

            Assert.DoesNotThrow(() =>
            {
                m_itemView.OnBeginDrag(down);
                m_itemView.OnDrag(moved);
                m_itemView.OnEndDrag(moved);
            });
            Assert.IsTrue(m_silhouette.enabled, "拖拽动作不能消耗未搜索物品");
        }

        [Test]
        public void Setup_TwoByTwoItem_UsesOneContinuousSilhouetteRect()
        {
            m_itemView.Setup(new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, new Vector2Int(2, 2), InventoryRotation.None),
                CreateUniformContainer(2, 2),
                new Vector2(80f, 80f), new Vector2(4f, 4f));

            Assert.AreEqual(new Vector2(164f, 164f), m_itemObject.GetComponent<RectTransform>().rect.size,
                "2x2 物品必须按一整块占格矩形显示");
            Assert.AreEqual(new Vector2(160f, 160f), m_silhouette.rectTransform.rect.size,
                "2x2 阴影必须跟随整块物品矩形内缩, 不能拆成四个 1x1");
        }

        [Test]
        public void Setup_OneByOneItem_InsetsBackgroundAndIconFromTheFullFootprint()
        {
            m_itemView.Setup(new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, Vector2Int.one, InventoryRotation.None),
                CreateUniformContainer(1, 1),
                new Vector2(80f, 80f), new Vector2(4f, 4f));

            Assert.AreEqual(new Vector2(74f, 74f), m_background.rectTransform.rect.size,
                "底板必须在完整占格基础上四周内缩, 给格子边界留出显示空间");
            Assert.AreEqual(new Vector2(3f, -3f), m_background.rectTransform.anchoredPosition,
                "底板内缩只能收掉描边宽度, 不能把物品整体缩小");
            Assert.AreEqual(new Vector2(74f, 74f), m_icon.rectTransform.rect.size,
                "图标必须在完整占格基础上四周内缩, 给格子边界留出显示空间");
        }

        [Test]
        public void ResolveEdgeKind_MixedRegionRow_SeparatesOnlyAdjacentRegions()
        {
            InventoryContainerVO container = CreateContainer(4, 1,
                new[] { true, true, true, true }, new[] { 1, 1, 2, 3 });

            Assert.AreEqual(InventorySlotEdgeKind.Connected,
                container.ResolveEdgeKind(0, 0, 1, 0, 1), "AA 内部必须保持连通");
            Assert.AreEqual(InventorySlotEdgeKind.Boundary,
                container.ResolveEdgeKind(1, 0, 1, 0, 1), "A 与 B 的交界必须分别断开");
            Assert.AreEqual(InventorySlotEdgeKind.Boundary,
                container.ResolveEdgeKind(2, 0, 1, 0, 2), "B 与 C 的交界必须分别断开");
            Assert.AreEqual(InventorySlotEdgeKind.Boundary,
                container.ResolveEdgeKind(2, 0, -1, 0, 2), "跨区交界两侧都要绘制边界");
        }

        [Test]
        public void Setup_ConnectedHorizontalCells_FillTheCellGapWithoutChangingOuterInset()
        {
            InventoryItemVO item = new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, new Vector2Int(2, 1), InventoryRotation.None);

            m_itemView.Setup(item, CreateUniformContainer(2, 1), new Vector2(80f, 80f), new Vector2(4f, 4f));

            Image secondSlice = GetBackgroundSlice(1);
            Assert.AreEqual(new Vector2(81f, 74f), m_background.rectTransform.rect.size,
                "第一格只向同区域接缝方向延伸, 外侧仍保留完整内缩");
            Assert.AreEqual(new Vector2(3f, -3f), m_background.rectTransform.anchoredPosition,
                "第一格的位置不能被接缝延伸推到外侧");
            Assert.AreEqual(new Vector2(77f, 74f), secondSlice.rectTransform.rect.size,
                "第二格只需收掉右侧外沿");
            Assert.AreEqual(new Vector2(84f, -3f), secondSlice.rectTransform.anchoredPosition,
                "第二格必须接在第一格留下的格距之后");
        }

        [Test]
        public void Setup_ConnectedVerticalCells_FillTheCellGapWithoutChangingOuterInset()
        {
            InventoryItemVO item = new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, new Vector2Int(1, 2), InventoryRotation.None);

            m_itemView.Setup(item, CreateUniformContainer(1, 2), new Vector2(80f, 80f), new Vector2(4f, 4f));

            Image secondSlice = GetBackgroundSlice(1);
            Assert.AreEqual(new Vector2(74f, 81f), m_background.rectTransform.rect.size,
                "第一格只向同区域接缝方向延伸, 外侧仍保留完整内缩");
            Assert.AreEqual(new Vector2(3f, -3f), m_background.rectTransform.anchoredPosition,
                "第一格的位置不能被接缝延伸推到外侧");
            Assert.AreEqual(new Vector2(74f, 77f), secondSlice.rectTransform.rect.size,
                "第二格只需收掉底部外沿");
            Assert.AreEqual(new Vector2(3f, -84f), secondSlice.rectTransform.anchoredPosition,
                "第二格必须接在第一格留下的格距之后");
        }

        [Test]
        public void Setup_CellHole_LeavesItsBackgroundSliceDisabled()
        {
            InventoryItemVO item = new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, new Vector2Int(2, 1), InventoryRotation.None);
            InventoryContainerVO container = CreateContainer(2, 1,
                new[] { true, false }, new[] { 1, -1 });

            m_itemView.Setup(item, container, new Vector2(80f, 80f), new Vector2(4f, 4f));

            Assert.IsFalse(GetBackgroundSlice(1).enabled, "空洞格不能生成可见底板");
        }

        [Test]
        public void SetSearching_ActiveSearch_ShowsOneCenteredIndicator()
        {
            m_itemView.Setup(new InventoryItemVO(1, 1, "测试物品", 1, null, Color.white,
                InventoryItemRarity.Common, 0, Vector2Int.zero, new Vector2Int(2, 2), InventoryRotation.None),
                CreateUniformContainer(2, 2),
                new Vector2(80f, 80f), new Vector2(4f, 4f));

            m_itemView.SetSearching(true, true);

            RectTransform rect = m_searchIndicator.rectTransform;
            Assert.IsTrue(m_searchIndicator.enabled, "当前正在计时的物品必须显示搜索图标");
            Assert.IsFalse(m_searchIndicator.raycastTarget, "搜索图标不能阻挡物品点击");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMin, "搜索图标必须锚定整块物品中心");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.anchorMax, "搜索图标必须锚定整块物品中心");
            Assert.AreEqual(new Vector2(0.5f, 0.5f), rect.pivot, "搜索图标必须围绕自身中心沿圆周轨迹移动");
            Assert.AreEqual(Vector2.zero, rect.anchoredPosition, "2x2 物品的搜索图标不能落在左上角");
        }

        [Test]
        public void SetSearching_NonActiveSearch_KeepsSilhouetteWithoutIndicator()
        {
            m_itemView.SetSearching(true, false);

            Assert.IsTrue(m_silhouette.enabled, "尚未揭晓但未轮到的物品仍需显示整块阴影");
            Assert.IsFalse(m_searchIndicator.enabled, "同一时刻只能给队列中的当前物品显示搜索图标");
        }

        [Test]
        public void AdvanceSearchIndicator_ActiveSearch_OrbitsWithoutRotatingItself()
        {
            m_itemView.SetSearching(true, true);

            InvokeAdvanceSearchIndicator(0.2f);

            RectTransform rect = m_searchIndicator.rectTransform;
            Assert.AreEqual(8f, rect.anchoredPosition.magnitude, 0.001f,
                "搜索图标必须沿物品中心 8px 半径的圆周轨迹移动");
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, rect.localEulerAngles.z), 0.001f,
                "搜索图标自身不能旋转, 只允许改变位置");
        }

        [Test]
        public void SetSearching_EndingSearch_HidesAndResetsIndicator()
        {
            m_itemView.SetSearching(true, true);
            InvokeAdvanceSearchIndicator(0.2f);

            m_itemView.SetSearching(false, false);

            Assert.IsFalse(m_searchIndicator.enabled, "搜索结束后必须隐藏搜索图标");
            Assert.AreEqual(Vector2.zero, m_searchIndicator.rectTransform.anchoredPosition,
                "搜索结束后必须把搜索图标复位到整块阴影中心");
            Assert.AreEqual(0f, m_searchIndicator.rectTransform.localEulerAngles.z, 0.001f,
                "搜索图标自身始终保持不旋转");
        }

        // 让物品内容铺满占格, 并按指定距离从四周内缩
        private static void StretchToItem(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(-inset * 2f, -inset * 2f);
        }

        // 造一份所有格子都在同一区域的容器快照
        private static InventoryContainerVO CreateUniformContainer(int width, int height)
        {
            int count = width * height;
            bool[] mask = new bool[count];
            int[] regions = new int[count];
            for (int i = 0; i < count; i++)
            {
                mask[i] = true;
                regions[i] = 0;
            }

            return CreateContainer(width, height, mask, regions);
        }

        // 按显式掩码与区域表造一份容器快照
        private static InventoryContainerVO CreateContainer(int width, int height, bool[] mask, int[] regions)
        {
            return new InventoryContainerVO(1, width, height, mask, regions, 0, 0f, 0f,
                new InventoryItemVO[0], "TEST_CONTAINER");
        }

        // 读取物品视图按需创建的第 n 个底板切片
        private Image GetBackgroundSlice(int index)
        {
            Transform slice = m_itemObject.transform.Find("Img_Background_" + index);
            Assert.IsNotNull(slice, "底板切片缺失: " + index);
            return slice.GetComponent<Image>();
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

        // EditMode 下 AddComponent 不会自动跑 Awake, 用反射补齐视图缓存
        private void InvokeAwake()
        {
            MethodInfo awake = typeof(UIInventoryItem).GetMethod("Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(m_itemView, null);
        }

        // 推进搜索图标的本地动画时钟, 避免 EditMode 依赖真实帧时间
        private void InvokeAdvanceSearchIndicator(float deltaTime)
        {
            MethodInfo advance = typeof(UIInventoryItem).GetMethod("AdvanceSearchIndicator",
                BindingFlags.Instance | BindingFlags.NonPublic);
            advance.Invoke(m_itemView, new object[] { deltaTime });
        }
    }
}
