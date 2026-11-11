# 搜刮与丢弃闭环实施计划

> 状态：待确认，尚未修改业务代码
> 范围：物资箱搜刮、右侧动态容器、物品丢弃、地面物品渲染、H 附近物品列表
> 暂不做：死者搜刮、网络同步、命令过期、服务器权威、玩家背包布局重构

**Goal:** 完成“靠近物资箱 -> 打开右侧物资容器 -> 转移物品 -> 右键或双击丢弃 -> 地面保留并显示物品 -> 关闭背包后按 H 查看附近物品”的单机闭环。

**Architecture:** 继续使用现有 MVVM 背包链路。固定装备槽和玩家容量载体不改，物资箱作为 `EquipmentPanelViewModel` 的独立动态右侧载体；地面物品使用独立运行时模型保存状态，世界视图只负责渲染和物理表现；丢弃通过临时地面容器转移，避免直接销毁物品实例。

**Tech Stack:** Unity、C#、现有 QTower MVVM、Unity Input System、NUnit EditMode、UniTask、Addressables、现有 InventoryContainer/InventoryViewModel

**Spec:** 物资箱显示在装备界面右侧；关闭界面后残货保留；右键菜单新增“丢弃”；左键双击也能丢弃；丢弃保留实例号、数量和朝向；地面物品可见；关闭背包后 H 显示附近地面物品；物理抛出和弹跳放在最后，不阻塞主流程。

## Global Constraints

- 不修改玩家现有背包、胸挂、口袋、安全箱和装备槽布局。
- 不把动态物资箱加入 `UIEquipmentPanel.Containers[]`，该数组只承载固定角色容器。
- 鼠标左右键继续走 UI `EventSystem`，不加入 ShootingPlayer Input Actions。
- `F` 只负责靠近物资箱交互，`H` 只负责查看附近地面物品。
- `H` 与现有 GM Console 冲突，GM Console 改为 `F1`。
- 不接网络，不新增命令过期、规则层命令契约或服务器回滚。
- 第一版整堆丢弃，不做部分丢弃。
- 丢弃必须经过临时地面容器，禁止直接从源容器删除。
- 装备槽中的物品按 `InventoryItemConfigSO.GetSizeAt(item.Rotation)` 计算地面占格，不能沿用装备槽的 `1x1` 表现尺寸。
- 关闭物资箱 UI 只销毁右侧动态视图模型，不注销模型容器；再次打开同一实体必须保留残货。
- 所有新增 `.cs` 必须带项目规定的文件头注释、方法注释和字段命名。
- 运行期日志使用项目既有 `QLog`，不要新增 Release 日志。
- 禁止运行 `Tools/背包7a/* BuildAll` 或菜单 `7`，避免覆盖用户手工调整过的 Prefab。

## Review Focus

- 动态物资箱视图模型的生命周期是否只销毁 VM，不误注销模型容器。
- 同配置的两个物资箱是否按实体 ID 隔离，绝不串货。
- 丢弃失败时原物品是否仍在源容器，且没有残留临时地面容器。
- 装备槽物品丢到地面时是否使用真实物品占格。
- 右键丢弃与双击丢弃是否调用同一条服务链路。
- H 列表是否只在背包关闭时打开，且不抢 GM Console。
- 地面物品模型与渲染是否解耦，后续接网络时不需要重写 UI。

## Current Facts

- 物资箱容器段：`InventoryContainerIds.LOOT = 20`。
- 物资箱缓存已改成实体号键：`Dictionary<string, LootContainerRecord>`，同配置多实体不再串货。
- 物资箱入口已改为 `int OpenLootContainer(string entityId, LootContainerConfigSO loot, int seed)`，无旧重载。
- 运行时容器号分配已收口到 `int CreateRuntimeContainer(InventoryContainerSpec spec)`。
- `Inventory.asmdef` 只引用 `Utils.asmdef`，asmdef 无法反向引用 Assembly-CSharp，这条边界决定脚本能放哪。
- 右侧容量区路径：`UIEquipmentPanel/Layout_Storage/ScrollView_Capacity/Viewport/Content`。
- 动态载体模板：`Assets/Prefabs/UI/Inventory/UIInventoryCarrier.prefab`。
- 右键菜单节点：`Panel_Menu/Btn_Rotate`、`Panel_Menu/Btn_Split`。
- 当前 `UIInventoryItem.OnPointerClick()` 只处理左键详情和右键菜单。
- 当前 `GameManager.Update()` 占用 `H` 打开 GM Console。
- 现有物资箱配置位于 `Assets/Config/Inventory/Loot/`。

T3 与 T4 落地的新事实：

- 丢弃入口是 `IInventoryModel.TryDropItem(sourceContainerId, instanceId, out groundContainerId)`，写在 `InventoryModel` 里，不建 Service 层，返回值直接复用 `InventoryMoveResult`。
- 丢弃新建的地面容器与物资箱共用 `CreateRuntimeContainer` 的运行时号段，显示名为「地面」，容器尺寸按 `Config.GetSizeAt(placement.Rotation)` 取真实占格。
- 地面物品登记表契约在 `UI.Inventory.IGroundItemModel`，实现 `GroundItemModel` 已由 `InventoryModule.Register` 注册，登记号形如 `ground-000001`。
- 世界坐标不进 `InventoryModel`：丢弃成功后由调用方拿 `GetContainerVO(groundId).Items[0]` 去 `IGroundItemModel.Register`。

## 目标流程

1. 玩家靠近物资箱，按 `F`。
2. 打开装备面板，并把该实体对应的物资箱容器显示在右侧容量区底部。
3. 玩家把物资箱物品拖入背包，或把背包物品拖入物资箱。
4. 右键物品选择“丢弃”，或在物品上左键双击。
5. 物品从源容器转移到临时地面容器，地面运行时模型登记物品。
6. 地面视图生成可见物品，可选地给刚体初速度并产生弹跳。
7. 关闭背包后按 `H`，显示附近地面物品的图标、名称、数量和距离。

---

## 文件结构

按职责与领域分目录，不设 `World/` 这类语义宽泛的杂项层；`UI/` 只放 2D 界面。

```text
Assets/Scripts/Module/Inventory/                [asmdef: Inventory，只引用 Utils]
|-- Interaction/LootBoxController.cs            场景 Override 标记组件，Awake 自建根触发体与配置校验 T6
|-- Interaction/LootInteractableScanner.cs      OverlapSphereNonAlloc 一次性粗筛 + TryGetComponent  T6
|-- Interaction/InteractableTargetSelector.cs   纯静态“选最近”平方距离，不依赖场景，EditMode 可测    T6
`-- Presentation/GroundItemView.cs              3D 表现：出生、billboard、抛物线、弹跳              T8 T10

