# Protocol_Evac Player 状态动画时长配置与 Inspector 同步记录

## 一、记录范围

本记录接续：

[../2026-7-30/1-Player Skill系统命名与编辑器路线记录.md](../2026-7-30/1-Player%20Skill系统命名与编辑器路线记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本次围绕 Player 状态动画时长的配置方式、普攻控制约束、配置目录归属和最小 Inspector 工具完成收口：

```text
1. 普攻期间不能移动，不允许主动取消，并且需要有明显前摇和后摇
2. 动画时长不再长期依赖手写数字，改为从 AnimationClip.length 在编辑期同步
3. 不新建独立 EditorWindow，直接在状态 Config SO 的 Inspector 上提供同步按钮
4. 用 PlayerStateCommonConfigSO 统一保存 Player 状态动画段落数组
5. Move / Air / Dodge / NormalAttack 四类 HFSM Config 均继承该通用基类
6. Input Config 与 View Config 按模块职责迁离 HFSM/Config
```

## 二、本次确认的设计

### 1. 普攻控制规则

用户已经明确第一版普攻的手感边界：

```text
普攻期间
├─ 锁定移动
├─ 不允许取消
├─ 需要明显前摇
└─ 需要明显后摇
```

“移动输入影响朝向”不作为第一版目标。普攻进入后应固定本次攻击的控制权，不允许玩家一边攻击一边移动。

当前需要区分：上述内容是已经确认的设计，但代码尚未全部表达。现有 `PlayerNormalAttackState` 能在状态期间锁移动并按总时长结束；独立前摇、有效帧、后摇和禁止取消规则仍未落地。

### 2. 动画时长采用编辑期同步

本次没有采用运行时遍历 `Animator.runtimeAnimatorController.animationClips` 作为正式数据来源，原因是状态 Config 需要稳定、可见、可序列化的时长数据。

当前方案：

```text
FBX AnimationClip 子资产
└─ 手动拖入 Config SO 的 AnimationClip 字段
   └─ 点击“同步全部动画时长”
      └─ 将 AnimationClip.length 写入序列化 Duration
         └─ 运行时只读取 Config 数据
```

该方案的边界：

```text
已实现：从已拖入的 AnimationClip 同步时长
未实现：自动搜索 FBX、自动匹配状态名、自动把 Clip 填入数组
```

### 3. 通用字段只覆盖动画段落

通用基类命名确定为：

```text
PlayerStateCommonConfigSO
```

它只负责所有 Player 状态 Config 共有的动画段落，不尝试统一移动速度、输入缓存、地面检测或技能字段。

数据结构：

```text
PlayerStateCommonConfigSO
└─ PlayerStateClipData[] StateClipValues
   └─ PlayerStateClipData
      ├─ AnimationClip StateClipValue
      └─ float StateDurationValue
```

数组可以承载 Move 的多种循环动画、Air 的跳跃/下落动画，以及 NormalAttack 后续第一段、第二段、第三段动画。数组索引的具体语义仍由各 Config 自己定义，不在通用基类中硬编码。

### 4. Inspector 工具路线

本次决定使用 Unity 原生 `CustomEditor`，直接挂在 `PlayerStateCommonConfigSO` 及其派生类的 Inspector 上。

```text
不创建独立 EditorWindow
不要求 NaughtyAttributes
不让运行时代码引用 UnityEditor
```

这只是当前最小同步工具，不影响未来 Skill 数据稳定后继续制作专用 Timeline EditorWindow。

## 三、当前实现状态

### 1. 通用运行时数据

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Config/Common/PlayerStateCommonConfigSO.cs
Assets/Scripts/Module/Player/HFSM/Config/Common/PlayerStateClipData.cs
```

`PlayerStateCommonConfigSO` 当前提供：

```text
StateClips
StateClipCount
GetStateClip(index)
GetStateDuration(index, defaultDuration)
SyncAllClipDurations()
```

`PlayerStateClipData.SyncDurationFromClip()` 会把当前 `AnimationClip.length` 写入 `StateDurationValue`。

### 2. Inspector 同步按钮

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Config/Editor/PlayerStateCommonConfigSOEditor.cs
```

该 Editor 使用：

```text
[CustomEditor(typeof(PlayerStateCommonConfigSO), true)]
```

因此所有派生 Config 都会显示“同步全部动画时长”按钮。同步操作已接入 `Undo.RecordObject` 和 `EditorUtility.SetDirty`。

### 3. 已继承通用基类的 Config

当前四个 HFSM 状态配置均已继承 `PlayerStateCommonConfigSO`：

```text
PlayerMoveConfigSO
PlayerAirConfigSO
PlayerDodgeConfigSO
PlayerNormalAttackConfigSO
```

其中：

```text
PlayerDodgeConfigSO.DodgeDuration
└─ 读取 GetStateDuration(0, 0.32f)

PlayerNormalAttackConfigSO.NormalAttackDuration
└─ 读取 GetStateDuration(0, 0.6f)
```

Move 与 Air 当前只继承动画段落数组，尚未添加基于索引的语义属性。循环动画时长目前也没有参与状态退出逻辑。

### 4. PlayerNormalAttackState

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
```

当前代码已经包含：

```text
Enter
├─ 重置计时器
├─ 消费 NormalAttack Buffer
├─ 设置 IsStateFinished = false
├─ 按 LockMovement 锁定移动
├─ 请求重播 SkillNormalAttack 动画
└─ 按 NormalAttackDuration 启动 DurationTimer

Tick
└─ 计时结束后设置 IsStateFinished = true

Exit
├─ 重置计时器
├─ 清除完成标记
└─ 解除移动锁
```

该状态尚未在 `PlayerController` 中完成配置注入和注册，也未发现 `PlayerSkillTransitionRules`、`PlayerSkillAnimRules` 的实际实现。

### 5. Config 目录归属调整

本次已经将非 HFSM 配置按职责迁移：

```text
PlayerInputConfigSO
└─ Assets/Scripts/Module/Player/Input/Config/

PlayerViewConfigSO
└─ Assets/Scripts/Module/Player/Core/View/Config/
```

HFSM/Config 当前只保留与状态行为直接相关的 Move、Air、Action、Skill 和 Common 配置。

## 四、关键架构边界

### 1. AnimationClip 是编辑期来源，Config 是运行时来源

```text
AnimationClip.length
└─ 只负责同步基础时长

PlayerStateClipData.StateDuration
└─ 是运行时读取的数据
```

后续即使需要人为缩短后摇、增加停顿或做动画速度倍率，也应明确区分“原始 Clip 时长”和“玩法状态时长”，不要在运行时临时遍历 Animator 猜测状态配置。

### 2. 数组元素表示动画段落，不等于技能事件时间轴

当前 `PlayerStateClipData[]` 可以表示多段动画，但每个元素只有 Clip 和总时长。它暂时不能表达：

```text
前摇结束时间
命中有效窗口
后摇开始时间
取消窗口
位移事件
```

因此明显前摇和后摇目前只能体现在动画资源及总状态时长中。等普攻第一段纵切跑通后，再按真实需求增加 Step/Event 数据，不提前扩张通用基类。

### 3. 普攻锁移动与禁止取消尚未完全收口

`PlayerNormalAttackConfigSO` 当前仍保留可配置的 `LockMovementValue`，默认值为 `true`。但用户已经确认普攻必须锁移动，后续需要决定是否删除该开关，或至少保证资产始终配置为 `true`。

“不允许取消”必须由 Transition Rules 和状态优先级共同保证，仅靠 `PlayerNormalAttackState` 自身计时不能阻止其他高优先级规则切走。

## 五、资源与配置状态

当前已发现的配置资产：

```text
Assets/Config/Player/Move/PlayerMoveConfig.asset
Assets/Config/Player/Air/PlayerAirConfig.asset
Assets/Config/Player/Action/PlayerDodgeConfig.asset
Assets/Config/Player/View/PlayerViewConfig.asset
```

当前未发现：

```text
PlayerNormalAttackConfig.asset
```

现有 Move、Air、Dodge 资产尚未序列化 `StateClipValues`。`PlayerDodgeConfig.asset` 中仍保留旧字段 `DodgeDurationValue: 0.32`，需要 Unity 完成脚本重载后，在 Inspector 中配置新数组并保存资产；旧字段届时会被 Unity 清理。

本次没有操作 Unity Editor，也没有完成 Unity 编译和 Inspector 按钮实机验证。

## 六、当前尚未完成

```text
Unity 编译验证
创建 PlayerNormalAttackConfig.asset
为 Move / Air / Dodge / NormalAttack 配置 StateClipValues
把各 FBX AnimationClip 拖入对应数组元素
点击同步按钮并保存资产
验证同步后的 StateDurationValue 与 Clip.length 一致
为数组索引建立稳定语义，避免后续顺序误配
实现并注册 PlayerSkillTransitionRules
实现 PlayerSkillAnimRules 和 normalAttack01 动画映射
在 PlayerController 中注入 NormalAttack Config 并注册状态
实现真正禁止取消的转换规则
决定前摇、有效帧、后摇采用 Step 字段还是 Event 数据
Play Mode 验证普攻锁移动、完整播放和结束回地面状态
```

## 七、下一步建议

下一步先完成配置资产验证，不继续扩数据结构：

```text
1. 打开 Unity，等待脚本编译完成
2. 检查四类 Config Inspector 是否都出现 State Clip Values 和同步按钮
3. 为 Move、Air、Dodge 配置现有动画 Clip
4. 创建 PlayerNormalAttackConfig.asset 并拖入 normalAttack01 Clip
5. 点击同步按钮，保存后检查资产中的 StateDurationValue
6. 确认没有编译错误后，再继续 PlayerController 注册与 Transition Rules
```

在普攻 Transition Rules 落地时，应先把“持续时间内绝不允许移动和取消”做成完整闭环，再讨论连段、命中帧和取消窗口。

## 八、协作偏好记录

用户继续采用“古法编程”节奏：

```text
先解释当前一步为什么这样设计
再由用户逐步编写或明确要求 AI 修改
不要因为未来有连段就一次铺开完整 SkillData / Timeline / Event 系统
命名需要直接表达职责，通用基类只收真正通用的动画段落字段
```

## 九、工作区注意事项

当前工作区包含用户已有修改和迁移，后续不得回退：

```text
PlayerController.cs
PlayerMotor.cs
PlayerViewController.cs
PlayerInputReader.cs
PlayerNormalAttackState.cs
Input Config 与 View Config 的目录迁移
```

另外仍有与本次无关的旧 `.meta` 删除和 SceneBackups 文件。`PlayerStateCommonConfigSO.cs` 在 Git 状态中还表现为旧路径 `AD`、新 Common 路径未跟踪，这是文件移动与暂存状态造成的显示，提交前需要仔细核对，不要直接重置工作区。
