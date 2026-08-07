# Protocol_Evac 千咲普攻 Begin-Recovery 连段闭环记录

## 一、记录范围

本记录接续：

[2-PlayerController装配收口与边界记录.md](../2026-8-6/2-PlayerController装配收口与边界记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

[../../战斗系统/战斗系统总开发文档.md](../../战斗系统/战斗系统总开发文档.md)

本记录保存千咲三段普攻动画切片、`Begin / Recovery` 阶段模型、连段分支、Animator 接线、Root Motion 与命中配置的当前状态。红色 Animator 连线已经用户确认没有问题，不再作为待修复项。

## 二、本次确认的设计

### 1. 每段普攻由攻击阶段与收招阶段组成

`PlayerSkillStepData` 现在为每一段分别保存：

```text
Begin
├─ AnimationClip
├─ Duration
└─ UseRootMotion

Recovery
├─ AnimationClip
├─ Duration
└─ UseRootMotion
```

连段过程中只在当前段 `Begin` 结束时做一次分支：

```text
Begin 结束
├─ 已缓存下一段输入 -> 下一段 Begin
└─ 未缓存下一段输入 -> 当前段 Recovery

Recovery 结束 -> 技能结束
```

因此 `Recovery` 不是额外的技能段，也不应加入独立 HFSM 状态。HFSM 继续只保留一个 `PlayerNormalAttackState`，具体段数和阶段由 `PlayerSkillTimeline` 管理。

### 2. 时间轴是连段分支的权威

新增阶段枚举：

```text
PlayerSkillStepPhase.Begin
PlayerSkillStepPhase.Recovery
```

`PlayerSkillTimeline` 负责当前段、当前阶段、阶段计时、推进输入缓存与阶段结束后的分支。`HitWindow` 和 `StepAdvanceWindow` 只在 `Begin` 阶段生效，收招阶段不再接受推进，也不产生攻击命中。

### 3. Animator 只负责表现

`PlayerAnimWriter` 根据 `NormalAttackIndex + NormalAttackPhase` 直接 CrossFade 到六个 Animator 状态。`normalAttackIndex` Animator 参数已经移除，Animator 不负责判断是否进入下一段或是否进入收招。

当前 Animator 中五条关系线为 `Muted Transition`，所以在 Animator 窗口中显示红色。这是禁用的旧过渡线，仅用于保留布局关系；运行时动画由代码直接 CrossFade，不依赖这些过渡线。用户已明确接受该显示，不要再仅为改变线条颜色重接业务过渡。

## 三、动画资源状态

六个独立动画位于：

```text
Assets/Animation/千咲/Clips/Attack/
├─ attack01_begin.anim
├─ attack01_end.anim
├─ attack02_begin.anim
├─ attack02_end.anim
├─ attack03_begin.anim
└─ attack03_end.anim
```

当前切片范围与同步后的持续时间为：

```text
Attack01
├─ attack01_begin：1-20 帧，0.6333s
└─ attack01_end：21-92 帧，2.3667s

Attack02
├─ attack02_begin：1-45 帧，1.4667s
└─ attack02_end：46-167 帧，4.0333s

Attack03
├─ attack03_begin：1-60 帧，1.9667s
└─ attack03_end：61-216 帧，5.1667s
```

完整动画不再作为当前普攻播放入口。原始 FBX 继续保存在 `Assets/Animation/千咲/Raw/Attack/`，独立 `.anim` 是当前技能配置直接引用的资源。

## 四、关键实现位置

```text
Assets/Scripts/Module/Player/Skill/Data/PlayerSkillStepData.cs
  └─ 保存 Begin / Recovery 动画、时长、Root Motion 与窗口数据

Assets/Scripts/Module/Player/Skill/PlayerSkillStepPhase.cs
  └─ 定义 Begin / Recovery 阶段

Assets/Scripts/Module/Player/Skill/Core/PlayerSkillTimeline.cs
  └─ 执行阶段计时与 Begin 结束后的连段分支

Assets/Scripts/Module/Player/HFSM/Animation/PlayerAnimWriter.cs
  └─ 将段数与阶段映射到六个 Animator 状态

Assets/Scripts/Module/Player/Editor/Skill/PlayerSkillStepDataDrawer.cs
  └─ 绘制技能段配置，保留归一化窗口滑动条

Assets/Animation/千咲/千咲_Animator.controller
  └─ 保存三段 Begin 与三段 Recovery 状态布局

Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
  └─ 当前三段普攻的实际配置
```

自定义 `PlayerSkillStepDataDrawer` 使用完整 SerializedProperty 路径隔离数组元素的输入控件，解决 Tri Inspector 在两个窗口组之间串值、无法输入的问题。后续修改 Inspector 时必须保留滑动条和当前分组可读性，不要退回普通且难以辨认的默认绘制。

## 五、当前普攻配置

```text
第 1 段
├─ Begin：0.6333s，Root Motion 开启
├─ Recovery：2.3667s，Root Motion 开启
├─ 推进窗口：0.4-0.75，约 0.253-0.475s
└─ 命中窗口：0.2-0.35，约 0.127-0.222s，伤害 10

第 2 段
├─ Begin：1.4667s，Root Motion 开启
├─ Recovery：4.0333s，Root Motion 开启
├─ 推进窗口：0.3-0.751，约 0.440-1.101s
└─ 命中窗口：0.2-0.35，约 0.293-0.513s，伤害 15

第 3 段
├─ Begin：1.9667s，Root Motion 开启
├─ Recovery：5.1667s，Root Motion 开启
├─ 推进窗口：关闭
└─ 命中窗口：关闭，伤害 0
```

全局普攻输入缓存时间为 `0.25s`，技能期间锁定普通移动，技能退出混合时间为 `0.15s`。

## 六、已验证行为

此前已完成以下验证：

```text
单击：attack01 Begin -> attack01 Recovery -> Finish
双击：attack01 Begin -> attack02 Begin -> attack02 Recovery -> Finish
三击：attack01 Begin -> attack02 Begin -> attack03 Begin -> attack03 Recovery -> Finish
```

纯 `PlayerSkillTimeline` 的单击、双击、三击流程均通过；Play Mode 中已确认实际进入对应 Animator 状态。

Root Motion 验证中，玩家世界坐标 `z` 从 `-1.1250` 移动到 `-0.6550`，攻击结束后没有传回攻击起点。最终 Unity Console 未发现项目代码产生的 Error 或 Exception。

## 七、当前限制与风险

### 1. 窗口时间需要按切片后的画面重新校准

现有归一化窗口从完整动画配置迁移而来。动画拆分后，归一化数值对应的实际秒数已经改变。当前数值能驱动流程，但不能据此断言命中帧和连段手感已经正确，下一轮必须在 Scene / Game View 中结合实际挥刀画面重新调节。

### 2. 第三击目前没有伤害

第三段 `UseHitWindowValue = false` 且 `DamageValue = 0`。这不是 CombatHitbox 故障，而是当前配置尚未完成。第三击会正常播放 Begin 与 Recovery，但不会产生伤害。

### 3. 输入缓存目前是单槽语义

`PlayerInputBuffer` 当前只缓存一次离散普攻输入。按每段推进窗口继续点击可以完成三连，但若三次点击全部极快地发生在第一段开始时，不保证三个输入都被排队保留。是否需要多槽队列应根据目标操作手感决定，不要在没有明确需求时提前扩展。

## 八、下一步建议

优先进行三段普攻的画面校准与伤害闭环：

```text
逐段观察刀刃实际接触帧
  -> 校准 Begin 内的 HitWindow
  -> 校准 StepAdvanceWindow 的手感范围
  -> 为第三段启用 HitWindow 并填写伤害
  -> 验证单击、双击、三击各自只命中一次
  -> 再决定是否需要多槽输入队列
```

不要先扩展新的连段状态或 Animator 条件。当前架构已经能表达“继续攻击”与“进入收招”，下一阶段的主要工作是配置校准和运行时命中验证。

## 九、工作区注意事项

当前提交为：

```text
34b943f attack前摇后摇动画分开
```

该提交除本次动画、技能和 Animator 相关内容外，还包含：

```text
.codex/work_init_CodexAI/01-项目宪章与核心原则.md
SceneBackups/99c9720ab356a0642a771bea13969a05/639217366713884762.backup
```

后续整理提交时应单独核对这两项，不要在未确认归属前擅自删除、回退或覆盖。归档创建前工作区无未提交改动。