Assets/Scripts/UI/Inventory/                    [Assembly-CSharp，能碰 IInventoryModel 与 MvvmRoot]
|-- LootBoxInteraction.cs                       按 F 的桥接：扫描 -> OpenLootContainer -> ShowAsync  T6
|-- GroundItemBinders.cs                        订阅地面物品事件，取图标与预制体喂给 GroundItemView   T8
|-- Model/Interfaces/IGroundItemModel.cs        地面物品登记表契约                                  T3
|-- Model/Interfaces/IInventoryModel.cs         丢弃入口 TryDropItem 的契约位置                      T4
|-- Model/InventoryModel.cs                     TryDropItem 落在这里，不开 Service 层                T4
|-- Model/GroundItemModel.cs                    登记表实现，只暴露数据与 event                       T3
|-- VO/GroundItemVO.cs                          地面物品快照：登记号+容器号+物品+落点+时刻          T3
|-- ViewModel/GroundItemPanelViewModel.cs       附近物品列表面板 VM                                  T9
`-- View/UIGroundItemPanel.cs                   附近物品 2D 列表面板                                 T9

Assets/Prefabs/
|-- UI/Inventory/UIGroundItemPanel.prefab       2D 列表面板                                          T9
`-- Loot/GroundItemView.prefab                  丢弃物世界预制体：Mesh+图标+Rigidbody                T8

Assets/Scripts/Tests/Inventory/Editor/
|-- QInventoryLootTests.cs                      已补运行时分配与实体隔离用例                         T1 T2
|-- QInventoryDropTests.cs                      原子丢弃与失败回滚                                   T4
|-- QGroundItemModelTests.cs                    登记表增删查                                         T3
`-- QInteractableTargetSelectorTests.cs         最近目标选择                                         T6
```

`GroundItemView` 下沉进 `Module/Inventory/Presentation/` 的代价是它读不到 QTower 的 `ResManager`，
所以图标与世界预制体由 Assembly-CSharp 侧的 `GroundItemBinders` 注入；这条接缝将来换成服务器下发的
表现指令也不用改表现层。

---

## Task 1：补齐运行时容器分配

已完成，落地方式与计划一致：`CreateRuntimeContainer` 从 `LOOT` 段找第一个空闲号并注册，
`OpenLootContainer` 与后续地面容器共用这一入口；规格宽高非正时报错并返回负值。

**Files**

- Modify: `Assets/Scripts/UI/Inventory/Model/Interfaces/IInventoryModel.cs`
- Modify: `Assets/Scripts/UI/Inventory/Model/InventoryModel.cs`
- Modify: `Assets/Scripts/Tests/Inventory/Editor/QInventoryLootTests.cs`

**Consumes**

- 现有 `RegisterContainer(int, InventoryContainerSpec)`
- 现有 `InventoryContainerIds.LOOT`
- 现有 `UnregisterContainer(int)`

**Produces**

```csharp
int CreateRuntimeContainer(InventoryContainerSpec spec);
```

行为：

- 从 `InventoryContainerIds.LOOT` 起寻找第一个未注册容器号。
- 注册成功后立即返回新容器号。
- `spec` 为空或注册失败时返回负值。
- 该接口供物资箱和临时地面容器共用，避免两套 ID 分配器碰撞。

**TDD**

1. 在 `QInventoryLootTests.cs` 增加 `CreateRuntimeContainer_AllocatesUniqueLootRangeIds`。
2. 连续创建两个容器，断言容器号不同、均大于等于 `LOOT`、两个容器都已注册。
3. 注销第一个后再创建，断言新容器号可复用且不与仍注册的第二个冲突。
4. 先运行测试，确认当前因接口不存在而失败。

**Implementation**

1. 在接口加入 `CreateRuntimeContainer`。
2. 把现有 `AllocateLootContainerId()` 提取为通用分配逻辑。
3. `OpenLootContainer` 和后续地面容器都调用同一分配入口。
4. 不改变现有 `OpenLootContainer` 行为。

**Verification**

```powershell
unity-mcp-cli run-tool tests-run --input '{"testMode":"EditMode","testAssembly":"Assembly-CSharp-Editor-testable","includePassingTests":false,"includeMessages":true,"includeStacktrace":false}'
```

**Commit**

```text
feat(inventory): add runtime container allocation
```

---

## Task 2：按物资箱实体隔离缓存

已完成，与计划有两处出入：

- 不保留 `OpenLootContainer(LootContainerConfigSO, int)` 旧重载，工程内除测试外没有调用方，
  留着等于把串货路径留在代码里，13 处测试调用一起迁移。
- 缓存值从裸容器号升成 `LootContainerRecord`（实体号+容器号+开箱时的掉落配置），
  顺带补两条防线：同一只箱子开着时换掉落表直接报错拒开；`TryResizeContainer` 走注销重注册时
  把记录摘出来再按新容器号挂回，避免换尺寸把物资箱身份洗掉。

**Files**

- Modify: `Assets/Scripts/UI/Inventory/Model/Interfaces/IInventoryModel.cs`
- Modify: `Assets/Scripts/UI/Inventory/Model/InventoryModel.cs`
- Modify: `Assets/Scripts/Tests/Inventory/Editor/QInventoryLootTests.cs`
- Modify: `Assets/Scripts/Module/Inventory/Core/Defines/InventoryContainerIds.cs`，仅当需要补充注释时

**Consumes**

- Task 1 的 `CreateRuntimeContainer`
- 现有 `LootContainerConfigSO`
- 现有 `LootRoller`

**Produces**

```csharp
int OpenLootContainer(string entityId, LootContainerConfigSO loot, int seed);
```

行为：

- 同一 `entityId` 重复打开返回原容器号，不重新摇奖。
- 不同 `entityId` 即使使用同一个 `LootContainerConfigSO`，也各自拥有独立容器。
- 保留旧 `OpenLootContainer(LootContainerConfigSO, int)` 重载，现有测试和临时调试不立即破坏。
- 空 `entityId` 或空配置返回负值。

**TDD**

1. 增加 `OpenLootContainer_TwoEntitiesWithSameConfig_DoNotShareContainer`。
2. 用同一配置、不同实体 ID 打开两个容器，断言容器号不同。
3. 修改第一个容器内容，断言第二个容器不受影响。
4. 增加 `OpenLootContainer_SameEntity_ReturnsCachedContainer`，重复打开断言容器号和内容不变。
5. 先运行测试，确认当前同配置串货测试失败。

