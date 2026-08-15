# Protocol_Evac Enemy 行为树、三段攻击与巡逻导航交接记录

## 一、记录范围

本记录接续：

[3-Player急停Generic根位移修复交接记录.md](../2026-8-14/3-Player急停Generic根位移修复交接记录.md)

主设计文档：

[敌人AI、能力与导航设计方案.md](../../玩家状态与敌人AI/敌人AI、能力与导航设计方案.md)

本记录保存 Ability Composer 通用化后，流浪者 Enemy 的配置、Fluid Behavior Tree、Playables 动画、三段普通攻击、战斗等待和局部网格 A* 巡逻的当前实现状态。用户已在 Play Mode 中完成实测并确认当前行为没有问题。

## 二、本次确认的设计与协作偏好

### 1. Player 与 Enemy 不共用运行时决策模型

```text
Player
└─ Input Buffer + HFSM + Player Skill

Enemy
└─ Context + Fluid Behavior Tree + Enemy Skill + Navigation + Motor
```

Player 已验证的 HFSM 不迁移到 Enemy。Enemy 直接使用 BT 编排行为，避免再维护一套只为播放动画服务的 Animator 状态机。

### 2. BT、Skill、Ability Composer 的职责固定

```text
BT
└─ 判断当前做什么：攻击、战斗等待、巡逻、待机

Enemy Skill
└─ 推进技能段落、阶段、冷却和命中窗口

Ability Composer
└─ 编辑动画事件、命中窗口、阶段推进窗口和预览
```

行为节点按大行为分类到 `Actions/Attack`、`Actions/Patrol`、`Actions/Common` 等目录。节点只执行一个明确行为，不把整个敌人流程堆进单个 Action。

### 3. 动画不再依赖 Enemy Animator Controller

Enemy 基础循环动画和技能动画统一由 `EnemyAnimWriter` 使用 Playables 输出。流浪者 Prefab 的 Animator Controller 引用已经清空，旧 `流浪者_Animator.controller` 已删除；Animator 窗口残留的旧状态图也已经关闭。

### 4. ECS 暂不进入第一版

当前先保留可读的 OOP BT、Skill、Navigation 与 Motor。ECS 只作为大量敌人时的局部避障或批量热点计算候选，不提前改写单体敌人逻辑。

## 三、Ability Composer 通用化状态

原 Player 专属的技能段落数据与编辑器已提取到：

```text
Assets/Scripts/Tools/AbilityComposer
├─ Data
│  ├─ AbilityConfigSO
│  ├─ AbilityStepData
│  ├─ AbilityStepPhase
│  └─ Window
└─ Editor
   ├─ Preview
   ├─ Selection
   ├─ UI
   └─ View
      ├─ Left
      ├─ Center
      └─ Right
```

程序集边界为：

```text
Ability.Data
Tools.AbilityComposer.Editor
Ability.Core
```

`Ability.Core/Hit/AbilityHitWindowController` 负责将通用命中窗口同步到 `CombatHitbox`。Player 与 Enemy 都复用 `AbilityConfigSO`、`AbilityStepData` 和窗口数据，但各自保留独立的运行时时间轴与控制器。

Ability Composer 当前支持：

```text
显示全局动画 = 关闭
└─ 只列出预览 Prefab 的 Animator 和依赖配置引用到的 AnimationClip

显示全局动画 = 开启
└─ 使用全局 AnimationClip 资源选择
```

从角色配置中的“打开动画编辑器”进入时，会传入当前段落的攻击阶段动画，不依赖动画名称前缀筛选。

## 四、Enemy 当前运行架构

当前模块结构：

```text
EnemyController
├─ EnemyContext
│  ├─ EnemyTargetContext
│  ├─ EnemyActionContext
│  └─ EnemyMovementContext
├─ EnemyTargetReader
├─ WandererBehaviorTree
├─ EnemySkillController / EnemySkillTimeline
├─ INavigationController / GridPathController
├─ EnemyMotor
└─ EnemyAnimWriter
```

运行顺序：

