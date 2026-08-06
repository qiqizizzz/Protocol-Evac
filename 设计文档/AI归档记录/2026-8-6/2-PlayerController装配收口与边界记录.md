# Protocol_Evac PlayerController 装配收口与边界记录

## 一、记录范围

本记录接续：

[1-千咲普通闪避动画Land_Roll定案记录.md](1-千咲普通闪避动画Land_Roll定案记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存 Player 配置聚合、HFSM 状态树装配收口，以及 `PlayerController` 当前职责边界。它不包含新的战斗功能、动画资源或 Unity 场景修改。

## 二、本次确认的设计

### 1. PlayerController 保持 Player 的组合根

`Assets/Scripts/Module/Player/Core/PlayerController.cs` 只负责：

```text
获取 Player 场景引用
读取 PlayerSettingsSO
创建并装配 Player 子模块
持有本地 ControllerManager
显式安排 Player 运行顺序
销毁本地 Controller
```

`Update()` 的顺序是运行契约，当前保持为：

```text
InputReader
ViewController
TransitionSelector
PlayerStateMachine
PlayerSkillController
PlayerAnimWriter
```

`FixedUpdate()` 保持为：

```text
PlayerStateMachine.FixedTick
PlayerMotor.FixedTick
```

不要把上述 Tick 调度移交给 `ControllerManager.TickAll()`，也不要为了缩短文件再拆出无具体职责的 Bootstrap、Runtime 或 Manager 类。

### 2. 已移除运行期总判空

当前 `PlayerController` 已不再包含：

```text
m_isInited
isRuntimeReady()
Update / FixedUpdate 中的总判空提前返回
```

Player 模块在 `Awake()` 内完成装配后直接进入显式调度。后续不要恢复按帧汇总多个模块判空的写法。

### 3. 配置与状态树的当前装配位置

Player 配置聚合入口为：

```text
Assets/Scripts/Module/Player/Config/PlayerSettingsSO.cs
Assets/Config/Player/PlayerSettings.asset
```

状态树组装入口为：

```text
Assets/Scripts/Module/Player/HFSM/Factory/PlayerStateFactory.cs
```

`PlayerStateFactory.Create(m_context, Settings, m_skillController)` 负责注册当前 Player 的状态树并初始化 `Grounded`。通用 `PlayerStateMachine` 只负责执行 HFSM，不承担具体 Player 状态的组装。

## 三、当前明确不做的改动

用户已确认：本轮不再继续调整 `PlayerController` 中其余字段的保存方式。

以下字段目前即使只参与初始化，也不要求为了形式上的瘦身改为局部变量：

```text
m_transitionController
m_animController
m_animResolver
m_rootMotionReceiver
```

同样不新增 `PlayerReferences`、`PlayerBootstrap`、生命周期事件字典、具体技能包装类或状态快捷属性。后续提出新的结构建议前，必须先读取工作区中的当前代码，不能沿用旧版本的假设。

## 四、技能与事件边界

当前边界保持：

```text
PlayerNormalAttackState
  └─ 自己负责普攻状态的 Enter / Tick / Exit

PlayerSkillController
  └─ 通用技能配置与时间轴控制，不持有具体状态

PlayerSkillTimeline
  └─ 当前段、计时、推进请求、推进窗口与结束
```

不要为了使用 `EventManager` 提前创建 Player 状态或技能事件。只有出现真实发布源、订阅方和数据需求时，才定义事件协议。

## 五、当前尚未完成

```text
PlayerSkillStepData 的 HitWindow 尚未接入 CombatHitbox
第一段普攻的命中闭环尚未完成
Player 模块事件目前没有真实调用方，不进行预注册
本次未进行 Unity Play Mode 或编译验证
```

下一步功能实现仍应优先从 `HitWindow -> CombatHitbox` 开始，并由真实命中需求反推是否需要事件。

## 六、资源与工作区注意事项

当前维护的 Player Prefab 是：

```text
Assets/Prefabs/Character/Hero_Chaisaki.prefab
```

`Assets/Prefabs/Character/Player.prefab` 已弃用，后续不要修改。

归档时工作区仍有用户原有的无关变更：

```text
D Assets/Lua.meta
D Assets/Scripts/Common.meta
D Assets/Scripts/Framework/QLua.meta
D Assets/Scripts/Net.meta
D Assets/Scripts/Tools/GM.meta
D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639214339907833159.backup
```

不要恢复、删除、暂存或提交这些文件。

## 七、验证记录

本次已重新读取当前工作区中的 `PlayerController`、`PlayerSettingsSO`、`PlayerStateFactory` 与 `PlayerSkillTimeline`，确认归档内容以当前文件为准。

`git diff --check` 已执行完成；仅输出 `.agents/skills/` 工作区文件的 LF/CRLF 提示，未报告差异格式错误。