**Implementation**

1. 新增 `Dictionary<string, int> m_lootEntityContainers`。
2. 新重载先查实体缓存，再创建容器并填充掉落。
3. 保留原字典仅服务旧重载，避免破坏既有测试语义。
4. 容器注销时同时清理实体缓存。

**Verification**

- 运行 `QInventoryLootTests`。
- 运行完整 EditMode 背包测试。

**Commit**

```text
fix(inventory): isolate loot containers by entity id
```

---

## Task 3：建立地面物品运行时模型

**状态：已完成（2026-11-11）**

**Files**

- Add: `Assets/Scripts/UI/Inventory/VO/GroundItemVO.cs`
- Add: `Assets/Scripts/UI/Inventory/Model/Interfaces/IGroundItemModel.cs`
- Add: `Assets/Scripts/UI/Inventory/Model/GroundItemModel.cs`
- Modify: `Assets/Scripts/UI/Inventory/InventoryModule.cs`
- Add: `Assets/Scripts/Tests/Inventory/Editor/QGroundItemModelTests.cs`

**Consumes**

- `InventoryItemVO`
- `IModel`
- `MvvmRoot.Instance.Models`

**Produces**

```csharp
public interface IGroundItemModel : IModel
{
    event Action OnChanged;

    string Register(int containerId, InventoryItemVO item, Vector3 worldPosition);
    bool TryGet(string groundItemId, out GroundItemVO item);
    IReadOnlyList<GroundItemVO> QueryNearby(Vector3 center, float radius);
    void Unregister(string groundItemId);
}
```

`GroundItemVO` 实际包含：

- `string GroundItemId`
- `int ContainerId`
- `InventoryItemVO Item`
- `Vector3 WorldPosition`
- `float DropTime`

**TDD**

1. 注册两个不同位置的地面物品，查询范围内只返回范围内的物品。
2. 查询结果按距离升序排列。
3. `Unregister` 后不再返回该物品。
4. 空 ID 或不存在 ID 查询返回 `false`。
5. 补：登记号递增不撞号、压线半径算圈内、坏参数主动报错、快照不被下一次查询清空、广播只在真动过东西时发。

**Implementation**

1. 用 `Dictionary<string, GroundItemVO>` 保存运行时状态。
2. ID 使用稳定前缀加递增序号，例如 `ground-000001`。
3. `QueryNearby` 过滤距离后按 `sqrMagnitude` 排序。
4. 在 `InventoryModule.Register` 注册 `IGroundItemModel`，不把地面模型塞进 `InventoryModel`。

**与计划的出入**

- 接口和实现没有塞进 `Model/Ground/` 子目录，直接沿用 `Model/` 与 `Model/Interfaces/`，和 `IInventoryModel`/`InventoryModel` 保持一致。
- `QueryNearby` 内部用一张复用表排序，交出去前 `ToArray()`，否则下一次查询会把调用方手上的清单清空。
- `Register` 传空物品快照、`QueryNearby` 传负半径都在编辑器下 `QLog.Error` 主动报错，不静默返回。
- 测试夹具会复用同一个测试实例，所以广播计数必须在 `SetUp` 里显式归零，靠字段初始值会串味。

**Verification**

- 运行 `QGroundItemModelTests`。
- 运行完整 EditMode 测试，确认模型注册不影响背包初始化。

实测：14 个用例全过，`Tests.Inventory` 全量 370 通过 / 0 失败。

**Commit**

```text
feat(inventory): add ground item runtime model
```

---

## Task 4：实现原子丢弃

**状态：已完成（2026-11-11）**

**Files**

- Modify: `Assets/Scripts/UI/Inventory/Model/Interfaces/IInventoryModel.cs`
- Modify: `Assets/Scripts/UI/Inventory/Model/InventoryModel.cs`
- Add: `Assets/Scripts/Tests/Inventory/Editor/QInventoryDropTests.cs`

**Consumes**

- `IInventoryModel.CreateRuntimeContainer`
- `IInventoryModel.TryTransferItem`

**不消费**

- `IGroundItemModel.Register`：世界坐标不进背包模型，登记地面物品由调用方（T7 的 VM）做
- `ItemRegistry`：占格直接取源容器里那份 placement 的配置，不必按 itemId 反查

**Produces**

```csharp
// 丢弃：把整件物品连堆带朝向挪进一只按实际占格新建的地面容器，失败时源容器分毫不动
InventoryMoveResult TryDropItem(int sourceContainerId, int instanceId, out int groundContainerId);
```

返回值直接复用 `InventoryMoveResult`，`groundContainerId` 失败时为 `-1`。不再另造一个 `InventoryDropResult` 包装体：丢弃本来就是一次带临时容器的转移，多套一层结果只会让调用方多学一种错误码。

流程：

1. 读取源容器和目标物品。
2. 按 `placement.Instance.Config.GetSizeAt(placement.Rotation)` 算真实占格。
3. 用 `CreateRuntimeContainer` 开同占格的临时地面容器。
4. 用 `TryTransferItem` 把整堆物品转移到临时容器 `Vector2Int.zero`。
5. 任一步失败则注销临时容器，源容器保持原状。

**TDD**

1. `TryDropItem_StandingStack_MovesWholeStackToGroundContainer`：整堆数量、实例号、容器尺寸全部保留。
2. `TryDropItem_RotatedItem_KeepsRotationAndOpensRotatedFootprint`：转倒过的物品按转倒后的占格开地面容器，朝向不许被转回来。
3. `TryDropItem_EquippedHelmet_GroundContainerUsesRealFootprint`：装备槽把头盔压成单格，地面容器必须按头盔真实占格开，否则转移根本放不下。
4. `TryDropItem_MissingInstance_LeavesSourceUntouched`：物品不存在时返回 `SourceNotFound`，源容器一件不少。
5. `TryDropItem_FailedDrop_DoesNotLeakRuntimeContainerId`：失败的丢弃没占走运行时容器号。
6. `TryDropItem_UnregisteredSource_ReportsErrorAndDropsNothing`：容器没注册时主动报错并返回 `None`。
7. `TryDropItem_TwoDropsInARow_EachGetsItsOwnGroundContainer`：两次丢弃各占一只地面容器，前一件不被顶掉。

**Implementation**