```text
Update
├─ EnemyTargetReader.Tick
├─ EnemySkillController.Tick
├─ EnemyBehaviorController.Tick
└─ EnemyAnimWriter.Tick

FixedUpdate
└─ EnemyMotor.FixedTick
```

`EnemyController` 只负责查找引用、检查配置、初始化和调度。行为选择在 BT，技能阶段在 Skill，路径在 Navigation，最终位移与旋转在 Motor，动画输出在 AnimWriter。

## 五、流浪者行为树与攻击闭环

当前使用依赖：

```text
com.fluid.behavior-tree
https://github.com/ashblue/fluid-behavior-tree.git?path=/Assets/com.fluid.behavior-tree#v2.3.0
```

当前 `WandererBehaviorTree`：

```text
选择行为 Selector
├─ 普通攻击 Sequence
│  ├─ 存在目标
│  ├─ 目标进入攻击范围
│  ├─ 普通攻击可用
│  └─ EnemyNormalAttackAction
├─ 战斗等待 Sequence
│  ├─ 存在目标
│  ├─ 目标处于攻击范围
│  └─ EnemyCombatWaitAction
├─ EnemyPatrolAction
└─ EnemyIdleAction
```

攻击完成后不会立刻做一次突兀的 Idle 切换。冷却期间，`EnemyCombatWaitAction` 会停步并持续面向目标；冷却结束返回成功，让根 Selector 重新检查普通攻击分支。

`EnemyNormalAttackAction` 会在时间轴运行期间持续请求下一段，因此一次攻击行为会自动完成当前配置中的三段普通攻击。

### 三段普通攻击配置

```text
第 1 段：Sword_Regular_A + Sword_Regular_A_Rec
命中窗口：0.23 - 0.70，伤害 10

第 2 段：Sword_Regular_B + Sword_Regular_B_Rec
命中窗口：0.20 - 0.66，伤害 12

第 3 段：Sword_Regular_C + Sword_Regular_B_Rec
命中窗口：0.16 - 0.52，伤害 18

普通攻击冷却：0.5 秒
攻击期间锁定移动：是
攻击期间允许转向：是
```

第三段暂时复用 B 的收招动画；这是当前可用配置，不代表最终动作资源方案。

## 六、巡逻与通用网格导航

新增独立程序集：

```text
Assets/Scripts/Module/Navigation
├─ Core/INavigationController.cs
├─ Grid/Config/GridNavigationConfigSO.cs
├─ Grid/Data/GridNavigationData.cs
├─ Grid/GridPathController.cs
├─ Grid/GridPathResolver.cs
└─ Navigation.asmdef
```

BT 只依赖 `INavigationController`。`GridPathController` 根据场景 Collider 同步采样局部网格，`GridPathResolver` 使用 A* 生成路径，`EnemyPatrolAction` 只负责选择巡逻目的地并把移动意图写入 Context。

当前行为是：玩家不在攻击范围时，流浪者在出生点半径内随机巡逻；抵达目的地后等待，再选择下一目的地。

关键配置：

```text
移动速度：1.6
转向速度：360 度/秒
巡逻半径：6
巡逻停留：1.2 秒
路径重试间隔：0.25 秒

网格尺寸：0.6
搜索外扩：2.5
单轴最大网格：64
角色半径：0.32
角色高度：1.7
最大台阶高度：0.35
拐点抵达距离：0.2
随机采样次数：12
```

## 七、场景、Prefab 与资源状态

流浪者 Prefab：

```text
Assets/Prefabs/Character/Enemy_流浪者.prefab
├─ Root Layer：Enemy
├─ Tag：Enemy
├─ CharacterController：1 个
│  ├─ Height：1.7
│  ├─ Radius：0.32
│  └─ Center Y：0.85
├─ WeaponHitbox Layer：Enemy
├─ EnemyController.Settings：Enemy_流浪者Settings
└─ Animator Controller：None
```

Enemy 配置聚合：

```text
Assets/Config/Enemy/Enemy_流浪者Settings.asset
├─ StatsConfig
├─ BehaviorConfig
├─ MoveConfig
├─ NavigationConfig
├─ AnimationConfig
└─ NormalAttackConfig
```

