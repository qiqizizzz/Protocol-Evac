# Protocol_Evac PlayerContext 与移动基础开发进展记录

## 一、记录范围

本记录接续：

[../2026-7-16/1-Player战斗系统开发交接记录.md](../2026-7-16/1-Player战斗系统开发交接记录.md)

[../2026-7-16/2-PlayerHFSM开发进展记录.md](../2026-7-16/2-PlayerHFSM开发进展记录.md)

[../2026-7-16/3-PlayerStateMachine开发进展记录.md](../2026-7-16/3-PlayerStateMachine开发进展记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次记录范围是 Player 第一阶段中 `PlayerContext`、玩家移动配置，以及进入 `PlayerController` / `PlayerMotor` 前的交接状态。

本阶段仍然只推进 Player，不实现 Enemy、Input Buffer、Ability System 或 Transition Evaluator。

## 二、设计文档中确认的顺序

主设计文档第 12 节的第一阶段落地顺序为：

```text
1. PlayerStateId / BasePlayerState / PlayerCompositeState
2. PlayerStateMachine
3. PlayerContext
4. PlayerController
5. PlayerMotor
6. Grounded / GroundedIdle / GroundedMove
7. Airborne / AirborneJump / AirborneFall
8. PlayerInputReader / PlayerInputBuffer
9. 确认输入缓存、移动、跳跃与落地流程稳定
```

因此当前完成 `PlayerContext` 与移动配置后，下一步应进入：

```text
PlayerController
```

`PlayerMotor` 紧随其后，但不应跳过 `PlayerController`，因为 Context 的创建、组件缓存、系统调度入口都由 Controller 持有。

## 三、必须纠正的运动后端决策

此前讨论中曾误提 Rigidbody / CapsuleCollider 路线，该说法不符合已确认设计。

当前项目 Player 移动后端应继续使用：

```text
CharacterController
```

相关边界继续沿用 7.16 交接记录中的结论：

- 使用 `CharacterController`
- 常规移动不使用 Root Motion
- 特殊攻击位移以后由 Ability Motion 驱动
- Ability 不直接操作 `CharacterController`
- `PlayerMotor` 是最终执行移动的唯一入口
- `PlayerController` 只做初始化、生命周期与调度

后续实现 `PlayerMotor` 时，应通过 `CharacterController.Move(...)` 执行最终位移，不引入 Rigidbody 驱动。

## 四、本次实际完成内容

### 1. PlayerStateMachine 补充修正

当前文件：

[../../../Assets/Scripts/Module/Player/HFSM/PlayerStateMachine.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerStateMachine.cs)

当前已确认存在以下能力：

```text
RegisterState()
Init()
ChangeState()
Tick()
FixedTick()
```

本次补充确认：

- `m_isExecutingLifecycle` 用于生命周期回调期间的重入保护
- `Enter` / `Exit` / `Tick` / `FixedTick` 期间发生状态切换重入时，使用 `QLog.Throw(new InvalidOperationException(...))`
- 生命周期回调异常后会标记 `IsFaulted`
- 故障状态机不应继续执行后续调度

该部分属于 HFSM 基础设施的收尾修正，不改变后续 Player 架构顺序。

### 2. PlayerContext 已创建

当前文件：

[../../../Assets/Scripts/Module/Player/Context/PlayerContext.cs](../../../Assets/Scripts/Module/Player/Context/PlayerContext.cs)

当前命名空间：

```csharp
Module.Player.Context
```

当前职责：

```text
保存玩家运行时上下文中的输入、环境状态与移动意图
```

当前字段包含：

```text
Transform
MoveInput
MoveDir
TargetMoveSpeed
Velocity
IsMovementLocked
IsGrounded
IsSprintPressed
IsInputLocked
```

当前 `ResetRunTimeData()` 会重置移动输入、移动方向、目标速度、速度、移动锁、落地状态、疾跑输入和输入锁。

目前该 Context 仍保持轻量，只覆盖移动与输入相关的最小运行时数据，没有提前塞入 Ability、Transition 或 Combat 数据。

### 3. 玩家移动配置已创建

当前文件：

[../../../Assets/Scripts/Module/Player/Config/Move/PlayerMoveConfigSO.cs](../../../Assets/Scripts/Module/Player/Config/Move/PlayerMoveConfigSO.cs)

当前命名空间：

```csharp
Module.Player.Config.Move
```

当前类名：

```csharp
PlayerMoveConfigSO
```

当前菜单：

```csharp
配置/玩家/移动/玩家移动配置
```

当前配置内容包括：

```text
WalkSpeed
SprintSpeed
Acceleration
Deceleration
RotationSpeed
GroundCheckDistance
GroundLayer
```

当前配置拆到 `Config/Move/` 是合理的，因为它只表达玩家移动参数，避免 `PlayerConfigSO` 过早膨胀成总配置。

## 五、当前需要注意的问题

### 1. PlayerContext 构造函数的空 Transform 处理

当前 `PlayerContext(Transform transform)` 中，当 `transform == null` 时使用：

```csharp
QLog.Error("Transform is null");
return;
```

这会导致对象仍然构造成功，但 `Transform` 没有被赋值。

按项目规范，“必要引用不应静默失效”。后续接入 `PlayerController` 前，建议改成真正中断流程的方式，例如：

```csharp
QLog.Throw(new ArgumentNullException(nameof(transform)));
```

如果暂时不想改，也至少要记住：当前写法会留下半初始化 Context 的风险。

### 2. PlayerContext 命名可后续统一

当前命名：

```text
MoveDir
ResetRunTimeData
```

后续可考虑统一为：

```text
MoveDirection
ResetRuntimeData
```

这不是当前阻塞项，不影响进入 `PlayerController`。

### 3. PlayerMoveConfigSO 文件头类名仍是旧名

当前 `PlayerMoveConfigSO.cs` 文件头中写的是：

```text
类    名: PlayerConfigSO.cs
```

但实际文件名与类名是：

```text
PlayerMoveConfigSO.cs
PlayerMoveConfigSO
```

后续整理代码规范时应修正文件头注释，避免归档、代码搜索和规范检查时产生误导。

### 4. PlayerMoveConfigSO 暂缺 CharacterController 移动常用参数

当前移动配置已有水平移动、旋转和地面检测参数。

后续实现 `CharacterController` 版 `PlayerMotor` 时，大概率还需要补充：

```text
Gravity
GroundedVerticalVelocity
```

其中 `GroundedVerticalVelocity` 用于角色落地时给一个轻微向下速度，避免 `CharacterController.isGrounded` 在斜坡或地面边缘附近抖动。

是否现在加入这两个参数，可以等写 `PlayerMotor` 时再决定。

## 六、下一步建议

下一步创建：

```text
Assets/Scripts/Module/Player/Core/PlayerController.cs
```

第一版 `PlayerController` 只做以下事情：

```text
1. 缓存 Transform
2. 校验并缓存 CharacterController
3. 创建 PlayerContext
4. 暂时持有 PlayerMoveConfigSO 引用
5. 预留 PlayerMotor 引用
6. 在 FixedUpdate 中预留 Motor 调度入口
```

第一版不要在 `PlayerController` 中写移动细节，不要读取复杂输入，不要接 Ability，也不要直接写状态切换规则。

`PlayerController` 的职责是把 Player 实例的生命周期和依赖关系先立起来。

紧随其后的步骤才是：

```text
Assets/Scripts/Module/Player/Core/PlayerMotor.cs
```

`PlayerMotor` 第一版应围绕 `CharacterController` 实现：

```text
1. Init(PlayerContext context, PlayerMoveConfigSO moveConfig)
2. FixedTick(float fixedDeltaTime)
3. 读取 Context.MoveDir / TargetMoveSpeed / IsMovementLocked
4. 根据加速度和减速度计算水平速度
5. 应用重力和贴地速度
6. CharacterController.Move(...)
7. 回写 Context.Velocity 和 Context.IsGrounded
```

## 七、当前不建议做的事情

暂时不要推进：

- 自动化测试
- New Input System
- Input Buffer
- Ability Runtime
- Transition Evaluator
- 普攻三连段
- 闪避无敌帧
- Hurt / Dead
- Enemy 侧任何内容
- Rigidbody 版移动实现

当前重点仍然是第一阶段移动基础闭环：

```text
Context
→ Controller
→ Motor(CharacterController)
→ Grounded / Idle / Move
```