1. 丢弃写在 `InventoryModel` 里，不建 Service 层：源容器、临时容器、转移全是模型自己的状态，中间人只会多一层调用。
2. 不使用 `InventoryContainer.TryRemove()` 直接删除物品。
3. 使用临时容器作为唯一丢弃载体。
4. 失败时只回滚本次创建的临时容器，不触碰源容器。
5. 负重上限沿用 `float.MaxValue`，单容器这层不设限，与物资箱一致。
6. `TryTransferItem` 内部已经调过 `SyncWornCapacities`，丢掉身上的容器装备后容量会自动回落，这里不再重复调。

**与计划的出入**

- 原计划的 `Model/Ground/InventoryDropService.cs` 与 `InventoryDropResult.cs` 两个文件不建，改成 `IInventoryModel.TryDropItem`。
- 「转移失败不留运行时容器」这条分支在现有规则下构造不出来：临时容器尺寸就是按物品真实占格开的，负重又不设限，`ResolveTransfer` 不可能在这一步拒绝。所以改成验证失败的丢弃不会白占容器号，回滚分支作为防御保留在代码里。

**Verification**

- 运行 `QInventoryDropTests`。
- 运行完整 EditMode 测试。

实测：7 个用例全过，全量 `Tests.Inventory` 370 通过 / 0 失败。

**Commit**

```text
feat(inventory): add atomic item drop
```

---

## Task 5：右侧动态物资箱载体

已完成，与计划有一处出入：

- 载体不走 Addressables 运行期加载，改在 `UIEquipmentPanel.prefab` 的容量区里放一只停用的
  `Carrier_LootTemplate` 模板，运行期 `Instantiate` 它。好处是模板的布局、字号、格子尺寸和固定载体
  天然同源，不用在脚本里补配置；代价是加箱样式要动预制体。
- `UIInventoryCarrier` 表头名字改成三级回落：`DefaultName` 覆盖 > 容器 `DisplayName` > 预制体原文案，
  动态载体没 `DefaultName`，于是直接显示箱子名。
- 详情弹窗请求绑定在动态载体自己的 `Scope` 上，不借用面板 `Scope`；`preferLeft` 只对装备槽一侧成立，
  物资箱继续右优先。

**Files**

- Modify: `Assets/Scripts/UI/Inventory/ViewModel/EquipmentPanelViewModel.cs`
- Modify: `Assets/Scripts/UI/Inventory/View/UIEquipmentPanel.cs`
- Modify: `Assets/Scripts/UI/Inventory/View/UIInventoryCarrier.cs`
- Modify: `Assets/Prefabs/UI/Inventory/UIEquipmentPanel.prefab`
- Modify: `Assets/Scripts/Tests/Inventory/Editor/QInventoryEquipmentPanelTests.cs`

**Consumes**

- Task 2 的实体物资箱容器号
- `Assets/Prefabs/UI/Inventory/UIInventoryCarrier.prefab`
- `UIEquipmentPanel/Layout_Storage/ScrollView_Capacity/Viewport/Content`

**Produces**

```csharp
public InventoryViewModel LootContainer { get; }
public void OpenLootContainer(int containerId);
public void CloseLootContainer();
```

行为：

- `LootContainer` 为空表示当前没有打开物资箱。
- 打开新物资箱时先销毁旧动态 VM，再创建新 VM。
- 关闭物资箱时销毁动态 VM，但不调用 `IInventoryModel.UnregisterContainer`。
- `EquipmentPanelViewModel.OnDisposeCore()` 只清理 VM，不清理模型容器。
- 动态载体插入 `ScrollView_Capacity/Viewport/Content` 的最后一个位置。

**TDD**

1. `EquipmentPanelViewModel_OpenLootContainer_CreatesDynamicContainer`。
2. `EquipmentPanelViewModel_CloseLootContainer_DoesNotUnregisterModelContainer`。
3. `EquipmentPanelViewModel_SwitchLootContainer_DisposesPreviousVm`。
4. `EquipmentPanelViewModel_Dispose_DoesNotUnregisterLootModelContainer`。
5. 先运行测试，确认当前 API 不存在。

**Implementation**

1. `EquipmentPanelViewModel` 增加一个非固定数组的动态 `InventoryViewModel`。
2. `UIEquipmentPanel` 缓存右侧 `Content`，按需实例化 `UIInventoryCarrier` 并绑定 `LootContainer`。
3. 固定 `Containers[]` 完全不动。
4. 关闭面板或切换物资箱时，只销毁动态载体 GameObject 和 VM。
5. 若载体实例化失败，使用 `QLog.Error` 明确指出路径或地址。

**Verification**

- 运行 `QInventoryEquipmentPanelTests`。
- 在 Unity Play Mode 打开两个不同物资箱，确认右侧只出现一个动态物资箱且切换正常。

**Commit**

```text
feat(inventory): show dynamic loot container on equipment panel
```

---

## Task 6：物资箱实体与 F 交互

**状态：已完成（2026-11-11）**

与计划的出入：`Interact` 绑在 **`UI`** map（不是 `ShootingPlayer`），因为交互发生在面板打开时且不参与战斗输入；
`Gas_Meter` / `Electric_Meter` 从容器清单里去掉（贴墙表箱，交互中心不正）；EditMode 不跑 `Awake`，
`InteractionCenter` 回落到 `transform.position`；模板克隆后要先 `SetActive(true)` 再 `Show`；
`RefreshLootCarrier` 加了 `EnsureLootCarrierVisible()` 自动滚动到物资箱；
`Carrier_LootTemplate` 关闭 `Slot_Equipment` 并把 `Slot_Container.x` 归零。
Play Mode 实测：靠近补给箱按 F，右侧显示「补给箱 15/24」与 7 件物品，格子完整落在视口内。

**Files**

- Add: `Assets/Scripts/Module/Inventory/Interaction/LootBoxController.cs`
- Add: `Assets/Scripts/Module/Inventory/Interaction/LootInteractableScanner.cs`
- Add: `Assets/Scripts/Module/Inventory/Interaction/InteractableTargetSelector.cs`
- Add: `Assets/Scripts/UI/Inventory/LootBoxInteraction.cs`
- Add: `Assets/Scripts/Tests/Inventory/Editor/QInteractableTargetSelectorTests.cs`
- Modify: `ProjectSettings/TagManager.asset`，新增 User Layer index 10 `LootInteractable`
- Modify: `Assets/Config/Input/ShootingPlayerInputActions.inputactions`
- Modify: `Assets/Scripts/Module/ShootingPlayer/Input/ShootingPlayerInputActions.cs`
- Modify: `Assets/Scripts/UI/Inventory/UIInventoryHotkey.cs`
- Modify: `Assets/Scenes/GameScene.unity`

