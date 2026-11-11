/*
* ┌──────────────────────────────────┐
* │  描    述: 地面垃圾堆登记表测试, 覆盖堆号分配、范围过滤、距离排序与销堆广播
* │  类    名: QGroundPileModelTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 地面垃圾堆登记表测试：模型只管「哪座堆在哪个坐标、由哪只容器承载」, 范围查询要给出可直接对齐实例的有序快照
    /// </summary>
    public class QGroundPileModelTests
    {
        private const int PILE_CONTAINER_ID = 9600;
        private const string FIRST_ID = "pile-000001";

        private GroundPileModel m_model;
        private int m_changedCount;

        [SetUp]
        public void SetUp()
        {
            //广播计数每次都要归零, 单靠字段初始值在复用测试实例时会串味
            m_changedCount = 0;

            m_model = new GroundPileModel();
            m_model.OnChanged += HandleChanged;
            m_model.OnRegister();
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnChanged -= HandleChanged;
            m_model.OnUnregister();
        }

        #region 起堆与查询
        [Test]
        public void CreatePile_FirstPile_GetsSequentialPileId()
        {
            string first = m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);
            string second = m_model.CreatePile(PILE_CONTAINER_ID + 1, Vector3.zero);

            Assert.AreEqual(FIRST_ID, first);
            Assert.AreEqual("pile-000002", second, "堆号要递增, 同一轮丢两处也不能撞号");
        }

        [Test]
        public void CreatePile_KeepsContainerIdAndAnchor()
        {
            Vector3 anchor = new Vector3(2.5f, 0f, -4f);

            string pileId = m_model.CreatePile(PILE_CONTAINER_ID, anchor);

            Assert.IsTrue(m_model.TryGet(pileId, out GroundPileVO stored));
            Assert.AreEqual(PILE_CONTAINER_ID, stored.ContainerId, "按 F 打开搜刮要靠容器号, 登记时丢了就接不上");
            Assert.AreEqual(pileId, stored.PileId);
            Assert.AreEqual(anchor, stored.AnchorPosition);
        }

        [Test]
        public void CreatePile_DuplicateContainer_ReportsErrorAndStoresNothing()
        {
            string first = m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);
            LogAssert.Expect(LogType.Error, new Regex("已经登记过堆"));

            Assert.IsNull(m_model.CreatePile(PILE_CONTAINER_ID, new Vector3(9f, 0f, 0f)),
                "同一只容器挂两座堆会让表现层对着同一份货反复生成");
            Assert.AreEqual(1, m_model.Piles.Count);
            Assert.AreEqual(first, m_model.Piles[0].PileId, "被拒的起堆不许动在册的堆");
            Assert.AreEqual(1, m_changedCount, "没起成功的东西不许广播变化");
        }

        [Test]
        public void TryGet_UnknownOrEmptyId_ReturnsFalse()
        {
            Assert.IsFalse(m_model.TryGet(null, out GroundPileVO nullId));
            Assert.IsFalse(m_model.TryGet(string.Empty, out GroundPileVO emptyId));
            Assert.IsFalse(m_model.TryGet("pile-999999", out GroundPileVO unknownId));
            Assert.IsNull(nullId);
            Assert.IsNull(emptyId);
            Assert.IsNull(unknownId);
        }

        [Test]
        public void TryGetByContainer_RegisteredContainer_ReturnsPile()
        {
            string pileId = m_model.CreatePile(PILE_CONTAINER_ID, Vector3.one);

            Assert.IsTrue(m_model.TryGetByContainer(PILE_CONTAINER_ID, out GroundPileVO pile));
            Assert.AreEqual(pileId, pile.PileId);
            Assert.IsFalse(m_model.TryGetByContainer(PILE_CONTAINER_ID + 5, out _), "没起过堆的容器不该被查出来");
        }
        #endregion

        #region 最近堆与范围查询
        [Test]
        public void TryFindNearest_TwoPilesInRadius_ReturnsTheCloser()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, new Vector3(3f, 0f, 0f));
            string nearer = m_model.CreatePile(PILE_CONTAINER_ID + 1, new Vector3(1f, 0f, 0f));

            Assert.IsTrue(m_model.TryFindNearest(Vector3.zero, 5f, out GroundPileVO pile));
            Assert.AreEqual(nearer, pile.PileId, "收拢丢弃物要并进最近那座, 不是先登记那座");
        }

        [Test]
        public void TryFindNearest_NothingInRadius_ReturnsFalse()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, new Vector3(0f, 0f, 4f));

            Assert.IsFalse(m_model.TryFindNearest(Vector3.zero, 1.5f, out _),
                "四米外不该被当成脚边那座, 否则整张图会合成一堆");
        }

        [Test]
        public void TryFindNearest_NegativeRadius_ReturnsFalse()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);

            Assert.IsFalse(m_model.TryFindNearest(Vector3.zero, -1f, out _), "负半径按查不到处理");
        }

        [Test]
        public void QueryNearby_PilesApart_ReturnsOnlyThoseInsideRadius()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, new Vector3(1f, 0f, 0f));
            m_model.CreatePile(PILE_CONTAINER_ID + 1, new Vector3(30f, 0f, 0f));

            IReadOnlyList<GroundPileVO> results = m_model.QueryNearby(Vector3.zero, 5f);

            Assert.AreEqual(1, results.Count, "三十米外那座不该出现在五米清单里");
            Assert.AreEqual(1f, results[0].AnchorPosition.x);
        }

        [Test]
        public void QueryNearby_PileExactlyOnRadius_IsIncluded()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, new Vector3(5f, 0f, 0f));

            Assert.AreEqual(1, m_model.QueryNearby(Vector3.zero, 5f).Count, "压线的堆按边界内处理");
        }

        [Test]
        public void QueryNearby_UnsortedRegistration_ReturnsNearestFirst()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, new Vector3(4f, 0f, 0f));
            m_model.CreatePile(PILE_CONTAINER_ID + 1, new Vector3(1f, 0f, 0f));
            m_model.CreatePile(PILE_CONTAINER_ID + 2, new Vector3(2f, 0f, 0f));

            IReadOnlyList<GroundPileVO> results = m_model.QueryNearby(Vector3.zero, 10f);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(1f, results[0].AnchorPosition.x);
            Assert.AreEqual(2f, results[1].AnchorPosition.x);
            Assert.AreEqual(4f, results[2].AnchorPosition.x, "按距离由近到远排, 玩家才不用翻整张清单");
        }

        [Test]
        public void QueryNearby_InvalidRadius_ReportsErrorAndReturnsNothing()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);
            LogAssert.Expect(LogType.Error, new Regex("地面堆查询半径无效"));

            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, -1f).Count);
        }

        [Test]
        public void QueryNearby_SnapshotStaysIntactAfterNextQuery()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);
            IReadOnlyList<GroundPileVO> first = m_model.QueryNearby(Vector3.zero, 1f);

            m_model.CreatePile(PILE_CONTAINER_ID + 1, Vector3.zero);
            IReadOnlyList<GroundPileVO> second = m_model.QueryNearby(Vector3.zero, 1f);

            Assert.AreEqual(1, first.Count, "上一次查到的清单不许被下一次查询清空");
            Assert.AreEqual(2, second.Count);
        }
        #endregion

        #region 销堆与广播
        [Test]
        public void RemovePile_PileDisappearsEverywhere()
        {
            string pileId = m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);

            m_model.RemovePile(pileId);

            Assert.IsFalse(m_model.TryGet(pileId, out _));
            Assert.IsFalse(m_model.TryGetByContainer(PILE_CONTAINER_ID, out _));
            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, 1000f).Count);
        }

        [Test]
        public void CreateAndRemove_BroadcastOnceEach()
        {
            string pileId = m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);
            Assert.AreEqual(1, m_changedCount);

            m_model.RemovePile(pileId);
            Assert.AreEqual(2, m_changedCount);
        }

        [Test]
        public void RemovePile_UnknownOrEmptyId_StaysSilent()
        {
            m_model.RemovePile("pile-999999");
            m_model.RemovePile(string.Empty);
            m_model.RemovePile(null);

            Assert.AreEqual(0, m_changedCount, "没动到东西就不该刷一遍场景实例");
        }

        [Test]
        public void OnUnregister_ClearsWholeTable()
        {
            m_model.CreatePile(PILE_CONTAINER_ID, Vector3.zero);
            m_model.CreatePile(PILE_CONTAINER_ID + 1, Vector3.zero);

            m_model.OnUnregister();

            Assert.AreEqual(0, m_model.Piles.Count, "换局要把上一局的堆清干净");
            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, 1000f).Count);
        }
        #endregion

        private void HandleChanged() => m_changedCount++;
    }
}
