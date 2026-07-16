# Protocol_Evac Player HFSM 开发进展记录

## 一、记录范围

本记录补充同目录下的：

[Player战斗系统开发交接记录.md](Player战斗系统开发交接记录.md)

主设计文档：

[玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次仍然只推进 Player HFSM，不实现 Enemy、Input、Ability 或具体 Player State。

## 二、今日确认的 HFSM 契约

命名空间继续使用：

```csharp
Module.Player.HFSM
```

状态机采用真正的 HFSM：

```text
单状态树 + 活动状态路径 + LCA 切换
```

已经确认以下规则：

1. State 先全部注册，再调用初始化
2. 初始化开始后锁定注册表，运行期间不能继续添加或替换 State
3. 注册表锁定不影响已注册状态之间的正常切换
4. 每个 Player 的每种 State 只创建一个实例，并在 Player 生命周期内复用
5. State 的临时数据在 `Enter()` 中自行重置
6. 复合状态可以作为切换目标，并自动展开到默认叶子状态
7. 请求切换到当前叶子状态时不执行 Exit/Enter，结果视为无变化
8. `Enter`、`Exit`、`Tick`、`FixedTick` 执行期间禁止重入切换
9. 生命周期回调内发生重入切换时立即抛出错误
10. 生命周期回调自身抛出异常时不自动回滚
11. 回调异常后状态机标记为故障，该实例不能继续使用
12. State 不直接持有状态机并切换；后续由 `PlayerTransitionEvaluator` 产生请求

复合状态默认叶子已经确定：

```text
Grounded → GroundedIdle
Airborne → AirborneFall
Action → ActionAttack
Disabled → DisabledHurt
```

当 Evaluator 已经知道具体行为时，应直接请求叶子状态。例如存在移动输入时直接请求 `GroundedMove`，不依赖 `Grounded` 的默认展开。

## 三、主设计文档同步内容

已更新 [玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)：

- Player 状态机目录由 `FSM` 统一为 `HFSM`
- `PlayerStateBase` 统一为当前实际类型名 `BasePlayerState`
- 补充 `Config` 目录
- 明确第一版必须是真正的 HFSM，不允许退化为普通 FSM
- 补充父状态 ID：`Grounded`、`Airborne`、`Action`、`Disabled`
- Player 命名空间统一记录为 `Module.Player.*`
- 第一阶段开发顺序调整为先完成 `PlayerStateMachine`
- 私有字段命名统一为 `m_` + camelCase
- 新增 HFSM 初始化、实例生命周期、默认展开、同状态无操作、重入和异常契约

以下内容按用户要求暂未修改：

```text
当前 State 和 Active Ability 是否应同时保存在 PlayerContext
```

该问题留到正式设计 `PlayerContext` 时再决定。

## 四、QLog 程序集问题与处理结果

日志类：

[QLog.cs](../../../Assets/Scripts/Utils/log/QLog.cs)

此前 `QLog.cs` 不属于任何自定义程序集，因此被编译进 `Assembly-CSharp`。Player 模块已经由以下 asmdef 隔离：

[Player.asmdef](../../../Assets/Scripts/Module/Player/Player.asmdef)

自定义程序集不能反向引用 `Assembly-CSharp`，所以 Player 代码无法直接使用 `QLog`。

当前已经存在独立日志程序集：

[Utils.log.asmdef](../../../Assets/Scripts/Utils/log/Utils.log.asmdef)

`Player.asmdef` 当前已经引用 `Utils.log`，Player 代码使用：

```csharp
using Utils.log;
```

新增了：

```csharp
QLog.Throw(Exception exception)
```

行为约定：

- Editor 下先通过 `Debug.LogException` 记录异常
- 所有构建中都会实际抛出异常
- `QLog.Throw` 本身不能添加 `[Conditional("UNITY_EDITOR")]`
- 使用 `ExceptionDispatchInfo` 保留被重新抛出异常的原始调用信息

普通信息仍然使用：

```csharp
QLog.Info(...)
QLog.Warning(...)
QLog.Error(...)
```

需要终止非法流程时使用：

```csharp
QLog.Throw(new InvalidOperationException(...))
QLog.Throw(new ArgumentException(...))
```

不能用只记录日志的 `QLog.Error` 直接替代异常，否则校验失败后代码仍会继续执行。

## 五、PlayerStateMachine 当前实现状态

当前文件：

[PlayerStateMachine.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerStateMachine.cs)

当前已经实现：

- 构造函数
- 状态字典
- 可写活动路径与只读活动路径包装
- 当前叶子状态 ID
- 初始化完成与故障标记
- `RegisterState()`
- `Init()`
- 初始化后注册表锁定
- 空状态、重复 ID、`None` ID 校验
- 父状态存在性校验
- 父状态必须为 `PlayerCompositeState`
- 父链环路校验
- 复合状态默认直接子节点校验
- 从目标状态向父级构建路径
- 复合状态自动展开到默认叶子状态
- 初始活动路径按父到子执行 `Enter()`
- 初始化回调异常后设置 `IsFaulted`
- 所有需要中断流程的校验统一使用 `QLog.Throw`

当前尚未实现：

- `ChangeState()`
- 当前路径与目标路径完全相同时的无操作判断
- LCA/公共前缀长度计算
- 叶子到父级的 Exit 路径
- 父级到叶子的 Enter 路径
- `Tick(float deltaTime)`
- `FixedTick(float fixedDeltaTime)`
- 生命周期回调重入保护的完整使用
- 故障状态机的后续调用保护
- EditMode 自动化测试
- 具体 Grounded/Airborne/Action/Disabled 状态

## 六、当前编译状态

Unity 已经重新生成：

```text
Library/ScriptAssemblies/Utils.log.dll
Library/ScriptAssemblies/Player.dll
```

两份程序集最后生成时间均为：

```text
2026-07-16 18:17:05
```

Editor 日志已出现两份程序集的 `CopyFiles` 记录，当前相关源码已经编译通过。

## 七、仍需注意的问题

### 1. API 命名尚未统一

主设计和早期交接记录使用：

```csharp
IsInitialized
Initialize(PlayerStateId initialStateId)
```

当前代码实际使用：

```csharp
IsInited
Init(PlayerStateId initStateId)
```

在 PlayerController 开始依赖状态机前，应确定最终命名，避免后续批量修改调用方。

### 2. Context 所有权尚未确定

暂未决定是否在 `PlayerContext` 中镜像：

```text
CurrentStateId
PreviousStateId
ActiveAbility
AbilityPhase
```

推荐在设计 Context 时比较：

- HFSM/Ability 保持唯一权威来源
- Context 保存可写镜像
- 所有者维护权威状态，向 Context 或只读快照发布跨系统结果

该问题不阻塞当前 HFSM 状态切换实现。

### 3. 尚未进行行为级测试

目前只确认编译通过，还没有具体 State 或测试替身验证生命周期顺序。

## 八、下一步工作

下一步继续只完善 `PlayerStateMachine`，不要提前创建 Context、Input 或 Ability。

建议顺序：

```text
1. 确定 Init/Initialize 与 IsInited/IsInitialized 的最终命名
2. 实现目标活动路径与当前活动路径的完全相等判断
3. 实现公共前缀长度计算
4. 实现 ChangeState()
5. Exit：当前叶子 → 公共祖先下一层
6. Enter：公共祖先下一层 → 目标叶子
7. 实现 Tick/FixedTick 父到子调用
8. 完成重入与故障保护
9. 使用最小测试 State 验证生命周期顺序
```

最小验证场景：

```text
初始化 Grounded
Enter Grounded
Enter GroundedIdle

GroundedIdle → GroundedMove
Exit GroundedIdle
Enter GroundedMove

GroundedMove → AirborneJump
Exit GroundedMove
Exit Grounded
Enter Airborne
Enter AirborneJump

AirborneJump → AirborneFall
Exit AirborneJump
Enter AirborneFall
```

以上验证全部通过后，再进入 `PlayerContext` 设计阶段。