**Consumes**

- `LootContainerConfigSO`
- `OpenLootContainer(string entityId, LootContainerConfigSO loot, int seed)`
- `EquipmentPanelViewModel.OpenLootContainer`

**Produces**

```csharp
public sealed class LootBoxController : MonoBehaviour
{
    public string EntityId { get; }
    public LootContainerConfigSO LootConfig { get; }
    public int ResolveSeed();
}
```

行为：

- 每个物资箱有稳定 `EntityId`，场景里不能重复。
- `F` 只查找交互半径内最近且可交互的物资箱。
- 打开物资箱时确保装备面板已打开，并把物资箱容器挂到右侧动态载体。
- 不修改 `PlayerController` 的战斗、移动或输入。

**TDD**

- `LootBoxController_StableEntityId_ReturnsSameSeed`。
- `InteractableTargetSelector_SelectsNearestBox`。
- `InteractableTargetSelector_NoBoxInRange_ReturnsFalse`。
- `LootBoxInteraction` 只在拿到目标时才打开面板，这条留给 Play Mode 手测。
- 逻辑提取到纯方法或可注入查找器，避免 EditMode 依赖场景物理。

**Implementation**

1. 不新建物资箱 Prefab，`LootBoxController` 以场景 Override 形式直接挂在 `TokyoStreet_DayEnvironment` 内的箱子实例上，交互体由 `Awake` 运行期自建，详见附录 B。
2. 交互宿主运行期自动常驻，和 `UIInventoryHotkey` 一样不挂到玩家战斗对象上。
3. Input Actions 的 `ShootingPlayer` Map 增加 `Interact = F`。
4. `UIInventoryHotkey` 订阅 `Interact`，调用 `LootBoxInteraction`。
5. 第一版只标附录 A 清单里的 8 个实例，不改 `Assets/Tokyo_Street` 下的商店预制体。

**Verification**

- 在 GameScene 中靠近物资箱按 `F`，装备面板打开，右侧出现物资箱。
- 关闭面板后再次按 `F`，同一箱子的残货仍存在。
- 两个同配置物资箱交替打开，内容不串。

**Commit**

```text
feat(loot): add loot box interaction and F key
```

---

## Task 7：右键丢弃与左键双击丢弃

**状态：已完成（2026-11-11）**

与计划的出入：丢弃不写在视图模型里也不建 Service 层，改由普通类 `GroundDropInteraction`（模型原子丢弃 + 地面登记）
承担，运行期宿主 `UIInventoryHotkey` 提供静态入口 `TryDropItemAtPlayer`，世界坐标始终不进 MVVM 层；
`Btn_Drop` 是 `Btn_Split` 的克隆；双击判定器只记「上次点击的实例号 + 调用方传入的时刻」，第三击重新按首次算；
双击丢弃成功后额外调 `InventoryViewModel.ClosePopups()` 关掉作废的详情。
新增文件：`GroundDropInteraction.cs`、`QItemDropInteractionTests.cs`；改动文件：`UIInventoryHotkey.cs`、`InventoryViewModel.cs`。
实测新增 9 个用例全过，`Tests.Inventory` 全量 434 总计 / 400 通过 / 0 失败。

**Files**

- Modify: `Assets/Prefabs/UI/Inventory/UIItemMenuPanel.prefab`
- Modify: `Assets/Scripts/UI/Inventory/View/UIItemMenuPanel.cs`
- Modify: `Assets/Scripts/UI/Inventory/ViewModel/ItemMenuViewModel.cs`
- Modify: `Assets/Scripts/UI/Inventory/View/UIInventoryItem.cs`
- Add: `Assets/Scripts/UI/Inventory/View/Tools/DoubleClickDetector.cs`
- Add: `Assets/Scripts/Tests/Inventory/Editor/QItemDropInteractionTests.cs`

**Consumes**

- 现有 `UIItemMenuPanel` 的 `Panel_Menu` 布局
- 现有 `UIInventoryItem.OnPointerClick`

**Produces**

```csharp
public IReadOnlyReactiveProperty<bool> CanDrop { get; }
public ISignal DropRequested { get; }
public void Drop();

public bool TryClick(int instanceId, float time, out bool isDoubleClick);
```

行为：

- 右键菜单新增 `Panel_Menu/Btn_Drop`，文案“丢弃”。
- 点击丢弃关闭菜单，再走模型层的统一丢弃入口。
- 左键第一次点击仍打开详情。
- 同一物品在 `0.3s` 内再次左键点击触发丢弃。
- 双击和右键丢弃调用同一个 `TryDropItem`，落到地面后的登记也走同一条路。
- 丢弃位置由运行时丢弃宿主提供，UI 不直接计算物理落点。

**TDD**

1. `DoubleClickDetector_SameItemWithinThreshold_ReturnsDoubleClick`。
2. `DoubleClickDetector_DifferentItem_DoesNotDoubleClick`。
3. `DoubleClickDetector_OutsideThreshold_DoesNotDoubleClick`。
4. `ItemMenuViewModel_Drop_EmitsDropRequested`。
5. `ItemMenuViewModel_OpenAndDrop_TargetsCurrentContainerAndInstance`。
6. 先运行测试，确认菜单和双击 API 不存在。

**Implementation**

1. 手工在 `UIItemMenuPanel.prefab` 的 `Panel_Menu` 下新增 `Btn_Drop`，保持 `VerticalLayoutGroup` 自动撑高。
2. `UIItemMenuPanel` 缓存并绑定 `Btn_Drop`。
3. `ItemMenuViewModel` 增加 `CanDrop` 和 `DropRequested`。
4. `UIInventoryItem` 引入 `DoubleClickDetector`，只在左键 `PointerClick` 时判断。
5. 丢弃后隐藏详情和菜单，避免弹窗引用已移动物品。

**Verification**

- 右键任意物品，菜单显示旋转、拆分、丢弃。
- 右键丢弃后物品从源容器消失，并出现在地面模型。
- 双击同一物品，结果与右键丢弃一致。
- 双击间隔较长时仍只打开详情，不丢弃。

**Commit**

```text
feat(inventory): add context-menu and double-click drop
```

---

## Task 8：地面物品世界渲染

**Files**

- Add: `Assets/Scripts/Module/Inventory/Presentation/GroundItemView.cs`
- Add: `Assets/Scripts/UI/Inventory/GroundItemBinders.cs`
- Add: `Assets/Prefabs/Loot/GroundItemView.prefab`
- Modify: `Assets/Scripts/UI/Inventory/InventoryModule.cs`
- Modify: `Assets/Scenes/GameScene.unity`

