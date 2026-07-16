# Protocol_Evac PlayerStateMachine 开发进展记录

## 一、记录范围

本记录接续同目录下的：

[1-Player战斗系统开发交接记录.md](1-Player战斗系统开发交接记录.md)

[2-PlayerHFSM开发进展记录.md](2-PlayerHFSM开发进展记录.md)

本次仍然只推进 Player HFSM 的状态机基础设施，不创建 `PlayerContext`、Input、Ability 或具体 Player State。

## 二、本次确认的 API 命名

当前继续沿用现有命名，不做统一重命名：

```csharp
IsInited
Init(PlayerStateId initStateId)
```

暂不改为：

```csharp
IsInitialized
Initialize(...)
```

原因是当前用户决定直接使用现有命名，后续调用方按当前 API 接入。

## 三、QLog 调整

日志类：

[QLog.cs](../../../Assets/Scripts/Utils/log/QLog.cs)

当前 `QLog.Info`、`QLog.Warning`、`QLog.Error` 已经自动为日志添加调用类名前缀。

调用方可以直接写：

```csharp
QLog.Error("注册状态失败：state 为空");
```

实际编辑器日志会输出类似：

```text
[PlayerStateMachine] 注册状态失败：state 为空
```

实现方式：

- `Info` / `Warning` / `Error` 保持 `[Conditional("UNITY_EDITOR")]`
- `QLog` 内部通过调用栈查找第一个非 `QLog` 的调用类型
- 调用方不再手写 `[{nameof(ClassName)}]`

`QLog.Throw(Exception exception)` 没有添加 `[Conditional("UNITY_EDITOR")]`。

原因是 `Throw` 的语义是：

```text
Editor 下记录异常，并在所有构建中实际抛出异常
```

如果给 `Throw` 添加 `[Conditional("UNITY_EDITOR")]`，Release 构建中的调用点会直接消失，导致异常不再抛出，与该方法职责冲突。

## 四、PlayerStateMachine 当前实现状态

当前文件：

[PlayerStateMachine.cs](../../../Assets/Scripts/Module/Player/HFSM/PlayerStateMachine.cs)

当前已经实现：

```text
RegisterState()
Init()
ChangeState()
Tick()
FixedTick()
```

状态机当前具备：

1. 注册状态
2. 初始化后锁定注册表
3. 拒绝空状态、重复状态 ID、`PlayerStateId.None` 作为真实状态 ID
4. 校验父状态存在
5. 校验父状态必须是 `PlayerCompositeState`
6. 校验父链不存在环路
7. 校验复合状态默认子状态存在
8. 校验复合状态默认子状态是直接子状态
9. 从目标状态构建顶层到叶子的路径
10. 复合状态自动展开到默认叶子状态
11. 初始路径按父到子执行 `Enter`
12. 状态切换前校验状态机是否可执行
13. 目标路径与当前路径相同时直接无操作
14. 计算当前路径与目标路径的公共前缀长度
15. 使用公共前缀长度执行 LCA 切换
16. `Exit` 顺序为叶子到父级
17. `Enter` 顺序为父级到叶子
18. `Tick` / `FixedTick` 顺序为父级到叶子
19. 生命周期回调期间通过 `m_isExecutingLifecycle` 阻止重入
20. 生命周期回调异常后标记 `IsFaulted`

## 五、状态树校验策略调整

由于当前用户不希望普通校验直接抛异常，状态树校验已经从 `QLog.Throw` 流程改为：

```text
QLog.Error(...) + bool 返回值
```

当前 `Init()` 中已经改为：

```csharp
if (!validateStateTree())
    return;
```

相关校验函数均返回 `bool`：

```csharp
private bool validateStateTree()
private bool validateParent(BasePlayerState state)
private bool validateParentChain(BasePlayerState state)
private bool validateInitialChild(PlayerCompositeState compositeState)
```

这样可以在校验失败时停止初始化，避免日志只打印但流程继续执行，导致状态机进入半坏状态。

## 六、ChangeState 当前行为

`ChangeState(PlayerStateId targetStateId)` 当前执行顺序：

```text
1. isStateMachineValid(nameof(ChangeState))
2. buildExpandedPath(targetStateId)
3. targetPath 为空则返回
4. isSameActivePath(targetPath) 为 true 则返回
5. getCommonPrefixLength(targetPath)
6. m_isExecutingLifecycle = true
7. exitCurrentPath(commonPrefixLength)
8. enterTargetPath(targetPath, commonPrefixLength)
9. 生命周期异常时 IsFaulted = true，并使用 QLog.Throw(exception)
10. finally 中恢复 m_isExecutingLifecycle = false
```

