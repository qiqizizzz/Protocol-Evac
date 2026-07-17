# Protocol_Evac PlayerController 开发前后交接记录

## 一、记录范围

本记录接续：

[1-PlayerContext与移动基础开发进展记录.md](1-PlayerContext与移动基础开发进展记录.md)

本次记录范围是 Player 第一阶段中 `PlayerController` 的接入方向、当前代码状态，以及下一次继续开发时的优先顺序。

当前仍然只推进 Player，不进入 Enemy、Input Buffer、Ability System 或 Transition Evaluator。

## 二、设计文档重新确认

主设计文档第一阶段顺序仍然是：

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

当前已经进入第 4 步：

```text
PlayerController
```

需要注意：设计文档中没有单独的 `PlayerCharacter.cs` 类名。这里的集成关系应理解为：

```text
PlayerCharacter(GameObject)
├─ CharacterController
└─ PlayerController
   ├─ PlayerContext
   ├─ PlayerMoveConfigSO
   └─ 后续 PlayerMotor
```

也就是说，`PlayerController` 是挂在玩家角色 GameObject 上的入口脚本，不是另起一个角色抽象层。

## 三、当前 PlayerController 状态

当前文件已创建：

```text
Assets/Scripts/Module/Player/Core/PlayerController.cs
```

当前职责：

```text
1. 缓存 Transform
2. 使用 GetComponent<CharacterController>() 获取 CharacterController
3. 校验 CharacterController 与 PlayerMoveConfigSO
4. 创建 PlayerContext
```

当前确认的写法偏好：

- `CharacterController` 获取逻辑直接写在 `Awake()` 中
- 不单独拆 `cacheComponents()`
- 这里允许直接使用 `GetComponent<CharacterController>()`
- 保留 `validateReferences()` 用于集中校验必要引用

当前不应加入：

- `PlayerMotor` 字段或调用逻辑，除非同时创建 `PlayerMotor.cs`
- 输入读取
- 状态机初始化
- Ability / Transition 相关逻辑
- 具体移动、跳跃或攻击逻辑

## 四、当前需要注意的问题

### 1. PlayerContext 仍有半初始化风险

当前 `PlayerContext(Transform transform)` 中仍是：

```csharp
if (transform == null)
{
    QLog.Error("Transform is null");
    return;
}
```

这会让 `PlayerContext` 构造成功但 `Transform` 未赋值。

后续建议在继续扩展 Controller 或 Motor 前修正为：

```csharp
QLog.Throw(new ArgumentNullException(nameof(transform)));
```

并补充：

```csharp
using System;
```

### 2. PlayerContext 命名仍可清理

当前命名：

```text
MoveDir
ResetRunTimeData
```

建议改为：

```text
MoveDirection
ResetRuntimeData
```

这不是架构阻塞项，但越早改越少牵连调用方。

### 3. PlayerController 当前尚未验证编译

本次归档前未执行完整编译验证。

下一次继续前建议先执行：

```powershell
dotnet build Player.csproj --no-restore
```

如果 Unity 已打开，也可以等 Unity 自动编译后查看 Console。

### 4. 工作区存在无关变更

当前工作区还存在一些与 PlayerController 无关的 `.meta` 删除和 `SceneBackups` 新目录。

继续开发或提交时不要把这些内容混入 Player 模块提交，除非确认是主动清理。

## 五、下一步建议

下一次开工第一步建议先做：

```text
修正 PlayerContext
```

最小修正内容：

```text
1. Transform 为空时使用 QLog.Throw(new ArgumentNullException(nameof(transform)))
2. MoveDir 改为 MoveDirection
3. ResetRunTimeData() 改为 ResetRuntimeData()
4. 构造函数中同步调用 ResetRuntimeData()
```

随后执行编译验证。

如果编译通过，再进入：

```text
Assets/Scripts/Module/Player/Core/PlayerMotor.cs
```

`PlayerMotor` 第一版目标：

```text
1. 持有 PlayerContext
2. 持有 PlayerMoveConfigSO
3. 持有 CharacterController
4. 提供 Init(...)
5. 提供 FixedTick(float fixedDeltaTime)
6. 读取 Context.MoveDirection / TargetMoveSpeed / IsMovementLocked
7. 通过 CharacterController.Move(...) 执行移动
8. 回写 Context.Velocity 与 Context.IsGrounded
```

`PlayerMotor` 完成后，再回到 `PlayerController` 接入：

```text
Awake()
├─ 获取 CharacterController
├─ 校验引用
├─ 创建 PlayerContext
└─ 初始化 PlayerMotor

FixedUpdate()
└─ PlayerMotor.FixedTick(Time.fixedDeltaTime)
```

## 六、古法编程协作偏好

后续继续时采用“古法编程”模式：

- 用户手敲代码
- AI 只给当前一步的目标、代码片段与注意点
- 不一次性堆完整系统
- 每次只推进一个明确小闭环
- 用户指出风格偏好后，后续代码建议应同步收敛
- 涉及架构边界时先确认，不擅自加新抽象

当前已确认偏好：

- 简单组件获取直接放在 `Awake()`
- 不为两三行逻辑拆过度小函数
- 必要引用校验可以集中在独立方法中
- 当前阶段保持 PlayerController 轻量，只做入口和生命周期

