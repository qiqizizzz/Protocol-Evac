/*
* ┌──────────────────────────────────┐
* │  描    述: 物资箱交互测试, 覆盖目标选择、物理视线、可交互判定、实体号种子与交互层登记
* │  类    名: QInteractableTargetSelectorTests.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Framework.QTower.Common.Defines;
using Framework.QTower.Event;
using Module.Inventory.Core.Config.Loot;
using Module.Inventory.Interaction;
using Module.Inventory.Presentation;
using NUnit.Framework;
using UI.Inventory;
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
        private readonly List<GameObject> m_scanObjects = new List<GameObject>();

        [SetUp]
        public void SetUp()
        {
            m_kit = new QInventoryTestKit();
            m_loot = m_kit.CreateLoot("测试物资箱", m_kit.CreateContainer(2, 3, "测试网格"), 1, 2);
            m_boxes.Clear();
            m_scanObjects.Clear();
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

            for (int i = m_scanObjects.Count - 1; i >= 0; i--)
            {
                GameObject scanObject = m_scanObjects[i];
                if (scanObject == null) continue;

                UnityEngine.Object.DestroyImmediate(scanObject);
            }

            m_scanObjects.Clear();
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

        #region 配置化目标选择
        [Test]
        public void SelectBest_PrefersTheAlignedBoxOverTheSlightlyCloserEdgeBox()
        {
            GroundTarget edge = new GroundTarget(new Vector3(0.9f, 0f, 1.07f));
            GroundTarget aligned = new GroundTarget(new Vector3(0f, 0f, 1.6f));

            ILootInteractable picked = InteractableTargetSelector.SelectBest(
                new ILootInteractable[] { edge, aligned }, 2, Vector3.zero, Vector3.forward,
                CreateLootBoxQuery(), null);

            Assert.AreSame(aligned, picked, "准星正前方的箱子应比更近但偏在视野边缘的箱子优先");
        }

        [Test]
        public void SelectBest_RejectsTheBoxOutsideTheConfiguredDistance()
        {
            GroundTarget tooFar = new GroundTarget(new Vector3(0f, 0f, 2.6f));

            ILootInteractable picked = InteractableTargetSelector.SelectBest(
                new ILootInteractable[] { tooFar }, 1, Vector3.zero, Vector3.forward,
                CreateLootBoxQuery(), null);

            Assert.IsNull(picked, "超过最大交互距离的箱子不应被选中");
        }

        [Test]
        public void SelectBest_RejectsTheBoxOutsideTheConfiguredAngle()
        {
            float angle = 50f * Mathf.Deg2Rad;
            GroundTarget tooWide = new GroundTarget(new Vector3(Mathf.Sin(angle) * 1.6f, 0f,
                Mathf.Cos(angle) * 1.6f));

            ILootInteractable picked = InteractableTargetSelector.SelectBest(
                new ILootInteractable[] { tooWide }, 1, Vector3.zero, Vector3.forward,
                CreateLootBoxQuery(), null);

            Assert.IsNull(picked, "超过最大夹角的箱子不应被选中");
        }

        [Test]
        public void SelectBest_AllowsTheNearbyBoxBehindThePlayer()
        {
            GroundTarget behind = new GroundTarget(new Vector3(0f, 0f, -1f));

            ILootInteractable picked = InteractableTargetSelector.SelectBest(
                new ILootInteractable[] { behind }, 1, Vector3.zero, Vector3.forward,
                CreateLootBoxQuery(), null);

            Assert.AreSame(behind, picked, "近身兜底距离内应允许背向交互");
        }

        [Test]
        public void SelectBest_PrefersTheAlignedFrontBoxOverTheCloserBoxBehind()
        {
            GroundTarget behind = new GroundTarget(new Vector3(0f, 0f, -1f));
            GroundTarget aligned = new GroundTarget(new Vector3(0f, 0f, 1.6f));

            ILootInteractable picked = InteractableTargetSelector.SelectBest(
                new ILootInteractable[] { behind, aligned }, 2, Vector3.zero, Vector3.forward,
                CreateLootBoxQuery(), null);

            Assert.AreSame(aligned, picked, "近身兜底不能忽略视角评分, 正前方目标仍应优先");
        }

        [Test]
        public void SelectBest_SkipsTheCloserBoxWhenVisibilityFails()
        {
            GroundTarget blocked = new GroundTarget(new Vector3(0f, 0f, 1.2f));
            GroundTarget visible = new GroundTarget(new Vector3(0f, 0f, 1.8f));

            ILootInteractable picked = InteractableTargetSelector.SelectBest(
                new ILootInteractable[] { blocked, visible }, 2, Vector3.zero, Vector3.forward,
                CreateLootBoxQuery(), target => target != blocked);

            Assert.AreSame(visible, picked, "视线被挡住的目标必须跳过并继续选择次优目标");
        }
        #endregion

        #region 面板交互门禁
        [Test]
        public void TryInteract_WhenContainerPanelIsVisible_SkipsScanningAndHint()
        {
            bool hintRequested = false;
            Action<string> captureHint = _ => hintRequested = true;
            EventManager.RegisterEvent(EventDefines.InteractionHintRequested, captureHint);

            try
            {
                LootBoxInteraction interaction = new LootBoxInteraction(() => true);

                bool opened = interaction.TryInteract(
                    Vector3.zero, Vector3.forward, CreateLootBoxQuery(), 0);

                Assert.IsFalse(opened);
                Assert.IsFalse(hintRequested);
            }
            finally
            {
                EventManager.UnregisterEvent(EventDefines.InteractionHintRequested, captureHint);
            }
        }

        [Test]
        public void TryInteractGroundPile_WhenContainerPanelIsVisible_SkipsScanning()
        {
            BuildGroundPile(1, Vector3.zero);
            Physics.SyncTransforms();

            LootBoxInteraction interaction = new LootBoxInteraction(() => true);

            bool opened = interaction.TryInteractGroundPile(Vector3.zero);

            Assert.IsFalse(opened, "面板已显示时 H 不能继续扫描并替换当前容器");
            LogAssert.NoUnexpectedReceived();
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

        [Test]
        public void InteractionBounds_UsesRenderedBoxInsteadOfTheExpandedInteractTrigger()
        {
            ExpectConfigError(EMPTY_ENTITY_ID_PATTERN);

            GameObject boxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObject.name = "loot_bounds_01";
            boxObject.transform.position = new Vector3(0f, 1f, 0f);

            LootBoxController box = boxObject.AddComponent<LootBoxController>();
            SetValue(box, "EntityIdValue", "loot_bounds_01");
            SetValue(box, "LootConfigValue", m_loot);
            m_boxes.Add(box);

            InvokePrivate(box, "BuildInteractTrigger");
            Physics.SyncTransforms();

            Transform triggerTransform = box.transform.Find("LootInteractTrigger");
            BoxCollider trigger = triggerTransform.GetComponent<BoxCollider>();

            Assert.AreEqual(Vector3.one, box.InteractionBounds.size,
                "遮挡采样必须使用真实渲染包围盒, 不能使用向外扩展的交互触发体");
            Assert.Greater(trigger.bounds.size.x, box.InteractionBounds.size.x,
                "扩展后的触发体仍应保留, 只负责物理粗筛");
        }
        #endregion

        #region 物理视线
        [Test]
        public void ScanBest_AcceptsTheTargetWhenOnlyTheUpperSampleClearsALowCover()
        {
            ScanningTarget target = BuildScanningTarget("loot_upper_clear_01",
                new Vector3(0f, 1.5f, 2.2f), new Vector3(1f, 2f, 1f));
            BuildObstacle("low_cover", new Vector3(0f, 0.65f, 1f), new Vector3(2f, 1.3f, 0.1f), 8);
            Physics.SyncTransforms();

            LootInteractableScanner scanner = new LootInteractableScanner();
            ILootInteractable picked = scanner.ScanBest<ScanningTarget>(new Vector3(0f, 1f, 0f), Vector3.forward,
                new InteractionQueryConfig(2.5f, 45f, 0f, 0.4f, 0.6f), 1 << 8);

            Assert.AreSame(target, picked, "中心点被半高掩体挡住时, 上沿采样通畅仍应允许交互");
        }

        [Test]
        public void ScanBest_IgnoresTheTargetOwnColliderDuringLineOfSight()
        {
            ScanningTarget target = BuildScanningTarget("loot_self_collider_01",
                new Vector3(0f, 0f, 2f), new Vector3(1f, 0.5f, 1f));
            Physics.SyncTransforms();

            int interactionLayer = LayerMask.NameToLayer("LootInteractable");
            LootInteractableScanner scanner = new LootInteractableScanner();
            ILootInteractable picked = scanner.ScanBest<ScanningTarget>(Vector3.zero, Vector3.forward,
                new InteractionQueryConfig(2.5f, 45f, 0f, 0.4f, 0.6f), 1 << interactionLayer);

            Assert.AreSame(target, picked, "物资箱自身及子级碰撞体不能把自己的视线挡掉");
        }
        #endregion

        #region 高亮
        [Test]
        public void HighlightController_MarksFocusedTargetAndKeepsOutsideInteractionRangeAvailable()
        {
            LootBoxController focused = BuildPhysicalBox("loot_highlight_focus_01", new Vector3(0f, 1f, 1.5f));
            LootBoxController nearby = BuildPhysicalBox("loot_highlight_nearby_01", new Vector3(4f, 1f, 0f));
            Physics.SyncTransforms();

            LootBoxHighlightController highlighter = new LootBoxHighlightController();
            LootBoxController focusedBox =
                highlighter.Refresh(new Vector3(0f, 1f, 0f), Vector3.forward, CreateLootBoxQuery(), 0, 12f);

            LootBoxHighlightView focusedView = focused.GetComponent<LootBoxHighlightView>();
            LootBoxHighlightView nearbyView = nearby.GetComponent<LootBoxHighlightView>();

            Assert.AreEqual(LootBoxHighlightView.HighlightState.Focused, focusedView.State,
                "当前 F 目标必须使用强描边");
            Assert.AreEqual(LootBoxHighlightView.HighlightState.Available, nearbyView.State,
                "超出 F 交互范围但仍在提示范围内的箱子必须保留弱描边");
            Assert.AreSame(focused, focusedBox, "高亮控制器必须把当前聚焦物资箱返回给 HUD");
        }

        [Test]
        public void HighlightController_HidesBoxesAfterLeavingTheHighlightRadius()
        {
            LootBoxController box = BuildPhysicalBox("loot_highlight_leave_01", new Vector3(0f, 1f, 1.5f));
            Physics.SyncTransforms();

            LootBoxHighlightController highlighter = new LootBoxHighlightController();
            highlighter.Refresh(new Vector3(0f, 1f, 0f), Vector3.forward, CreateLootBoxQuery(), 0, 12f);

            LootBoxHighlightView view = box.GetComponent<LootBoxHighlightView>();
            Assert.AreEqual(LootBoxHighlightView.HighlightState.Focused, view.State);

            highlighter.Refresh(new Vector3(30f, 1f, 30f), Vector3.forward, CreateLootBoxQuery(), 0, 12f);
            Assert.AreEqual(LootBoxHighlightView.HighlightState.None, view.State,
                "离开可开启提示范围后必须关闭描边");
        }

        [Test]
        public void HighlightView_CreatesOutlineCopyWithTheDedicatedShader()
        {
            GameObject boxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObject.name = "loot_highlight_mesh_01";
            LootBoxHighlightView view = boxObject.AddComponent<LootBoxHighlightView>();
            m_scanObjects.Add(boxObject);

            view.SetState(LootBoxHighlightView.HighlightState.Available);

            MeshRenderer[] renderers = boxObject.GetComponentsInChildren<MeshRenderer>(true);
            MeshRenderer outlineRenderer = null;
            for (int i = 0; i < renderers.Length; i++)
            {
                if (!renderers[i].gameObject.name.StartsWith("LootHighlightOutline")) continue;

                outlineRenderer = renderers[i];
                break;
            }

            Assert.IsNotNull(outlineRenderer, "启用高亮时必须生成独立描边网格副本");
            Assert.AreEqual("Protocol_Evac/FX/LootInteractableOutline", outlineRenderer.sharedMaterial.shader.name,
                "描边副本必须使用物资箱专用 URP Shader");
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

        // 物资箱交互参数使用正式设计值, 测试不复制生产默认值来源
        private static InteractionQueryConfig CreateLootBoxQuery()
        {
            return new InteractionQueryConfig(2.5f, 45f, 1.2f, 0.4f, 0.6f);
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

        // 建一只带真实渲染器与交互触发体的箱子, 用于物理扫描与高亮测试
        private LootBoxController BuildPhysicalBox(string entityId, Vector3 position)
        {
            ExpectConfigError(EMPTY_ENTITY_ID_PATTERN);

            GameObject boxObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            boxObject.name = entityId;
            boxObject.transform.position = position;

            LootBoxController box = boxObject.AddComponent<LootBoxController>();
            SetValue(box, "EntityIdValue", entityId);
            SetValue(box, "LootConfigValue", m_loot);
            InvokePrivate(box, "BuildInteractTrigger");

            m_boxes.Add(box);
            return box;
        }

        // 建一只不依赖掉落配置的物理扫描目标, 只验证候选收集与视线
        private ScanningTarget BuildScanningTarget(string objectName, Vector3 center, Vector3 boundsSize)
        {
            GameObject targetObject = new GameObject(objectName);
            targetObject.transform.position = center;
            targetObject.layer = LayerMask.NameToLayer("LootInteractable");

            BoxCollider targetCollider = targetObject.AddComponent<BoxCollider>();
            targetCollider.size = Vector3.one;

            ScanningTarget target = targetObject.AddComponent<ScanningTarget>();
            target.Configure(new Bounds(center, boundsSize));
            m_scanObjects.Add(targetObject);
            return target;
        }

        // 摆一座可被物理粗筛命中的地面堆, 面板门禁必须在选择容器前截断
        private GroundPileView BuildGroundPile(int containerId, Vector3 position)
        {
            GameObject pileObject = new GameObject("ground_pile_gate_01");
            pileObject.transform.position = position;
            pileObject.layer = LayerMask.NameToLayer("LootInteractable");
            pileObject.AddComponent<BoxCollider>();

            GroundPileView pile = pileObject.AddComponent<GroundPileView>();
            pile.Configure(containerId, 2, 2);
            m_scanObjects.Add(pileObject);
            return pile;
        }

        // 建一块指定层的遮挡体, 层号与正式环境层保持一致
        private void BuildObstacle(string objectName, Vector3 center, Vector3 size, int layer)
        {
            GameObject obstacle = new GameObject(objectName);
            obstacle.transform.position = center;
            obstacle.layer = layer;

            BoxCollider obstacleCollider = obstacle.AddComponent<BoxCollider>();
            obstacleCollider.size = size;
            m_scanObjects.Add(obstacle);
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

        // 编辑模式不会自动触发 Awake, 测试显式调用构建触发体以覆盖真实范围与扩展范围的分工
        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (method == null)
            {
                Assert.Fail($"{target.GetType().Name} 缺少方法 {methodName}, 测试与实现的调用关系已经脱节");
                return;
            }

            method.Invoke(target, null);
        }

        // 用纯测试目标验证按类型过滤, 不依赖场景预制体或物理查询
        private sealed class GroundTarget : ILootInteractable
        {
            public GroundTarget(Vector3 interactionCenter)
            {
                InteractionCenter = interactionCenter;
                InteractionBounds = new Bounds(interactionCenter, Vector3.one);
            }

            public Vector3 InteractionCenter { get; }

            public Bounds InteractionBounds { get; }

            public bool CanInteract() => true;
        }

        // 物理扫描测试目标, 只暴露可调包围盒与恒真的可交互判定
        private sealed class ScanningTarget : MonoBehaviour, ILootInteractable
        {
            private Bounds m_bounds;

            public void Configure(Bounds bounds)
            {
                m_bounds = bounds;
            }

            public Vector3 InteractionCenter => m_bounds.center;

            public Bounds InteractionBounds => m_bounds;

            public bool CanInteract() => true;
        }
    }
}