同路径无操作用于避免重复切换当前状态。例如当前路径为：

```text
Grounded → GroundedIdle
```

再次请求 `GroundedIdle` 或请求会展开为同一路径的复合状态时，不执行 `Exit` / `Enter`。

公共前缀长度用于决定 LCA 切换边界。例如：

```text
当前：Grounded → GroundedIdle
目标：Grounded → GroundedMove
公共前缀长度 = 1
```

切换时保留 `Grounded`，只执行：

```text
Exit GroundedIdle
Enter GroundedMove
```

跨父状态切换示例：

```text
当前：Grounded → GroundedMove
目标：Airborne → AirborneJump
公共前缀长度 = 0
```

切换时执行：

```text
Exit GroundedMove
Exit Grounded
Enter Airborne
Enter AirborneJump
```

## 七、Tick 与 FixedTick 当前行为

`Tick(float deltaTime)` 与 `FixedTick(float fixedDeltaTime)` 当前均按活动路径父到子执行。

例如当前活动路径：

```text
Grounded → GroundedMove
```

`Tick` 执行顺序：

```text
Grounded.Tick(deltaTime)
GroundedMove.Tick(deltaTime)
```

`FixedTick` 执行顺序：

```text
Grounded.FixedTick(fixedDeltaTime)
GroundedMove.FixedTick(fixedDeltaTime)
```

两者都会：

- 先执行 `isStateMachineValid(...)`
- 设置 `m_isExecutingLifecycle = true`
- 捕获生命周期异常
- 异常后标记 `IsFaulted = true`
- 使用 `QLog.Throw(exception)` 保留异常语义
- 在 `finally` 中恢复 `m_isExecutingLifecycle = false`

## 八、当前编译状态

本次完成后已经执行：

```text
dotnet build Player.csproj --no-restore
```

结果：

```text
0 个错误
0 个警告
```

`Utils.log.csproj` 作为依赖也在构建过程中成功生成。

## 九、当前尚未完成

以下内容尚未实现：

```text
EditMode 自动化测试
最小测试 State
具体 Grounded / Airborne / Action / Disabled 状态类
PlayerContext
PlayerController
PlayerMotor
Input Reader
Input Buffer
Ability Runtime
Transition Evaluator
普攻三连段
闪避和无敌帧
Hurt / Dead
```

用户当前决定暂不写自动化测试，先把当前阶段写入归档文档。

## 十、仍需注意的问题

### 1. 普通校验不再抛异常

当前大量非法输入场景使用：

```csharp
QLog.Error(...);
return;
```

这符合当前用户偏好，但也意味着：

- 调用方不会通过异常感知普通校验失败
- 后续如果需要强约束，需要明确区分“普通校验失败”和“生命周期异常”
- 当前生命周期回调异常仍然使用 `QLog.Throw(exception)`，语义上仍会终止流程

### 2. PlayerStateMachine 行数已经偏长

当前 `PlayerStateMachine` 已经包含：

```text
注册与初始化
状态树校验
路径构建
LCA 切换
Tick / FixedTick 调度
运行期保护
```

这些仍属于状态机本体职责，暂时可以保留在同一文件中。

但后续不要继续把以下内容放进该文件：

```text
具体 State 实现
测试替身 State
Transition 规则
Input 判断
Ability 逻辑
Motor 移动逻辑
```

如果状态树校验继续膨胀，可以在 HFSM 主体跑通后考虑抽出：

```text
PlayerStateTreeValidator
```

当前不建议立即拆分，避免在 HFSM 尚未正式验证前过早抽象。

## 十一、建议下一步

如果继续遵循“每一步保持可编译、可验证”的原则，推荐下一步是：

```text
创建最小 EditMode 测试，验证 PlayerStateMachine 生命周期顺序
```

验证场景仍是：

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

如果暂时不写测试，则下一步可以开始设计：

```text
PlayerContext
```

但进入 `PlayerContext` 前需要先重新确认：

```text
CurrentStateId / PreviousStateId 是否镜像到 Context
ActiveAbility / AbilityPhase 是否镜像到 Context
HFSM 与 Ability 的权威数据边界
Context 只保存高频运行时数据，不成为无边界数据桶
```