**Consumes**

- `IGroundItemModel`
- `GroundItemVO`
- `InventoryItemVO.Icon`
- `InventoryItemVO.DisplayName`
- `InventoryItemVO.StackCount`

**Produces**

```csharp
public sealed class GroundItemView : MonoBehaviour
{
    public string GroundItemId { get; }
    public void Render(GroundItemVO vo, Sprite icon, GameObject iconPrefab);
}
```

渲染器不再单独成类：出生、billboard、抛物线、弹跳都是同一只视图的表现状态，拆开会逼着两边同步同一份
`GroundItemId`。图标与世界预制体由 `GroundItemBinders` 从 Assembly-CSharp 侧注入。

行为：

- 地面物品在世界中可见，不只存在于模型。
- 使用物品图标作为主视觉，显示名称和数量。
- 视图跟随 `GroundItemVO.WorldPosition`。
- 物品被拾取或注销后，对应视图自动销毁。
- 不依赖 UI Canvas，关闭背包后仍然可见。

**TDD / Verification**

- EditMode 测试地面模型变更事件能通知渲染器。
- Play Mode 手动检查：丢弃后立刻能在场景里看到物品。
- 检查图标为空时有颜色方块兜底，不出现完全透明物体。
- 检查物品数量文本不会遮挡图标。

**Implementation**

1. 创建 `GroundItemView.prefab`，根节点挂 `GroundItemView` 和碰撞体，子节点放 SpriteRenderer 与 TMP 文本。
2. `GroundItemRenderer` 在场景启动时订阅 `IGroundItemModel.OnChanged`。
3. 按 `GroundItemId` 增量创建和销毁视图，不每帧重建。
4. 第一版视觉保持简单，重点是状态与可见性闭环。

**Commit**

```text
feat(inventory): render dropped items in world
```

---

## Task 9：H 查看附近地面物品

**Files**

- Add: `Assets/Scripts/UI/Inventory/View/UIGroundItemPanel.cs`
- Add: `Assets/Scripts/UI/Inventory/ViewModel/GroundItemPanelViewModel.cs`
- Add: `Assets/Prefabs/UI/Inventory/UIGroundItemPanel.prefab`
- Modify: `Assets/Scripts/UI/Inventory/InventoryModule.cs`
- Modify: `Assets/Scripts/UI/Inventory/UIInventoryHotkey.cs`
- Modify: `Assets/Config/Input/ShootingPlayerInputActions.inputactions`
- Modify: `Assets/Scripts/Module/ShootingPlayer/Input/ShootingPlayerInputActions.cs`
- Modify: `Assets/Scripts/Tools/GM/GameManager.cs`
- Add: `Assets/Scripts/Tests/Inventory/Editor/QNearbyGroundItemsTests.cs`

**Consumes**

- `IGroundItemModel.QueryNearby`
- 当前玩家或相机世界位置
- Task 6 的 Input Actions

**Produces**

```csharp
public sealed class NearbyGroundItemsViewModel
{
    public IReadOnlyList<GroundItemVO> Items { get; }
    public void Refresh(Vector3 center, float radius);
}

public bool CanOpenNearbyList { get; }
```

行为：

- `H` 只在装备面板关闭时打开附近物品列表。
- 装备面板打开时 `H` 不响应，避免与背包操作抢焦点。
- 列表显示图标、名称、数量和距离。
- 查询半径第一版固定为 `8m`，后续可配置。
- GM Console 从 `H` 改为 `F1`。

**TDD**

1. `GroundItemPanelViewModel_Refresh_ReturnsOnlyItemsInRadius`。
2. `GroundItemPanelViewModel_Refresh_SortsByDistance`。
3. `UIInventoryHotkey_InventoryOpen_BlocksNearbyList`。
4. `UIInventoryHotkey_InventoryClosed_AllowsNearbyList`。
5. 先运行测试，确认 API 不存在。

**Implementation**

1. Input Actions 增加 `NearbyItems = H`。
2. `UIInventoryHotkey` 统一处理 B、F、H，所有 UI 开关只走一个宿主。
3. `GameManager` 的 GM Console 快捷键改为 `KeyCode.F1`。
4. 列表面板复用现有弹窗注册方式，不挂到装备面板预制体内部。
5. 空列表显示空状态，不报错。

**Verification**

- 关闭背包按 `H`，能看到附近地面物品。
- 打开背包按 `H`，背包不被关闭，附近列表不弹出。
- 按 `F1` 能打开原 GM Console。

**Commit**

```text
feat(inventory): add nearby ground item list on H
```

---

## Task 10：可选物理抛出与弹跳

> 该任务不阻塞前九个任务，先完成可玩的搜刮与丢弃闭环再决定是否做。

**Files**

- Modify: `Assets/Prefabs/Loot/GroundItemView.prefab`
- Modify: `Assets/Scripts/Module/Inventory/Presentation/GroundItemView.cs`
- Modify: `Assets/Scripts/UI/Inventory/GroundItemBinders.cs`

**Consumes**

- 丢弃时的玩家朝向和手部/胸口参考位置
- `GroundItemVO.WorldPosition`

**Produces**

```csharp
public void ThrowFrom(Vector3 origin, Vector3 direction, float impulse);
```

行为：

- 刚丢弃时从玩家身上朝前方抛出一小段距离。
- 使用 Rigidbody、Collider 和物理材质产生少量弹跳。
- 稳定后停止或进入休眠。
- 物品最终位置回写到地面模型，H 列表距离保持准确。

**TDD / Verification**

- Play Mode 丢弃物品，确认不会穿地。
- 连续丢弃多个物品，确认不会全部堆在同一点。
- 关闭背包后物品仍在场景中可见。

**Commit**

```text
feat(inventory): add optional drop physics
```

---

## Task 11：整体验收与归档

**Files**

- Modify: `设计文档/AI归档记录/<当天日期>/<序号-搜刮与丢弃闭环>.md`
- Verify: 全部修改文件

**Verification**

1. 运行完整 EditMode：

```powershell
unity-mcp-cli run-tool tests-run --input '{"testMode":"EditMode","testAssembly":"Assembly-CSharp-Editor-testable","includePassingTests":false,"includeMessages":true,"includeStacktrace":false}'
```

2. 运行 `git diff --check`。
3. Unity Play Mode 手动验收：

