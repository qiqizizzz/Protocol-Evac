# Protocol_Evac Player 战斗系统开发交接记录

## 一、项目与文档位置

项目根路径：

```text
<项目根目录>
```

主要设计文档：

[玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

当前只实现 Player 战斗系统，不实现 Enemy 部分。

## 二、当前目标与范围

目标是制作类似《鸣潮》的单角色动作战斗系统，但当前范围明确为：

- 只支持一个玩家角色
- 单机
- 不支持多角色切换
- 暂不实现软锁定、硬锁定、自动朝向和目标切换
- 使用 `CharacterController`
- 常规移动不使用 Root Motion
- 特殊攻击位移以后由 Ability Motion 驱动
- 战斗逻辑由 Ability 自身时间轴驱动
- Animator 只负责表现，不作为唯一逻辑来源
- 目标是干净、可扩展、职责清晰的架构

## 三、确定的 Player 架构

设计文档确定的核心架构：

```text
Input Buffer + HFSM + Ability System + Transition Evaluator
```

职责如下：

- `Input Buffer`：预输入、连招输入和输入容错
- `HFSM`：管理 Grounded、Airborne、Action、Disabled 等层级状态
- `Ability System`：管理攻击、闪避、技能、受击的生命周期
- `Transition Evaluator`：统一裁决状态切换、取消、打断和优先级
- `PlayerContext`：保存单个玩家实例的共享运行时数据
- `PlayerMotor`：最终执行移动
- `PlayerAnimatorDriver`：读取运行结果并更新动画
- `PlayerController`：只负责初始化、生命周期和调度

明确禁止：

- HFSM 状态直接读取 Unity Input
- Ability 直接操作 `CharacterController`
- Motor 决定状态切换
- Animator Event 直接切换 HFSM
- 状态之间直接互相调用
- `PlayerContext` 注册为 QFramework Model
- State/Ability 直接依赖 QFramework

## 四、目录结构

用户已经创建完 [Player 模块目录](../../../Assets/Scripts/Module/Player/)，采用以下结构：

```text
Assets/Scripts/Module/
└─ Player/
   ├─ Core/
   ├─ Context/
   ├─ Input/
   ├─ HFSM/
   │  └─ States/
   │     ├─ Grounded/
   │     ├─ Airborne/
   │     ├─ Action/
   │     └─ Disabled/
   ├─ Transition/
   ├─ Ability/
   │  └─ Abilities/
   └─ Config/
```

目录职责：

```text
Core         PlayerController、PlayerMotor、PlayerAnimatorDriver
Context      PlayerContext
Input        输入读取、输入快照、Input Buffer
HFSM         HFSM 基础设施
States       具体父状态和叶子状态
Transition   切换、取消、打断、优先级裁决
Ability      Ability 调度、生命周期、运行时实例
Abilities    普攻、闪避、技能、受击实现
Config       PlayerConfigSO、AbilityDefinitionSO 等配置
```

没有使用 `Common` 文件夹。

HFSM 基础文件直接放在 `HFSM` 根目录：

```text
HFSM/
├─ PlayerStateId.cs
├─ BasePlayerState.cs
├─ PlayerCompositeState.cs
├─ PlayerStateMachine.cs
└─ States/
```

当前已存在的 HFSM 文件：

- [PlayerStateId.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerStateId.cs)
- [BasePlayerState.cs](../../../Assets/Scripts/Module/Player/HFSM/BasePlayerState.cs)
- [PlayerCompositeState.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerCompositeState.cs)

`PlayerStateMachine.cs` 尚未创建。

原因是 `Common` 语义不清晰，容易变成杂物目录。目前也不抽取通用 HFSM Framework，因为这套实现尚未证明需要跨模块复用。

## 五、程序集状态

用户已在 Unity 中创建 Player Assembly Definition：

[Player.asmdef](../../../Assets/Scripts/Module/Player/Player.asmdef)

当前实际配置：

```text
Assembly Name: Player
Root Namespace: 未在 asmdef 中配置
```

暂时不引用 QFramework。

当前 HFSM 代码实际使用的命名空间是：

```csharp
Module.Player.HFSM
```

项目此前检查时尚未安装 Unity New Input System。后续实现 Input 时需要安装：

```text
com.unity.inputsystem
```

安装后给 Player asmdef 添加对应程序集引用。

## 六、HFSM 结构

要求实现真正的 HFSM，不接受退化为普通 FSM。

状态树：

```text
Player HFSM
├─ Grounded
│  ├─ GroundedIdle
│  ├─ GroundedMove
│  └─ GroundedSprint
├─ Airborne
│  ├─ AirborneJump
│  └─ AirborneFall
├─ Action
│  ├─ ActionAttack
│  ├─ ActionSkill
│  └─ ActionDodge
└─ Disabled
   ├─ DisabledHurt
   └─ DisabledDead
```

采用“单状态树 + 活动路径”模型，不在每个复合状态内部再嵌套一台状态机。

活动路径示例：

```text
Grounded → GroundedMove
```

生命周期顺序：

```text
Enter：父 → 子
Tick：父 → 子
FixedTick：父 → 子
Exit：子 → 父
```

跨父状态切换：

```text
GroundedMove → AirborneJump

Exit GroundedMove
Exit Grounded
Enter Airborne
Enter AirborneJump
```

同父状态切换：

```text
GroundedIdle → GroundedMove

Exit GroundedIdle
Enter GroundedMove
```

状态机需要使用 LCA（Lowest Common Ancestor，最近公共祖先）计算退出和进入路径。

## 七、确定的 HFSM 类型命名

保留：

```text
PlayerStateId
BasePlayerState
PlayerCompositeState
PlayerStateMachine
```

不要把 `PlayerStateId` 改成 `PlayerState`。

原因：

```csharp
BasePlayerState currentState;
PlayerStateId currentStateId;
```

语义明确；`PlayerState` 会与状态实例产生歧义。

建议的 `PlayerStateId`：

```text
None

Grounded
GroundedIdle
GroundedMove
GroundedSprint

Airborne
AirborneJump
AirborneFall

Action
ActionAttack
ActionSkill
ActionDodge

Disabled
DisabledHurt
DisabledDead
```

`BasePlayerState` 的基础生命周期：

```text
Id
ParentId
Enter()
Exit()
Tick(float deltaTime)
FixedTick(float fixedDeltaTime)
```

`PlayerCompositeState` 继承 `BasePlayerState`，并提供：

```text
GetInitialChildId()
```

它用于决定进入复合状态后默认进入哪个直接子状态。

## 八、下一步工作

以下三个文件已经创建：

- [PlayerStateId.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerStateId.cs)
- [BasePlayerState.cs](../../../Assets/Scripts/Module/Player/HFSM/BasePlayerState.cs)
- [PlayerCompositeState.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerCompositeState.cs)

进入下一步前，应先确认 Unity 编译通过。

随后实现：

```text
PlayerStateMachine.cs
```

第一版状态机需要具备：

1. 注册状态，拒绝重复 `PlayerStateId`
2. 验证每个状态的父状态存在
3. 验证父节点确实是 `PlayerCompositeState`
4. 验证 `GetInitialChildId()` 返回直接子状态
5. 设置初始状态
6. 从复合状态自动展开到默认叶子状态
7. 维护只读的活动状态路径
8. 使用 LCA 计算状态切换路径
9. Exit 按叶子到父级执行
10. Enter 按父级到叶子执行
11. Tick/FixedTick 按父级到叶子执行
12. 防止生命周期回调期间重入切换
13. 暴露当前叶子状态 ID
14. 不负责优先级、打断和切换合法性

关键边界：

```text
PlayerStateMachine = 执行已经批准的状态切换
PlayerTransitionEvaluator = 判断切换是否合法
```

具体 State 不应直接持有状态机并调用 `ChangeState()`。后续由 `TransitionEvaluator` 生成切换请求，再交给状态机执行。

## 九、后续开发顺序

状态机完成后：

```text
1. PlayerContext
2. PlayerController
3. PlayerMotor
4. Grounded + Idle/Move
5. Airborne + Jump/Fall
6. Input Reader
7. Input Buffer
8. Ability Runtime
9. Transition Evaluator
10. 普攻三连段
11. 闪避和无敌帧
12. Hurt/Dead
```

第一个战斗竖切目标：

```text
Idle/Move
→ 地面普攻三连段
→ Cast/Active/Recovery
→ Active 阶段产生一次命中判定
→ Recovery 窗口允许闪避取消
→ 闪避具有无敌帧
→ 受击进入 Hurt
→ 生命归零进入 Dead
```

暂时不要实现：

- 技能和大招
- 多角色系统
- 目标锁定
- 自动朝向
- 通用 Enemy Ability
- 网络同步
- 复杂配置编辑器
- 通用 HFSM Framework 抽取

## 十、设计文档中的重要边界

`PlayerContext` 是单角色运行时上下文，不是 QF Model。

```text
PlayerContext：
当前输入、落地状态、速度、移动意图、锁定标记等高频运行数据

QF Model：
玩家等级、成长数值、已解锁技能、长期存档数据
```

Context 不应该成为无边界数据桶。以下信息尽量由真正所有者维护：

- 当前活动状态：由 HFSM 所有
- 当前 Ability：由 AbilityController 所有
- Ability 阶段：由 AbilityRuntime 所有
- 固定移动参数：由 PlayerConfigSO 所有
- 切换请求：一帧请求，不长期保存在 Context

## 十一、协作要求

用户希望被当作 Unity 高级阶段工程师对待：

- 不需要初学者式解释
- 每次推进一个明确步骤
- 先确定生命周期、所有权和依赖方向
- 不要未经讨论擅自把 HFSM 简化成 FSM
- 不要一次性堆完整系统
- 每一步都应保持可编译、可验证
- 遇到会影响架构的决策，应先询问用户再继续
