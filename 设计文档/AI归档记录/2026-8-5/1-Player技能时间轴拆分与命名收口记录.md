# Protocol_Evac Player 技能时间轴拆分与命名收口记录

## 一、记录范围

本记录接续：

[2026-8-4/2-Player技能总控装配边界与协作纠偏记录.md](../2026-8-4/2-Player技能总控装配边界与协作纠偏记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录保存 `PlayerSkillController` 当前的实际落盘状态、`PlayerSkillTimeline` 从控制器内拆为独立类文件的结果、命名收口过程中的最终边界，以及下一步继续接 `PlayerNormalAttackState` 与 `CombatHitbox` 前需要注意的事项。

```text
1. PlayerSkillController 保持为技能门面，只做配置注册与对外转发
2. PlayerSkillTimeline 必须是独立新类，不内嵌在 PlayerSkillController 中
3. Register 命名收口为 RegisterConfig，避免语义过宽
4. PlayerController 仍作为单个 Player 的唯一组合根
5. 当前阶段先稳定纯时间轴，再继续接普攻状态与命中盒
```

## 二、本次确认的设计 / 协作偏好

### 1. 时间轴必须独立成新类文件

用户已明确纠正：`PlayerSkillTimeline` 不能放在 `PlayerSkillController` 类内部，也不能只是同文件同层的辅助块，而必须作为一个新的独立类文件存在。

当前结构已经收口为：

```text
Assets/Scripts/Module/Player/Skill/Core/PlayerSkillController.cs
└─ 只保留门面接口与配置转发

Assets/Scripts/Module/Player/Skill/Core/PlayerSkillTimeline.cs
└─ 独立承载技能时间轴状态与段落推进
```

### 2. 控制器命名继续朝“门面 + 时间轴”收缩

当前用户反馈的核心问题不是“有没有技能控制器”，而是“类太胖、函数命名太口语化”。

本轮已经按这个方向收敛为：

```text
PlayerSkillController
└─ RegisterConfig / Open / Tick / Close / RequestStepAdvance

PlayerSkillTimeline
└─ 真正的时间轴状态、段落推进、结束态
```

后续若再拆，优先继续拆时间轴内部状态，而不是把更多运行逻辑塞回门面类。

### 3. `PlayerController` 保持薄组合根

用户再次确认 `PlayerController` 不要臃肿。

当前它只做：

```text
创建 PlayerContext
初始化 Input / Skill / HFSM / Anim
每帧调度 Tick
```

不要把技能段落、命中窗口、伤害或 CombatHitbox 逻辑回塞进 `PlayerController`。

## 三、当前实现状态

### 1. PlayerController 当前接入点

当前实际文件：

```text
Assets/Scripts/Module/Player/Core/PlayerController.cs
```

已存在的最小接入：

```text
initCore()
initSkill()
initHFSM()
initAnim()
```

`Update()` 中已调用：

```text
m_skillController.Tick(Time.deltaTime)
```

`initSkill()` 当前注册：

```text
PlayerSkillType.NormalAttack -> NormalAttackConfig
```

### 2. PlayerSkillController 当前 API

当前实际文件：

```text
Assets/Scripts/Module/Player/Skill/Core/PlayerSkillController.cs
```

对外接口已经收口为：

```text
RegisterConfig(PlayerSkillType skillType, PlayerSkillConfigSO config)
Open(PlayerSkillType skillType)
Tick(float deltaTime)
Close()
RequestStepAdvance()
```

它内部不再承载时间轴实现，只通过 `PlayerSkillTimeline` 暴露当前技能、当前段、归一化时间与完成状态。

### 3. PlayerSkillTimeline 当前职责

当前实际文件：

```text
Assets/Scripts/Module/Player/Skill/Core/PlayerSkillTimeline.cs
```

当前职责为：

```text
当前技能类型
当前段索引
当前段计时
段落推进请求缓存
StepAdvanceWindow 识别
结束态写回 PlayerContext
Root Motion 开关写回 PlayerContext
```

当前时间轴仍是纯运行骨架，尚未接 `CombatHitbox`。

## 四、关键架构边界

### 1. 门面与时间轴分层

```text
PlayerSkillController
├─ 持有技能配置字典
├─ 暴露对外 API
└─ 转发给 PlayerSkillTimeline

PlayerSkillTimeline
├─ 持有 DurationTimer
├─ 维护当前段与推进缓存
├─ 负责段落切换与结束态
└─ 直接写回 PlayerContext 的运行时标记
```

### 2. QF 仍不接 PlayerSkillController

当前确认不把 `PlayerSkillController` 或 `PlayerSkillTimeline` 注册成 QF `System`。

原因仍然是：

```text
它们是单个 Player 实例的高频本地对象
生命周期跟随 Player GameObject
不适合提升到全局架构容器
```

### 3. 普攻状态仍是外层生命周期，不是技能细节容器

`PlayerNormalAttackState` 后续只应负责：

```text
Enter -> Open(NormalAttack)
Tick  -> 必要时 RequestStepAdvance
Exit  -> Close
```

不要把旧的 Timer、连段索引推进、窗口判断继续保留在 State 里。

## 五、当前需要注意的问题

```text
PlayerSkillTimeline 仍未接 CombatHitbox
PlayerSkillController 仍只是门面，不负责伤害窗口
Open / Close 的语义已经稳定，不要再换名
当前实现尚未跑 Unity 编译验证
```

本轮本地环境里没有可用的 `.NET SDK` / `MSBuild`，所以只能做文本与引用检查，没法在命令行直接完成 C# 编译确认。

## 六、当前尚未完成

```text
PlayerNormalAttackState 尚未迁移到 PlayerSkillController
RequestStepAdvance 的真实调用方尚未接好
CombatHitbox.Open / Close 尚未接入时间轴窗口
Damage 与 HitWindow 仍只是配置字段
第一段普攻的命中闭环尚未验证
```

## 七、下一步建议

下一步优先做一件事：把 `PlayerNormalAttackState` 的职责瘦下来。

```text
1. 让 PlayerNormalAttackState 只管进入、请求推进、退出
2. 把旧的普攻 Timer 与段落推进从 State 中迁走
3. 让 State 只依赖 PlayerSkillController / PlayerSkillTimeline 的结果
4. 再把 CombatHitbox 的开关接到时间轴窗口边界
5. 最后做第一段普攻的 Play Mode 验证
```

## 八、工作区注意事项

当前工作区仍存在与本次技能归档无关的状态：

```text
D Assets/Lua.meta
D Assets/Scripts/Common.meta
D Assets/Scripts/Framework/QLua.meta
D Assets/Scripts/Net.meta
D Assets/Scripts/Tools/GM.meta
D Assets/Scripts/UI.meta
?? Assets/Scripts/Module/Player/Skill/Core/PlayerSkillTimeline.cs.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639214339907833159.backup
```

不要擅自恢复或清理这些文件。当前新增的归档文件属于本次预期结果，不是异常改动。
