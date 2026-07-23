# Protocol_Evac 千咲动画导入与 Player 动画边界纠偏记录

## 一、记录范围

本记录接续：

[../2026-7-21/1-Player声明式状态转换规则框架记录.md](../2026-7-21/1-Player声明式状态转换规则框架记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要处理两类问题：

```text
1. 千咲模型与 Mixamo 动作进入 Unity 后的形变、偏移和 Avatar 导入排查
2. PlayerAnimatorDriver 与 Transition / StateSelector 的职责边界纠偏
```

本次没有进入 Airborne、Jump、Fall、Ability、Input Buffer、Hurt、Dead 或 Enemy 侧开发。

## 二、本次确认的资源结论

### 1. Mixamo 下载设置

用户当前 Mixamo 下载设置为：

```text
Format: FBX for Unity(.fbx)
Skin: Without Skin
Frames per Second: 30
Keyframe Reduction: none
```

该设置本身是合理的。对于已经在 Unity 中保留模型与材质的角色，后续下载动作时使用 `Without Skin` 是正确方向。

本次确认：贴图不是动画变形的根因。静态模型贴图正常，问题主要出现在动画重定向、Root Transform、Root Motion / 代码位移叠加和动作导入设置。

### 2. 千咲模型与动作资源

当前关键资源路径：

```text
Assets/Art/Models/千咲/Chisaki_MeshOnly_ForMixamo.fbx
Assets/Animation/千咲/Running.fbx
Assets/Animation/千咲/Walking.fbx
Assets/Animation/千咲/千咲_Animator.controller
```

Unity 侧排查结果：

```text
Chisaki_MeshOnly_ForMixamoAvatar 为 valid / human
SkinnedMeshRenderer bones 与 bindposes 数量一致
Running.fbx / Walking.fbx 当前 avatarSetup = CopyFromOther
Running.fbx / Walking.fbx 当前 Source Avatar 指向 Chisaki_MeshOnly_ForMixamoAvatar
Running.fbx / Walking.fbx 当前 loopTime = 1
```

当前仍需注意：

```text
Running.fbx / Walking.fbx 的 keepOriginalPositionXZ = 0
```

这表示动作的 Root Transform Position (XZ) 当前没有 Bake Into Pose。若后续仍看到模型围绕 Player 根节点前后偏移，应优先检查：

```text
1. Mixamo 下载前是否勾选 In Place
2. Unity Animation Clip 中 Root Transform Position (XZ) 是否需要 Bake Into Pose
3. Animator.applyRootMotion 是否保持关闭，避免与 PlayerMotor 的 CharacterController.Move 叠加
```

## 三、本次 GameScene 验证结果

用户已在场景中手动验证移动闭环：

```text
WASD 移动状态正常
按住 Shift 可以加速
```

这说明以下主链路已基本打通：

```text
PlayerInputReader
→ PlayerContext
→ StateSelector
→ PlayerStateMachine
→ PlayerMoveState
→ PlayerMotor
→ CharacterController.Move
```

本次尚未完成完整动画体验验证。下一次仍需在 Play Mode 中确认：

```text
无输入时是否保持 Idle 表现
WASD 时是否进入 Walk 表现
Shift + WASD 时是否进入 Run 表现
松开输入后是否回到 Idle 表现
Console 是否无 Error / Exception / Warning
```

## 四、本次关键架构纠偏

### 1. 错误方向

本次一度尝试让 `PlayerAnimatorDriver` 直接读取输入事实判断动画：

```text
MoveInput
IsSprintPressed
IsInputLocked
IsMovementLocked
```

该方向已经确认不合适，因为它会在表现层复制 `MoveRules.canMove(...)` 的业务条件。

问题后果：

```text
1. MoveRules 与 AnimatorDriver 中出现两份移动条件
2. 后续 AirRules / AbilityRules / StatusRules 无法自然压住移动动画
3. Hurt / Dead / Dodge / Attack 可能与 Walk / Run 表现打架
4. PlayerController 和表现层会绕开 Transition 的状态裁决结果
```

该方向不应继续扩展。

### 2. 最终确认方向

`Transition` 是 Player 状态裁决入口，不只是 Idle / Move 的临时 if 判断。正确数据流应保持：

```text
Input / Context 事实
→ Rules/*Rules
→ StateSelector
→ PlayerStateMachine
→ PlayerState
→ PlayerMotor / PlayerAnimatorDriver
```

其中：

```text
Rules/*Rules
└─ 负责把运行时事实结构化成状态转换边

StateSelector
└─ 负责按 Status / Air / Ability / Move 优先级统一裁决目标状态

PlayerStateMachine
└─ 负责执行已经确定的状态切换

PlayerState
└─ 负责当前状态行为，例如写入移动意图

PlayerAnimatorDriver
└─ 只负责消费状态机结果与运动结果，更新 Animator 参数
```

### 3. PlayerAnimatorDriver 当前边界

当前 `PlayerAnimatorDriver` 已改为读取：

```text
PlayerStateMachine.CurrentLeafStateId
PlayerContext.TargetMoveSpeed
PlayerContext.Velocity
PlayerMoveConfigSO.WalkSpeed
```

当前语义：

```text
isMoving = CurrentLeafStateId == GroundedMove
isSprinting = isMoving && TargetMoveSpeed > WalkSpeed
moveSpeed = 水平 Velocity magnitude
```

这表示动画表现跟随 `StateSelector` 已经裁决出的状态结果，而不是重新判断输入能不能移动。

## 五、当前实现状态

当前涉及的代码路径：

```text
Assets/Scripts/Module/Player/Core/PlayerController.cs
Assets/Scripts/Module/Player/Core/PlayerAnimatorDriver.cs
Assets/Scripts/Module/Player/Transition/RuleLevel.cs
Assets/Scripts/Module/Player/Transition/StateRule.cs
Assets/Scripts/Module/Player/Transition/StateSelector.cs
Assets/Scripts/Module/Player/Transition/Rules/MoveRules.cs
```

当前 `PlayerController` 边界：

```text
1. 缓存必要组件引用
2. 创建 PlayerContext
3. 初始化 PlayerInputReader
4. 初始化 PlayerMotor
5. 注册并初始化 PlayerStateMachine
6. 初始化 PlayerAnimatorDriver
7. 初始化 StateSelector
8. 在 Update / FixedUpdate 中安排调度顺序
```

当前 `PlayerController` 不应写入：

```text
具体状态转换条件
Idle / Walk / Run 的表现判断
Air / Ability / Status 的业务判断
```

当前 `千咲_Animator.controller` 已确认包含：

```text
状态：Idle / Walk / Run
参数：moveSpeed / isMoving / isSprinting
```

注意：`Idle` 当前只是占位状态，仍建议后续下载或制作真正的 Idle 动作。

## 六、当前需要注意的问题

### 1. Root Transform XZ

若继续看到跑步动作偏移，需要优先处理动作根运动：

```text
Running.fbx / Walking.fbx
→ Animation
→ Root Transform Position (XZ)
→ 视情况勾选 Bake Into Pose
```

或从 Mixamo 重新下载动作时勾选：

```text
In Place
```

项目当前移动由 `PlayerMotor` 和 `CharacterController.Move(...)` 执行，原则上不应依赖 Animator Root Motion 推动角色。

### 2. Sprint 暂不作为独立状态

当前 `GroundedSprint` 仍未落地为独立状态。疾跑仍是 `GroundedMove` 下的速度与表现差异：

```text
GroundedMove
└─ TargetMoveSpeed = WalkSpeed / SprintSpeed
```

如果后续疾跑要承载体力消耗、转向限制、特殊动画、打断规则，再考虑让 `GroundedSprint` 成为真实状态并增加对应 `MoveRules`。

### 3. 动画表现不应反推状态

后续不要让 Animator 参数或 Animator 当前状态反向决定 `PlayerStateMachine` 状态。状态来源仍应是：

```text
PlayerContext 事实
→ Rules
→ StateSelector
→ PlayerStateMachine
```

## 七、当前尚未完成

```text
Play Mode 中重新验证 Idle / Walk / Run 动画表现
下载或制作真正 Idle 动作替换占位 Idle
确认 Running / Walking 是否需要 In Place 或 Bake Into Pose
移动朝向旋转
Airborne / Jump / Fall 状态与 AirRules
落地时根据输入直接选择 GroundedIdle / GroundedMove
Input Buffer 与 Jump 输入缓存
Ability System 与 AbilityRules
Hurt / Dead 数据与 StatusRules
Enemy 侧内容
```

## 八、下一步建议

下一步先不要进入 Jump。建议先完成地面层表现闭环：

```text
1. 退出 Play Mode 后重新进入 Play Mode
2. 验证无输入时 Animator 进入 Idle
3. 验证 WASD 时 Animator 进入 Walk
4. 验证 Shift + WASD 时 Animator 进入 Run
5. 验证松开输入后回到 Idle
6. 验证 Console 无 Error / Exception / Warning
7. 若跑步偏移仍明显，优先处理 Running.fbx 的 In Place / Root Transform Position (XZ)
```

如果地面动画表现稳定，再进入：

```text
Transition/Rules/AirRules.cs
```

第一批 Air 场景仍建议保持：

```text
Grounded → AirborneJump
Grounded → AirborneFall
AirborneJump → AirborneFall
AirborneFall → GroundedIdle / GroundedMove
```

## 九、工作区注意事项

当前工作区仍有需要筛选的改动：

```text
M Assets/Scripts/Module/Player/Core/PlayerAnimatorDriver.cs
M Assets/Scripts/Module/Player/Core/PlayerController.cs
D Assets/Scripts/Module/Player/Ability/Abilities.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639204402515929121.backup
```

其中：

```text
PlayerAnimatorDriver.cs / PlayerController.cs
└─ 属于本次 Player 动画边界纠偏相关改动

Abilities.meta 删除
SceneBackups 新增备份
└─ 不是本次 Player 动画边界纠偏的核心内容，后续提交前必须单独判断是否保留
```

后续提交时需要显式筛选，不要把无关 `.meta` 删除或 `SceneBackups` 混入 Player 功能提交。
