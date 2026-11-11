/*
* ┌──────────────────────────────────┐
* │  描    述: 地面物品登记表测试, 覆盖登记号分配、范围过滤、距离排序与注销广播
* │  类    名: QGroundItemModelTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config.Type;
using NUnit.Framework;
using UI.Inventory;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 地面物品登记表测试：模型只管「哪件东西落在哪」这份运行时状态, 范围查询要给出可直接渲染的有序快照
    /// </summary>
    public class QGroundItemModelTests
    {
        private const int GROUND_CONTAINER_ID = 9600;
        private const string FIRST_ID = "ground-000001";

        private GroundItemModel m_model;
        private int m_changedCount;

        [SetUp]
        public void SetUp()
        {
            //广播计数每次都要归零, 单靠字段初始值在复用测试实例时会串味
            m_changedCount = 0;

            m_model = new GroundItemModel();
            m_model.OnChanged += HandleChanged;
            m_model.OnRegister();
        }

        [TearDown]
        public void TearDown()
        {
            m_model.OnChanged -= HandleChanged;
            m_model.OnUnregister();
        }

        #region 登记与查询
        [Test]
        public void Register_FirstItem_GetsSequentialGroundId()
        {
            string first = m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), Vector3.zero);
            string second = m_model.Register(GROUND_CONTAINER_ID + 1, BuildItemVO(2), Vector3.zero);

            Assert.AreEqual(FIRST_ID, first);
            Assert.AreEqual("ground-000002", second, "登记号要递增, 同一次掉落两件也不能撞号");
        }

        [Test]
        public void Register_KeepsContainerIdAndItemSnapshot()
        {
            InventoryItemVO item = BuildItemVO(7);
            Vector3 landing = new Vector3(2.5f, 0.9f, -4f);

            string groundItemId = m_model.Register(GROUND_CONTAINER_ID, item, landing);

            Assert.IsTrue(m_model.TryGet(groundItemId, out GroundItemVO stored));
            Assert.AreEqual(GROUND_CONTAINER_ID, stored.ContainerId, "捡回背包要靠容器号做转移, 登记时丢了就捡不回来");
            Assert.AreSame(item, stored.Item);
            Assert.AreEqual(groundItemId, stored.GroundItemId);
            Assert.AreEqual(landing, stored.WorldPosition);
        }

        [Test]
        public void Register_NullItem_ReportsErrorAndStoresNothing()
        {
            LogAssert.Expect(LogType.Error, new Regex("登记地面物品缺少物品快照"));

            Assert.IsNull(m_model.Register(GROUND_CONTAINER_ID, null, Vector3.zero));
            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, 1000f).Count);
            Assert.AreEqual(0, m_changedCount, "没登记成功的东西不许广播变化");
        }

        [Test]
        public void TryGet_UnknownOrEmptyId_ReturnsFalse()
        {
            Assert.IsFalse(m_model.TryGet(null, out GroundItemVO nullId));
            Assert.IsFalse(m_model.TryGet(string.Empty, out GroundItemVO emptyId));
            Assert.IsFalse(m_model.TryGet("ground-999999", out GroundItemVO unknownId));
            Assert.IsNull(nullId);
            Assert.IsNull(emptyId);
            Assert.IsNull(unknownId);
        }
        #endregion

        #region 范围查询
        [Test]
        public void QueryNearby_ItemsApart_ReturnsOnlyThoseInsideRadius()
        {
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), new Vector3(1f, 0f, 0f));
            m_model.Register(GROUND_CONTAINER_ID + 1, BuildItemVO(2), new Vector3(30f, 0f, 0f));

            IReadOnlyList<GroundItemVO> results = m_model.QueryNearby(Vector3.zero, 5f);

            Assert.AreEqual(1, results.Count, "三十米外的那件不该出现在五米清单里");
            Assert.AreEqual(1, results[0].Item.InstanceId);
        }

        [Test]
        public void QueryNearby_ItemExactlyOnRadius_IsIncluded()
        {
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), new Vector3(5f, 0f, 0f));

            Assert.AreEqual(1, m_model.QueryNearby(Vector3.zero, 5f).Count, "压线的东西按边界内处理");
        }

        [Test]
        public void QueryNearby_UnsortedRegistration_ReturnsNearestFirst()
        {
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(3), new Vector3(4f, 0f, 0f));
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), new Vector3(1f, 0f, 0f));
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(2), new Vector3(2f, 0f, 0f));

            IReadOnlyList<GroundItemVO> results = m_model.QueryNearby(Vector3.zero, 10f);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual(1, results[0].Item.InstanceId);
            Assert.AreEqual(2, results[1].Item.InstanceId);
            Assert.AreEqual(3, results[2].Item.InstanceId, "按距离由近到远排, 玩家才不用翻整张清单");
        }

        [Test]
        public void QueryNearby_InvalidRadius_ReportsErrorAndReturnsNothing()
        {
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), Vector3.zero);

            LogAssert.Expect(LogType.Error, new Regex("地面物品查询半径无效"));

            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, -1f).Count);
        }

        [Test]
        public void QueryNearby_SnapshotStaysIntactAfterNextQuery()
        {
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), Vector3.zero);
            IReadOnlyList<GroundItemVO> first = m_model.QueryNearby(Vector3.zero, 1f);

            m_model.Register(GROUND_CONTAINER_ID + 1, BuildItemVO(2), Vector3.zero);
            IReadOnlyList<GroundItemVO> second = m_model.QueryNearby(Vector3.zero, 1f);

            Assert.AreEqual(1, first.Count, "上一次查到的清单不许被下一次查询清空");
            Assert.AreEqual(2, second.Count);
        }
        #endregion

        #region 注销与广播
        [Test]
        public void Unregister_ItemDisappearsEverywhere()
        {
            string groundItemId = m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), Vector3.zero);
            m_model.Unregister(groundItemId);

            Assert.IsFalse(m_model.TryGet(groundItemId, out _));
            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, 1000f).Count);
        }

        [Test]
        public void RegisterAndUnregister_BroadcastOnceEach()
        {
            string groundItemId = m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), Vector3.zero);
            Assert.AreEqual(1, m_changedCount);

            m_model.Unregister(groundItemId);
            Assert.AreEqual(2, m_changedCount);
        }

        [Test]
        public void Unregister_UnknownOrEmptyId_StaysSilent()
        {
            m_model.Unregister("ground-999999");
            m_model.Unregister(string.Empty);
            m_model.Unregister(null);

            Assert.AreEqual(0, m_changedCount, "没动到东西就不该刷一遍界面");
        }

        [Test]
        public void OnUnregister_ClearsWholeTable()
        {
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(1), Vector3.zero);
            m_model.Register(GROUND_CONTAINER_ID, BuildItemVO(2), Vector3.zero);

            m_model.OnUnregister();

            Assert.AreEqual(0, m_model.QueryNearby(Vector3.zero, 1000f).Count, "换局要把上一局的地面物品清干净");
        }
        #endregion

        // 造一件最小可用的物品快照, 地面模型只认引用不认配置
        private static InventoryItemVO BuildItemVO(int instanceId)
        {
            return new InventoryItemVO(instanceId, 90000 + instanceId, "TEST_" + instanceId, 1, null, Color.white,
                InventoryItemRarity.None, 0, Vector2Int.zero, Vector2Int.one, InventoryRotation.None);
        }

        private void HandleChanged() => m_changedCount++;
    }
}
