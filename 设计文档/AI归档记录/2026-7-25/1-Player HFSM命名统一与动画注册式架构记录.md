# Protocol_Evac Player HFSM 命名统一与动画注册式架构记录

## 一、记录范围

本记录接续：

[../2026-7-23/1-千咲动画导入与Player动画边界纠偏记录.md](../2026-7-23/1-千咲动画导入与Player动画边界纠偏记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要处理 Player HFSM 相关代码的命名、目录和动画表现层边界：

```text
1. 将 Transition 相关类型统一命名为 PlayerTransition*
2. 将动画表现层从 PlayerAnimatorDriver 调整为 PlayerAnimWriter / Resolver / Rule / Binder
3. 将动画参数解析改为 Rules.Create + Binder.Bind 的注册式结构
4. 将 PlayerController.Awake() 拆成 initCore / initHFSM / initAnim 三段初始化
```

本次没有进入 Airborne、Jump、Fall、Input Buffer、Ability、Hurt、Dead 或 Enemy 侧开发。

## 二、本次确认的命名与目录约定

当前 Player HFSM 相关代码采用以下命名边界：

```text
State
└─ 具体 HFSM 节点，例如 PlayerMoveState

Transition
└─ 状态转换规则、优先级与选择器，例如 PlayerTransitionRule

Animation
└─ 状态机结果到 Animator 参数的表现层，例如 PlayerAnimResolver

Rules
└─ 按功能域声明一组规则，例如 PlayerMoveTransitionRules / PlayerMoveAnimRules

Binder
└─ 收集当前 Player 启用的规则，例如 PlayerTransitionBinder / PlayerAnimBinder

Selector
└─ 每帧选择可触发的状态转换规则

Resolver
└─ 每帧根据当前状态解析动画参数

Writer
└─ 将动画参数写入 Animator
```

用户确认的偏好：

```text
1. HFSM 本身就是状态机核心，可以承载 Transition 和 Animation 子目录
2. State / HFSM 状态类不要被其他装配概念污染
3. 不使用 Registry 命名；当前只保留 Rules + Binder
4. 不再使用 PlayerAnimatorDriver，改用 PlayerAnimWriter，避免职责继续膨胀
5. 不新增 PlayerAnimationStateId，继续以 PlayerStateId 作为唯一状态键
6. PlayerAnimParams 不能通过长构造函数传递参数，应作为可写参数包扩展
```

## 三、当前实现状态

当前 Player 关键目录为：

```text
Assets/Scripts/Module/Player/
├─ Core/
│  ├─ PlayerController.cs
│  └─ PlayerMotor.cs
├─ Context/
│  └─ PlayerContext.cs
├─ Config/Move/
│  └─ PlayerMoveConfigSO.cs
├─ Input/
│  ├─ PlayerInputActions.cs
│  └─ PlayerInputReader.cs
└─ HFSM/
   ├─ PlayerStateId.cs
   ├─ BasePlayerState.cs
   ├─ PlayerCompositeState.cs
   ├─ PlayerStateMachine.cs
   ├─ States/Ground/
   │  ├─ PlayerGroundedState.cs
   │  ├─ PlayerIdleState.cs
   │  └─ PlayerMoveState.cs
   ├─ Transition/
   │  ├─ PlayerTransitionPriority.cs
   │  ├─ PlayerTransitionRule.cs
   │  ├─ PlayerTransitionSelector.cs
   │  ├─ Binders/PlayerTransitionBinder.cs
   │  └─ Rules/PlayerMoveTransitionRules.cs
   └─ Animation/
      ├─ PlayerAnimParams.cs
      ├─ PlayerAnimRule.cs
      ├─ PlayerAnimResolver.cs
      ├─ PlayerAnimWriter.cs
      ├─ Binders/PlayerAnimBinder.cs
      └─ Rules/PlayerMoveAnimRules.cs
```

当前 Transition 数据流：

```text
PlayerMoveTransitionRules.Create(m_context)
→ PlayerTransitionBinder.Bind(...)
→ PlayerTransitionSelector(m_stateMachine, m_transitionBinder.Rules)
→ PlayerStateMachine.ChangeState(...)
```

当前 Animation 数据流：

```text
PlayerMoveAnimRules.Create(m_context, MoveConfig)
→ PlayerAnimBinder.Bind(...)
→ PlayerAnimResolver.Init(m_stateMachine, m_animBinder.Handlers)
→ PlayerAnimWriter.Tick(...)
→ Animator.SetBool / SetFloat
```

`PlayerAnimResolver` 当前不再直接依赖：

```text
PlayerContext
PlayerMoveConfigSO
未来 AirConfig / AbilityController / Status 数据
```

它只依赖：

```text
PlayerStateMachine
IReadOnlyDictionary<PlayerStateId, PlayerAnimRule.ResolveHandler>
```

## 四、关键架构边界

当前职责边界确定为：

```text
PlayerController
└─ 缓存必要引用，初始化各模块，安排 Update / FixedUpdate 调度

PlayerInputReader
└─ 读取当前帧输入并写入 PlayerContext

PlayerContext
└─ 保存当前 Player 实例的运行时事实和运动意图

PlayerState
└─ 当前状态行为，写入移动意图等结果，不决定下一个状态

PlayerTransitionRule
└─ 描述一条状态转换边：来源、目标、优先级、条件

PlayerMoveTransitionRules
└─ 声明地面移动相关状态转换规则

PlayerTransitionBinder
└─ 收集当前 Player 启用的状态转换规则，不写业务条件

PlayerTransitionSelector
└─ 每帧按优先级选择第一条满足条件的转换规则

PlayerAnimRule
└─ 描述一个状态对应的动画参数处理函数

PlayerMoveAnimRules
└─ 声明 GroundedIdle / GroundedMove 的动画参数处理逻辑

PlayerAnimBinder
└─ 收集当前 Player 启用的动画规则处理函数

PlayerAnimResolver
└─ 根据当前 PlayerStateId 查找并执行动画规则，生成 PlayerAnimParams

PlayerAnimWriter
└─ 只负责把 PlayerAnimParams 写入 Animator

PlayerMotor
└─ 读取 PlayerContext 中的运动意图并执行 CharacterController.Move
```

`PlayerController.Awake()` 当前已拆为：

```text
Awake
├─ 缓存 Transform / CharacterController / Animator
├─ 校验 CharacterController / Animator / MoveConfig
├─ initCore()
│  ├─ PlayerContext
│  ├─ PlayerInputReader
│  └─ PlayerMotor
├─ initHFSM()
│  ├─ RegisterAllStates()
│  ├─ PlayerTransitionBinder
│  └─ PlayerTransitionSelector
├─ initAnim()
│  ├─ PlayerAnimBinder
│  ├─ PlayerAnimResolver
│  └─ PlayerAnimWriter
└─ m_isInited = true
```

## 五、当前需要注意的问题

### 1. 当前代码尚未 Unity 编译验证

本次在本地执行过：

```text
git diff --check -- Assets/Scripts/Module/Player
```

结果没有空白错误，仅有 CRLF 提示。

但本次没有通过 Unity MCP / Unity Editor 完成脚本编译验证。当前环境中曾出现：

```text
unity-mcp-cli 不在 PATH
```

因此下一次继续前，应优先让 Unity 编译一次并检查 Console。

### 2. Animation 注册式结构只是第一版

当前 `PlayerAnimParams` 只包含：

```text
IsMoving
IsSprinting
MoveSpeed
```

后续加入 Jump / Fall / Attack / Dodge / Hurt / Dead 时，可以继续扩字段，但不要恢复长构造函数形式。

### 3. Transition 与 Animation 目录已经移动

旧路径不应再使用：

```text
Assets/Scripts/Module/Player/Transition/
Assets/Scripts/Module/Player/Core/PlayerAnimatorDriver.cs
```

当前应使用：

```text
Assets/Scripts/Module/Player/HFSM/Transition/
Assets/Scripts/Module/Player/HFSM/Animation/
```

## 六、当前尚未完成

```text
Unity 编译验证
Console Error / Exception / Warning 检查
GameScene Play Mode 验证 Idle / Walk / Run 动画表现
下载或制作真正 Idle 动作替换占位 Idle
确认 Running / Walking 是否需要 In Place 或 Bake Into Pose
移动朝向旋转
Airborne / Jump / Fall 状态与对应 TransitionRules / AnimRules
落地时根据输入直接选择 GroundedIdle / GroundedMove
Input Buffer 与 Jump 输入缓存
Ability System 与 Action TransitionRules / AnimRules
Hurt / Dead 数据与 Status TransitionRules / AnimRules
Enemy 侧内容
```

## 七、下一步建议

下一步不建议马上写 Airborne。建议先做一次工程安全验证：

```text
1. 打开 Unity，等待脚本编译完成
2. 检查 Console 是否存在 Error / Exception / Warning
3. 若有编译错误，优先修复命名空间、文件名与类名不一致问题
4. Play Mode 验证地面移动与动画：
   - 无输入时 Idle
   - WASD 时 Walk
   - Shift + WASD 时 Run
   - 松开输入回 Idle
5. 如果动画表现稳定，再进入移动朝向旋转
```

如果编译和地面表现均稳定，再进入下一批代码：

```text
HFSM/States/Air/
├─ PlayerAirborneState.cs
├─ PlayerJumpState.cs
└─ PlayerFallState.cs

HFSM/Transition/Rules/
└─ PlayerAirTransitionRules.cs

HFSM/Animation/Rules/
└─ PlayerAirAnimRules.cs
```

第一批 Air 场景仍建议保持：

```text
Grounded → AirborneJump
Grounded → AirborneFall
AirborneJump → AirborneFall
AirborneFall → GroundedIdle / GroundedMove
```

## 八、工作区注意事项

归档创建时执行：

```text
git status --short --untracked-files=all
```

当前 Git 只显示新增归档文件：

```text
?? 设计文档/AI归档记录/2026-7-25/1-Player HFSM命名统一与动画注册式架构记录.md
```

也就是说，当前 Player 代码文件在 Git 视角下没有未提交差异；但文件内容已经呈现为本记录描述的结构：

```text
Assets/Scripts/Module/Player/HFSM/Animation/
Assets/Scripts/Module/Player/HFSM/Transition/
```

后续如果准备提交，应重新执行 `git status --short --untracked-files=all`，以当时状态为准。
