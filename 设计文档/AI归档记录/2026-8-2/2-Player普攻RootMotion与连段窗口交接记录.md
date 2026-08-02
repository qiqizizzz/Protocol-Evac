# Protocol_Evac Player 普攻 RootMotion 与连段窗口交接记录

## 一、记录范围

本记录接续：

[1-Player普攻连段与MCP排查记录.md](1-Player普攻连段与MCP排查记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次记录围绕 Player 普攻三段动画衔接、attack03 动画位移接入、每段连段窗口配置，以及 Inspector 折叠尝试的最终取舍做交接。

```text
1. 普攻三段动画已按 NormalAttackIndex 播放 attack01 / attack02 / attack03
2. 普攻结束回 Idle / Move 使用较短 CrossFade，解决硬切回 Idle 的生硬感
3. attack03 的动画 XZ 位移被保留，并由代码接入 CharacterController.Move
4. 每段动画数据追加了连段窗口字段，但默认未启用，旧配置逻辑仍可回退
5. 曾尝试使用 CustomPropertyDrawer 折叠“连段窗口”，用户反馈样式不理想，已撤回该新类
6. 用户明确希望后续不要为了 Inspector 折叠随意新增膨胀的编辑器类
```

## 二、本次确认的设计 / 协作偏好

### 1. 普攻收招感来源

本次确认：当前看到的“收招感”不是代码生成了收招动画，而是：

```text
攻击动画尾段本身存在一定回位姿态
再叠加普攻退出时回 Grounded_Common 的短 CrossFade
```

相关配置：

```text
Assets/Scripts/Module/Player/HFSM/Config/Skill/PlayerNormalAttackConfigSO.cs
└─ NormalAttackExitBlendDurationValue = 0.15f
```

相关行为：

```text
PlayerNormalAttackState.Exit
└─ RequestAnimReplay(GroundedIdle / GroundedMove, NormalAttackExitBlendDuration)

PlayerAnimWriter.applyReplayRequest
└─ GroundedIdle / GroundedMove 使用 AnimReplayBlendDuration CrossFade 到 Grounded_Common
```

### 2. attack03 位移选择“代码跟随动画”

用户最终选择不把 attack03 改成完全原地动画，而是保留动画自带前冲位移，让代码跟着该动画位移走。

当前口径：

```text
动画资源负责输出 root motion XZ 位移
PlayerRootMotionReceiver 只负责收 Animator.deltaPosition
PlayerContext 只缓存 root motion 位移和开关
PlayerNormalAttackState 决定当前普攻段是否允许 root motion
PlayerMotor 负责通过 CharacterController.Move 执行最终位移
```

这避免把 `attack03` 的动画状态名硬写在 root motion 接收器中，也保持“状态写意图，Motor 执行运动”的边界。

### 3. 连段窗口按每段动画数据配置

用户确认后续普攻段数不一定固定为 3 段，因此不要写成：

```text
attack01Open
attack02Open
attack03Open
```

当前实现选择：

```text
继续使用现有 StateClipValues 数组
在每个 PlayerStateClipData 上追加连段窗口字段
不改 StateClipValues 的数组结构
不破坏已有 SO 中已配置的 Clip 和 StateDurationValue
```

每段数据新增字段：

```text
UseComboWindowValue
ComboOpenNormalizedTimeValue
ComboCloseNormalizedTimeValue
```

默认：

```text
UseComboWindowValue = false
ComboOpenNormalizedTimeValue = 0.35
ComboCloseNormalizedTimeValue = 0.75
```

因此旧 SO 在不勾选 `UseComboWindowValue` 时仍回退到原先的输入缓存判断。

### 4. Inspector 折叠偏好

用户希望“连段窗口”区域可以折叠，但不满意新增 `PlayerStateClipDataDrawer` 后的 Inspector 表现。

已确认：

```text
[Header] / [Space] / [Tooltip] 不能实现真正折叠
真正折叠通常需要 CustomEditor 或 PropertyDrawer
本次新增的 PlayerStateClipDataDrawer 已删除
后续若继续做折叠，优先考虑在既有 PlayerStateCommonConfigSOEditor 内完成
不要随意新增单独 drawer 导致 Inspector 风格膨胀
```

## 三、当前实现状态

### 1. 普攻退出混合

相关文件：

```text
Assets/Scripts/Module/Player/Context/PlayerContext.cs
Assets/Scripts/Module/Player/HFSM/Animation/PlayerAnimWriter.cs
Assets/Scripts/Module/Player/HFSM/Config/Skill/PlayerNormalAttackConfigSO.cs
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
```

当前关键数据流：

```text
PlayerNormalAttackState.Exit
└─ RequestAnimReplay(GroundedIdle / GroundedMove, NormalAttackExitBlendDuration)
   └─ PlayerContext.AnimReplayBlendDuration
      └─ PlayerAnimWriter.applyReplayRequest
         └─ Animator.CrossFadeInFixedTime(Grounded_Common, blendDuration)
```

### 2. attack03 RootMotion 接入

相关文件：

```text
Assets/Scripts/Module/Player/Context/PlayerContext.cs
Assets/Scripts/Module/Player/Core/PlayerController.cs
Assets/Scripts/Module/Player/Core/PlayerMotor.cs
Assets/Scripts/Module/Player/HFSM/Animation/PlayerAnimWriter.cs
Assets/Scripts/Module/Player/HFSM/Animation/PlayerRootMotionReceiver.cs
Assets/Scripts/Module/Player/HFSM/Config/Skill/PlayerNormalAttackConfigSO.cs
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
```

当前结构：

```text
PlayerAnimWriter.Init
└─ m_animator.applyRootMotion = true

PlayerController.initAnim
├─ m_rootMotionReceiver = m_animator.GetComponent<PlayerRootMotionReceiver>()
├─ 如果没有则 AddComponent<PlayerRootMotionReceiver>()
└─ m_rootMotionReceiver.Init(m_animator, m_context)

PlayerNormalAttackState
├─ Enter / tryAdvanceCombo 调用 refreshRootMotionMoveEnabled
└─ Exit 调用 SetRootMotionMoveEnabled(false)

PlayerNormalAttackConfigSO
└─ RootMotionAttackIndexValues = { 2 }

PlayerRootMotionReceiver.OnAnimatorMove
├─ 仅在 context.IsRootMotionMoveEnabled 时读取 Animator.deltaPosition
├─ 清掉 y
└─ AddRootMotionDeltaPosition

PlayerMotor.FixedTick
├─ ConsumeRootMotionDeltaPosition
├─ 若存在 root motion 位移，调用 applyRootMotionMove
└─ 使用 CharacterController.Move(rootMotionDeltaPosition)
```

用户口头反馈：当前 attack03 跟随动画位移的效果“非常完美”。

### 3. 连段窗口

相关文件：

```text
Assets/Scripts/Module/Player/HFSM/Config/Common/PlayerStateClipData.cs
Assets/Scripts/Module/Player/HFSM/Config/Skill/PlayerNormalAttackConfigSO.cs
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
```

当前逻辑：

```text
PlayerStateClipData
├─ TryGetComboWindow
└─ UseComboWindowValue=false 时返回 false

PlayerNormalAttackConfigSO
└─ TryGetComboWindow(index, out open, out close)

PlayerNormalAttackState.Tick
├─ 记录上一帧 NormalizedTime
├─ Timer.Tick
├─ refreshComboBufferedInput(previousNormalizedTime, currentNormalizedTime)
└─ 计时结束后 tryAdvanceCombo

refreshComboBufferedInput
├─ 当前段未启用连段窗口则不处理
├─ 当前时间段覆盖 [open, close] 则检查 InputBuffer
└─ 窗口内存在 NormalAttack 缓存则 m_hasComboBufferedInput = true

canAdvanceCombo
├─ 当前段启用连段窗口：只看 m_hasComboBufferedInput
└─ 当前段未启用连段窗口：回退旧的 InputBuffer.Has
```

当前设计重点：

```text
窗口只决定“是否承认这次输入可接下一段”
当前仍在本段计时结束后切到下一段
如果未来想做到窗口内立刻切段，需要另行调整 tryAdvanceCombo 调用时机
```

## 四、场景 / 资源 / 配置状态

### 1. attack03 导入状态

当前资源：

```text
Assets/Animation/千咲/Attack03.fbx
Assets/Animation/千咲/Attack03.fbx.meta
```

当前位移相关导入字段：

```text
loopBlendPositionXZ: 0
keepOriginalPositionY: 1
keepOriginalPositionXZ: 0
```

含义：

```text
attack03 保留 XZ root motion 输出
Y 方向仍保持原始处理，代码侧最终把 deltaPosition.y 清零并由 PlayerMotor 处理重力 / 贴地
```

### 2. PlayerNormalAttackConfig.asset

当前资产：

```text
Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
```

当前 diff 显示新增序列化字段：

```text
StateClipValues[0..2]
├─ UseComboWindowValue: 0
├─ ComboOpenNormalizedTimeValue: 0.35
└─ ComboCloseNormalizedTimeValue: 0.75

NormalAttackExitBlendDurationValue: 0.15
RootMotionAttackIndexValues: 02000000
```

注意：

```text
UseComboWindowValue 当前为 0，所以连段窗口逻辑对当前手感不会立刻改变
RootMotionAttackIndexValues 当前表示 attackIndex=2，也就是 attack03 使用 root motion
```

### 3. 其他配置资产

当前 `git status --short` 还显示以下配置资产修改：

```text
Assets/Config/Player/Action/PlayerDodgeConfig.asset
Assets/Config/Player/Air/PlayerAirConfig.asset
Assets/Config/Player/Move/PlayerMoveConfig.asset
```

这些资产很可能受 `PlayerStateClipData` 追加字段影响，Unity 重新序列化后写入新字段。后续提交前需要确认这些资产 diff 是否只是新增连段窗口默认字段，而不是用户误改了其他配置。

## 五、当前需要注意的问题

### 1. Inspector 折叠尚未完成

曾新增：

```text
Assets/Scripts/Module/Player/HFSM/Config/Editor/PlayerStateClipDataDrawer.cs
```

用户反馈显示效果不理想，并质疑是否需要新增类。该类及其 `.meta` 已删除。

当前结论：

```text
暂时不要继续做折叠
如果未来继续做，先说明 Unity 默认 Attribute 无法折叠
再讨论是否在 PlayerStateCommonConfigSOEditor 中局部手绘 StateClipValues
```

### 2. 连段窗口当前默认关闭

因为 `UseComboWindowValue` 默认是 false，当前运行逻辑对已有配置仍走旧输入缓存路径。若用户期望立即启用窗口，需要在 `PlayerNormalAttackConfig.asset` 的对应段手动勾选。

推荐初始值：

```text
attack01: open 0.35 / close 0.75
attack02: open 0.30 / close 0.75
attack03: 通常不需要启用连段窗口
```

### 3. MCP 连接状态

本次归档前执行过 Unity `assets-refresh`，返回成功。随后最后一次读取 Console 日志时，MCP 返回：

```text
Connection refused at http://localhost:22868/api/tools/console-get-logs
Is the MCP server running? Start Unity Editor with the MCP plugin first.
```

因此本记录只确认早前刷新成功，不声称最终 Console 日志再次验证成功。

## 六、当前尚未完成

```text
是否启用 PlayerNormalAttackConfig.asset 中 attack01 / attack02 的 UseComboWindowValue
是否为连段窗口提供更轻量、用户满意的 Inspector 折叠方案
是否把 root motion 倍率做成配置项，以便微调 attack03 前冲距离
是否把窗口内输入后“本段结束再切”升级为“达到接招窗口后立即切”
提交前确认 Dodge / Air / Move 配置资产的序列化 diff 是否可接受
提交前处理 D Assets/Scripts/Module/Player/Skill/Core.meta
提交前处理未跟踪 SceneBackups 备份文件
```

## 七、下一步建议

如果继续开发，建议顺序如下：

```text
1. 暂停 Inspector 折叠实现，避免继续在编辑器代码上消耗
2. 在 PlayerNormalAttackConfig.asset 中只启用 attack01 / attack02 的 UseComboWindowValue
3. Play Mode 验证窗口是否只在指定区间承认下一段输入
4. 如果用户觉得“按了但等到本段结束才切”太慢，再调整为窗口内立即推进
5. 最后再决定是否做 Inspector 折叠；若做，优先改现有 PlayerStateCommonConfigSOEditor，不再新增独立 drawer
```

## 八、工作区注意事项

当前工作区存在未提交改动：

```text
M  Assets/Config/Player/Action/PlayerDodgeConfig.asset
M  Assets/Config/Player/Air/PlayerAirConfig.asset
M  Assets/Config/Player/Move/PlayerMoveConfig.asset
M  Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
M  Assets/Scripts/Module/Player/HFSM/Config/Common/PlayerStateClipData.cs
M  Assets/Scripts/Module/Player/HFSM/Config/Skill/PlayerNormalAttackConfigSO.cs
M  Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
D  Assets/Scripts/Module/Player/Skill/Core.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639212679172188995.backup
```

注意：`PlayerRootMotionReceiver.cs` 当前是已跟踪文件且本次归档时无未提交 diff，但它是当前 root motion 架构的一部分，后续不要误删。