- 靠近物资箱按 `F` 打开右侧物资箱。
- 两个同配置物资箱内容隔离。
- 关闭再打开同一物资箱，残货保留。
- 背包物品拖入物资箱，物资箱物品拖入背包。
- 右键丢弃和双击丢弃都有效。
- 装备槽物品丢弃后地面占格正确。
- 地面物品可见。
- 关闭背包按 `H` 看到附近物品。
- `F1` 打开 GM Console，`H` 不再被 GM 截获。
- 物理抛出开启后，物品能落地、弹跳并稳定。

4. 按 `project-archive-record` 规范写 AI 归档，记录完成项、测试结果、遗留项和下一步。

**Commit**

```text
docs: archive loot and drop closed loop
```

---

## 推荐执行顺序

1. Task 1：运行时容器分配
2. Task 2：物资箱实体隔离
3. Task 3：地面物品模型
4. Task 4：原子丢弃
5. Task 5：右侧动态物资箱
6. Task 6：物资箱实体与 F 交互
7. Task 7：右键与双击丢弃
8. Task 8：地面物品渲染
9. Task 9：H 附近物品列表
10. Task 10：可选物理抛出
11. Task 11：整体验收与归档

## 执行模式

- `Native`：当前线程按任务顺序直接实现，每完成一个任务跑对应测试。
- `Subagent-driven`：每个任务交给独立子代理实现并审查，主线程负责集成和最终验证。

---

## 附录 A：GameScene 构成与容器候选清单

实测于 `Assets/Scenes/GameScene.unity`（7 个根对象）与 `Assets/Prefabs/Environment/TokyoStreet_DayEnvironment.prefab`（约 14 万行，1710 处子物体名覆盖）。

### 场景根对象

- `GameScene`
- `MvvmHost`
- `UIRoot`
- `Hero_Chaisaki`（玩家，战斗系统所有，不改）
- `Enemy_流浪者`
- `TokyoStreet_DayEnvironment`（小东西全在这里）
- `EventSystem` / `Directional Light` / `Global Volume`

### 环境 Prefab 一层结构

```text
TokyoStreet_DayEnvironment/
  Props          街道小物件，容器候选主要来源
  Environment    大型设备与容器，Firebox / Dumpster / Vending 在这里
  Terrain        地形
  Street         路面与标线，不作容器
  House          建筑外壳，第一版不进室内
  Decals         贴花，不作容器
  Background_(Not_Interior)  远景背景，不作容器
  Lighting       灯光
```

### 第一版容器候选（推荐）

| 场景物件 | 所在父节点 | 数量 | 映射配置 |
| --- | --- | --- | --- |
| `Plastic_Box_01/02/03/04` | Props | 6 | `LootConfig_补给箱` |
| `Bucket` ~ `Bucket (4)` | Props | 5 | `LootConfig_补给箱` |
| `Firebox` ~ `Firebox (3)` | Environment | 4 | `LootConfig_医疗物资箱` |
| `Dumpster` ~ `Dumpster (5)` | Environment | 6 | `LootConfig_补给箱` |
| `Gas_Meter_01/03` | Props | 4 | `LootConfig_贵重品箱` |
| `Electric_Meter_01~04` | Props | 17 | `LootConfig_武器箱`（数量多，第一版只挑 2 个） |

### 语义可用但视觉偏弱，暂不启用

`Conditioner_01~09`、`Ventilation_01~05`、`Water_Heater`、`Mailbox`、`Parking_Meter`、`Showcase_Store_01`、`Vending_Machine_01/02`、`Small_Table`、`Table_Cafe`

自动售货机和餐桌含义上不怪，但交互体积大、需要额外的“打开”表现，留到搜刮第二版。

### 明确排除

`Kerb`(129)、`Downpipes`(124)、`Wires`(69)、`Wall`(61)、`Road`(42)、`Fence`(41)、`Floor`(41)、`Flowers`(33)、`Parapet`(32)、`Decals`(31)、`BG`(30)、`Paving`、`*Marking`、`Glass_Curtains`、`Signboard`、`Cone`、`Chair_Cafe`、`Ladder`、`Gate`。

### EntityId 规则

```text
loot_<物件名小写下划线>_<序号>
例：loot_plastic_box_03_01 / loot_firebox_02 / loot_dumpster_05
```

由 `LootBoxController` 上的序列化字段手写，不按 Transform 顺序自动生成，避免场景重排后 ID 漂移导致残货错位。

### 摆放原则

- 识别方式是在箱子实例上挂 `LootBoxController`（场景 Override），不新建独立交互体，详见附录 B。
- 商店资产 `Assets/Tokyo_Street` 与大环境 `TokyoStreet_DayEnvironment.prefab` 第一版都保持零改动。
- 第一版只标 8 个：2 个 `Plastic_Box`、1 个 `Bucket`、1 个 `Dumpster`、1 个 `Firebox`、1 个 `Gas_Meter`、2 个 `Electric_Meter`。

---


---

## 附录 B：容器物件的识别方式（已定）

### 实测：碰撞体是有的，但挂在 LOD0 上

`Assets/Tokyo_Street/Prefabs` 下每个箱子预制体都是这个结构：

```text
Plastic_Box_03            Transform + LODGroup
  Plastic_Box_03_LOD0     Transform + MeshFilter + MeshRenderer + BoxCollider
  Plastic_Box_03_LOD1     Transform + MeshFilter + MeshRenderer
  Plastic_Box_03_LOD2     Transform + MeshFilter + MeshRenderer
```

| 预制体 | 碰撞体 | isTrigger | 实测尺寸 |
| --- | --- | --- | --- |
| `Plastic_Box_01~04` | LOD0 上 1 个 BoxCollider | 0 | 0.314 x 0.307 x 0.465（Box_03） |
| `Dumpster` | LOD0 上 1 个 BoxCollider | 0 | 0.546 x 0.867 x 1.183 |
| `Firebox` / `Bucket` / `Electric_Meter_04` | LOD0 上 1 个 BoxCollider | 0 | 同类结构 |
| `Gas_Meter_03` | 无碰撞体 | - | 壁挂表箱，本身不挡人 |

坑在 LOD。`LODGroup` 切级是整套 GameObject 的开关，不只是 Renderer。`Plastic_Box_03` 的参数：`m_Size = 0.465`，LOD0 阈值 `screenRelativeHeight = 0.3688`，LOD1 = `0.0954`，LOD2 = `0.0178`。一个 0.47 米的塑料箱只在画面占比高于 36.9% 时留在 LOD0，退开一两米 LOD0 整个物体被禁用，它身上的 BoxCollider 一起消失。

