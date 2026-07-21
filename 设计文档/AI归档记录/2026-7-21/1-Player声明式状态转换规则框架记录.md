# Protocol_Evac Player 声明式状态转换规则框架记录

## 一、记录范围

本记录接续：

[../2026-7-20/1-Player输入接入与状态切换架构讨论记录.md](../2026-7-20/1-Player输入接入与状态切换架构讨论记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次完成 Player 状态转换决策位置的定案与第一版实现，打通：

```text
PlayerInputReader
→ PlayerContext
→ StateSelector
→ PlayerStateMachine
→ PlayerState
→ PlayerMotor
```

第一版只落地 `GroundedIdle <-> GroundedMove`，没有进入 Airborne、Ability、Hurt、Dead 或 Input Buffer 开发。

对应提交：

```text
5ae7d54 新增玩家状态转换规则框架
```

## 二、本次确认的设计

### 1. 最终采用声明式规则表

状态转换不放入具体 State，不写入 `PlayerController`，也不继续扩大 `PlayerStateMachine`。

当前采用：

```text
StateRule
└─ 描述一条转换边的来源、目标、优先级、同级顺序与条件

Rules/*Rules
└─ 按 Player 功能域集中声明转换边与共享条件

StateSelector
└─ 对全部规则进行稳定排序，每帧选择第一条满足条件的规则

PlayerStateMachine
└─ 只执行已经确定的目标状态切换
```

当前目录：

```text
Assets/Scripts/Module/Player/Transition/
├─ RuleLevel.cs
├─ StateRule.cs
├─ StateSelector.cs
└─ Rules/
   └─ MoveRules.cs
```

### 2. 没有采用的方案

本次曾讨论或短暂试验以下方向，最终均未保留：

```text
单一 PlayerTransitionEvaluator 内集中编写所有 if / else
IStatePolicy + Policy Chain
MovePolicy 自己枚举 Idle / Move 转换
State.TryGetNextState() 由每个 State 自行提出自然转换
完整 Request + Source + Rule + Priority 转换框架
```

未采用原因：

```text
单一 Evaluator 后期会膨胀
Policy 仍然容易在单个类中堆积状态边判断
State 自行转换会让转换关系分散，难以查看完整状态图
完整转换框架对当前阶段过重
```

声明式规则表保留集中可查性，同时不把全部业务塞进一个类。

### 3. 转换判断无法被真正消除

状态转换的本质仍然是：

```text
来源状态 + 运行时条件 → 目标状态
```

当前设计不是为了隐藏条件判断，而是为了：

```text
1. 将转换边结构化
2. 将同一领域的规则集中在一个文件
3. 复用领域条件，例如 MoveRules.canMove(...)
4. 统一处理跨领域优先级
5. 保持 State 只负责状态行为
```

## 三、当前实现状态

### 1. RuleLevel

文件：

[../../../Assets/Scripts/Module/Player/Transition/RuleLevel.cs](../../../Assets/Scripts/Module/Player/Transition/RuleLevel.cs)

当前优先级从低到高为：

```text
Move = 100
Ability = 200
Air = 300
Status = 400
```

预期裁决顺序：

```text
Status
→ Air
→ Ability
→ Move
```

高优先级规则优先执行。相同 `RuleLevel` 下使用 `Order` 决定顺序，数值越大越优先；`Order` 也相同时保留规则声明顺序。

### 2. StateRule

文件：

[../../../Assets/Scripts/Module/Player/Transition/StateRule.cs](../../../Assets/Scripts/Module/Player/Transition/StateRule.cs)

当前保存：

```text
SourceId
TargetId
Level
Order
Func<bool> Condition
```

规则来源语义：

```text
SourceId = 具体状态
└─ 只要该状态存在于当前 ActiveStatePath，规则就可参与条件判断

SourceId = PlayerStateId.None
└─ Any State 规则，可从任意活动状态参与判断
```

因此来源既可以是叶子状态，也可以是复合父状态。例如未来可以声明：

```text
Grounded → AirborneJump
Any State → DisabledDead
```

构造时已经校验：

```text
TargetId 不能是 PlayerStateId.None
Condition 不能为 null
```

### 3. StateSelector

文件：

[../../../Assets/Scripts/Module/Player/Transition/StateSelector.cs](../../../Assets/Scripts/Module/Player/Transition/StateSelector.cs)

初始化时：

```text
1. 校验 PlayerStateMachine 与规则集合
2. 按 Level 降序排列
3. 按 Order 降序排列
4. 相同 Level / Order 保留声明顺序
```

每帧：

```text
1. 从最高优先级规则开始检查
2. 跳过来源不匹配或条件不成立的规则
3. 第一条满足条件的规则立即终止本帧裁决
4. 目标不是当前叶子时调用 ChangeState(...)
5. 目标已经是当前叶子时保持当前状态并终止裁决
```

最后一条是必要的优先级短路语义。例如未来 `DisabledDead` 的 Status 规则持续成立且玩家已经处于 Dead 时，不能继续向下检查 Ability 或 Move 规则。

### 4. MoveRules

文件：

[../../../Assets/Scripts/Module/Player/Transition/Rules/MoveRules.cs](../../../Assets/Scripts/Module/Player/Transition/Rules/MoveRules.cs)

当前集中声明两条转换边：

```text
GroundedIdle → GroundedMove
条件：canMove(context)

GroundedMove → GroundedIdle
条件：!canMove(context)
```

共享条件：

```text
未锁定输入
且未锁定移动
且 MoveInput.sqrMagnitude > 0.01f
```

当前 `GroundedSprint` 没有独立转换规则。疾跑仍由 `PlayerMoveState` 根据 `IsSprintPressed` 选择 `WalkSpeed` 或 `SprintSpeed`。

## 四、关键架构边界

当前职责确定为：

```text
PlayerInputReader
└─ 读取输入并写入当前帧输入事实

PlayerContext
└─ 保存同一 Player 实例的运行时事实与运动意图

StateRule
└─ 描述一条转换边，不执行状态切换

Rules/*Rules
└─ 按功能域集中声明转换边和共享条件

StateSelector
└─ 统一排序、裁决并提交本帧第一条有效转换

PlayerStateMachine
└─ 维护活动状态路径并执行 LCA / Exit / Enter

PlayerState
└─ 执行当前状态行为并写入运动意图，不决定下一个状态

PlayerMotor
└─ 读取运动意图并通过 CharacterController 执行最终位移

PlayerController
└─ 创建依赖并安排生命周期顺序，不编写具体转换条件
```

现有 `PlayerStateMachine.ChangeState(...)`、活动路径和 LCA 核心没有为了规则框架进行重构。

## 五、当前调度顺序

当前 `PlayerController.Update()`：

```text
PlayerInputReader.Tick()
└─ 更新 MoveInput / IsSprintPressed

StateSelector.Tick()
└─ 使用最新输入和 Context 事实选择目标状态

PlayerStateMachine.Tick(Time.deltaTime)
└─ 更新完成切换后的当前活动状态路径
```

当前 `PlayerController.FixedUpdate()`：

```text
PlayerStateMachine.FixedTick(Time.fixedDeltaTime)
└─ 当前 State 写入运动意图

PlayerMotor.FixedTick(Time.fixedDeltaTime)
└─ 消费运动意图并执行 CharacterController.Move(...)
```

因此移动输入出现时，会在同一 Update 中先完成 `GroundedIdle → GroundedMove`，随后 Tick 新的活动状态路径；物理帧再由 `PlayerMoveState` 写入移动意图并由 Motor 执行。

## 六、验证结果

本次通过 Unity MCP 执行：

```text
AssetDatabase ForceSynchronousImport
Unity 脚本编译
Console Error / Exception / Warning 检查
动态 C# 纯逻辑冒烟测试
```

结果：

```text
Unity 编译成功
Console 无 Error
Console 无 Exception
Console 无 Warning
GroundedIdle → GroundedMove 通过
GroundedMove → GroundedIdle 通过
高优先级当前状态规则阻断低优先级规则通过
git diff --check 通过
```

动态测试只创建临时 GameObject 和最小测试 State，执行完立即销毁，没有创建或保存项目资源。

本次没有进入 GameScene Play Mode 手动操作 WASD，因此实际模型移动、动画表现和摄像机下的完整体验仍需后续人工验证。

## 七、协作与规范更新

继续采用“古法编程”协作方式：

```text
用户手敲时每次只推进一个明确文件或步骤
架构边界先讨论再实现
不为了隐藏 if 使用字典、反射或复杂语法
不把转换边分散到所有具体 State
不提前制造没有真实消费者的接口和空实现
```

本次同步更新 C# 规范：

[../../../.codex/work_init_CodexAI/04-编码规范-CSharp.md](../../../.codex/work_init_CodexAI/04-编码规范-CSharp.md)

明确：

```text
普通类、结构体成员必须显式声明访问修饰符
接口成员默认 public，不要求重复显式声明 public
```

## 八、当前尚未完成

```text
GameScene 中手动验证 Idle / Move 与实际位移
移动朝向旋转
Airborne / Jump / Fall 状态与 AirRules
落地时根据输入直接选择 GroundedIdle / GroundedMove
Input Buffer 与 Jump 输入缓存
Ability System 与 AbilityRules
Hurt / Dead 数据与 StatusRules
PlayerAnimatorDriver
Idle / Move 动画状态接线
Enemy 侧内容
```

当前 Transition 框架尚未处理：

```text
输入消费
转换成功结果
Ability 启动的两阶段提交
规则运行期动态增删
转换调试历史
```

这些能力应在出现真实需求时按现有结构扩展，不提前创建 Request、Source 或 Rule 接口。

## 九、下一步建议

下一步优先进行一次 GameScene Play Mode 手动验证：

```text
1. 无输入时保持 GroundedIdle
2. WASD 输入后进入 GroundedMove 并发生位移
3. 松开输入后回到 GroundedIdle
4. 按住 Shift 时仍处于 GroundedMove，但使用 SprintSpeed
5. Console 无状态机重入、目标状态未注册或空引用错误
```

如果移动闭环正常，再进入 `Airborne / Jump / Fall`。对应规则建议新增：

```text
Transition/Rules/AirRules.cs
```

第一批真实场景：

```text
Grounded → AirborneJump
Grounded → AirborneFall
AirborneJump → AirborneFall
AirborneFall → GroundedIdle / GroundedMove
```

其中落地目标需要根据移动输入直接决定具体叶子状态，避免先进入 Idle、下一帧再进入 Move。

## 十、工作区注意事项

状态转换规则框架已经提交：

```text
5ae7d54 新增玩家状态转换规则框架
```

提交只包含本次规则框架、Controller 接线、规范更新和既有文件末尾空行修正。

当前工作区仍有未包含在该提交中的既有改动：

```text
.codex/config.toml 修改
PlayerStateMachine.cs 工作区格式变化
若干旧目录 .meta 删除
SceneBackups 新增备份文件
```

后续提交时仍需显式筛选，不能把这些无关改动混入 Player 功能提交。
