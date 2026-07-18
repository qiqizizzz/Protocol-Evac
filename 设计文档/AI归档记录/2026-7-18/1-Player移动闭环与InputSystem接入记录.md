# Protocol_Evac Player 移动闭环与 Input System 接入记录

## 一、记录范围

本记录接续：

[../2026-7-17/1-PlayerContext与移动基础开发进展记录.md](../2026-7-17/1-PlayerContext与移动基础开发进展记录.md)

[../2026-7-17/2-PlayerController开发前后交接记录.md](../2026-7-17/2-PlayerController开发前后交接记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次记录范围是 Player 第一阶段中 `PlayerMotor`、Grounded 最小状态组、场景挂载、动画测试与 Unity New Input System 接入。

当前仍然只推进 Player 移动基础闭环，不进入 Ability System、Input Buffer、攻击、闪避、受击或 Enemy 侧内容。

## 二、本次确认的协作与代码风格偏好

后续继续采用“古法编程”模式：

- 用户手敲代码
- AI 只给当前一步的目标、代码片段与边界说明
- 每次只推进一个明确小闭环
- 不一次性堆完整系统
- 涉及架构边界时先确认，再继续

本次新增确认的偏好：

- 内部装配链路中，调用方已经保证依赖时，不为每个 `Init(...)` 参数重复写三段式空校验
- 不写多余日志，不用日志掩盖架构问题
- `PlayerController` 继续保持轻量，只做生命周期与调度
- 不接受“一个脚本一个空节点”的层级拆分；场景层级应按项目级 Actor 语义组织
- `PlayerInputReader` 只翻译输入，不解释输入，不切状态，不消费输入

## 三、PlayerMotor 当前状态

当前文件：

[../../../Assets/Scripts/Module/Player/Core/PlayerMotor.cs](../../../Assets/Scripts/Module/Player/Core/PlayerMotor.cs)

当前职责：

```text
玩家移动执行器，负责基于 CharacterController 执行最终位移
```

当前已经实现：

```text
1. 持有 CharacterController
2. 持有 PlayerContext
3. 持有 PlayerMoveConfigSO
4. Init(CharacterController, PlayerContext, PlayerMoveConfigSO)
5. FixedTick(float fixedDeltaTime)
6. 读取 Context.Velocity
7. 根据 Context.MoveDir / TargetMoveSpeed / IsMovementLocked 计算水平速度
8. 使用 Acceleration / Deceleration 做速度逼近
9. 使用 CharacterController.isGrounded 与 Physics.gravity 处理竖直速度
10. 通过 CharacterController.Move(...) 执行最终移动
11. 回写 Context.Velocity 与 Context.IsGrounded
```

当前有意没有实现：

```text
移动朝向旋转
跳跃
相机朝向移动
Ability Motion
Root Motion 驱动
```

关于“移动朝向旋转”的边界已确认：

```text
State：写移动意图，例如 MoveDir / TargetMoveSpeed
Motor：执行最终 Transform / CharacterController 结果
TransitionEvaluator：决定状态切换
```

因此后续如果要旋转角色根节点，执行位置可以在 `PlayerMotor`，但是否允许转向应由 Context / Ability / 状态锁定标记表达，例如未来增加：

```text
IsRotationLocked
```

当前移动朝向旋转不是阻塞项，可以在 TransitionEvaluator 最小版跑通后再做。

## 四、PlayerController 当前状态

当前文件：

[../../../Assets/Scripts/Module/Player/Core/PlayerController.cs](../../../Assets/Scripts/Module/Player/Core/PlayerController.cs)

当前已经接入：

```text
1. 缓存 Transform
2. 获取 CharacterController
3. 创建 PlayerContext
4. 创建并初始化 PlayerMotor
5. 创建 PlayerStateMachine
6. 注册 Grounded / GroundedIdle / GroundedMove
7. Init 到 Grounded
8. Update 中 Tick 状态机
9. FixedUpdate 中先 FixedTick 状态机，再 FixedTick Motor
```

当前调度顺序：

```text
Update
└─ PlayerStateMachine.Tick(Time.deltaTime)

FixedUpdate
├─ PlayerStateMachine.FixedTick(Time.fixedDeltaTime)
└─ PlayerMotor.FixedTick(Time.fixedDeltaTime)
```

曾经用于测试的旧输入临时代码已决定删除，不应继续保留在 Controller 中：

```csharp
float horizontal = Input.GetAxis("Horizontal");
float vertical = Input.GetAxis("Vertical");

m_context.MoveInput = new Vector2(horizontal, vertical);
m_context.IsSprintPressed = Input.GetKey(KeyCode.LeftShift);

if (m_context.MoveInput.sqrMagnitude > 0.01f)
    m_stateMachine.ChangeState(PlayerStateId.GroundedMove);
else
    m_stateMachine.ChangeState(PlayerStateId.GroundedIdle);
```

原因：

```text
Controller 不读输入
Controller 不判断 Idle / Move
Controller 不直接承担当帧切换规则
```

后续应由：

```text
PlayerInputReader
└─ 写 Context.MoveInput / IsSprintPressed

PlayerTransitionEvaluator
└─ 根据 Context 与当前状态决定 GroundedIdle / GroundedMove
```

## 五、Grounded 最小状态组

当前目录：

[../../../Assets/Scripts/Module/Player/HFSM/States/Ground/](../../../Assets/Scripts/Module/Player/HFSM/States/Ground/)

当前已创建：

```text
PlayerGroundedState.cs
PlayerIdleState.cs
PlayerMoveState.cs
```

当前命名空间：

```csharp
Module.Player.HFSM.States.Ground
```

需要注意：设计文档中原建议目录是 `States/Grounded/`。当前实际目录和命名空间使用 `Ground`，代码可编译，暂不阻塞。后续如要统一术语，可以整体改为：

```text
States/Grounded
Module.Player.HFSM.States.Grounded
```

### 1. PlayerGroundedState

职责：

```text
玩家地面复合状态，提供地面状态默认子状态
```

当前返回：

```csharp
GetInitialChildId() => PlayerStateId.GroundedIdle
```

### 2. PlayerIdleState

职责：

```text
玩家地面待机状态，清空移动意图
```

当前在 `Enter()` 与 `FixedTick(...)` 中写入：

```text
MoveDir = Vector3.zero
TargetMoveSpeed = 0f
```

### 3. PlayerMoveState

职责：

```text
玩家地面移动状态，根据输入写入移动方向与目标速度
```

当前在 `FixedTick(...)` 中：

```text
读取 Context.MoveInput
写入 Context.MoveDir
根据 IsSprintPressed 写入 WalkSpeed 或 SprintSpeed
```

## 六、Context 引用边界说明

本次讨论中明确：

```text
不是每个状态各自拥有一个 Context
而是每个 Player 实例拥有一个 PlayerContext
该 Player 的多个状态共享同一个 Context 引用
```

示例关系：

```text
Player A
├─ PlayerContext A
├─ PlayerIdleState  -> 引用 Context A
└─ PlayerMoveState  -> 引用 Context A

Player B
├─ PlayerContext B
├─ PlayerIdleState  -> 引用 Context B
└─ PlayerMoveState  -> 引用 Context B
```

`PlayerContext` 是引用类型。构造状态时传入的是同一个运行时对象引用，不是字段快照。

当前暂不把 `PlayerContext` 放入 `BasePlayerState`，原因：

```text
BasePlayerState 应保持 HFSM 最小生命周期基类
并非所有状态都需要 Context
复合状态、测试状态或只提供默认子状态的状态不应被迫持有 Context
```

后续如果多个状态重复持有 `PlayerContext` 明显增多，可以再抽中间基类：

```text
PlayerContextState : BasePlayerState
```

当前不提前抽象。

## 七、场景挂载与 Actor 分层

当前场景：

```text
Assets/Scenes/GameScene.unity
```

当前 Player 层级采用项目级 Actor 分层：

```text
Player
├─ PlayerController
├─ CharacterController
└─ Chisaki_MeshOnly_ForMixamo
   ├─ Animator
   ├─ mixamorig:Hips
   └─ 鸣潮_千咲1.02_mesh
```

该分层语义：

```text
Player：玩家实体根节点，承载逻辑、移动、碰撞、状态机、输入入口
Chisaki_MeshOnly_ForMixamo：视觉模型子节点，承载 Animator、骨骼与网格
```

不采用以下过细拆法：

```text
Player
├─ MotorNode
├─ InputNode
├─ StateNode
└─ ...
```

当前确认：

```text
PlayerController 挂在 Player 根节点
CharacterController 挂在 Player 根节点
Animator 保留在模型子节点 Chisaki_MeshOnly_ForMixamo
PlayerController.MoveConfig 已指向 PlayerMoveConfig.asset
```

CharacterController 推荐参数曾按模型 Bounds 给出：

```text
Center: (0, 0.82, 0)
Radius: 0.3
Height: 1.65
```

后续需要根据实际脚底贴地、碰撞体覆盖和模型视觉再微调。

## 八、动画资源问题与处理结果

本次排查了 `Walking` 动画无法正常循环 / 无法播放的问题。

涉及资源：

```text
Assets/Animation/千咲/千咲_Animator.controller
Assets/Animation/千咲/Chisaki_MeshOnly_ForMixamo@Walking.fbx
Assets/Animation/千咲/Walking.anim
```

发现的问题：

```text
1. Animator Controller 中 Walking 状态曾存在 Motion 引用断裂风险
2. FBX 内嵌 Walking 能驱动模型，但默认只播放一次
3. 独立 Walking.anim 可以开启 Loop，但播放时模型静止
```

最终建议路线：

```text
使用 Chisaki_MeshOnly_ForMixamo@Walking.fbx 内嵌的 Walking Clip
不要使用独立 Walking.anim 作为当前动作源
```

原因：

```text
FBX 内嵌 Walking 已证明能驱动当前模型与 Avatar
Walking.anim 虽有曲线且可 Loop，但与当前 Humanoid Animator / Avatar 的绑定不稳定
```

正确处理方式：

```text
1. 在 Project 中选中父 FBX 文件，不是展开后的子 Clip
2. Inspector -> Animation 页签
3. 选中 Clips 中的 mixamo.com / Walking
4. 勾选 Loop Time
5. 勾选 Loop Pose
6. Apply
7. Animator Controller 的 Walking 状态 Motion 使用该 FBX 内嵌 Clip
```

同时保持：

```text
Animator.ApplyRootMotion = false
```

因为当前位移由 `CharacterController` 与 `PlayerMotor` 驱动，动画只负责表现。

当前结果：

```text
Walking 动画循环问题已处理
```

## 九、Unity Input System 接入状态

当前已安装 Unity New Input System：

```text
com.unity.inputsystem: 1.14.2
```

相关文件：

```text
Packages/manifest.json
Packages/packages-lock.json
ProjectSettings/ProjectSettings.asset
```

当前 Project Settings：

```text
activeInputHandler: 2
```

含义：

```text
Both
```

这样旧输入和新输入暂时都可用。后续完全迁移后，可以考虑改成 New Input System Only。

Player 程序集已需要引用：

```text
Unity.InputSystem
```

当前 `Player.asmdef` 应包含：

```text
Utils.log
Unity.InputSystem
```

如果代码中 `UnityEngine.InputSystem` 或生成类不可见，优先检查：

```text
Assets/Scripts/Module/Player/Player.asmdef
```

## 十、Input Actions 资产与生成代码分层

当前输入资产：

[../../../Assets/Config/Input/PlayerInputActions.inputactions](../../../Assets/Config/Input/PlayerInputActions.inputactions)

当前已创建 Action Map：

```text
Player
```

当前已创建 Actions：

```text
Move
Sprint
Jump
```

当前绑定：

```text
Move
└─ 2D Vector
   ├─ Up: W
   ├─ Down: S
   ├─ Left: A
   └─ Right: D

Sprint: Left Shift
Jump: Space
```

本次明确的目录分层：

```text
Assets/Config/Input/PlayerInputActions.inputactions
```

用于保存输入配置资产，这是合理的。

生成代码不应继续留在 `Assets/Config/Input/`，因为该目录不在 `Player.asmdef` 下，会导致生成类编译进默认 `Assembly-CSharp`。自定义程序集 `Player` 不能反向引用默认程序集，因此 `PlayerInputReader` 会无法识别 `PlayerInputActions`。

推荐结构：

```text
Assets/Config/Input/PlayerInputActions.inputactions      // 配置资产
Assets/Scripts/Module/Player/Input/PlayerInputActions.cs // 生成代码
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs  // 手写代码
```

当前磁盘状态中已出现：

```text
Assets/Scripts/Module/Player/Input/PlayerInputActions.cs
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs
```

后续若生成类再次丢失或不可识别，检查 `.inputactions` Inspector 中：

```text
Generate C# Class
Class Name: PlayerInputActions
Namespace: Module.Player.Input
C# Class File: Assets/Scripts/Module/Player/Input/PlayerInputActions.cs
```

## 十一、PlayerInputReader 设计边界

当前文件：

[../../../Assets/Scripts/Module/Player/Input/PlayerInputReader.cs](../../../Assets/Scripts/Module/Player/Input/PlayerInputReader.cs)

本次确认：

```text
PlayerInputReader 只翻译输入，不解释输入
```

它应该负责：

```text
读取 PlayerInputActions
写入 Context.MoveInput
写入 Context.IsSprintPressed
后续可把 Jump / Attack / Dodge 送入 PlayerInputBuffer
```

它不应该负责：

```text
判断能不能移动
判断能不能跳
判断能不能攻击
切换状态
消费输入
处理连招缓存
处理取消窗口
```

职责关系：

```text
PlayerInputReader
└─ 读取 InputActions，写当前帧输入事实

PlayerInputBuffer
└─ 缓存 Jump / Attack / Dodge 等离散输入

PlayerTransitionEvaluator
└─ 根据 Context + Buffer + 当前状态决定状态切换

AbilityController
└─ 处理 Attack / Dodge / Skill 的生命周期
```

当前只需要让 `PlayerInputReader` 完成移动与疾跑输入读取：

```text
MoveInput
IsSprintPressed
```

`Jump` 已经在 Input Actions 中建好，但暂不接入实际跳跃逻辑。

## 十二、当前尚未完成

以下内容尚未完成或尚未正式接入：

```text
PlayerInputReader 接入 PlayerController
PlayerTransitionEvaluator 最小版
根据 MoveInput 切 GroundedIdle / GroundedMove
PlayerInputBuffer
Jump 输入缓存
Airborne / Jump / Fall 状态
移动朝向旋转
相机方向移动
PlayerAnimatorDriver
Idle / Move 动画切换
Ability System
攻击、闪避、受击、死亡
Enemy 侧任何内容
```

## 十三、下一步建议

下一步建议先完成：

```text
PlayerInputReader 接入 PlayerController
```

目标：

```text
1. PlayerController 持有 PlayerInputReader
2. Awake 中创建并 Init
3. Update 中先 Tick InputReader，再 Tick StateMachine
4. OnDestroy 或 OnDisable 中 UnInit
5. 保持 Controller 不判断 Idle / Move
```

随后进入：

```text
PlayerTransitionEvaluator 最小版
```

第一版只做一件事：

```text
GroundedIdle <-> GroundedMove
```

依据：

```text
Context.MoveInput.sqrMagnitude
```

注意：切换逻辑不要重新塞回 `PlayerController`，应进入 `Transition` 模块。

推荐后续顺序：

```text
1. 完成 PlayerInputReader 正式接入
2. 创建最小 PlayerTransitionEvaluator
3. 由 Evaluator 决定 GroundedIdle / GroundedMove
4. 确认 Input System + HFSM + Motor 移动闭环
5. 再补移动朝向旋转
6. 再进入 Airborne / Jump / Fall
```

## 十四、当前工作区注意事项

当前工作区存在一些与本次 Player 输入/移动推进不完全相关的改动或资源变动，例如：

```text
动画资源移动/删除
GameScene 场景改动
Ultimate Editor Enhancer Settings 变动
SceneBackups 新目录
Ability/Transition 目录 meta 删除
Prefabs 目录新增
```

后续提交时需要按意图筛选，不要把无关变更混入单一 Player 输入或移动模块提交。

如果要提交本阶段建议拆成至少两类：

```text
1. Player 移动 / HFSM / Input System 代码与配置
2. 场景挂载与动画资源调整
```

避免把工具设置、备份目录、无关 `.meta` 清理混入。

