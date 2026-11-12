/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱交互测试, 覆盖最近目标选择、可交互判定、实体号种子与交互层登记
* │  类    名: QInteractableTargetSelectorTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Interaction;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Inventory
{
    /// <summary>
    /// 物资箱交互测试：物理粗筛只圈候选, 选谁由交互体中心的平方距离决定, 可交互判定由组件自己把关
    /// </summary>
    public class QInteractableTargetSelectorTests
    {
        private const int INVALID_LAYER = -1;
        private const string EMPTY_ENTITY_ID_PATTERN = "实体号为空";
        private const string MISSING_LOOT_PATTERN = "未指定物资箱配置";
        private const string MISSING_GRID_PATTERN = "缺少网格定义";

        private QInventoryTestKit m_kit;
        private LootContainerConfigSO m_loot;
        private readonly List<LootBoxController> m_boxes = new List<LootBoxController>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_loot = m_kit.CreateLoot("测试物资箱", m_kit.CreateContainer(2, 3, "测试网格"), 1, 2);
            m_boxes.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = m_boxes.Count - 1; i >= 0; i--)
            {
                LootBoxController box = m_boxes[i];
                if (box == null) continue;

                UnityEngine.Object.DestroyImmediate(box.gameObject);
            }

            m_boxes.Clear();
            m_kit.Dispose();
        }

        #region 最近目标选择
        [Test]
        public void SelectNearest_PicksTheBoxWhoseCenterIsClosestToTheOrigin()
        {
            LootBoxController left = BuildBox("loot_left_01", new Vector3(-4f, 0f, 0f), m_loot);
            LootBoxController middle = BuildBox("loot_middle_01", new Vector3(0f, 0f, 0f), m_loot);
            LootBoxController right = BuildBox("loot_right_01", new Vector3(3f, 0f, 0f), m_loot);

            ILootInteractable picked = InteractableTargetSelector.SelectNearest(
                new[] { left, middle, right }, 3, new Vector3(2f, 0f, 0f));

            Assert.AreSame(right, picked, "原点在右侧, 应该选中 3 米处那只箱子");
        }

        [Test]
        public void SelectNearest_SkipsTheCloserBoxThatCannotInteract()
        {
            // 近处这只没配掉落表, 玩家对着它按 F 也不该打开一个空容器
            LootBoxController unusable = BuildBox("loot_broken_01", new Vector3(1f, 0f, 0f), null);
            LootBoxController usable = BuildBox("loot_usable_01", new Vector3(5f, 0f, 0f), m_loot);

            // 报错按到达顺序核对, 所以要等建箱噪音消化完再登记这一条
            ExpectConfigError(MISSING_LOOT_PATTERN);

            ILootInteractable picked = InteractableTargetSelector.SelectNearest(
                new[] { unusable, usable }, 2, Vector3.zero);

            Assert.AreSame(usable, picked, "最近的一只不可交互时必须顺延到次近的候选");
        }

        [Test]
        public void SelectNearest_ReturnsNullWhenEveryCandidateIsUnusable()
        {
            LootBoxController noEntityId = BuildBox(string.Empty, new Vector3(1f, 0f, 0f), m_loot);
            LootBoxController noLoot = BuildBox("loot_noloot_01", new Vector3(2f, 0f, 0f), null);

            ExpectConfigError(EMPTY_ENTITY_ID_PATTERN);
            ExpectConfigError(MISSING_LOOT_PATTERN);

            ILootInteractable picked = InteractableTargetSelector.SelectNearest(
                new[] { noEntityId, noLoot }, 2, Vector3.zero);

            Assert.IsNull(picked, "候选全都缺配置时不应该返回任何箱子");
        }

        [Test]
        public void SelectNearest_IgnoresArraySlotsBeyondTheValidCount()
        {
            LootBoxController valid = BuildBox("loot_valid_01", new Vector3(1f, 0f, 0f), m_loot);
            LootBoxController stale = BuildBox("loot_stale_01", new Vector3(0f, 0f, 9f), m_loot);

            // 复用缓冲里上一轮残留的引用不能被算进本轮结果, 所以只交给选择器 1 个有效项
            ILootInteractable picked = InteractableTargetSelector.SelectNearest(
                new[] { valid, stale, null, null }, 1, Vector3.zero);

            Assert.AreSame(valid, picked, "count 之外的数组槽位必须视为不存在");
        }

        [Test]
        public void SelectNearest_ReturnsNullWhenTheCandidateCountIsZero()
        {
            LootBoxController box = BuildBox("loot_ignore_01", Vector3.zero, m_loot);

            ILootInteractable picked = InteractableTargetSelector.SelectNearest(
                new[] { box }, 0, Vector3.zero);

            Assert.IsNull(picked, "零候选是正常情况, 返回 null 而不是异常");
        }

        [Test]
        public void SelectNearestOfType_SkipsNearerTargetsOfAnotherType()
        {
            GroundTarget pile = new GroundTarget(Vector3.zero);
            LootBoxController box = BuildBox("loot_typed_01", new Vector3(5f, 0f, 0f), m_loot);

            ILootInteractable picked = InteractableTargetSelector.SelectNearest<LootBoxController>(
                new ILootInteractable[] { pile, box }, 2, Vector3.zero);

            Assert.AreSame(box, picked, "F 只应该打开物资箱, 更近的地面堆必须被类型过滤掉");
        }

        [Test]
        public void SelectNearest_ThrowsWhenTheCandidateArrayIsNull()
        {
            Assert.Throws<ArgumentNullException>(
                () => InteractableTargetSelector.SelectNearest(null, 0, Vector3.zero));
        }

        [Test]
        public void SelectNearest_ThrowsWhenTheCountExceedsTheArrayLength()
        {
            LootBoxController[] candidates = { BuildBox("loot_only_01", Vector3.zero, m_loot) };

            Assert.Throws<ArgumentException>(
                () => InteractableTargetSelector.SelectNearest(candidates, 2, Vector3.zero),
                "有效数量超出数组长度属于调用方越界, 必须暴露而不是静默截断");
        }
        #endregion

        #region 可交互判定与种子
        [Test]
        public void CanInteract_RequiresEntityIdLootConfigAndGrid()
        {
            LootBoxController ready = BuildBox("loot_ready_01", Vector3.zero, m_loot);
            LootBoxController noEntityId = BuildBox(string.Empty, Vector3.zero, m_loot);
            LootBoxController noLoot = BuildBox("loot_noloot_02", Vector3.zero, null);
            LootBoxController noGrid = BuildBox("loot_nogrid_02", Vector3.zero,
                m_kit.CreateLoot("缺少网格的物资箱", null, 1, 2));

            ExpectConfigError(EMPTY_ENTITY_ID_PATTERN);
            ExpectConfigError(MISSING_LOOT_PATTERN);
            ExpectConfigError(MISSING_GRID_PATTERN);

            Assert.IsTrue(ready.CanInteract(), "配置齐备的箱子必须可交互");
            Assert.IsFalse(noEntityId.CanInteract(), "实体号为空无法定位容器, 不可交互");
            Assert.IsFalse(noLoot.CanInteract(), "没选掉落表不可交互");
            Assert.IsFalse(noGrid.CanInteract(), "掉落表没带网格定义不可交互");
        }

        [Test]
        public void ResolveSeed_RepeatsWithinOneEntityAndDiffersAcrossEntities()
        {
            LootBoxController boxA = BuildBox("loot_plastic_box_03_01", Vector3.zero, m_loot);
            LootBoxController boxAAgain = BuildBox("loot_plastic_box_03_01", Vector3.one, m_loot);
            LootBoxController boxB = BuildBox("loot_dumpster_01_01", Vector3.zero, m_loot);

            Assert.AreEqual(boxA.ResolveSeed(), boxAAgain.ResolveSeed(), "同一实体号必须摇出同一份货");
            Assert.AreNotEqual(boxA.ResolveSeed(), boxB.ResolveSeed(), "不同实体号应该开出不同掉落");
        }

        [Test]
        public void ResolveSeed_StaysInsideNonNegativeIntRange()
        {
            LootBoxController box = BuildBox("loot_valuable_case_02_07", Vector3.zero, m_loot);

            Assert.GreaterOrEqual(box.ResolveSeed(), 0, "种子要直接喂给抽取器, 不能是负数");
        }

        [Test]
        public void InteractionCenter_FallsBackToTheTransformPositionWithoutTheTrigger()
        {
            // 交互体在 Awake 才自建, 编辑模式 AddComponent 不触发 Awake, 这里锁定的就是退回坐标
            LootBoxController box = BuildBox("loot_fallback_01", new Vector3(2f, 1f, -3f), m_loot);

            Assert.AreEqual(new Vector3(2f, 1f, -3f), box.InteractionCenter,
                "交互体缺失时判距中心必须退回自身位置, 不能让选择器拿到空坐标");
        }
        #endregion

        #region 交互层
        [Test]
        public void ResolveInteractableLayer_IsRegisteredAndStableAcrossCalls()
        {
            int first = LootBoxController.ResolveInteractableLayer();
            int second = LootBoxController.ResolveInteractableLayer();

            Assert.Greater(first, INVALID_LAYER, "Layer 未在项目里登记时整条交互链路都会失效");
            Assert.AreEqual(first, second, "层序号会被缓存复用, 两次解析必须一致");
            Assert.AreEqual(LayerMask.NameToLayer("LootInteractable"), first, "解析结果必须就是物资箱交互层");
        }
        #endregion

        // 负面判定会走 QLog.Error, 测试框架把未登记的报错判成失败, 所以每条报错都要先预期一次
        private static void ExpectConfigError(string pattern)
        {
            LogAssert.Expect(LogType.Error, new Regex(pattern));
        }

        /*
         * 摆一只物资箱到指定位置。AddComponent 会当场触发一次 OnValidate, 那时实体号还是空串,
         * 这条报错属于编辑器噪音, 建箱时先预期掉。配置走反射写入, 避免 SerializedObject 再触发一轮校验,
         * 让每只箱子恒定只留一条建箱噪音。
         */
        private LootBoxController BuildBox(string entityId, Vector3 position, LootContainerConfigSO loot)
        {
            ExpectConfigError(EMPTY_ENTITY_ID_PATTERN);

            GameObject boxObject = new GameObject(string.IsNullOrEmpty(entityId) ? "LootBoxEmpty" : entityId);
            boxObject.transform.position = position;

            LootBoxController box = boxObject.AddComponent<LootBoxController>();
            SetValue(box, "EntityIdValue", entityId);
            SetValue(box, "LootConfigValue", loot);

            m_boxes.Add(box);
            return box;
        }

        // 按字段名写私有序列化字段, 字段名改了这里会直接判失败而不是静默跑空
        private static void SetValue(LootBoxController box, string fieldName, object value)
        {
            FieldInfo field = typeof(LootBoxController).GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (field == null)
            {
                Assert.Fail($"LootBoxController 缺少字段 {fieldName}, 测试与实现的字段名已经脱节");
                return;
            }

            field.SetValue(box, value);
        }

        // 用纯测试目标验证按类型过滤, 不依赖场景预制体或物理查询
        private sealed class GroundTarget : ILootInteractable
        {
            public GroundTarget(Vector3 interactionCenter)
            {
                InteractionCenter = interactionCenter;
            }

            public Vector3 InteractionCenter { get; }

            public bool CanInteract() => true;
        }
    }
}
