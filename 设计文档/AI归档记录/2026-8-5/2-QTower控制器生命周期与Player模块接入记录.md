# Protocol_Evac QTower 控制器生命周期与 Player 模块接入记录

## 一、记录范围

本记录接续：

[1-Player技能时间轴拆分与命名收口记录.md](1-Player技能时间轴拆分与命名收口记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存 QTower 轻量控制器框架的最终生命周期、事件注册方式、程序集边界，以及 Player 模块中四个纯 C# Controller 的实际接入结果。

```text
1. QTower 只统一 Controller 生命周期，不替代 PlayerController 的组合根职责
2. ControllerManager 只负责 Register / Init 与逆序 Destroy，不接管 Tick 顺序
3. BaseController 使用固定入口 Init / Destroy 和派生扩展点 OnInit / OnDestroy
4. 模块事件由具体 Controller 注册，由真实事件源发布，并在 Destroy 时自动解绑
5. Player 本地 Controller 注册在 PlayerController 持有的 Manager，不注册到 GameApp
```

对应代码已由用户提交：

```text
2b0ed8c QTower框架-增加控制器基类，事件管理器，控制器管理器，并应用在Player模块中的部分控制器上
```

## 二、本次确认的设计 / 协作偏好

### 1. 采用轻量模板生命周期

`BaseController` 的公共生命周期入口固定为：

```text
Init
Tick
Destroy
```

其中 `Init()` 与 `Destroy()` 不允许派生类重写，只由 `ControllerManager` 调用：

```text
Init
├─ OnInit
└─ RegisterModuleEvent

Destroy
├─ RemoveModuleEvent
└─ OnDestroy
```

派生 Controller 按需重写：

```text
OnInit
Tick
OnDestroy
RegisterModuleEvent
```

这样不会要求派生类记忆 `base.Init()` 或 `base.Destroy()`，也不会因为漏调基类方法破坏事件解绑。

### 2. ControllerManager 只管理所有权生命周期

`ControllerManager.Register(controller)` 负责保存 Controller、调用 `Init()` 并返回原实例；`Destroy()` 按注册逆序销毁全部 Controller。

Manager 不统一调用 Tick。Player 当前运行顺序必须继续显式保留：

```text
Input
View
Transition
StateMachine
Skill
AnimWriter
```

如果 Manager 连续驱动全部 Tick，会改变 View 与 Skill 在状态机前后的既定位置，因此本轮明确删除 Manager 的 Tick 调度。

### 3. 不把 Player Controller 注册到 GameApp

Controller 的生命周期跟随实际拥有者：

```text
GameApp
└─ 只负责现有全局 QF 架构与全局系统

PlayerController
├─ 持有本地 ControllerManager
├─ 创建并注册单个 Player 的子 Controller
├─ 显式安排 Tick 顺序
└─ OnDestroy 时统一销毁本地 Controller
```

未来若出现真正的全局 Controller，可以由 GameApp 持有独立 Manager；不能把单个 Player 的 Controller 提升到全局作用域。

### 4. 保持框架代码直接，不添加无实际职责的辅助层

本轮用户明确要求：

```text
不添加隐藏生命周期状态位
不添加第二套重复生命周期命名
不为简单日志拆 validateEvent / logEventTypeMismatch 等辅助函数
不添加过度防御式判空和无恢复价值的日志
不创建多份事件注册字典
修改时保留用户刚调整的方法与语句顺序
```

`ControllerManager` 虽属于项目通用规范中通常禁止新增的 `Manager` 后缀，但该名称是用户本轮明确选择并确认的 QTower 框架类型。后续不得未经用户同意擅自改名。

### 5. 本轮结论替代旧归档中的暂缓决定

上一阶段归档曾记录“当前不新增 BaseController / ControllerRegistry”。本轮用户基于统一生命周期和自动解绑的实际需求重新作出决定，现已正式引入：

```text
BaseController
ControllerManager
EventManager
```

后续应以本记录和当前代码为准，不再恢复旧的“禁止接入 BaseController”结论。

## 三、QTower 当前实现状态

当前目录：

```text
Assets/Scripts/Framework/QTower/
├─ QTower.asmdef
├─ Controller/
│  ├─ BaseController.cs
│  └─ ControllerManager.cs
└─ EventManager.cs
```

### 1. BaseController

`BaseController` 当前负责：

```text
固定执行 Init / Destroy 模板流程
提供 OnInit / Tick / OnDestroy 扩展点
提供 RegisterModuleEvent 扩展点
记录当前 Controller 的事件解绑 Action
按事件名解绑单组事件
Destroy 时自动解绑全部模块事件
```

### 2. ControllerManager

`ControllerManager` 当前只有两个职责：

```text
Register<T>(T controller)
└─ 保存实例、调用 Init、返回实例

Destroy()
└─ 按注册逆序调用 Destroy，随后清空集合
```

它不是全局单例，也不负责查找 Controller、服务定位或自动扫描类型。

### 3. EventManager

事件接口当前为：

```text
RegisterEvent<TEvent>(string eventName, Action<TEvent> callback)
UnregisterEvent<TEvent>(string eventName, Action<TEvent> callback)
PublishEvent<TEvent>(string eventName, TEvent eventData)
```

设计含义：

```text
eventName 负责区分事件语义
TEvent 负责约束事件数据类型
同一 eventName 可以注册多个相同 TEvent 的回调
同一 eventName 必须始终使用同一种 TEvent
Controller 只注册监听；PublishEvent 由真实事件源调用
```

`EventManager` 使用 `Dictionary<string, Delegate>`，因为不同事件需要保存不同的 `Action<TEvent>`；无参数 `Action` 无法承载多种泛型回调类型。

### 4. 程序集边界

QTower 已建立独立 `QTower.asmdef`，Player 程序集通过 GUID 显式引用 QTower：

```text
Player -> QTower
QTower -X-> Player
```

这解决了 Player 独立程序集不能引用默认 `Assembly-CSharp` 中框架代码的问题，并保持单向依赖。

## 四、Player 模块接入状态

以下四个纯 C# Controller 已继承 `BaseController`：

```text
PlayerViewController
PlayerAnimController
PlayerTransitionController
PlayerSkillController
```

### 1. 构造与初始化

View、Anim、Transition Controller 的稳定依赖已改为构造函数注入，原有带参数 `Init(...)` 的装配逻辑迁入 `OnInit()`。

`PlayerSkillController` 仍先创建并注册技能配置，再交给 Manager：

```text
new PlayerSkillController
RegisterConfig
ControllerManager.Register
└─ BaseController.Init
```

该顺序保证未来 `OnInit()` 或 `RegisterModuleEvent()` 使用技能配置时不会处于半初始化状态。

### 2. PlayerController 仍是唯一组合根

`PlayerController` 当前持有一个本地 `ControllerManager`，分别在 `initCore`、`initSkill`、`initHFSM`、`initAnim` 中注册对应子 Controller。

`Update()` 继续直接调用 View 与 Skill 的 Tick，不经 Manager 转发；`OnDestroy()` 调用 Manager 统一销毁，然后清理 `PlayerInputReader`。

### 3. 技能销毁

`PlayerSkillController.OnDestroy()` 当前调用 `Close()`，保证 Player 销毁时关闭仍在运行的技能时间轴。后续接入 `CombatHitbox` 后，`Close()` 仍应作为关闭命中盒和技能运行资源的统一入口。

## 五、关键架构边界

```text
QTower
└─ 提供通用 Controller 生命周期和事件能力

PlayerController
└─ 拥有单个 Player 的 Manager、装配关系和 Tick 顺序

Player 子 Controller
└─ 负责各自模块初始化、运行与销毁

PlayerStateMachine / 其他事件源
└─ 在真实事件发生时调用 EventManager.PublishEvent

GameApp / QF
└─ 继续管理全局系统，不持有 Player 本地 Controller
```

QTower 与 QF 当前并存。不要在没有明确迁移计划时让同一对象同时注册到 QF Architecture 和 QTower ControllerManager。

## 六、验证状态

本轮已确认：

```text
QTower 脚本通过 Unity 自带 Roslyn 编译器独立检查
QTower.asmdef 与 Player.asmdef JSON 有效
Unity 导入 QTower 程序集后成功 Loaded All Assemblies
修复程序集引用后没有新的相关 C# 编译错误
git diff --check 通过
```

尚未执行 Player Play Mode 行为验证。

## 七、当前尚未完成

```text
PlayerSkillController 尚未注册任何真实模块事件
Player 事件名称常量和事件数据类型尚未定义
PlayerStateMachine 尚未发布状态进入 / 退出事件
PlayerNormalAttackState 尚未瘦身为技能生命周期入口
CombatHitbox 尚未接入 PlayerSkillTimeline 的 HitWindow
第一段普攻命中闭环尚未完成 Play Mode 验证
```

不要为了展示 EventManager 而提前创建无调用方的事件协议。下一步应从真实需求出发，先确认哪个事件源、哪个订阅者和事件数据，再新增事件名称与发布点。

## 八、下一步建议

下一步先完成 PlayerNormalAttackState 与 PlayerSkillController 的真实接线，再决定是否需要事件：

```text
1. 明确 PlayerNormalAttackState Enter / Tick / Exit 与技能总控的调用关系
2. 保持 PlayerSkillController 不硬编码某一种具体技能处理流程
3. 若确实需要状态事件，由 PlayerStateMachine 统一发布通用状态事件
4. 在 PlayerSkillController.RegisterModuleEvent 中注册真实监听
5. 完成第一段技能时间轴 Play Mode 验证
6. 再接 CombatHitbox 的 HitWindow
```

## 九、工作区注意事项

QTower 与 Player 接入代码已经位于提交 `2b0ed8c`，本归档提交只允许包含当前 Markdown 文件。

归档时工作区仍有与本记录提交无关的用户改动：

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
