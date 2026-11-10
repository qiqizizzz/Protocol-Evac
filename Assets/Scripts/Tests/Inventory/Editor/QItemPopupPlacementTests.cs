/*
* ┌──────────────────────────────────┐
* │  描    述: 物品详情弹窗定位测试, 验证完整占格右侧优先与左侧翻转规则
* │  类    名: QItemPopupPlacementTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using NUnit.Framework;
using UI.Inventory.View.Tools;
using UnityEngine;

namespace Tests.Inventory
{
    /// <summary>
    /// 详情弹窗定位测试：固定卡片与物品完整占格之间的间距, 并验证右侧不足时翻到左侧
    /// </summary>
    public class QItemPopupPlacementTests
    {
        private const float ROOT_SIZE = 1000f;
        private const float CARD_WIDTH = 320f;
        private const float CARD_HEIGHT = 372f;
        private const float ITEM_GAP = 12f;

        private GameObject m_rootObject;
        private GameObject m_cardObject;
        private RectTransform m_root;
        private RectTransform m_card;

        [SetUp]
        public void SetUp()
        {
            m_rootObject = new GameObject("Root", typeof(RectTransform));
            m_root = (RectTransform)m_rootObject.transform;
            m_root.position = new Vector3(ROOT_SIZE * 0.5f, ROOT_SIZE * 0.5f, 0f);
            m_root.sizeDelta = new Vector2(ROOT_SIZE, ROOT_SIZE);

            m_cardObject = new GameObject("Card", typeof(RectTransform));
            m_card = (RectTransform)m_cardObject.transform;
            m_card.SetParent(m_root, false);
            m_card.anchorMin = new Vector2(0.5f, 0.5f);
            m_card.anchorMax = new Vector2(0.5f, 0.5f);
            m_card.pivot = new Vector2(0f, 1f);
            m_card.sizeDelta = new Vector2(CARD_WIDTH, CARD_HEIGHT);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_cardObject);
            Object.DestroyImmediate(m_rootObject);
        }

        [Test]
        public void TryReadItemRectArgs_WithContainerInstanceAndRect_ReadsAllValues()
        {
            Rect itemScreenRect = new Rect(100f, 400f, 200f, 100f);

            bool result = ItemPopupPlacement.TryReadItemRectArgs(
                new object[] { 7, 19, itemScreenRect },
                out int containerId,
                out int instanceId,
                out Rect actualItemScreenRect);

            Assert.IsTrue(result, "三个有效参数应能打开详情弹窗");
            Assert.AreEqual(7, containerId);
            Assert.AreEqual(19, instanceId);
            Assert.AreEqual(itemScreenRect, actualItemScreenRect);
        }

        [Test]
        public void AnchorToScreenRect_WhenRightSideFits_UsesRightSideWithGap()
        {
            Rect itemScreenRect = new Rect(100f, 400f, 200f, 100f);

            ItemPopupPlacement.AnchorToScreenRect(m_root, m_card, itemScreenRect, ITEM_GAP);

            Assert.AreEqual(new Vector2(-188f, 0f), m_card.anchoredPosition,
                "右侧能完整容纳时, 卡片左边界应距物品右边界 12 像素");
        }

        [Test]
        public void AnchorToScreenRect_WhenRightSideDoesNotFit_UsesLeftSideWithGap()
        {
            Rect itemScreenRect = new Rect(600f, 400f, 200f, 100f);

            ItemPopupPlacement.AnchorToScreenRect(m_root, m_card, itemScreenRect, ITEM_GAP);

            Assert.AreEqual(new Vector2(-232f, 0f), m_card.anchoredPosition,
                "右侧放不下时, 卡片右边界应距物品左边界 12 像素");
        }
    }
}
