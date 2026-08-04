# Protocol_Evac Player 技能配置统一与普攻数据迁移记录

## 一、记录范围

本记录接续：

[3-通用CombatHitbox开发前交接记录.md](../2026-8-3/3-通用CombatHitbox开发前交接记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录保存 Player 技能配置的最终命名、三段普攻从状态动画数据迁移到统一 Step 数据的实际结果、当前能使用的功能、尚未接通的 CombatHitbox 运行时链路，以及技能编辑器的下一步边界。

```text
1. 通用技能配置正式命名为 PlayerSkillConfigSO，不使用 Base 前缀
2. 单段技能数据正式命名为 PlayerSkillStepData
3. PlayerNormalAttackConfigSO 改为继承 PlayerSkillConfigSO
4. 普攻资产已从 StateClipValues 完整迁移为唯一的 StepValues
5. 第一版不采用 SerializeReference、多态 Event Data 或大量派生 Data 类
6. PlayerSkillConfigSO 只作为通用父类型，不提供裸配置资产创建菜单
7. PlayerSkillController 当前仍是空壳，HitWindow 与 Damage 尚未接入 CombatHitbox
```

## 二、本次确认的设计结论

### 1. PlayerSkillConfigSO 是通用配置父类型

当前正式继承关系：

```text
PlayerSkillConfigSO
└─ StepValues : PlayerSkillStepData[]

PlayerNormalAttackConfigSO : PlayerSkillConfigSO
├─ NormalAttackBufferTime
├─ LockMovement
└─ NormalAttackExitBlendDuration
```

`PlayerSkillConfigSO` 的职责是提供所有 Player 技能可复用的段落容器和读取 API，不承载普攻输入、退出混合或其他具体技能规则。

当前类不是抽象类，原因是项目规范要求抽象类使用 `Base` 前缀，而用户已经明确确认该类型命名为：

```text
PlayerSkillConfigSO
```

当前 `PlayerSkillConfigSO` 已移除 `[CreateAssetMenu]`。因此它不会在 Unity 创建菜单中生成没有运行时消费者的裸 `PlayerSkillConfig.asset`；项目中也已确认不存在这类通用资产。

具体可创建资产继续由具体配置类型提供菜单，例如：

```text
PlayerNormalAttackConfigSO
```

### 2. 技能段落只有一份权威数据

普攻不再继承：

```text
PlayerStateCommonConfigSO
└─ PlayerStateClipData[]
```

当前唯一技能段落数据为：

```text
PlayerSkillConfigSO
└─ PlayerSkillStepData[]
```

禁止后续重新为同一个技能并存：

```text
StateClipValues
StepValues
单独伤害数组
单独 Root Motion 索引数组
单独命中窗口数组
```

动画、持续时间、Root Motion、推进窗口、命中窗口和伤害必须以同一个 Step 为索引单位保存。

### 3. 第一版不建立多态事件数据体系

本轮已经否定以下中间方案：

```text
BasePlayerSkillEventData
PlayerSkillHitboxEventData
PlayerSkillComboWindowEventData
[SerializeReference] List<BasePlayerSkillEventData>
每种事件建立一个派生 Data 类
```

原因是当前只有固定的一组段落窗口，提前引入多态事件会增加序列化、编辑器和运行时调度复杂度，并让技能编辑器反向驱动运行时架构。

第一版直接由 `PlayerSkillStepData` 保存固定字段。只有真实出现一段内多个同类窗口、任意数量事件或复杂分支时，才重新评估事件列表。

### 4. StepAdvanceWindow 使用通用语义

通用 Step 不使用 `ComboWindow` 命名，而使用：

```text
StepAdvanceWindow
```

它表示“何时允许推进下一段”。普攻通过：

```text
PlayerNormalAttackConfigSO.TryGetComboWindow()
```

将该窗口解释为连段输入窗口。SpecialSkill 或 Ultimate 后续可以复用 Step 推进能力，而不被迫继承普攻 Combo 语义。

### 5. Event 目录只放真正的事件协议

当前目录保留：

```text
Assets/Scripts/Module/Player/Skill/Event/
```

该目录当前为空。以后只有真正出现技能事件协议、事件类型或运行时事件契约时才向其中添加代码，不把配置 Data 放入 `Event/`，也不为占位目的创建空事件类。

## 三、当前实现状态

### 1. Skill 目录

当前实际结构：

```text
Assets/Scripts/Module/Player/Skill/
├─ PlayerSkillType.cs
├─ Data/
│  ├─ PlayerSkillConfigSO.cs
│  ├─ PlayerSkillStepData.cs
│  └─ PlayerNormalAttackConfigSO.cs
├─ Core/
│  └─ PlayerSkillController.cs
├─ Event/
└─ Editor/
   └─ PlayerSkillConfigSOEditor.cs
```

旧目录已经移除：

```text
Assets/Scripts/Module/Player/HFSM/Config/Skill/
```

`PlayerNormalAttackConfigSO.cs.meta` 的 GUID 在移动时保持：

```text
3e0df1ecc7fe4bc79183bd3e385e2664
```

因此原有 `PlayerNormalAttackConfig.asset` 和场景中的 Inspector 引用没有断裂，不需要重新拖拽配置。

### 2. PlayerSkillConfigSO 当前 API

```text
Steps
StepCount
GetStep(index)
GetStepDuration(index)
SyncAllStepDurations()
```

`PlayerSkillConfigSOEditor` 继续提供：

```text
同步全部动画时长
```

该 Inspector 同时作用于 `PlayerSkillConfigSO` 的具体派生配置，因此迁移后没有丢失原来的动画时长同步能力。

### 3. PlayerSkillStepData 当前字段

```text
AnimationClip
Duration
UseRootMotion
UseStepAdvanceWindow
StepAdvanceOpenNormalizedTime
StepAdvanceCloseNormalizedTime
UseHitWindow
HitOpenNormalizedTime
HitCloseNormalizedTime
Damage
```

当前读取 API：

```text
SyncDurationFromClip()
TryGetStepAdvanceWindow()
TryGetHitWindow()
```

窗口读取时会将时间限制在 `0~1`，并保证结束时间不早于开始时间。

### 4. 普攻运行时代码已经读取 Step

以下代码已经改为引用 `Module.Player.Skill.Data`：

```text
PlayerController
PlayerNormalAttackState
PlayerSkillTransitionRules
PlayerTransitionController
```

`PlayerNormalAttackState` 当前使用：

```text
StepCount
GetStepDuration()
GetStep().UseRootMotion
TryGetComboWindow()
```

普攻已经不再读取 `PlayerStateClipData`、`StateClipCount`、`GetStateDuration()` 或 Root Motion 索引数组。

### 5. PlayerSkillController 仍是空壳

当前文件：

```text
Assets/Scripts/Module/Player/Skill/Core/PlayerSkillController.cs
```

只有空类定义，尚未承担：

```text
技能段落推进
HitWindow 边界检测
CombatHitbox.Open / Close 调度
技能结束与中断清理
SpecialSkill / Ultimate 调度
```

后续不要因为类已经存在就假设 Skill Controller 已经接入运行时。

## 四、普攻资产迁移结果

当前资产：

```text
Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
```

只包含一个 `StepValues` 权威数组，共三段：

```text
Step 0 / Attack01
├─ Duration: 1.2666668
├─ UseRootMotion: false
└─ StepAdvanceWindow: 0.2 ~ 0.75

Step 1 / Attack02
├─ Duration: 1.8333334
├─ UseRootMotion: false
└─ StepAdvanceWindow: 0.2 ~ 0.75

Step 2 / Attack03
├─ Duration: 1.8000001
├─ UseRootMotion: true
└─ StepAdvanceWindow: disabled
```

三段当前统一保持：

```text
UseHitWindow: false
Damage: 0
```

这是有意的迁移默认状态。旧数据没有正式伤害值和命中窗口，迁移时没有擅自创造设计参数。

动画 GUID 保持：

```text
Attack01: 4ef9dee05d899e74e875ca517e55b350
Attack02: ab00617f0cb353d4e95c4cf6c5ba57fa
Attack03: f291699eed259524ebeb67f9fc64c8cd
```

## 五、CombatHitbox 与 Skill 的当前边界

通用 `CombatHitbox` 当前已经存在：

```text
Assets/Scripts/Module/Combat/Hitbox/CombatHitbox.cs
```

它负责：

```text
OverlapBoxNonAlloc 检测
Target Layer 过滤
排除伤害来源自身
同一窗口 IDamageable 去重
创建 DamageData
调用 IDamageable.TakeDamage()
```

当前检测调用已经显式传入：

```text
TargetLayers.value
```

避免在 `OverlapBoxNonAlloc` 调用点依赖 `LayerMask -> int` 的隐式转换。

Skill 与 Hitbox 尚未接通。`PlayerSkillStepData` 中的 `HitWindow` 和 `Damage` 目前只是可序列化设计数据，填写后不会自动造成伤害。

继续保持依赖方向：

```text
Combat
   ↑
Player Skill
```

禁止让 `CombatHitbox` 反向读取：

```text
PlayerContext
PlayerNormalAttackState
PlayerSkillStepData
NormalAttackIndex
动画归一化时间
```

应由 Player Skill 读取当前 Step，并在窗口边界调用 `CombatHitbox.Open(step.Damage, source)` 与 `Close()`。

## 六、设计文档状态

以下两份常驻设计文档已经同步到当前架构：

```text
设计文档/玩家状态与敌人AI/技能系统与编辑器设计方案.md
设计文档/玩家状态与敌人AI/玩家状态与敌人AI设计方案.md
```

已经移除或否定：

```text
PlayerSkillDataSO 命名
BasePlayerSkillEventData 体系
SerializeReference 多态事件列表
Hitbox / ComboWindow 各建一个派生 Data 类
Data 放在 Event 目录
```

当前文档统一使用：

```text
PlayerSkillConfigSO
PlayerSkillStepData
PlayerNormalAttackConfigSO : PlayerSkillConfigSO
固定 StepAdvanceWindow 与 HitWindow 字段
UI Toolkit 技能编辑器只编辑权威 Skill Config
```

## 七、验证状态

当前 Unity MCP Manager 无法连接，本地也没有可用的 `unity-mcp-cli`，因此本轮没有通过 MCP 调用刷新或 Console 工具。

Unity Editor 自身已经自动刷新并重新生成：

```text
Player.csproj
Library/ScriptAssemblies/Player.dll
```

归档前 `Player.dll` 的最后生成时间为：

```text
2026-08-04 13:04:34 +08:00
```

该时间晚于最后一次 `PlayerSkillConfigSO` 修改。读取 `Editor.log` 尾部未发现：

```text
error CS
Compilation failed
```

因此当前 C# 已由 Unity 完成实际编译，但以下内容仍未验证：

```text
PlayerNormalAttackConfig.asset 的 Inspector 视觉检查
HitWindow 实际开关行为
CombatHitbox 与 CombatTargetDummy 的 Play Mode 命中
三段普攻完整命中与去重
技能中断时是否可靠关闭 Hitbox
```

## 八、当前尚未完成

```text
PlayerSkillController 尚未实现
三段普攻尚未填写正式 Damage
三段普攻尚未填写正式 HitWindow
Player Skill 尚未持有或定位 CombatHitbox
HitWindow 尚未桥接 CombatHitbox.Open / Close
状态 Exit / 中断尚未统一关闭 Hitbox
尚未完成 CombatTargetDummy 单段命中验证
SpecialSkill 与 Ultimate 尚无具体 ConfigSO 和运行时状态
UI Toolkit 技能编辑器尚未创建
PlayerStateClipData 中旧 Combo 字段已无 Skill 消费者，尚未单独评估清理
```

## 九、下一步建议

下一步先完成运行时最小闭环，不立即开始完整时间轴编辑器：

```text
1. 在 PlayerNormalAttackConfig.asset 中为第一段填写正式 Damage 与 HitWindow
2. 明确 Player 侧如何持有 CombatHitbox，避免 Combat 反向依赖 Player
3. 实现一次 HitWindow 开始与结束边界检测
4. 窗口开始调用 CombatHitbox.Open(step.Damage, playerGameObject)
5. 窗口结束、状态 Exit 和中断时调用 CombatHitbox.Close()
6. 用一个 CombatTargetDummy 验证同一窗口只造成一次伤害
7. 单段闭环稳定后再补第二、三段
8. 运行时数据契约稳定后，再创建 UI Toolkit 技能编辑器
```

技能编辑器第一版应直接编辑：

```text
PlayerSkillConfigSO
└─ PlayerSkillStepData[]
```

不要创建一套编辑器专属 Skill Data，也不要让时间轴工具反过来决定运行时类型层次。

## 十、工作区注意事项

创建本归档前，`git status --short` 中只剩以下与技能迁移无关的状态：

```text
D Assets/Lua.meta
D Assets/Scripts/Common.meta
D Assets/Scripts/Framework/QLua.meta
D Assets/Scripts/Net.meta
D Assets/Scripts/Tools/GM.meta
D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639214339907833159.backup
```

技能迁移已包含在当前 Git 历史中，最近相关提交为：

```text
1dd91ab 重构玩家技能配置类 计划写技能编辑器中
07fed06 同上
```

后续不要恢复或清理上述无关 `.meta` 删除和场景备份。本归档文件本身会作为新的未跟踪文档出现，属于本次归档的预期结果。
