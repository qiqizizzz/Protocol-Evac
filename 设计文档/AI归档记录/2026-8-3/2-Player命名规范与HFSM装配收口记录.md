# Protocol_Evac Player 命名规范与 HFSM 装配收口记录

## 一、记录范围

本记录接续：

[1-Combat基础伤害契约与古法编程协作记录.md](1-Combat基础伤害契约与古法编程协作记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录接续 Combat 基础伤害契约与 Player 普攻命中前置讨论，记录本轮命名规范修正、Player HFSM 动画 / 状态转换装配命名收口、目录检查结论，以及用户对 Combat / Damage 边界的最新确认。

```text
1. 已修正 04 编码规范中的命名规则，BasePlayerState、ConfigSO、GameTimerData 均为保留方向
2. PlayerBufferedInputSlot 已收口为 PlayerBufferedInputData
3. PlayerAnimBinder / PlayerTransitionBinder 不再沿用 Binder 命名
4. RuleRegistry 命名曾被尝试，但用户认为过重，最终收口为 Controller
5. PlayerController 中重复 Register(Create(...)) 装配调用已下沉到子 Controller
6. 用户确认不删除空目录，Combat/Hitbox 与 Player/Skill/Core 保留
7. 用户澄清 NormalAttackIndex 不是要删除，问题在于 DamageData 不能绑定普攻专属语义
```

## 二、本次确认的设计 / 协作偏好

### 1. Base 与 ConfigSO 命名规则

用户明确确认：

```text
BasePlayerState 是正确命名
抽象基类使用 Basexxx，而不是 xxxBase
ScriptableObject 配置类使用 ConfigSO 即可
不要把 PlayerStateCommonConfigSO 强行改成 PlayerStateConfigBaseSO
GameTimerData 当前命名没问题，不要乱改
```

当前 `.codex/work_init_CodexAI/04-编码规范-CSharp.md` 已按该口径更新。后续 AI 不应再提出：

```text
BasePlayerState -> PlayerStateBase
PlayerStateCommonConfigSO -> PlayerStateConfigBaseSO
GameTimerData -> GameTimerTask
```

这类迁移建议。

### 2. Anim 缩写可以沿用

用户已确认当前 Player 动画体系中 `Anim` 命名可以保留。后续不要因为完整英文偏好强行把现有同族类型改成 `Animation`。

当前保留：

```text
PlayerAnimController
PlayerAnimResolver
PlayerAnimWriter
PlayerAnimRule
PlayerAnimParams
PlayerMoveAnimRules
PlayerAirAnimRules
PlayerActionAnimRules
PlayerSkillAnimRules
```

如果未来新建同族动画类，优先保持 `Anim` 一致，不在同一职责内混用 `Anim` 与 `Animation`。

### 3. RuleRegistry 命名过重，当前改用 Controller

本轮曾将 `Binder` 收口为 `RuleRegistry`，但用户指出：

```text
m_animRuleRegistry.Register(PlayerMoveAnimRules.Create(m_context));
```

这类调用既长又重复，`RuleRegistry` 对当前项目过重。最终确认：

```text
PlayerAnimController
PlayerTransitionController
```

由子 Controller 负责装配同族规则，`PlayerController` 只进行一次初始化调用。

### 4. DamageData 必须保持通用战斗载荷

用户澄清此前关于“当前攻击段索引”的意见：

```text
不是不要 NormalAttackIndex
而是 Damage / Combat 不能只服务普攻某一个人
DamageData 不能因为接入普攻命中而变成普攻专属类
```

后续 Combat / Damage 设计应保持：

```text
DamageData
├─ 伤害数值
├─ 来源对象
├─ 命中点
└─ 命中方向
```

这类通用命中事实。普攻段数、技能段数、连段索引等应留在 Player Skill / Animation / Hitbox 意图侧，不应硬塞进共享 `DamageData` 作为核心字段。

## 三、当前实现状态

### 1. Player HFSM 动画装配

当前目录：

```text
Assets/Scripts/Module/Player/HFSM/Animation/
├─ Controllers/
│  └─ PlayerAnimController.cs
├─ Rules/
│  ├─ PlayerMoveAnimRules.cs
│  ├─ PlayerAirAnimRules.cs
│  ├─ PlayerActionAnimRules.cs
│  └─ PlayerSkillAnimRules.cs
├─ PlayerAnimParams.cs
├─ PlayerAnimResolver.cs
├─ PlayerAnimRule.cs
├─ PlayerAnimWriter.cs
└─ PlayerRootMotionReceiver.cs
```

`PlayerAnimController` 当前职责：

```text
Init(PlayerContext context)
├─ register(PlayerMoveAnimRules.Create(context))
├─ register(PlayerAirAnimRules.Create(context))
├─ register(PlayerActionAnimRules.Create(context))
└─ register(PlayerSkillAnimRules.Create(context))
```

`PlayerController` 当前只保留：

```text
m_animController = new PlayerAnimController();
m_animController.Init(m_context);
m_animResolver.Init(m_stateMachine, m_animController.Handlers);
```

### 2. Player HFSM 状态转换装配

当前目录：

```text
Assets/Scripts/Module/Player/HFSM/Transition/
├─ Controllers/
│  └─ PlayerTransitionController.cs
├─ Rules/
│  ├─ PlayerMoveTransitionRules.cs
│  ├─ PlayerAirTransitionRules.cs
│  ├─ PlayerActionTransitionRules.cs
│  └─ PlayerSkillTransitionRules.cs
├─ PlayerTransitionPriority.cs
├─ PlayerTransitionRule.cs
└─ PlayerTransitionSelector.cs
```

`PlayerTransitionController` 当前职责：

```text
Init(context, airConfig, dodgeConfig, normalAttackConfig)
├─ register(PlayerMoveTransitionRules.Create(context))
├─ register(PlayerAirTransitionRules.Create(context, airConfig))
├─ register(PlayerActionTransitionRules.Create(context, dodgeConfig))
└─ register(PlayerSkillTransitionRules.Create(context, normalAttackConfig))
```

`PlayerController` 当前只保留：

```text
m_transitionController = new PlayerTransitionController();
m_transitionController.Init(m_context, AirConfig, DodgeConfig, NormalAttackConfig);
m_transitionSelector = new PlayerTransitionSelector(m_stateMachine, m_transitionController.Rules);
```

### 3. Input Buffer 命名

此前命名：

```text
PlayerBufferedInputSlot
```

已收口为：

```text
Assets/Scripts/Module/Player/Input/Buffer/PlayerBufferedInputData.cs
```

用户认为 `Slot` 更像 UI / 装备槽位命名，不适合 Input Buffer 内部值数据。当前 `Data` 命名符合项目口径。

## 四、目录检查结论

当前 Player / Combat 目录整体可继续使用，无需再做大规模迁移。

### 1. 明确保留的空目录

用户确认不删除目录，因此以下空目录保留：

```text
Assets/Scripts/Module/Combat/Hitbox/
Assets/Scripts/Module/Player/Skill/Core/
```

说明：

```text
Combat/Hitbox 是后续普攻命中闭环预留位置
Player/Skill/Core 是后续 Skill System 核心层预留位置
```

后续 AI 不要再主动建议删除这两个目录，除非用户重新要求清理空目录。

### 2. 当前不需要改的命名

```text
BasePlayerState
PlayerCompositeState
PlayerGroundedState
PlayerAirborneState
PlayerActionState
PlayerSkillState
PlayerStateCommonConfigSO
GameTimerData
DamageData
CombatTargetDummy
PlayerAnim*
```

其中 `PlayerCompositeState` 是正确抽象，不应被误改为 `Base` 命名；`GameTimerData` 与 `DamageData` 均按当前项目口径保留。

### 3. 仍可后续顺手处理的小拼写

当前仍存在：

```text
PlayerContext.ResetRunTimeData()
```

如果后续用户同意，可改为：

```text
ResetRuntimeData()
```

这是拼写统一问题，不是架构问题，不建议和 Combat / Hitbox 重构混在一起处理。

## 五、关键架构边界

### 1. PlayerController 边界

`PlayerController` 只做生命周期与模块调度，不继续堆积规则装配细节。

当前边界：

```text
PlayerController
├─ 创建 PlayerContext
├─ 初始化 InputReader / Motor / ViewController
├─ 初始化 StateMachine 与状态树
├─ 初始化 PlayerTransitionController
├─ 初始化 PlayerAnimController / Resolver / Writer
└─ Tick / FixedTick 调度
```

后续新增规则时，优先进入对应 `Rules` 和子 `Controller`，不要让 `PlayerController` 出现越来越长的规则注册列表。

### 2. Combat 与 Player 依赖方向

继续保持：

```text
Combat
   ↑
Player
```

`DamageData` 与 `IDamageable` 仍属于 `Module.Combat.Damage`，不依赖 Player。Player 后续命中检测可创建通用 `DamageData` 并调用 `IDamageable.TakeDamage`。

### 3. NormalAttackIndex 的归属

当前 `NormalAttackIndex` 可继续用于：

```text
PlayerContext
PlayerNormalAttackState
PlayerAnimParams
PlayerAnimWriter
PlayerSkillAnimRules
```

它表达 Player 普攻段落与动画播放需求，不代表 Combat 伤害协议需要知道“当前第几段普攻”。如果未来某段攻击伤害不同，应通过 Player Hitbox / Skill 配置在生成 `DamageData` 前完成选择，而不是让 `DamageData` 直接变成普攻段落数据。

## 六、当前尚未完成

```text
Unity MCP 本轮未连接，未通过 MCP 刷新 AssetDatabase
本机 dotnet SDK 不可用，未通过 dotnet build 校验 Player.csproj
尚未实现 Combat/Hitbox 下的 PlayerAttackHitbox 或通用 Hitbox 类
Player.asmdef 是否已经引用 Combat 需要进入下一步前再次确认
尚未完成普攻命中窗口到 Hitbox 的运行时桥接
尚未完成场景内 CombatTargetDummy 命中验证
ResetRunTimeData 拼写暂未处理
```

## 七、下一步建议

下一步进入 Combat Hitbox 前，建议按小步推进：

```text
1. 先确认 Unity Console 当前无编译错误
2. 确认 Player.asmdef 是否已引用 Combat
3. 在 Combat/Hitbox 或 Player 侧确认第一版 Hitbox 类归属
4. 设计通用 Hitbox 输入数据，不把 DamageData 绑死到普攻段数
5. 由 PlayerNormalAttackState 或后续 Skill System 只写命中窗口 / 段落意图
6. 由 Hitbox 执行 Overlap、同窗口目标去重、DamageData 创建与 IDamageable.TakeDamage
7. 先验证单段普攻命中 CombatTargetDummy，再接三段连段
```

不要在下一步同时处理 Enemy AI、完整 Skill Runner、全局 CombatSystem、硬直死亡和复杂伤害结算。

## 八、工作区注意事项

本轮归档前 `git status --short` 显示当前仍有与本次命名讨论无关的工作区状态：

```text
D Assets/Lua.meta
D Assets/Scripts/Common.meta
D Assets/Scripts/Framework/QLua.meta
D Assets/Scripts/Net.meta
D Assets/Scripts/Tools/GM.meta
D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639210032524788533.backup
```

后续不要把这些 `.meta` 删除和场景备份误当作 Player 命名收口或 Combat Hitbox 改动处理。

