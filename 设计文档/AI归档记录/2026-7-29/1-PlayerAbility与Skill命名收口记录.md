# Protocol_Evac Player Ability 与 Skill 命名收口记录

## 一、记录范围

本记录接续：

[../2026-7-28/1-GameApp与Timer系统开发交接记录.md](../2026-7-28/1-GameApp与Timer系统开发交接记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要围绕 Player 侧能力层语义、HFSM 状态分层命名，以及后续多角色复用的边界做了收口讨论：

```text
1. 确认 Ability 不再按“所有动作能力”泛化，而是收窄为技能系统语义
2. 确认 Dodge、Hurt、Idle、Walk、Sprint 不纳入 Ability
3. 确认 Attack 可作为 Ability / Skill 的候选能力
4. 确认 HFSM 继续保留姿态与移动状态
5. 确认技能相关状态文件夹单独放入 States/Skill
6. 确认外层 Player/Ability 目录继续保留为能力系统层入口，占位并等待后续设计
```

本次没有进入代码实现，没有改动 Player 状态机、Ability 运行时、Animator 或输入链路。

## 二、本次确认的设计

### 1. Ability 语义收窄为技能系统

用户明确倾向把 `Ability` 定义为技能系统，而不是泛指所有动作能力。

当前确认边界：

```text
Ability
└─ 更适合表达攻击、法术、主动技能、蓄力类技能等可复用能力

不纳入 Ability
└─ Dodge、Hurt、Idle、Walk、Sprint 等不应强行塞入
```

原因很直接：

```text
Dodge
└─ 更像动作 / 位移状态，不是技能系统核心语义

Hurt
└─ 更像受击反应与控制状态，不是主动技能

Idle / Walk / Sprint
└─ 属于 locomotion，不应进入技能层
```

### 2. HFSM 继续保留大姿态与移动状态

当前约定没有改变：

```text
HFSM
└─ 继续负责玩家姿态、移动、动作、受击等宏观状态
```

也就是说，`Grounded / Airborne / Action / Disabled` 仍然保留，下面的 `Idle / Move / Sprint / Jump / Fall / Dodge / Hurt` 仍然主要属于状态机职责，不会被 Ability 全量接管。

### 3. Attack 更适合进入 Ability / Skill

用户认为 `Attack` 更适合放到 Ability / Skill 层，这个判断是合理的。

原因是：

```text
不同角色的攻击方式差异大
└─ 适合抽成共享能力系统

攻击通常有阶段和配置
└─ 前摇、持续、后摇、取消窗、资源消耗、动画映射

攻击是可复用行为
└─ 比单个 ActionState 更值得抽象
```

### 4. 状态文件夹使用 Skill，避免语义冲突

用户提出外层 `Module/Player/Ability` 目录已经存在，所以需要把状态层命名和系统层命名区分开。

当前偏好：

```text
Module/Player/Ability
└─ 技能系统层

Module/Player/HFSM/States/Skill
└─ 技能相关状态壳
```

不建议再在状态层使用 `Ability` 作为文件夹名，否则会出现“Ability 里再套 Ability”的语义重叠。

## 三、当前实现状态

### 1. 现有代码仍以 HFSM 为主

当前项目里，Player 侧仍是以 HFSM、Input Buffer、Transition Rules、Animator Writer 为主，能力层尚未真正落地。

当前可见状态：

```text
PlayerDodgeState
└─ 仍然是 HFSM Action 状态，不是独立 Ability Runtime

Player/Ability 目录
└─ 已存在目录占位，但尚未形成完整能力实现
```

### 2. 目录层级仍需要后续再收

当前这次只确认了命名方向，没有动到目录结构本身。

后续需要继续明确：

```text
1. Ability 是否继续放在 Module/Player 下，还是抽成更共享的 Module/Combat/Ability
2. Skill 状态层具体是 AttackSkill、CastSkill 还是统一 SkillState 壳
3. Action 与 Skill 在状态机里怎么分层最顺
```

## 四、关键架构边界

```text
HFSM
└─ 负责姿态、控制权、移动状态、动作状态

Ability / Skill
└─ 负责技能类行为的生命周期、配置与复用

States/Skill
└─ 放技能相关状态壳，不和系统层 Ability 重名

Idle / Walk / Sprint
└─ 继续留在移动层，不进入 Ability

Dodge / Hurt
└─ 继续留在状态或受击层，不进入技能系统
```

这次讨论的核心不是“要不要 Ability”，而是把 `Ability` 的词义收窄到更适合后续多角色复用的范围。

## 五、当前尚未完成

```text
Ability 系统具体文件结构
AbilityDefinitionSO / Runtime / Controller 的最终命名
Skill 状态层的具体类命名
Attack 与现有 Action 状态的边界整理
是否把 Ability 抽到 Player 之外做共享模块
```

## 六、下一步建议

下一次建议先把命名与目录边界定死，再开始写代码：

```text
1. 确认 Ability 作为技能系统的共享范围
2. 确认 Skill 状态文件夹下的第一批类名
3. 确认 Attack 是否从 Action 状态演进到 Skill 层
4. 再开始落 AbilityDefinitionSO、Runtime、Controller
```

这样可以避免后面一边写一边改名，越改越乱。

## 七、工作区注意事项

归档创建前执行：

```text
git status --short --untracked-files=all
```

当前工作区可见的无关改动仍然存在：

```text
 D Assets/Lua.meta
 D Assets/Scripts/Common.meta
 D Assets/Scripts/Framework/QLua.meta
 D Assets/Scripts/Net.meta
 D Assets/Scripts/Tools/GM.meta
 D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639196445354525317.backup
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639201340524186779.backup
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639209411834547579.backup
```

本次归档仅新增文档记录，没有改动 C# 代码。

