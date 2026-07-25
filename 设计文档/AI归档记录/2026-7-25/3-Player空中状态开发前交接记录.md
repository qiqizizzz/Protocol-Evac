# Protocol_Evac Player 空中状态开发前交接记录

## 一、记录范围

本记录接续：

[2-Player视角切换与相机控制记录.md](2-Player视角切换与相机控制记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要记录 Player 视角闭环验证后的开发断点、AI 协作边界补充，以及下一步进入 Airborne / Jump / Fall 前的准确状态：

```text
1. 用户确认 Play Mode 视角与地面移动验证已经全部完成
2. 用户确认 Jump 输入已在 Input Actions 中绑定到 Space
3. 补充 AI 协作边界：保留简洁方法签名
4. 补充 AI 协作边界：内部 Init 不写重复防御式判空
5. 明确下一步进入 Player 空中状态最小闭环
```

本次没有实现 Airborne、Jump、Fall、Input Buffer、Ability、Hurt、Dead 或 Enemy 侧代码。

## 二、本次确认的协作偏好

当前 `.codex/work_init_CodexAI/02-AI协作与实现边界.md` 已补充两条用户明确偏好。

### 1. 保留简洁方法签名

方法参数较少、单行可读性足够时，不要擅自改成一行一个参数的折叠格式。

推荐保持：

```csharp
public void Init(PlayerContext context, PlayerViewConfigSO viewConfig, Transform viewRoot, Camera playerCamera)
```

不要无必要改成：

```csharp
public void Init(
    PlayerContext context,
    PlayerViewConfigSO viewConfig,
    Transform viewRoot,
    Camera playerCamera)
```

### 2. 内部 Init 不写重复防御式判空

内部装配流程调用的 `Init(...)`，例如 Binder / Resolver / Motor / ViewController 这类由 `PlayerController` 统一创建和传参的模块，不要层层添加重复判空、`QLog.Error` 和 `return`。

推荐：

```csharp
public void Init(PlayerStateMachine stateMachine, IReadOnlyDictionary<PlayerStateId, PlayerAnimRule.ResolveHandler> handlers)
{
    m_stateMachine = stateMachine;
    m_handlers = handlers;
}
```

不推荐：

```csharp
public void Init(PlayerStateMachine stateMachine, IReadOnlyDictionary<PlayerStateId, PlayerAnimRule.ResolveHandler> handlers)
{
    if (stateMachine == null)
    {
        QLog.Error("初始化玩家动画参数解析器失败：PlayerStateMachine 为空");
        return;
    }

    if (handlers == null)
    {
        QLog.Error("初始化玩家动画参数解析器失败：动画规则集合为空");
        return;
    }

    m_stateMachine = stateMachine;
    m_handlers = handlers;
}
```

边界原则：

```text
场景层级 / Inspector 配置 / 资源加载 / 外部输入
└─ 在具体获取位置记录明确错误

内部模块装配参数
└─ 不层层补重复防御式判空
```

## 三、当前实现状态

当前 Player 地面与视角链路已由用户在 Play Mode 中确认完成：

```text
WASD / Shift 地面移动
Idle / Walk / Run 地面动画表现
F1 第一人称视角切换
F3 第三人称视角切换
鼠标 Look 视角控制
第三人称移动朝向旋转
PlayerCamera 与旧 Main Camera 干扰检查
```

当前 Jump 输入绑定状态：

```text
Assets/Config/Input/PlayerInputActions.inputactions
└─ Player
   └─ Jump
      └─ Space [Keyboard]
```

当前生成代码中已存在：

```text
Assets/Scripts/Module/Player/Input/PlayerInputActions.cs
└─ m_Player_Jump
└─ PlayerActions.Jump
```

但当前业务读取层尚未接入 Jump：

```text
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs
└─ Tick() 当前只读取 Move / Sprint / Look / SwitchToFirstPerson / SwitchToThirdPerson
```

当前 `PlayerContext` 尚未包含：

```text
IsJumpPressed / RequestJump / JumpInput
```

当前 `PlayerMoveConfigSO` 尚未包含：

```text
JumpForce
```

当前 HFSM 已预留状态 Id：

```text
PlayerStateId.Airborne
PlayerStateId.AirborneJump
PlayerStateId.AirborneFall
```

但尚未创建对应状态类、转换规则或动画规则。

## 四、关键架构边界

下一步空中状态仍应沿用当前 Player 架构边界：

```text
PlayerInputReader
└─ 只读取 Jump 当前帧输入，并写入 PlayerContext

PlayerContext
└─ 保存 Jump 输入事实、地面事实、速度与运动意图

PlayerTransitionRule / PlayerTransitionSelector
└─ 根据 Context 事实决定 Grounded / Airborne 的状态转换

PlayerStateMachine
└─ 执行已经选定的状态切换

PlayerJumpState / PlayerFallState
└─ 写入跳跃或空中运动意图，不直接读取 InputActions

PlayerMotor
└─ 继续统一执行 CharacterController.Move 与重力 / 竖直速度

PlayerAnimRule / PlayerAnimResolver / PlayerAnimWriter
└─ 根据状态机结果写动画参数，不反推状态
```

注意：

```text
1. 不要把 Jump 判断写回 PlayerController
2. 不要让 PlayerMotor 自己决定进入 Jump / Fall 状态
3. 不要在第一版同时引入 Input Buffer
4. 不要在第一版混入 Ability / Dodge / Hurt / Dead
```

## 五、当前需要注意的问题

### 1. Jump 输入已绑定，但业务层未读取

下一次开发第一步应先在 `PlayerInputReader.Tick()` 中读取：

```csharp
m_context.IsJumpPressed = m_inputActions.Player.Jump.WasPressedThisFrame();
```

字段命名可按实际实现确认，但建议使用表达当前帧事实的名称，第一版不需要上升为 Input Buffer。

### 2. Jump 力度需要进入移动配置

建议在 `PlayerMoveConfigSO` 中新增：

```text
JumpForce
```

后续如果需要更细手感，再扩展：

```text
GravityMultiplier
FallGravityMultiplier
CoyoteTime
JumpBufferTime
```

第一版只做最小闭环，不要一次性加完整手感系统。

### 3. 空中移动要保留当前视角相对方向

当前 `PlayerMoveState` 已经实现第一人称 / 第三人称视角相对移动方向。空中第一版可以先延续地面水平移动结果，或新增轻量空中移动逻辑，但不要破坏当前视角移动边界。

### 4. `IsGrounded` 当前由 CharacterController 更新

当前 `PlayerMotor.FixedTick()` 在移动后写入：

```text
m_context.IsGrounded = m_characterController.isGrounded
```

空中转换规则要注意这个值的刷新时机。第一版可以接受一帧延迟，先验证闭环，再考虑更细的落地检测。

## 六、当前尚未完成

```text
PlayerInputReader 读取 Jump.WasPressedThisFrame()
PlayerContext 增加 Jump 输入事实并在 ResetRunTimeData() 中重置
PlayerMoveConfigSO 增加 JumpForce
PlayerMotor 支持由 Jump 状态写入竖直速度或跳跃请求
PlayerAirborneState
PlayerJumpState
PlayerFallState
PlayerAirTransitionRules
PlayerAirAnimRules
PlayerController.RegisterAllStates() 注册空中状态
PlayerTransitionBinder 绑定 Air 规则
PlayerAnimBinder 绑定 Air 动画规则
Play Mode 验证 Space 起跳、下落、落地回 Idle / Move
Input Buffer 与 Jump 缓存
Ability System 与 Action TransitionRules / AnimRules
Hurt / Dead 数据与 StatusRules
Enemy 侧内容
```

## 七、下一步建议

下一次建议直接进入 Player 空中状态最小闭环。

第一批目标：

```text
按 Space 起跳
自然下落
落地后无输入回 GroundedIdle
落地后有移动输入回 GroundedMove
```

推荐实现顺序：

```text
1. PlayerContext 增加 Jump 输入事实
2. PlayerInputReader.Tick() 读取 Player.Jump.WasPressedThisFrame()
3. PlayerMoveConfigSO 增加 JumpForce
4. 创建 HFSM/States/Air/
   ├─ PlayerAirborneState.cs
   ├─ PlayerJumpState.cs
   └─ PlayerFallState.cs
5. 创建 HFSM/Transition/Rules/PlayerAirTransitionRules.cs
6. 必要时扩展 PlayerMotor 的竖直速度写入入口
7. 在 PlayerController 中注册 Air 状态并绑定 Air TransitionRules
8. Play Mode 验证 Jump / Fall / Landing
```

暂时不做：

```text
Jump Buffer
Coyote Time
多段跳
空中攻击
Dodge
Ability
Hurt / Dead
Enemy
```

## 八、工作区注意事项

归档创建前执行：

```text
git status --short --untracked-files=all
```

当前没有未提交改动。

本次归档创建后预期新增：

```text
?? 设计文档/AI归档记录/2026-7-25/3-Player空中状态开发前交接记录.md
```

后续如果准备提交，应重新执行：

```text
git status --short --untracked-files=all
```

以当时状态为准。
