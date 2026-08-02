# Protocol_Evac Player 普攻连段与 MCP 排查记录

## 一、记录范围

本记录接续：

[../2026-7-31/1-Player状态动画时长配置与Inspector同步记录.md](../2026-7-31/1-Player状态动画时长配置与Inspector同步记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本次记录围绕 Player Skill / NormalAttack 的状态机连线、输入解释器拆分、普攻连段实现和 Unity MCP 排查结果做交接。

```text
1. Skill 已作为顶层复合状态，NormalAttack 是 Skill 的子状态
2. 普攻输入使用 PlayerBufferedInputType.NormalAttack，不使用按键名作为业务语义
3. 普攻连段当前按 normalAttackIndex 从 0 开始驱动 attack01 / attack02 / attack03
4. Sprint 长按/短按解释逻辑已从 PlayerInputReader 拆到 PlayerSprintInterpreter
5. MCP 排查确认当前 Editor 快照未进入 Play Mode，因此左键输入在该快照下不会触发运行时逻辑
6. Animator 中 attack03 当前没有 outgoing transition，后续 Play Mode 若出现最后一段卡住，应优先排查这里
```

## 二、本次确认的设计 / 协作偏好

### 1. Skill 状态机边界

当前口径保持：

```text
Grounded
Action
Skill
└─ NormalAttack
   ├─ attack01
   ├─ attack02
   └─ attack03
```

`Skill` 是 HFSM 顶层复合状态，不再把普攻挂到 `Action` 下。Animator 侧对应结构为：

```text
Base Layer
└─ Skill
   └─ NormalAttack
      ├─ attack01
      ├─ attack02
      └─ attack03
```

代码层只把 `SkillNormalAttack` 作为玩法状态；`attack01 / attack02 / attack03` 是动画子状态，由 `normalAttackIndex` 驱动，不应在 HFSM 里平铺成三个顶层状态。

### 2. 普攻索引从 0 开始

用户最终确认 `NormalAttackIndex` 从 0 开始更适合代码：

```text
0 -> attack01
1 -> attack02
2 -> attack03
```

这是当前 `PlayerNormalAttackState` 与 Animator 参数一致的索引约定。

### 3. 不写默认兜底配置

用户已明确禁止在 Player 运行时代码中继续添加类似：

```text
DEFAULT_SPRINT_HOLD_TIME
DEFAULT_NORMAL_ATTACK_DURATION
```

运行时必须读取配置资产数据。配置缺失是开发期配置问题，应通过明确配置和 Editor 检查解决，不在状态 Tick / Enter 内反复兜底。

### 4. 不做高频无意义判空

已记录的协作偏好：

```text
不要在每次 Tick 里判断一次 inputConfig 是否为空
不要在每次 Enter 普攻时判断 NormalAttackConfig 是否为空
必要依赖应在初始化阶段暴露问题
不要用静默 return 掩盖配置错误
```

后续新增 Player 运行时代码时，应优先在初始化阶段完成依赖校验，避免高频分支把架构写散。

### 5. 输入读取保持 generated wrapper 风格

用户明确反对在 `PlayerInputReader` 中单独写：

```text
m_inputActions.asset.FindActionMap("Player").FindAction("Attack")
```

当前应继续使用 New Input System 生成类访问：

```text
m_inputActions.Player.Attack.WasPressedThisFrame()
```

这符合现有代码风格，也避免字符串路径散落。

## 三、当前实现状态

### 1. PlayerController 已接入 Skill

当前文件：

```text
Assets/Scripts/Module/Player/Core/PlayerController.cs
```

当前 `PlayerController` 已持有：

```text
MoveConfig
InputConfig
AirConfig
DodgeConfig
NormalAttackConfig
ViewConfig
```

初始化链路包含：

```text
initCore
├─ PlayerContext
├─ PlayerInputReader
├─ PlayerMotor
└─ PlayerViewController

initHFSM
├─ RegisterAllStates
├─ PlayerMoveTransitionRules
├─ PlayerAirTransitionRules
├─ PlayerActionTransitionRules
└─ PlayerSkillTransitionRules

initAnim
├─ PlayerMoveAnimRules
├─ PlayerAirAnimRules
├─ PlayerActionAnimRules
└─ PlayerSkillAnimRules
```

已注册状态包括：

```text
PlayerSkillState
PlayerNormalAttackState
```

### 2. 输入读取已接入普攻

当前文件：

```text
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs
```

当前每帧读取：

```text
Move
Sprint
Jump
Attack
Look
SwitchToFirstPerson
SwitchToThirdPerson
```

鼠标左键通过：

```text
m_inputActions.Player.Attack.WasPressedThisFrame()
```

写入：

```text
PlayerBufferedInputType.NormalAttack
```

### 3. Sprint 输入解释器已拆分

当前文件：

```text
Assets/Scripts/Module/Player/Input/Interpreter/PlayerSprintInterpreter.cs
```

职责边界：

```text
PlayerInputReader
└─ 只读取本帧 InputActions 并转发输入事实

PlayerSprintInterpreter
├─ 持有 m_sprintPressedTime
├─ 持有 m_isSprintPressing
├─ 长按后写 context.IsSprintPressed
└─ 短按松开时写入 PlayerBufferedInputType.Dodge
```

该拆分符合用户要求：`m_sprintPressedTime` 和 `m_isSprintPressing` 不继续放在 `PlayerInputReader`。

### 4. 普攻状态已实现三段连段逻辑

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
```

当前行为：

```text
Enter
├─ m_currentAttackIndex = 0
├─ context.NormalAttackIndex = 0
├─ Consume NormalAttack
├─ IsStateFinished = false
├─ IsMovementLocked = NormalAttackConfig.LockMovement
├─ RequestAnimReplay(SkillNormalAttack)
└─ Start NormalAttackDuration

Tick
├─ DurationTimer.Tick
├─ 未结束则等待
├─ 计时结束后尝试 tryAdvanceCombo
└─ 没有下一段输入则 IsStateFinished = true

tryAdvanceCombo
├─ nextAttackIndex = current + 1
├─ 超过 StateClipCount 则失败
├─ 检查 NormalAttack 输入缓存
├─ Consume NormalAttack
├─ NormalAttackIndex = nextAttackIndex
└─ Start GetStateDuration(nextAttackIndex)

Exit
├─ Reset timer
├─ IsStateFinished = false
├─ IsMovementLocked = false
├─ NormalAttackIndex = 0
└─ grounded 时请求回 GroundedMove / GroundedIdle 动画
```

注意：当前连段推进是在本段计时结束后检查输入缓存，不是动画中段立即跳转。若后续手感需要更早接段，应引入连段窗口数据，而不是在状态里硬编码比例或默认时间。

### 5. Skill Transition Rules 已存在

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Transition/Rules/PlayerSkillTransitionRules.cs
```

当前规则：

```text
Grounded -> Skill
└─ 输入未锁、移动未锁、已落地、NormalAttack 缓存有效

SkillNormalAttack -> AirborneFall
└─ IsStateFinished && !IsGrounded

SkillNormalAttack -> GroundedMove
└─ IsStateFinished && IsGrounded && canMove

SkillNormalAttack -> GroundedIdle
└─ IsStateFinished && IsGrounded
```

当前从 `Grounded` 切到 `Skill`，由 HFSM 自动展开到 `SkillNormalAttack`。这和当前 Animator 复合状态结构一致。

### 6. 动画写入已接 normalAttackIndex

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Animation/PlayerAnimWriter.cs
Assets/Scripts/Module/Player/HFSM/Animation/Rules/PlayerSkillAnimRules.cs
```

当前写入 Animator 参数：

```text
moveSpeed
verticalSpeed
isGrounded
normalAttackIndex
```

当前普攻重播路径：

```text
Base Layer.Skill.NormalAttack.attack01
```

## 四、场景 / 资源 / 配置状态

### 1. 当前场景

MCP 确认当前打开场景：

```text
Assets/Scenes/GameScene.unity
```

根物体包含：

```text
Directional Light
Global Volume
Player
Plane
```

`Player` 层级下存在：

```text
Player/Chisaki_MeshOnly_ForMixamo
Player/ViewRoot/PlayerCamera
```

### 2. PlayerController 引用

MCP 读取 `Player` 上的 `Module.Player.Core.PlayerController`，确认以下引用已赋值：

```text
MoveConfig
InputConfig
AirConfig
DodgeConfig
NormalAttackConfig
ViewConfig
```

其中 `NormalAttackConfig` 指向：

```text
Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
```

### 3. 普攻配置资产

当前文件：

```text
Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
```

当前配置：

```text
StateClipValues[0]
├─ attack01 Clip
└─ StateDurationValue: 1.2666668

StateClipValues[1]
├─ attack02 Clip
└─ StateDurationValue: 1.8333334

StateClipValues[2]
├─ attack03 Clip
└─ StateDurationValue: 1.8000001

NormalAttackBufferTimeValue: 0.25
LockMovementValue: 1
```

### 4. Animator Controller 状态

当前文件：

```text
Assets/Animation/千咲/千咲_Animator.controller
```

已确认：

```text
Base Layer
└─ Skill
   └─ NormalAttack
      ├─ attack01
      ├─ attack02
      └─ attack03
```

参数：

```text
normalAttackIndex: int
```

过渡：

```text
attack01 -> attack02
└─ condition: normalAttackIndex == 1

attack02 -> attack03
└─ condition: normalAttackIndex == 2
```

风险点：

```text
attack03
└─ m_Transitions: []
```

`attack03` 当前没有 outgoing transition。若 Play Mode 中第三段结束后动画卡住，应优先考虑补 `attack03 -> Exit` 或继续依赖代码层 Exit 后的 CrossFade，并验证 HFSM 是否确实退出。

## 五、MCP 排查结论

本次用户要求“运行 MCP，看普攻问题原因，不要直接改代码”。已执行 MCP 检查，结论如下：

```text
Application.isPlaying = false
EditorApplication.isPlaying = false
EditorApplication.isPaused = false
EditorApplication.isCompiling = false
PlayerController.m_isInited = false
```

因此在当前 MCP 快照中，玩家运行时并未进入 Play Mode。此时左键输入不会经过：

```text
PlayerController.Update
└─ PlayerInputReader.Tick
   └─ InputBuffer.Record(NormalAttack)
```

当前 Console 中没有抓到有效的普攻运行时异常。可见错误主要来自 MCP 早前错误读取路径，例如把 `StateClipValues` 当作 `PlayerNormalAttackConfigSO` 直接字段路径读取时产生的工具错误，不代表玩法代码运行异常。

本次没有修改任何运行时代码。

## 六、当前需要注意的问题

### 1. 当前“左键没反应”的直接解释

就 MCP 快照而言，直接原因是 Editor 没进 Play Mode：

```text
Editor 不在 Play Mode
└─ PlayerController.Awake / Update 不处于运行测试语境
   └─ PlayerInputReader 不会正常响应实机输入
```

下一轮如果用户仍然反馈“左键没反应”，必须先确认 Play Mode 状态，不要直接改输入代码。

### 2. 当前“按完左键卡姿势”的高风险解释

如果 Play Mode 中确实进入了 `attack01` 或后续攻击姿势，但无法退出，需要优先验证：

```text
PlayerStateMachine.CurrentLeafStateId
PlayerContext.IsStateFinished
PlayerContext.IsGrounded
PlayerContext.NormalAttackIndex
Animator 当前状态路径
```

最可疑点：

```text
attack03 没有 outgoing transition
```

其次需要确认：

```text
SkillNormalAttack 是否在 DurationTimer 结束后设置 IsStateFinished
PlayerSkillTransitionRules 是否从 SkillNormalAttack 回 GroundedIdle / GroundedMove
PlayerNormalAttackState.Exit 是否触发 RequestAnimReplay(GroundedIdle/GroundedMove)
```

### 3. 连段窗口当前太粗

当前连段推进逻辑只在本段 `DurationTimer` 结束后检查输入缓存。配置中 `NormalAttackBufferTime = 0.25`，而攻击动画时长约 1.26 / 1.83 / 1.80 秒。

这意味着玩家必须在每段结束前很短时间内输入下一段，手感上可能偏苛刻。后续如果要“高级 Unity 工程师”的结构，应引入明确的连段窗口数据，而不是把窗口硬写在状态里。

推荐下一阶段数据方向：

```text
PlayerStateClipData 或后续 PlayerSkillStepData
├─ StateDuration
├─ ComboInputOpenTime
└─ ComboInputCloseTime
```

或者直接进入 Skill Event：

```text
CancelWindow / ComboWindow Event
├─ StartTime
└─ EndTime
```

### 4. Animator Exit 与代码 CrossFade 的职责需要收口

当前 `PlayerNormalAttackState.Exit()` 会请求动画回 `Grounded_Common`。这表示退出表现主要由代码层控制。

但 Animator 里部分状态仍存在 Exit 线，例如 `dodge -> Exit`。后续需要统一口径：

```text
方案 A：玩法状态退出时由代码 CrossFade 回目标动画
方案 B：Animator 内部每个动作状态都配置完整 Exit
```

当前系统更接近方案 A。若继续方案 A，`attack03` 没有 Exit 不一定立刻是 bug，但必须验证 HFSM 退出一定发生。若希望 Animator 图更自洽，则应为 `attack03` 补出口。

## 七、当前尚未完成

```text
Play Mode 中复测左键是否进入 SkillNormalAttack
Play Mode 中观察 CurrentLeafStateId / IsStateFinished / NormalAttackIndex
确认 attack01 不连段时能否按时回 GroundedIdle / GroundedMove
确认 attack01 -> attack02、attack02 -> attack03 的输入窗口是否符合手感
确认 attack03 结束后是否通过代码 CrossFade 回地面动画
决定是否给 attack03 补 Animator Exit 线
决定连段窗口继续用 NormalAttackBufferTime，还是升级为每段 ComboWindow
把普攻命中帧、前摇、后摇、取消窗口从“总时长”升级为显式数据
```

## 八、下一步建议

下一步不要先改代码，先在 Play Mode 中做最小可观测验证：

```text
1. 进入 Play Mode
2. 按一次左键
3. 观察是否进入 SkillNormalAttack
4. 观察 NormalAttackIndex 是否为 0
5. 等待 1.27 秒左右，观察 IsStateFinished 是否变 true
6. 观察是否切回 GroundedIdle / GroundedMove
7. 在第一段结束前 0.25 秒内再次左键，观察 NormalAttackIndex 是否变 1
8. 第三段结束后重点观察是否卡在 attack03
```

如果需要临时加调试，不要把日志散在状态里长期保留。建议后续做一个 Editor-only 的 Player Debug 面板，集中显示：

```text
ActiveStatePath
CurrentLeafStateId
IsGrounded
IsMovementLocked
IsStateFinished
NormalAttackIndex
InputBuffer 中 NormalAttack 是否有效
Animator 当前状态
```

## 九、工作区注意事项

本次归档前 `git status --short` 显示：

```text
 D Assets/Scripts/Module/Player/Skill/Core.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639212679172188995.backup
```

这些不是本次 MCP 排查或归档主动产生的运行时代码修改。后续提交前需要确认：

```text
是否保留 Skill/Core.meta 删除
是否忽略或删除 SceneBackups 备份文件
```

另外，当前项目偏好已经非常明确：后续 AI 修改 Player 代码前，必须先说明修改原因和影响范围；除非用户明确要求，排查阶段不要直接改代码。

