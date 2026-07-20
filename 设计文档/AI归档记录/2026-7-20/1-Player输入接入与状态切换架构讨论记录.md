# Protocol_Evac Player 输入接入与状态切换架构讨论记录

## 一、记录范围

本记录接续：

[../2026-7-18/1-Player移动闭环与InputSystem接入记录.md](../2026-7-18/1-Player移动闭环与InputSystem接入记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次实际完成 `PlayerInputReader` 到 `PlayerController` 的接入，并围绕下一步状态切换职责、命名和复用边界进行了讨论。

本次没有创建 `PlayerTransitionEvaluator`、Transition Request、Transition Rule、State Resolver 或通用 HFSM Framework；上述方案均未被确认为最终架构。

## 二、本次实际完成内容

当前文件：

[../../../Assets/Scripts/Module/Player/Core/PlayerController.cs](../../../Assets/Scripts/Module/Player/Core/PlayerController.cs)

`PlayerController` 已完成以下输入接线：

```text
1. 引用 Module.Player.Input
2. 持有 PlayerInputReader
3. Awake 中创建 PlayerInputReader
4. 使用同一个 PlayerContext 调用 Init(...)
5. Update 中先执行 PlayerInputReader.Tick()
6. 随后执行 PlayerStateMachine.Tick(...)
7. OnDestroy 中调用 PlayerInputReader.UnInit()
```

当前 Update 调度顺序为：

```text
PlayerInputReader.Tick()
└─ 写入 Context.MoveInput / IsSprintPressed

PlayerStateMachine.Tick(Time.deltaTime)
└─ 更新当前活动状态路径
```

当前尚未加入根据输入执行 `GroundedIdle` 与 `GroundedMove` 切换的代码，因此输入虽然已经写入 Context，状态机仍会停留在初始 `GroundedIdle`。

## 三、本次确认的架构问题

### 1. ChangeState 与状态决策不是同一个职责

现有：

```text
PlayerStateMachine.ChangeState(targetStateId)
```

已经负责：

```text
目标状态路径构建
LCA / 公共路径计算
Exit 子到父
Enter 父到子
活动路径更新
生命周期重入保护
故障状态保护
```

它负责的是“目标已经确定后如何完成切换”，但当前仍缺少“何时切换以及目标是谁”的业务决策位置。

### 2. PlayerStateMachine 不应继续承载 Player 业务判断

当前文件：

[../../../Assets/Scripts/Module/Player/HFSM/PlayerStateMachine.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerStateMachine.cs)

当前约有：

```text
426 行
```

它已经承担状态注册、树结构校验、活动路径、LCA 切换、生命周期调度、重入保护和故障保护。

后续不应直接把以下业务继续塞进状态机：

```text
MoveInput 判断
Idle / Move / Sprint 规则
Jump / Fall / Land 规则
Ability 取消窗口
Hurt / Dead 优先级
输入消费
```

否则状态机基础设施会与 Player 业务耦合，并继续膨胀。

### 3. 不接受命名和抽象层级混乱

本次讨论过但未确认的类型包括：

```text
PlayerTransitionEvaluator
PlayerTransitionRequest
PlayerTransitionRule
PlayerInterruptPriority
PlayerStateResolver
StateGraph
StateTransition
```

用户明确不认可在当前整洁的目录中一次引入大量 Request / Rule / Evaluator / Resolver 等胶水类型，也不认可为了当前移动闭环立即进行泛型 HFSM、独立程序集和完整框架重构。

后续设计需要满足：

```text
1. 命名与现有 Ability / Context / Core / HFSM / Input 体系一致
2. 一个概念只保留一套术语
3. 不为理论上的复用提前制造大量空类型
4. 不把业务判断塞回 PlayerController
5. 不让 PlayerStateMachine 继续承担具体 Player 业务
6. 能在未来扩展 Jump / Ability / Hurt / Dead
7. 改造范围必须可控，不重写已经完成的 HFSM 核心
```

## 四、已讨论但尚未定案的方向

### 方案 A：独立 Transition Evaluator

曾考虑让 `PlayerTransitionEvaluator` 读取 Context 并决定 Idle / Move。

未采用原因：

```text
如果直接硬编码所有状态规则，会快速形成巨型 if / else
如果继续拆 Request / Rule / Priority，又会产生过多胶水类型
当前目录和命名不够协调
```

### 方案 B：注册式 Transition

曾考虑给状态机增加：

```csharp
AddTransition(sourceStateId, targetStateId, condition)
```

未采用原因：

```text
PlayerStateMachine 已经较长
Transition 存储、优先级、AnyState 与条件执行会继续扩大状态机职责
当前还没有证明需要完整状态图框架
```

### 方案 C：State 自己返回下一个状态

曾考虑由 State 实现类似：

```text
EvaluateNextState()
```

当前没有确认。需要继续评估：

```text
自然流转是否可以放在具体 State
跨分支切换由谁决定
Ability / Hurt / Dead 等强制打断如何覆盖自然流转
是否会让 State 同时承担行为和切换决策而变重
```

### 方案 D：完整通用 HFSM Framework

曾考虑抽取通用 `StateGraph / StateTransition / StateMachine`，并使用泛型和独立程序集。

当前明确不做，原因：

```text
改造成本过高
会影响现有 Player HFSM
当前项目尚未出现第二个真实复用方
不符合每次推进一个小闭环的协作方式
```

## 五、当前确定的所有权边界

以下边界仍然有效：

```text
PlayerInputReader
└─ 读取输入并写入当前帧输入事实

PlayerContext
└─ 保存同一 Player 实例共享的运行时事实和运动意图

PlayerState
└─ 执行当前状态的生命周期与行为

PlayerStateMachine
└─ 维护活动状态路径并执行已经确定的状态切换

PlayerMotor
└─ 读取运动意图并通过 CharacterController 执行最终位移

PlayerController
└─ 创建依赖并安排 Update / FixedUpdate 生命周期顺序
```

尚未确定的唯一关键边界是：

```text
谁负责根据运行时事实选择下一个 PlayerStateId
```

## 六、当前实现状态

Player 模块当前已有：

```text
Context/PlayerContext.cs
Core/PlayerController.cs
Core/PlayerMotor.cs
HFSM/BasePlayerState.cs
HFSM/PlayerCompositeState.cs
HFSM/PlayerStateId.cs
HFSM/PlayerStateMachine.cs
HFSM/States/Ground/PlayerGroundedState.cs
HFSM/States/Ground/PlayerIdleState.cs
HFSM/States/Ground/PlayerMoveState.cs
Input/PlayerInputActions.cs
Input/PlayerInputReader.cs
```

当前 `Transition` 目录为空，没有状态切换业务类落盘。

本次未通过 Unity MCP 或 Unity Console 验证编译状态，因此 `PlayerInputReader` 新接线应在下一次继续前由 Unity Editor 编译确认。

## 七、当前尚未完成

```text
状态切换决策位置的最终设计
GroundedIdle <-> GroundedMove 自动切换
Input System + HFSM + Motor 完整移动验证
移动朝向旋转
Airborne / Jump / Fall
PlayerInputBuffer
Jump 输入缓存
PlayerAnimatorDriver
Idle / Move 动画切换
Ability System
攻击、闪避、受击、死亡
Enemy 侧内容
```

## 八、下一步建议

下一次不要直接创建新架构类型，先用纸面契约回答以下问题：

```text
1. 状态选择逻辑按“当前 State”组织，还是按“Player 功能域”组织
2. 自然流转与强制打断是否使用同一个入口
3. 状态选择结果只需要 PlayerStateId，还是确实需要额外元数据
4. 最少需要新增几个类型才能覆盖 Move、Jump、Ability、Hurt、Dead
5. 这个方案是否无需修改现有 ChangeState / LCA 核心
```

建议先用以下四组真实场景验证设计，而不是先追求抽象上的通用：

```text
Idle <-> Move
Grounded -> AirborneFall
AirborneFall -> GroundedIdle
Any Active State -> DisabledDead
```

能够用少量、统一命名的代码清晰覆盖这四组场景后，再确定最终落盘结构。

## 九、协作偏好

继续采用“古法编程”：

```text
用户手敲代码
AI 每次只给一个明确步骤
架构边界先讨论再实现
不一次性堆完整系统
不擅自创建大量抽象类型
不擅自重构已经可用的 HFSM 核心
```

本次新增确认：

```text
用户重视目录与命名的一致性
不接受职责含混的胶水类
希望设计具备合理扩展性，但不接受为了“未来复用”立即大规模框架化
```

## 十、工作区注意事项

当前工作区除 `PlayerController.cs` 外仍存在无关改动：

```text
.codex/config.toml 修改
若干旧目录 .meta 删除
SceneBackups 新增备份文件
```

后续提交 Player 输入接线时，需要继续筛选变更，不要混入上述无关内容。
