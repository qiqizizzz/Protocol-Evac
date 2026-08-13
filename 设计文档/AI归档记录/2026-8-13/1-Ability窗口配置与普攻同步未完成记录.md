# Protocol_Evac Ability 窗口配置与普攻同步未完成记录

## 一、记录范围

本记录接续：

[1-Player急停脚步事件与Grounded混合树交接记录.md](../2026-8-9/1-Player急停脚步事件与Grounded混合树交接记录.md)

主设计文档：

[../../工具与编辑器/AbilityComposer设计方案.md](../../工具与编辑器/AbilityComposer设计方案.md)

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录保存本轮 Ability Composer 窗口轨道、普攻配置 Inspector 与运行时命中窗口同步工作的现状。任务尚未完成，下一次开发应从“统一数据源与运行时读取”继续。

## 二、本次确认的用户要求

用户明确指出窗口配置并非不存在，真实配置位于：

```text
Assets/Config/Player/Skill/Window/
├─ attack01_begin_WindowTrack.asset
├─ attack02_begin_WindowTrack.asset
└─ attack03_begin_WindowTrack.asset
```

用户希望三个攻击段共用一个 `PlayerNormalAttackConfigSO` 时，不在 Inspector 中显示三份重复的独立命中窗口数据；切换攻击动画后，窗口内容应对应切换，未配置时隐藏内容但保留窗口页签。

## 三、当前实现状态

### 1. Ability Composer

已具备以下功能（以当前代码为准）：

```text
AnimationClip 切换
窗口轨道按 AnimationClip 匹配
事件 / 窗口页签
窗口拖拽与帧编辑
事件保存、窗口保存、一键保存、撤销上次保存
无配置时隐藏窗口检查器内容，但保留页签
```

当前工作区另有一项尚未验证的分辨率适配改动：

```text
Assets/Scripts/Tools/Editor/AbilityComposer/AbilityComposerView.cs
Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uss/AbilityComposerWindow.uss
```

这两处改动只涉及响应式布局，未完成 Unity Editor 验证；不要把它们与窗口数据同步任务混为已完成内容。

### 2. 窗口资产真实内容

已读取到以下数据：

```text
attack01_begin_WindowTrack
└─ 命中窗口 0.1579 - 0.2631，伤害 1

attack02_begin_WindowTrack
├─ 命中窗口 0.1363 - 0.2045，伤害 1
├─ 命中窗口 0.2727 - 0.3181，伤害 1
└─ 命中窗口 0.3409 - 0.3863，伤害 20

attack03_begin_WindowTrack
├─ 命中窗口 0.1017 - 0.1864，伤害 20
└─ 命中窗口 0.4407 - 0.5424，伤害 30
```

`attack02` 和 `attack03` 已证明命中窗口不是单一的开始/结束区间，不能用一个 `HitOpen / HitClose / Damage` 三字段替代。

### 3. 普攻总配置当前状态

目标资产：

```text
Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
```

该资产仍包含三个 `PlayerSkillStepData`，并且每段仍保存旧的：

```text
UseHitWindowValue
HitOpenNormalizedTimeValue
HitCloseNormalizedTimeValue
DamageValue
```

当前旧数据与窗口轨道不一致：

```text
attack01: 0.02 - 0.18，伤害 10
attack02: 0.2 - 0.35，伤害 15
attack03: 未启用，伤害 0
```

## 四、已确认的架构问题

当前存在两套命中窗口数据源：

```text
AbilityWindowTrackSO
└─ Assets/Config/Player/Skill/Window/*.asset

PlayerSkillStepData
└─ UseHitWindow / HitOpen / HitClose / Damage
```

Ability Composer 保存的是 `AbilityWindowTrackSO`，但运行时 `PlayerSkillController.SyncHitWindow()` 仍调用：

```text
PlayerSkillStepData.TryGetHitWindow()
```

因此 Composer 中已配置的多窗口数据不会自动驱动运行时；Inspector 看到的旧字段也不会随动画切换显示对应的窗口轨道。这是本次任务未完成的根因，不是窗口资产缺失。

## 五、推荐的统一方案

保留 `PlayerNormalAttackConfigSO` 作为普攻段落总配置，保留三个已有 `AbilityWindowTrackSO` 作为唯一窗口数据源：

```text
PlayerNormalAttackConfigSO.StepValues[i].BeginAnimationClip
    -> 按 AnimationClip 精确匹配 AbilityWindowTrackSO.AnimationClip
    -> Inspector 显示该轨道的全部窗口
    -> Ability Composer 编辑同一份轨道资产
    -> 运行时读取同一份轨道资产
```

具体边界：

```text
PlayerSkillStepData
├─ 保留动画、持续时间、Root Motion、推进窗口等段落配置
└─ 旧命中字段不再作为权威数据

AbilityWindowTrackSO
└─ 保存 Hit / Invincible 等全部时间窗口及类型参数
```

匹配不到轨道时，Inspector 不显示旧命中窗口内容，只显示“当前动画未配置窗口”；除非用户在 Composer 点击“保存”，否则不自动创建资产。

## 六、当前尚未完成

```text
1. 未给 PlayerNormalAttackConfigSO 增加按 BeginAnimationClip 查找并展示 AbilityWindowTrackSO 的 Inspector 逻辑
2. 未将 PlayerSkillController.SyncHitWindow() 从旧 TryGetHitWindow() 改为读取窗口轨道
3. 未定义运行时多个命中窗口的进入 / 离开状态处理
4. 未决定旧字段是迁移后删除，还是保留为只读兼容字段
5. 未完成 Unity 刷新、编译与 PlayMode 验证
6. 未验证 attack02 的三个命中窗口和 attack03 的两个命中窗口能否按时间依次触发
```

## 七、下一步建议

```text
第一步：实现一个按 AnimationClip 查找 AbilityWindowTrackSO 的共享查询入口
第二步：让 PlayerNormalAttackConfig Inspector 只展示匹配轨道，不再展示三份旧命中窗口编辑字段
第三步：把 PlayerSkillController 改为按当前归一化时间扫描轨道中的全部 Hit 窗口
第四步：明确同一窗口重复进入时 CombatHitbox 的开启策略，避免每帧重复 Open
第五步：完成旧字段兼容迁移，再决定是否从 PlayerSkillStepData 序列化结构中删除
第六步：刷新 Unity、检查 Console，再分别验证 attack01 / attack02 / attack03
```

运行时扫描建议保持窗口数据不可变读取；每个窗口使用稳定 `IdValue` 作为进入状态标识，而不是只用段落索引，以支持同一动画内多个命中窗口。

## 八、工作区注意事项

本次归档没有修改 C#、Unity 资产或场景。归档时 `git status --short` 显示以下已有修改：

```text
M Assets/Scripts/Tools/Editor/AbilityComposer/AbilityComposerView.cs
M Assets/Scripts/Tools/Editor/AbilityComposer/UI/Uss/AbilityComposerWindow.uss
```

上述修改属于之前的分辨率适配工作，当前未验证，不应回退或覆盖。三个窗口轨道资产均已存在，也不应因 Inspector 未同步而删除或重复创建。