当前 Tokyo Street 的大量 Collider 仍在 `Default` Layer，所以导航地面与障碍掩码暂时都包含 `Default`。这只是首版可运行配置，不是最终场景分类方案。

后续场景分类不要用 Tag 承担导航语义。建议 Layer：

```text
NavigationGround
└─ 道路、地面

NavigationObstacle
└─ 建筑、墙体和不可互动障碍

Interactable
└─ 车辆、售货机、箱子等可互动且会阻挡导航的物体
```

导航障碍掩码包含 `NavigationObstacle + Interactable`；搜刮和交互语义由 `IInteractable`、容器组件和配置表达。Tag 只保留 `Hero`、`Enemy` 等主体身份。

## 八、验证结果

用户已在 `Assets/Scenes/GameScene.unity` 的 Play Mode 中完成实测，并确认当前结果没有问题。

本次归档前重新执行程序集编译：

```text
Enemy.csproj：0 个警告，0 个错误
Navigation.csproj：0 个警告，0 个错误
```

另外已确认：

```text
旧流浪者 Animator Controller 资源数量：0
Prefab Animator 的有效 Controller 引用数量：0
Prefab 中 CharacterController 数量：1
```

本阶段没有新增自动化 EditMode / PlayMode 测试；当前通过状态来自用户实机 Play Mode 验证与程序集编译。

## 九、当前尚未完成

```text
1. Chase、LastSeenPosition、Search 行为尚未接入当前流浪者树
2. Enemy 受击、死亡、中断技能、关闭 Hitbox 的完整闭环尚未完成
3. Tokyo Street 尚未批量拆分 NavigationGround、NavigationObstacle、Interactable Layer
4. GridPathResolver 尚未添加纯网格 A* 自动化测试
5. 多敌人局部避障和 ECS 性能评估尚未开始
6. Behavior Graph 可视化编辑器尚未实现，当前树由代码构造
7. `WandererBehaviorTree` 等少量文件的描述仍需同步当前巡逻职责
8. `INavigationController` 公共 API 仍需按项目规范补 XML 注释
9. 主设计文档中的部分旧名 `IPathAgent` 与当前 `INavigationController` 尚待统一
```

当前不要重新改已验证的 Player HFSM，也不要为了一致性把 Enemy 运行时强行抽到 Player Skill 中。

## 十、下一步建议

建议按以下顺序继续：

```text
1. 先完成 Enemy Hurt / Dead 与攻击中断清理
2. 再补 Chase -> LastSeenPosition -> Search -> Patrol 行为链
3. 为 GridPathResolver 添加 EditMode 测试
4. 在 Tokyo Street 小范围试点三类导航 Layer，再批量整理场景
5. 单体行为稳定后，再做 Behavior Graph 可视化与多敌人性能评估
```

下一步的验收重点应是 Enemy 在受击或死亡时能可靠中断 BT、Skill、Navigation 和 Hitbox，而不是继续调整已经通过测试的三段攻击。

## 十一、提交与工作区注意事项

本阶段主要提交：

```text
9928c5a  敌人基础配置数据
46bcf77  忽略美术资源
cca8be9  删除多余资源
e18f7b0  重构技能编辑器，兼容敌人和玩家，接入敌人配置
93805ae  引入 BT 插件，敌人使用行为树
d47110e  区分 BT 和技能编辑器职责并重构 Enemy
89117b2  Enemy 巡逻基础逻辑与 A* 寻路
```

归档前 `HEAD`：

```text
89117b20aa1a322c43945ad04c818dc23ea3ce17
```

归档前工作区为干净状态。创建本记录后，只应新增当前 Markdown 归档文件。

仓库当前只保留代码与框架，美术和大型第三方资源继续本地使用：

```text
Assets/Animation/
Assets/Art/
Assets/Tokyo_Street/
Assets/Prefabs/Character/Enemy_流浪者.prefab
```

以上路径已由 `.gitignore` 排除。不要因为 Git 中看不到这些资源，就误判本地 Prefab、模型、动画或材质已删除。
