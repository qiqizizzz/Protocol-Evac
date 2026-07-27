# Protocol_Evac Player 闪避 Action 纵切与 Shift 输入分流记录

## 一、记录范围

本记录接续：

[../2026-7-26/1-Player空中闭环与InputBuffer系统记录.md](../2026-7-26/1-Player空中闭环与InputBuffer系统记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要完成 Player 闪避 Action 纵切、Shift 短按 / 长按输入分流，以及一次性 Dodge 动画资源问题的验证闭环：

```text
1. Shift 短按触发 Dodge，Shift 长按维持 Sprint
2. Dodge 作为 Action 分支叶子状态，不归入 Grounded
3. Dodge 输入接入 Input Buffer，并在 ActionDodge.Enter() 中消费
4. Action / ActionDodge HFSM 分支与 Transition Rules 落地
5. Dodge 位移通过 PlayerContext 写入强制水平速度，由 PlayerMotor 统一执行
6. Dodge 动画通过 PlayerAnimWriter 的一次性重播请求播放
7. Dodge.fbx 通过 Root Transform Rotation / Bake Into Pose 解决进入瞬间一帧闪动
```

本次没有进入 Attack、Skill、完整 Ability System、闪避冷却、无敌帧、体力消耗、Hurt / Dead 或 Enemy 侧开发。

## 二、本次确认的设计

### 1. Dodge 属于 Action，不属于 Grounded

本次确认 Dodge 不是地面移动状态，而是主动动作状态。它可以从地面触发，但语义上归入：

```text
Action
└─ ActionDodge
```

原因：

```text
Grounded
└─ 表达站立、移动、疾跑等地面移动形态

Action
└─ 表达攻击、闪避、技能、交互、使用道具等主动动作
```

后续 Attack、Skill、UseItem、Interact 也应继续归入 Action 分支，而不是塞进 Grounded。

### 2. Shift 一个按键分流两种意图

用户当前已经将 Shift 绑定为 Sprint 输入，本次没有新增第二个 InputAction，而是在 `PlayerInputReader` 中按按压时长解释：

```text
Shift 按下
└─ 记录按下时间

Shift 持续按住超过 SprintHoldTime
└─ IsSprintPressed = true

Shift 在 SprintHoldTime 前松开
└─ InputBuffer.Record(Dodge)
```

当前默认长按阈值由 `PlayerInputConfigSO.SprintHoldTime` 提供；配置缺失时使用代码默认值 `0.2f`。

### 3. Transition Rules 只判断，State Enter 才消费输入

本次延续上一份归档的 Input Buffer 边界：

```text
PlayerActionTransitionRules
└─ 只判断是否存在可用 Dodge 输入，以及当前状态是否允许进入 ActionDodge

PlayerDodgeState.Enter()
└─ m_context.InputBuffer.Consume(PlayerBufferedInputType.Dodge)
```

不要在 Transition Rule 中消费 Buffer，避免规则探测阶段改变输入状态。

### 4. Dodge 位移不写进 InputReader 或 Controller

当前 Dodge 的位移链路是：

```text
PlayerDodgeState.Enter()
├─ 解析本次闪避方向
├─ m_context.MoveDir = dodgeDirection
└─ m_context.SetForcedMoveVelocity(dodgeDirection * DodgeSpeed)

PlayerMotor.FixedTick()
└─ 统一读取 Context 并执行 CharacterController.Move(...)
```

`PlayerMotor` 仍是唯一移动执行者；`PlayerDodgeState` 只写入意图和临时运动事实。

### 5. 一次性动作动画不依赖 Trigger 主流程

本次 Dodge 动画沿用 Jump 已验证的“一次性动画重播请求”思路：

```text
PlayerDodgeState.Enter()
└─ m_context.RequestAnimReplay(PlayerStateId.ActionDodge)

PlayerAnimWriter.Tick()
└─ 消费请求并播放 Base Layer.Action.dodge
```

当前 Dodge 使用 `CrossFadeInFixedTime(..., 0f, 0, 0f)` 直接从起点切入，避免混合过程中带入上一帧姿势。

## 三、当前实现状态

### 1. 配置

本次涉及配置脚本：

```text
Assets/Scripts/Module/Player/Config/Input/PlayerInputConfigSO.cs
Assets/Scripts/Module/Player/Config/Action/PlayerDodgeConfigSO.cs
```

职责：

```text
PlayerInputConfigSO
└─ 提供 SprintHoldTime，用于区分 Shift 短按 Dodge 与长按 Sprint

PlayerDodgeConfigSO
└─ 提供 DodgeSpeed / DodgeDuration 等闪避位移参数
```

### 2. 输入

本次涉及输入脚本：

```text
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs
Assets/Scripts/Module/Player/Input/Buffer/PlayerBufferedInputType.cs
```

当前输入事实：

```text
Jump
└─ 在 recordBufferedInputs() 中按 WasPressedThisFrame 写入 InputBuffer

Dodge
└─ 由 Shift 短按松开时写入 InputBuffer

Sprint
└─ 由 Shift 长按超过 SprintHoldTime 后写入 IsSprintPressed
```

注意：Dodge 当前不是一个独立 InputAction，而是 Sprint 这个 InputAction 的短按分支。

### 3. Context / Motor / Direction Resolver

本次涉及运行时事实与移动执行：

```text
Assets/Scripts/Module/Player/Context/PlayerContext.cs
Assets/Scripts/Module/Player/Core/PlayerMotor.cs
Assets/Scripts/Module/Player/Core/PlayerMoveDirectionResolver.cs
```

当前边界：

```text
PlayerContext
├─ 保存 IsActionFinished
├─ 保存 IsMovementLocked
├─ 保存强制水平速度
└─ 保存一次性动画重播请求

PlayerMoveDirectionResolver
├─ 统一按相机 / 角色朝向解析移动方向
└─ 被 Move / Airborne / Dodge 共用

PlayerMotor
└─ 统一执行普通移动、空中移动与 Dodge 强制水平位移
```

### 4. HFSM Action 分支

本次涉及 Action 状态与规则：

```text
Assets/Scripts/Module/Player/HFSM/States/Action/PlayerActionState.cs
Assets/Scripts/Module/Player/HFSM/States/Action/PlayerDodgeState.cs
Assets/Scripts/Module/Player/HFSM/Transition/Rules/PlayerActionTransitionRules.cs
Assets/Scripts/Module/Player/HFSM/Animation/Rules/PlayerActionAnimRules.cs
```

当前 ActionDodge 运行流程：

```text
GroundedIdle / GroundedMove / GroundedSprint
└─ PlayerActionTransitionRules 判断存在 Dodge Buffer
   └─ 切换到 ActionDodge
      ├─ Consume(Dodge)
      ├─ IsMovementLocked = true
      ├─ SetForcedMoveVelocity(...)
      ├─ RequestAnimReplay(ActionDodge)
      ├─ 计时到 DodgeDuration 后 IsActionFinished = true
      └─ 退出时 ClearForcedMoveVelocity()
```

当前暂不支持空中闪避。空中短按 Shift 即使记录了 Dodge，也不会在 Airborne 分支被消费成 ActionDodge。

### 5. Animator 与动画资源

本次涉及动画写入与资源：

```text
Assets/Scripts/Module/Player/HFSM/Animation/PlayerAnimWriter.cs
Assets/Animation/千咲/Dodge.fbx
Assets/Animation/千咲/千咲_Animator.controller
```

用户已在 Animator 中配置：

```text
Base Layer
└─ Action
   └─ dodge
```

代码侧按状态路径播放：

```text
Base Layer.Action.dodge
```

## 四、关键架构边界

当前 Dodge 纵切需要继续保持以下边界：

```text
PlayerInputReader
└─ 只解释输入并写入 Context / InputBuffer
└─ 不判断 Dodge 能不能触发
└─ 不移动角色

PlayerInputBuffer
└─ 保存 Jump / Dodge 等离散输入意图
└─ 不知道 Grounded / Action / Ability

PlayerActionTransitionRules
└─ 只判断当前状态是否能进入 ActionDodge
└─ 不消费 Buffer
└─ 不播放动画

PlayerDodgeState
└─ 消费 Dodge 输入
└─ 锁定移动
└─ 写入强制位移
└─ 请求一次性动画
└─ 计时结束后标记 Action finished

PlayerMotor
└─ 只执行最终移动
└─ 不关心输入按键来源
└─ 不判断是否应该 Dodge

PlayerAnimWriter
└─ 统一写 Animator 参数和一次性动画重播
└─ State 不直接持有 Animator

PlayerController
└─ 只初始化配置、注册状态、注册规则、调度 Tick / FixedTick
```

不要把 Shift 分流、Dodge 触发条件、Action 结束回落、动画播放细节写回 `PlayerController`。

## 五、动画资源与导入经验

本次用户反馈 Dodge 进入时出现一帧闪动，最终通过 `Dodge.fbx` 导入设置解决：

```text
Dodge.fbx
└─ Root Transform Rotation
   ├─ Bake Into Pose: 开启
   ├─ Based Upon: Body Orientation
   └─ Offset: 0
```

该经验已同步记录到：

[../../../.codex/work_init_CodexAI/02-AI协作与实现边界.md](../../../.codex/work_init_CodexAI/02-AI协作与实现边界.md)

后续闪避、翻滚、攻击、技能等一次性动作如果出现进入瞬间闪动，优先检查：

```text
1. Clip 是否关闭 Loop Time / Loop Pose
2. Root Transform Rotation 是否需要 Bake Into Pose
3. Root Transform Position Y / XZ 是否需要 Bake Into Pose
4. Clip 第 0~1 帧是否存在突兀姿势
5. 代码是否从起点重播，而不是沿用 Animator 上一次 normalized time
```

不要一开始就把问题归因到 HFSM 或 CharacterController 位移。

## 六、当前已验证

本次收尾验证已由用户确认成功，当前结论：

```text
Shift 短按
└─ 进入 ActionDodge，播放 Dodge / Roll 动画

Shift 长按
└─ 进入或维持 Sprint，不触发 Dodge

Dodge 期间
├─ 普通移动不会抢掉闪避位移
├─ 动画进入不再出现一帧闪动
└─ 计时结束后能回到 GroundedIdle / GroundedMove

Airborne
└─ 当前不会触发地面 Dodge

重复短按
└─ 不会卡在 ActionDodge，也不会持续强制位移
```

此前代码侧已完成的验证：

```text
1. Unity AssetDatabase Refresh 成功
2. dotnet build .\Assembly-CSharp.csproj --no-restore 成功，0 Error
3. Unity Console 无近期 Error
4. Play Mode 探针确认 HFSM 可进入 ActionDodge
5. Play Mode 探针确认 Animator 可命中 Base Layer.Action.dodge
```

## 七、当前尚未完成

Dodge 当前是可运行纵切，不是完整 Ability：

```text
Dodge 冷却
Dodge 无敌帧
Dodge 体力消耗
Dodge 取消攻击 / 技能的规则
Dodge 被 Hurt / Dead 打断的规则
空中 Dodge
完整 DodgeAbility / AbilityDefinitionSO
```

Action 分支仍未完成：

```text
ActionAttack
ActionSkill
ActionInteract
ActionUseItem
完整 Ability System
完整 Transition Evaluator 优先级 / 打断规则
```

Enemy 侧本次没有开始。

## 八、下一步建议

下一步建议进入 `ActionAttack` 第一版，而不是立刻做完整 Ability System。

建议目标保持最小纵切：

```text
1. 确认 Attack 输入键位，例如鼠标左键或 J
2. 在 PlayerInputReader 中记录 Attack Buffer
3. 新增或补齐 ActionAttack 状态
4. Grounded -> ActionAttack 规则接入 PlayerActionTransitionRules
5. ActionAttack.Enter() 消费 Attack Buffer
6. ActionAttack 锁移动并请求攻击动画从起点播放
7. 用 AttackDuration 计时结束，回到 GroundedIdle / GroundedMove
```

第一版 Attack 暂时不要做：

```text
伤害判定
连招
取消窗口
命中停顿
武器碰撞盒
完整 AbilityDefinitionSO
```

原因是当前最需要验证的是 Action 分支是否能承载第二个动作类型。等 Attack 与 Dodge 都能稳定进入、锁定、播放、退出后，再抽 Ability System 会更自然，不容易提前写成石山。

## 九、工作区注意事项

归档创建前执行：

```text
git status --short --untracked-files=all
```

当前工作区在创建本归档前无未提交改动。

本次归档创建后预期新增：

```text
?? 设计文档/AI归档记录/2026-7-27/1-Player闪避Action纵切与Shift输入分流记录.md
```

如果后续准备提交，应重新执行：

```text
git status --short --untracked-files=all
```

以当时状态为准。

用户已在 Unity Editor 中完成并验证 Animator / `Dodge.fbx` 相关配置。后续 AI 不应覆盖这些用户侧资源设置，除非先读取资源状态并得到明确修改目标。

