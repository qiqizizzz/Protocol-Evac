# Protocol_Evac Player 空中闭环与 Input Buffer 系统记录

## 一、记录范围

本记录接续：

[../2026-7-25/3-Player空中状态开发前交接记录.md](../2026-7-25/3-Player空中状态开发前交接记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要完成 Player 空中状态最小闭环、Animator 地面 Blend Tree 参数整理，以及 Input Buffer 系统第一版：

```text
1. Player Jump / Airborne / Fall HFSM 代码链路落地
2. Jump 参数归入 PlayerAirConfigSO，不混入 PlayerMoveConfigSO
3. Animator 地面表现改为 Grounded_Common Blend Tree
4. Animator 参数改为 moveSpeed / verticalSpeed / isGrounded
5. 新增 Input/Buffer 目录与通用离散输入缓存系统
6. Jump 接入 Input Buffer、Jump Buffer Time 与 Coyote Time
7. 修复连续跳时 jump 动画可能卡尾帧的问题：进入 AirborneJump 时请求动画从起点重播
```

本次没有进入 Attack、Dodge、Skill、完整 Ability System、Hurt / Dead 或 Enemy 侧开发。

## 二、本次确认的设计

### 1. Jump 配置属于 Air，不属于 Move

用户明确指出：Jump 从语义上属于空中模块，不应放入 `PlayerMoveConfigSO`。

本次按该边界新增：

```text
Assets/Scripts/Module/Player/Config/Air/PlayerAirConfigSO.cs
Assets/Config/Player/Air/PlayerAirConfig.asset
```

当前 `PlayerAirConfigSO` 字段：

```text
JumpForce
AirMoveSpeed
JumpBufferTime
CoyoteTime
```

当前 `PlayerMoveConfigSO` 继续只负责：

```text
WalkSpeed
SprintSpeed
Acceleration
Deceleration
RotationSpeed
GroundCheckDistance
GroundLayer
```

### 2. Animator 不照搬 HFSM

本次用户决定地面层使用 Blend Tree，而不是 `Idle / Walk / Run` 三状态互切。

当前 Animator 设计方向：

```text
Base Layer
├─ Grounded_Common   // Blend Tree，使用 moveSpeed 混合 Idle / Walk / Run
└─ jump              // 当前使用完整 jump clip：起跳、上升、下落、落地
```

Animator 参数从旧的标签式参数：

```text
moveSpeed
isMoving
isSprinting
```

调整为运动事实参数：

```text
moveSpeed
verticalSpeed
isGrounded
```

关键结论：

```text
locomotion 使用连续事实参数驱动，例如 moveSpeed
one-shot 动画使用事件 / 请求驱动，例如 jump 从起点重播
HFSM 负责玩法状态，Animator 负责表现状态，不要求二者一一同构
```

### 3. Input Buffer 与 Coyote Time 是两个概念

本次确认的核心边界：

```text
Input Buffer
└─ 玩家提前按了什么，属于输入容错

Coyote Time
└─ 当前状态还允许什么，属于状态宽容窗口
```

也就是说，Jump Buffer 记录“玩家刚刚按过 Jump”，Coyote Time 记录“玩家离开地面后仍可被允许起跳的一小段时间”。二者组合得到更自然的跳跃手感。

## 三、Input Buffer 系统作用

### 1. 解决按键必须精确命中状态帧的问题

没有 Input Buffer 时，Jump 只有在某一帧满足以下条件才会生效：

```text
Jump.WasPressedThisFrame()
&& IsGrounded
```

这会导致两类手感问题：

```text
1. 玩家落地前一瞬间提前按 Space，输入被丢弃
2. 玩家刚离开平台边缘一瞬间按 Space，输入被丢弃
```

Input Buffer 让离散输入保留短暂时间，Coyote Time 让状态许可保留短暂时间：

```text
提前按 Jump
└─ InputBuffer 记住 Jump，在 JumpBufferTime 内仍有效

刚离地按 Jump
└─ LastGroundedTime 仍在 CoyoteTime 内，允许起跳
```

这不是为了让玩家“多跳”，而是为了让玩家意图不会因为帧级时机误差消失。

### 2. 分离输入意图与玩法状态

本次没有用 `PlayerStateId` 作为 Buffer key，而是新增 `PlayerBufferedInputType`：

```text
Jump
Attack
Dodge
Skill
```

原因：

```text
Jump 输入不等于 AirborneJump 状态
Attack 输入不等于某一个具体 AttackState
Dodge 输入未来可能触发地面闪避、空中闪避或被状态规则拒绝
```

Input Buffer 保存的是“玩家输入意图”，Transition Rules 才决定这个意图能不能变成某个状态。

### 3. TransitionRule 只判断，不消费

本次确认并采用：

```text
PlayerAirTransitionRules.canGroundedJump()
PlayerAirTransitionRules.canCoyoteJump()
PlayerAirTransitionRules.hasBufferedJump()
```

这些方法只做判断，不调用 `Consume(...)`。

真正消费输入的位置是：

```text
PlayerJumpState.Enter()
└─ m_context.InputBuffer.Consume(PlayerBufferedInputType.Jump)
```

这样可以避免“规则选择器只是试探某条规则是否满足，却改变了输入缓存状态”的隐性问题。

## 四、当前实现状态

### 1. Input Buffer 文件结构

当前新增目录：

```text
Assets/Scripts/Module/Player/Input/Buffer/
├─ PlayerBufferedInputType.cs
├─ PlayerBufferedInputSlot.cs
└─ PlayerInputBuffer.cs
```

职责：

```text
PlayerBufferedInputType
└─ 定义可缓存的离散输入意图：Jump / Attack / Dodge / Skill

PlayerBufferedInputSlot
└─ 保存一次离散输入记录：Type / PressedTime / IsConsumed / HasValue

PlayerInputBuffer
└─ 管理离散输入记录：Record / Has / Consume / Clear / ClearAll
```

当前 `PlayerInputBuffer` 不知道：

```text
HFSM
Grounded / Airborne
Animator
Ability
技能冷却
```

它只知道输入是否被记录、是否过期、是否被消费。

### 2. Jump 输入数据流

当前 Jump 数据流：

```text
PlayerInputReader.Tick()
└─ recordBufferedInputs()
   └─ recordBufferedInput(PlayerBufferedInputType.Jump, Jump.WasPressedThisFrame())
      └─ PlayerInputBuffer.Record(Jump, Time.time)

PlayerMotor.FixedTick()
└─ CharacterController.Move(...)
└─ m_context.IsGrounded = m_characterController.isGrounded
└─ if IsGrounded:
   └─ m_context.LastGroundedTime = Time.time

PlayerAirTransitionRules
├─ Grounded -> AirborneJump
│  └─ canGroundedJump()
│     └─ IsGrounded && hasBufferedJump()
└─ AirborneFall -> AirborneJump
   └─ canCoyoteJump()
      └─ !IsGrounded && hasBufferedJump() && now - LastGroundedTime <= CoyoteTime

PlayerJumpState.Enter()
├─ InputBuffer.Consume(Jump)
├─ RequestAnimReplay(AirborneJump)
└─ Velocity.y = JumpForce
```

### 3. PlayerContext 当前新增事实

当前 `PlayerContext` 已持有：

```text
InputBuffer
LastGroundedTime
AnimReplayStateId
```

`IsJumpPressed` 临时字段已经移除，Jump 不再有两套事实来源。

### 4. PlayerAirConfig 当前配置

当前资源：

```text
Assets/Config/Player/Air/PlayerAirConfig.asset
```

当前默认值：

```text
JumpForceValue: 6
AirMoveSpeedValue: 4
JumpBufferTimeValue: 0.12
CoyoteTimeValue: 0.1
```

调手感时优先调整：

```text
JumpForce       // 跳跃高度与动画时长匹配
JumpBufferTime  // 落地前预输入容忍时间
CoyoteTime      // 离地后仍允许起跳的宽容时间
```

### 5. 动画重播请求

用户反馈连续跳时出现 jump 动画卡尾帧 / 僵直问题。原因分析：

```text
完整 jump clip 包含起跳到落地
连续跳时，代码可能已经再次进入 AirborneJump
但 Animator 可能还未真正完成 jump -> Grounded_Common -> jump 往返
导致第二次跳没有从 jump 起点重新播放，而停在上一段 jump 尾帧附近
```

本次采用方案：

```text
PlayerJumpState.Enter()
└─ m_context.RequestAnimReplay(PlayerStateId.AirborneJump)

PlayerAnimWriter.Tick()
└─ applyReplayRequest()
   └─ m_animator.CrossFadeInFixedTime("Base Layer.jump", 0.03f, 0, 0f)
```

边界：

```text
PlayerJumpState 不直接拿 Animator
PlayerContext 只保存一次性表现请求
PlayerAnimWriter 统一消费请求并执行 Animator 操作
```

该请求是一次性的，`ConsumeAnimReplayRequest()` 会清空请求，避免每帧把 jump 卡回第一帧。

## 五、关键架构边界

当前 Player 空中 / Buffer 边界如下：

```text
PlayerInputReader
└─ 读取 Input System
└─ 连续输入直接写 Context：Move / Sprint / Look / ViewMode
└─ 离散输入写入 InputBuffer：Jump

PlayerInputBuffer
└─ 保存离散输入事件
└─ 不判断输入能不能生效
└─ 不消费状态，只消费输入记录

PlayerContext
└─ 保存运行时事实：InputBuffer、IsGrounded、LastGroundedTime、Velocity
└─ 保存一次性表现请求：AnimReplayStateId

PlayerAirTransitionRules
└─ 组合输入缓存与状态事实，判断能否切换状态
└─ 只判断，不消费 Buffer

PlayerJumpState
└─ 进入状态时消费 Jump 输入
└─ 写入起跳竖直速度
└─ 发起 jump 动画重播请求

PlayerMotor
└─ 执行 CharacterController.Move
└─ 刷新 IsGrounded / LastGroundedTime

PlayerAnimWriter
└─ 写入 Animator 参数：moveSpeed / verticalSpeed / isGrounded
└─ 消费一次性动画重播请求
```

不要把以下逻辑写回 `PlayerController`：

```text
Jump 是否可触发
Buffer 是否有效
Coyote Time 是否有效
Animator jump 是否应该重播
```

`PlayerController` 仍只负责初始化与调度。

## 六、当前需要注意的问题

### 1. Grounded Jump 与 Coyote Jump 必须分开

本次曾出现连续跳僵直问题，最初原因之一是：

```text
Grounded -> AirborneJump
AirborneFall -> AirborneJump
```

共用同一个 `canJump()`，导致落地缓存跳可能在 `AirborneFall` 中抢先触发，跳过 `AirborneFall -> Grounded`。

当前已拆为：

```text
canGroundedJump()
└─ context.IsGrounded && hasBufferedJump()

canCoyoteJump()
└─ !context.IsGrounded && hasBufferedJump() && now - LastGroundedTime <= CoyoteTime
```

后续扩展其它 Buffer 输入时也要注意：同一个输入意图在不同状态下可能对应不同条件，不能偷懒共用一个过宽的 canXxx。

### 2. 当前 Animator 仍只使用一个完整 jump clip

当前没有拆 `JumpStart / FallLoop / Land` 动画状态。用户当前使用完整 `jump` clip，代码通过 `CrossFadeInFixedTime` 确保每次进入 `AirborneJump` 时从起点重播。

后续如果资源允许，更标准的动作游戏 Animator 结构仍可改为：

```text
Grounded_Common -> JumpStart -> FallLoop -> Grounded_Common
```

但在当前资源条件下，保留完整 jump clip 并使用一次性重播请求是更小的改动。

### 3. Buffer 当前只接入 Jump

`PlayerBufferedInputType` 已预留：

```text
Attack
Dodge
Skill
```

但当前业务层只记录和消费：

```text
Jump
```

后续接入 Attack / Dodge 时，应继续沿用：

```text
InputReader.Record(...)
TransitionRules.Has(...)
ActionState.Enter().Consume(...)
```

不要让 `InputBuffer` 直接知道 Ability 或 HFSM。

## 七、当前尚未完成

```text
连续跳接入动画重播请求后的最终 Play Mode 复测
JumpBufferTime / CoyoteTime 的手感微调
Attack / Dodge / Skill 输入接入 InputBuffer
Ability System 第一版
Action 状态层与 Action TransitionRules / AnimRules
Hurt / Dead 数据与 StatusRules
Enemy 侧内容
必要时将完整 jump clip 拆成 JumpStart / FallLoop / Land
```

## 八、下一步建议

下一次建议先做验证，不急着进入 Ability：

```text
1. Play Mode 连续测试 Space 跳跃
2. 验证落地前提前按 Space 是否能落地后自动起跳
3. 验证刚离地后按 Space 是否能在 CoyoteTime 内起跳
4. 验证连续跳时 jump 动画是否从起点重播，不再卡尾帧
5. 根据手感调 PlayerAirConfig.asset：
   - JumpBufferTime
   - CoyoteTime
   - JumpForce
```

如果验证稳定，再进入：

```text
Player Ability System 第一版
├─ Dodge 输入接入 InputBuffer
├─ Action / Dodge 状态
├─ DodgeAbility 或轻量 DodgeState
└─ Action TransitionRules / AnimRules
```

建议先从 Dodge 而不是 Attack 开始，因为 Dodge 更容易验证 Action 状态、输入缓存、移动锁定、动画重播请求这几条链路。

## 九、工作区注意事项

归档创建前执行：

```text
git status --short --untracked-files=all
```

当前工作区在创建本归档前无未提交改动。

本次归档创建后预期新增：

```text
?? 设计文档/AI归档记录/2026-7-26/1-Player空中闭环与InputBuffer系统记录.md
```

后续如果准备提交，应重新执行：

```text
git status --short --untracked-files=all
```

以当时状态为准。
