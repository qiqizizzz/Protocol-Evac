/*
 * ┌──────────────────────────────────┐
 * │  描    述: 背包格子边框视觉测试, 覆盖外沿竖线不能穿过上下横线
 * │  类    名: QInventorySlotVisualTests.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Reflection;
using NUnit.Framework;
using UI.Inventory;
using UI.Inventory.View;
using UnityEngine;
using UnityEngine.UI;

namespace Tests.Inventory
{
    /// <summary>
    /// 背包格子边框测试：外沿竖线必须收在上下横线的内侧
    /// </summary>
    public class QInventorySlotVisualTests
    {
        private GameObject m_slotObject;
        private UIInventorySlot m_slotView;
        private Image m_borderRight;
        private Image m_borderLeft;

        [SetUp]
        public void SetUp()
        {
            m_slotObject = new GameObject("UIInventorySlot", typeof(RectTransform), typeof(Image));
            CreateImage("Img_Background");
            CreateImage("Img_BorderTop");
            m_borderRight = CreateImage("Img_BorderRight");
            CreateImage("Img_BorderBottom");
            m_borderLeft = CreateImage("Img_BorderLeft");

            m_slotView = m_slotObject.AddComponent<UIInventorySlot>();
            InvokePrivate(m_slotView, "OnAwake");

            SetPrivateField("m_topEdge", InventorySlotEdgeKind.Boundary);
            SetPrivateField("m_rightEdge", InventorySlotEdgeKind.Boundary);
            SetPrivateField("m_bottomEdge", InventorySlotEdgeKind.Boundary);
            SetPrivateField("m_leftEdge", InventorySlotEdgeKind.Boundary);
            InvokePrivate(m_slotView, "ApplyVisual");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_slotObject);
        }

        [Test]
        public void BoundaryFrame_VerticalBordersStopInsideHorizontalBorders()
        {
            AssertVerticalBorderInsideHorizontalBorders(m_borderRight, "右侧");
            AssertVerticalBorderInsideHorizontalBorders(m_borderLeft, "左侧");
        }

        // 校验竖线上下端点均避开横线的完整厚度
        private static void AssertVerticalBorderInsideHorizontalBorders(Image border, string side)
        {
            RectTransform rect = border.rectTransform;
            Assert.AreEqual(3f, rect.offsetMin.y, $"{side}竖线下端必须收进下横线内侧");
            Assert.AreEqual(-3f, rect.offsetMax.y, $"{side}竖线上端必须收进上横线内侧");
        }

        // 建一个图像子节点
        private Image CreateImage(string objectName)
        {
            GameObject child = new GameObject(objectName, typeof(RectTransform));
            child.transform.SetParent(m_slotObject.transform, false);
            return child.AddComponent<Image>();
        }

        // 调用格子视图的私有初始化与刷新入口
        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            method.Invoke(target, null);
        }

        // 写入格子视图的私有边类型字段
        private void SetPrivateField(string fieldName, object value)
        {
            FieldInfo field = typeof(UIInventorySlot).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            field.SetValue(m_slotView, value);
        }
    }
}
