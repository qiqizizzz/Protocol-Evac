# Protocol_Evac Player Skill 系统命名与编辑器路线记录

## 一、记录范围

本记录接续：

[../2026-7-29/1-PlayerAbility与Skill命名收口记录.md](../2026-7-29/1-PlayerAbility与Skill命名收口记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

新增设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本次主要围绕 Player 侧 Skill 系统命名、HFSM 分支边界、普攻最小纵切前置代码，以及后续技能编辑器路线做了收口：

```text
1. 确认系统层统一使用 Skill，不再使用 Ability 作为玩家技能系统命名
2. 确认 Dodge 不进入 Skill，继续作为 ActionDodge 留在 Action 分支
3. 确认普攻、特殊技能、大招进入 Skill 分支
4. 确认 NormalAttack 动画路径使用 Base Layer.Skill.normalAttack01
5. 确认第一版编辑器路线为 NaughtyAttributes + ScriptableObject 列表，后续再做自研 Timeline EditorWindow
6. 确认协作节奏：先设计和确认，再按用户明确范围逐步写代码，避免一次性铺开目录与空壳
```

## 二、本次确认的设计

### 1. Action 与 Skill 分支边界

当前 Skill 口径已经从上一份归档的“Ability 语义收窄”继续推进为更明确的状态树边界：

```text
Action
└─ Dodge / Interact / UseItem 等通用动作

Skill
├─ NormalAttack
├─ SpecialSkill
└─ Ultimate
```

本次用户明确倾向：

```text
PlayerActionState
└─ 闪避等通用动作

PlayerSkillState
└─ 普攻、E 技能、R 大招等技能行为
```

因此后续不要再把普攻做成 `PlayerAttackState`，也不要把 `PlayerSkillState` 塞在 Action 文件夹里。

### 2. 命名收口

本次确认的命名规则：

```text
PlayerSkillType
└─ 技能类型：NormalAttack / SpecialSkill / Ultimate

PlayerStateId
└─ HFSM 状态 ID：Skill / SkillNormalAttack / SkillSpecial / SkillUltimate

PlayerBufferedInputType
└─ 输入缓存语义：NormalAttack / SpecialSkill / Ultimate / Dodge / Jump
```

不再提前创建：

```text
PlayerSkillId
PlayerSkillPhase
PlayerSkillEventType
PlayerSkillSlot
```

原因：

```text
PlayerSkillId
└─ 目前还没有具体技能资源和配置表，提前建容易和 Type 重复

PlayerSkillPhase
└─ 当前普攻纵切仍由 State + DurationTimer 推进，暂不需要 Cast / Active / Recovery

PlayerSkillEventType
└─ 事件数据尚未落地，等 Hitbox / Motion / CancelWindow 等真实事件出现后再定

PlayerSkillSlot
└─ Slot 会混淆键位、技能类型和装备槽位，除非后续真的有技能槽数据结构，否则不使用
```

### 3. 技能编辑器路线

本次新增了技能系统与编辑器设计文档，当前路线：

```text
第一阶段
└─ NaughtyAttributes + ScriptableObject 列表

第二阶段
└─ 自研轻量 Skill Timeline EditorWindow

第三阶段
└─ 如出现技能分支、条件连段、多路径释放，再评估 xNode / Graph Toolkit 等节点图方案
```

关键边界：

```text
运行时不依赖 Odin / NaughtyAttributes / EditorWindow
编辑器工具只负责编辑 Skill Data
战斗逻辑只读取数据，不引用 UnityEditor 命名空间
```

本次明确不建议直接拷贝其他项目里的 `Assets/Plugins/Sirenix`。Odin 是商业插件；如果使用 Odin，应通过正版 Personal / Educational 等授权导入。当前免费路线优先考虑 NaughtyAttributes。

## 三、当前实现状态

### 1. PlayerStateId

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/PlayerStateId.cs
```

当前已包含 Skill 分支：

```text
Action
└─ ActionDodge

Skill
├─ SkillNormalAttack
├─ SkillSpecial
└─ SkillUltimate
```

旧的：

```text
ActionAttack
ActionSkill
```

已经不再保留。

### 2. PlayerBufferedInputType

当前文件：

```text
Assets/Scripts/Module/Player/Input/Buffer/PlayerBufferedInputType.cs
```

当前输入缓存类型：

```text
Jump
NormalAttack
SpecialSkill
Ultimate
Dodge
```

后续输入读取侧需要把真实按键映射到这些语义输入，不应在 Skill 系统内部使用 E / R 作为类型名。

### 3. PlayerSkillType

当前文件：

```text
Assets/Scripts/Module/Player/Skill/PlayerSkillType.cs
```

当前仅保留类型枚举：

```text
NormalAttack
SpecialSkill
Ultimate
```

这是目前唯一 Skill 系统枚举。后续新增枚举前，应先确认它是否和 `PlayerSkillType`、`PlayerStateId` 或 `PlayerBufferedInputType` 重复。

### 4. PlayerNormalAttackConfigSO

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Config/Skill/PlayerNormalAttackConfigSO.cs
```

当前字段：

```text
NormalAttackDuration
NormalAttackBufferTime
LockMovement
```

该配置只服务第一版普攻状态纵切，不承载伤害、连段、取消窗口或事件数据。

### 5. Skill 状态壳

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerSkillState.cs
Assets/Scripts/Module/Player/HFSM/States/Skill/PlayerNormalAttackState.cs
```

当前状态：

```text
PlayerSkillState
└─ 已作为复合状态，默认子状态返回 SkillNormalAttack

PlayerNormalAttackState
└─ 当前仍是空叶子状态，只声明 Id / ParentId
```

尚未接入：

```text
Enter 消费 NormalAttack Buffer
DurationTimer
移动锁定
动画重播请求
Transition Rules
Animation Rules
```

### 6. 状态完成标记

当前文件：

```text
Assets/Scripts/Module/Player/Context/PlayerContext.cs
```

已将旧的：

```text
IsActionFinished
```

统一改为：

```text
IsStateFinished
```

原因是该标记将同时服务 `ActionDodge` 和后续 `SkillNormalAttack`，不应继续带 Action 语义。

### 7. TransitionPriority 命名

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Transition/PlayerTransitionPriority.cs
```

已将旧的：

```text
Ability = 200
```

改为：

```text
Action = 200
```

当前只是清除旧 Ability 命名；后续是否需要单独 `Skill` 优先级，等 `PlayerSkillTransitionRules` 接入时再决定，不提前铺。

## 四、关键架构边界

### 1. 当前阶段不做完整 SkillData / Event / Core

尽管设计文档中已经记录未来方向：

```text
Skill/Data
Skill/Event
Skill/Core
```

但本次实际代码没有创建完整数据层和 Runner。用户已明确要求不要一次性铺代码，后续必须按确认范围逐步实现。

### 2. 普攻先用 HFSM Config 跑纵切

第一版普攻建议继续使用：

```text
PlayerNormalAttackConfigSO
DurationTimer
PlayerNormalAttackState
```

只验证：

```text
输入能进入 SkillNormalAttack
能锁移动
能播放 Base Layer.Skill.normalAttack01
能按时长结束
能回到 GroundedIdle / GroundedMove / AirborneFall
```

不要在第一版加入：

```text
伤害
连段
取消窗口
SkillDataSO
Timeline Editor
```

### 3. 编辑器插件不进入运行时依赖

即使后续使用 NaughtyAttributes，也只应用在 Inspector 体验上。运行时核心不能依赖编辑器插件。

## 五、当前尚未完成

```text
PlayerNormalAttackState.Enter / Tick / Exit 具体实现
PlayerSkillTransitionRules
PlayerSkillAnimRules
PlayerController 注册 Skill 状态与规则
PlayerAnimWriter 命中 Base Layer.Skill.normalAttack01
InputReader 将普攻按键写入 PlayerBufferedInputType.NormalAttack
Unity 编译验证
Play Mode 验证普攻进入、播放和退出
SpecialSkill / Ultimate 动画路径命名
普攻是否允许移动输入影响朝向
普攻期间是否允许 Dodge 取消
Hitbox 第一版验证方式
```

## 六、下一步建议

下一步建议只做 `PlayerNormalAttackState` 最小纵切，不要进入 SkillData 和编辑器：

```text
1. 在 PlayerNormalAttackState 中注入 PlayerContext 和 PlayerNormalAttackConfigSO
2. Enter 消费 NormalAttack Buffer
3. Enter 设置 IsStateFinished = false
4. 按 LockMovement 设置 IsMovementLocked
5. 请求 RequestAnimReplay(PlayerStateId.SkillNormalAttack)
6. 用 DurationTimer 按 NormalAttackDuration 推进
7. Tick 到时后设置 IsStateFinished = true
8. Exit 重置 Timer、IsStateFinished 和移动锁
```

然后再接：

```text
PlayerSkillTransitionRules
└─ Grounded -> SkillNormalAttack
└─ SkillNormalAttack -> AirborneFall / GroundedMove / GroundedIdle

PlayerSkillAnimRules
└─ SkillNormalAttack 动画参数解析
```

## 七、协作偏好记录

本次用户明确纠正：

```text
不要在“看看下一步做什么”时直接铺完整代码
需要先设计、确认，再按用户明确范围逐步写
命名必须干净，避免 Slot、Ability、Runtime、Definition 等含义不准的词
如果只是 Type，就命名为 PlayerSkillType
State 本来就是 State，完成标记应命名为 IsStateFinished，不要搞特殊
```

后续 AI 应优先遵守该节，避免过度主动实现。

## 八、工作区注意事项

归档创建前执行：

```text
git status --short --untracked-files=all
```

当前可见工作区仍存在旧的无关改动：

```text
 D Assets/Lua.meta
 D Assets/Scripts/Common.meta
 D Assets/Scripts/Framework/QLua.meta
 D Assets/Scripts/Net.meta
 D Assets/Scripts/Tools/GM.meta
 D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639210032524788533.backup
```

这些不是本次 Skill 系统工作内容，后续提交前需要再次确认实际工作区状态。