所以：贴近时确实能撞、能射，你说的没错；但把交互查询建在这个碰撞体上不稳，`OverlapSphere` 半径稍大就漏检、结果会闪。`Gas_Meter` 这类壁挂件干脆没有碰撞体。

### 为什么不改 Tokyo_Street 里的原预制体

`Assets/Tokyo_Street` 是商店资产，目录里带 `Readme.pdf`、`Models`、`Materials`、`Scenes` 一整套。

- 包重导入或升级会覆盖我们对预制体的修改。
- 预制体是共享的：`Plastic_Box_03` 街上有 2 个实例，`Electric_Meter_04` 有 5 个。脚本和配置写在预制体上等于所有实例自动变物资箱，而且 `EntityId` 完全相同，两个箱子直接串货，正是我们要修的那个 bug。
- 想区分实例仍然得逐个做 Override，工作量一点没省，只是把改动压进了第三方文件。

"在箱子那边改"这个方向我同意，改的是**实例**这一层，不是商店预制体。

### 采用方案：给箱子实例挂 LootBoxController

Unity 为 2022.3.62f3，支持嵌套预制体实例的 Override。`Plastic_Box_03` 是 `TokyoStreet_DayEnvironment` 里的嵌套实例，在场景里选中它 Add Component，改动以 Override 形式落在 `Assets/Scenes/GameScene.unity`，商店预制体和我们的环境预制体都不动。

每个箱子只需要三样东西，全部是实例上的序列化字段：

```text
LootBoxController
  EntityIdValue        = loot_plastic_box_03_01
  LootConfigValue      = LootConfig_补给箱
  InteractRadiusValue  = 1.2
```

碰撞体不靠编辑资产：`LootBoxController.Awake` 用自己的 `Renderer.bounds` 生成一个子节点 trigger `BoxCollider`，Layer 设为 `LootInteractable`。这样绕开 LOD0 碰撞体被禁用的问题，不需要往任何预制体里塞碰撞体，`Gas_Meter` 这种没碰撞体的物件也自动获得交互体。

固化时机：等 `LootBoxController` 字段稳定后，再 `Apply Override to Prefab` 到 `TokyoStreet_DayEnvironment.prefab`，让配置跟着预制体而不是跟着场景。第一版先留场景 Override，迭代快、能单点删除。

### Layer

新增 `LootInteractable`。`ProjectSettings/TagManager.asset` 当前 Tag 只有 `Enemy` / `Hero`，Layer 实测占用 0-9：`Default`、`TransparentFX`、`Ignore Raycast`、3 号内置空位、`Water`、`UI`、`Enemy`、`Player`、`Building`(8)、`Ground`(9)。新的用户层落 index 10。

用途限定为物理粗过滤：按 `F` 时对玩家周围做一次 `Physics.OverlapSphere` 带该层掩码，不遍历全部箱子。业务分类交给 `LootBoxController`，Tag 不参与。

### EntityId

手写序列化字段，格式 `loot_<物件名小写>_<序号>`，例如 `loot_plastic_box_03_01`。

不从名字、路径或 siblingIndex 运行时推导。场景重排会让推导值漂移，残货错位到别的箱子上，这类问题很难排查。

### 第一版清单

2 个 `Plastic_Box`、1 个 `Bucket`、1 个 `Dumpster`、1 个 `Firebox`、1 个 `Gas_Meter`、2 个 `Electric_Meter`，共 8 个。

编辑期批量扫描工具不进第一版。箱子过 30 个或要做第二张图时，再加一张 `名字前缀 -> LootConfig` 规则表和 `Tools/背包/扫描生成物资箱` 菜单。

### 识别与发现机制

组件即标记，不引入 Tag、名字白名单或接口扫描。判定只有一句：命中物上 `TryGetComponent<LootBoxController>` 成功即为可交互容器。

发现方式定为按 `F` 时一次性查询，不维护常驻的"附近箱子表"：

1. `Physics.OverlapSphereNonAlloc(playerPos, maxRadius, m_results, LootInteractableMask)`，缓冲数组与容量告警沿用 `CombatHitbox` 的写法（`const` 容量 + `readonly Collider[]` + 达到上限只告警一次）。
2. 逐个 `TryGetComponent<LootBoxController>`，要求 `CanInteract`。
3. 用交互体合并 bounds 的中心与玩家做平方距离，取最近的一个。`OverlapSphere` 只当粗筛，选谁由中心距离决定，避免 `Dumpster` 这种 1.18 米长的箱体边缘够得着、中心却差很远带来的抖动。

不选 `OnTriggerEnter/Exit` 常驻列表：要维护进出一致性，箱子销毁、禁用、场景卸载都得补，8 个箱子一次 overlap 是微秒级开销，不值得换那份状态。不选静态注册表单例：工程 world 侧没有服务容器（背包 Model 由 `MvvmRoot` 持有，属 MVVM），硬加一个 `LootInteractionHost` 是新抽象。

### 交互体生成

`LootBoxController.Awake` 合并自身子级 `Renderer.bounds`，在根节点加一个 `BoxCollider`，`isTrigger = true`，`gameObject.layer = 10`。Layer 在运行期赋值，不改资产。这样 LOD 切级不会让交互体消失，`Gas_Meter_03` 这类无碰撞体物件也自动获得交互体。

### 配置校验

`CanInteract` 的语义条件，缺配置按项目规范在 `#if UNITY_EDITOR` 主动报错，不静默跳过：

- `LootConfig` 为空或其 `Container` 网格为空 -> 开不出箱。
- `EntityId` 为空 -> 无法定位容器。
- 场景内 `EntityId` 重复 -> 复制实例忘改号，直接串货。`LootInteractionController` 在场景加载后做一次重复检测并 `QLog.Error`。

第一版没有"箱子打坏"或"一次性搜刮"，除配置缺失外恒可交互。

### seed 来源

不用 `EntityId.GetHashCode()`。.NET 字符串哈希按进程随机化，重启后同一箱子会换货，将来服务器校验也对不上。改用确定性 FNV-1a 在 `EntityId` 上取值，再与本局 run seed 混合。

### 编辑器审计

选中箱子时看 `OnDrawGizmosSelected` 画出的交互半径球，配合 Hierarchy 上的组件，就是人工识别"哪些箱子被标过"的手段。`OnValidate` 同步校验 `EntityId` 非空。

第一版不做描边高亮，按 `F` 无目标只出一条编辑器日志。玩家侧可交互提示 UI 等有交互按钮再做。
