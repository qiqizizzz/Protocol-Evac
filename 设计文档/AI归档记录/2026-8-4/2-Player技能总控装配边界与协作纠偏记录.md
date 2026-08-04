# Protocol_Evac Player 技能总控装配边界与协作纠偏记录

## 一、记录范围

本记录接续：

[1-Player技能配置统一与普攻数据迁移记录.md](1-Player技能配置统一与普攻数据迁移记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录保存 `PlayerSkillController` 作为 Player 技能总控制器的职责定位、配置与依赖的装配方式、是否接入 QF 或建立 `BaseController` 的取舍，以及本轮因错误理解 `Open / Close` 语义产生的协作纠偏。

```text
1. PlayerSkillController 是 Player 全技能总控制器，不是普攻命中窗口适配器
2. PlayerSkillController 对外生命周期命名确认使用 Open / Tick / Close
3. 技能配置和稳定运行依赖应在构造函数中一次性注入
4. Open 不接收 PlayerSkillConfigSO 或 PlayerSkillStepData，只接收技能语义
5. CombatHitbox.Open / Close 只允许由 PlayerSkillController 内部在命中窗口边界调用
6. Player 内部子控制器不注册到 QF，不新增 BaseController 或通用 Controller 容器
7. PlayerController 继续作为单个 Player 实例的唯一组合根与显式调度入口
```

## 二、本次确认的设计结论

### 1. PlayerSkillController 是技能总控制器

禁止把 `PlayerSkillController` 降级成：

```text
普攻 HitWindow 计时器
CombatHitbox 的简单包装类
只保存一个 PlayerSkillStepData 的段落适配器
```

它最终应统一管理：

```text
当前技能类型
当前技能配置
当前 Step
当前 Step 计时
StepAdvanceWindow
HitWindow
技能完成状态
技能中断与资源清理
CombatHitbox.Open / Close 调度
```

HFSM 的 `PlayerNormalAttackState` 只承载“玩家当前处于普通攻击技能状态”的宏观生命周期，不应永久持有另一套技能段落计时与命中窗口执行逻辑。

### 2. 对外生命周期固定使用 Open / Tick / Close

用户已明确指定 `PlayerSkillController` 的生命周期命名：

```csharp
Open(...)
Tick(float deltaTime)
Close()
```

后续不得擅自替换为：

```text
StartSkill / StopSkill
BeginStep / EndStep
Cancel
Play / Release
```

建议的最终语义：

```text
Open(PlayerSkillType skillType)
└─ 从构造时注入的配置集合中选出技能并开始运行

Tick(float deltaTime)
└─ 推进当前 Step、窗口、结束标记与运行时效果

Close()
└─ 无论正常结束还是中断，都统一关闭 Hitbox 并清理当前技能
```

`Open` 不应要求调用方再次传入配置或 Step：

```csharp
// 不采用
Open(PlayerSkillType skillType, PlayerSkillConfigSO config);
Open(PlayerSkillStepData stepData);
```

配置属于控制器的稳定装配依赖，不属于每次开启技能时重复传入的运行参数。

### 3. 稳定依赖使用构造函数注入

`PlayerSkillController` 是纯 C# 长生命周期对象，配置和必要引用在创建后不会任意替换，因此应优先通过构造函数一次性注入。

建议方向：

```csharp
PlayerSkillController(
    PlayerContext context,
    IReadOnlyDictionary<PlayerSkillType, PlayerSkillConfigSO> skillConfigs,
    CombatHitbox combatHitbox,
    GameObject source)
```

这只是下一轮实现时的职责示意，具体集合类型可以在落盘前继续收口，但以下原则已经确认：

```text
配置在构造函数注入
Context 在构造函数注入
Hitbox 与伤害来源在构造函数注入
Open 只接收 PlayerSkillType 等技能语义
不采用先 new 空对象、随后多次 Init 填字段的半初始化状态
```

当前项目内多个旧 Player 子模块仍使用 `new + Init`。本次不批量改造旧模块；`PlayerSkillController` 作为新实现按构造完整性优先落地，后续再根据实际收益逐步统一。

## 三、装配方式结论

### 1. PlayerController 继续作为唯一组合根

单个 Player 的本地运行模块统一由 `PlayerController` 创建和持有：

```text
PlayerController
├─ 从 Inspector 持有 ConfigSO、CombatHitbox 与场景引用
├─ 创建 PlayerContext
├─ 构造 PlayerSkillController
├─ 构造 HFSM、Transition、Animation、Input 与 View 模块
├─ 将需要的控制器引用显式传给 State
└─ 按确认顺序执行 Tick / FixedTick
```

推荐在 `PlayerController` 内增加职责明确的私有装配方法，例如：

```text
initCore
initSkill
initHFSM
initAnim
```

这里的“统一注册”是指由同一个组合根显式构造和装配，不是创建全局 Controller 注册表。

### 2. 不新增 BaseController

当前 Player 子控制器并没有统一生命周期：

```text
PlayerAnimController
└─ 装配动画规则，本身不按帧 Tick

PlayerTransitionController
└─ 装配转换规则，本身不按帧 Tick

PlayerViewController
└─ 每帧处理视角

PlayerSkillController
└─ Open / Tick / Close 技能生命周期
```

因此建立：

```text
BaseController
ControllerRegistry
RegisterAllControllers
统一 Init / Tick / Close 虚方法
```

会制造没有真实共性的继承层，并把明确的装配关系隐藏到注册表和基类中。当前不采用。

如果未来至少三个 Player 本地模块真实拥有完全一致的生命周期和调度需求，再优先评估一个很薄的接口，而不是带状态与容器逻辑的基类：

```text
IPlayerTickable
└─ Tick(float deltaTime)
```

该接口当前也不创建，只保留判断标准。

### 3. 不把 PlayerSkillController 注册成 QF System

现有 QF `Architecture` 容器适合：

```text
全局 TimerSystem
长期 Model
跨模块事件、Command 与 Query
```

`PlayerSkillController` 属于单个 Player 实例的高频本地运行对象：

```text
生命周期跟随 Player GameObject
依赖该 Player 的 Context、Config、Hitbox 与伤害来源
未来可能同时存在多个 Player 或测试实例
```

将其注册成 QF `ISystem` 会把实例级状态提升到全局容器，模糊生命周期并增加多实例冲突风险。因此继续保持：

```text
QF：全局与跨模块服务
PlayerController：单个 Player 的组合根
PlayerSkillController：单个 Player 的本地技能总控
```

## 四、运行边界建议

推荐数据流：

```text
PlayerNormalAttackState.Enter
└─ PlayerSkillController.Open(PlayerSkillType.NormalAttack)

PlayerController.Update
└─ PlayerSkillController.Tick(deltaTime)
   ├─ 推进当前 Step
   ├─ 处理 StepAdvanceWindow
   ├─ 命中窗口开始时调用 CombatHitbox.Open(damage, source)
   ├─ 命中窗口结束时调用 CombatHitbox.Close()
   └─ 写入 IsFinished 等只读运行结果

PlayerNormalAttackState.Exit
└─ PlayerSkillController.Close()
   └─ 无条件确保 CombatHitbox.Close()
```

必须继续保持依赖方向：

```text
Player Skill
└─ 依赖 CombatHitbox

CombatHitbox
└─ 不读取 PlayerContext、PlayerSkillConfigSO、Step 或 HFSM State
```

## 五、本轮错误方案与纠偏

本轮曾短暂尝试：

```csharp
PlayerSkillController.Open(PlayerSkillStepData stepData);
PlayerSkillController.Close();
```

并让 `PlayerNormalAttackState` 直接驱动该控制器的段落命中窗口。这一方案错误地把总控制器降级为 Hitbox 窗口适配器，已被用户明确否定并撤回。

当前实际文件：

```text
Assets/Scripts/Module/Player/Skill/Core/PlayerSkillController.cs
└─ 仍为空壳，没有落盘错误的 Open(step) / Tick / Close 实现
```

以下错误接线也已撤回：

```text
PlayerController.NormalAttackHitbox 字段
PlayerController.m_skillController 字段
PlayerNormalAttackState 对 PlayerSkillController 的错误段落驱动
```

后续不得从被撤回代码继续开发。

## 六、Inspector 与包状态

本轮普通 Inspector 已从 NaughtyAttributes 迁移到：

```text
Tri Inspector 1.15.1
```

当前技能和 State 数据使用 Tri 的列表、分组、折叠与条件显示。旧的 Skill / State 自定义 Inspector 已移除，避免 `DrawDefaultInspector()` 覆盖 Tri 绘制。

当前已确认：

```text
PlayerSkillStepData
├─ 动画与段落
├─ 段落推进窗口
└─ 命中窗口

PlayerStateClipData
├─ 动画与段落
└─ 连段窗口
```

窗口分组使用独立 `FoldoutGroup`，启用字段与折叠状态分离，不再使用会绑定业务布尔值的 `ToggleGroup`。

## 七、协作偏好

后续 AI 必须遵守：

```text
1. 用户明确指定 Open / Close 时，不擅自改成 Start / Stop、Begin / End 或 Cancel
2. 先判断类型在架构中的主次与职责，再设计方法参数
3. 总控制器 API 不得按某一个子功能临时降级
4. 配置与稳定依赖优先放在构造函数，不在 Open 时重复传入
5. 写入 Rider 当前打开的文件前，注意未保存内存缓冲可能覆盖磁盘修改
6. 若发生外部覆盖，以用户实际看到的内容为准，不用一次性磁盘检查与用户争辩
```

## 八、当前尚未完成

```text
PlayerSkillController 正式构造函数尚未实现
PlayerSkillController.Open / Tick / Close 尚未实现
技能配置集合的具体承载类型尚未最终落盘
PlayerController 中 initSkill 的装配顺序尚未实现
现有普攻 Timer 与段数尚未从 PlayerNormalAttackState 迁移到技能总控
HitWindow 尚未桥接 CombatHitbox.Open / Close
Player Prefab 尚未配置 CombatHitbox
Damageable Layer 与 CombatTargetDummy 尚未配置
第一段普攻单窗口单次伤害尚未完成 Play Mode 验证
```

## 九、下一步建议

下一轮只做 `PlayerSkillController` 的骨架与装配契约，不同时修改场景：

```text
1. 最终确认构造函数参数及技能配置集合形式
2. 实现只读运行状态：当前技能、当前 Step、归一化时间、是否完成
3. 实现 Open(PlayerSkillType)、Tick(deltaTime)、Close()
4. 在 PlayerController.initSkill 中通过构造函数创建总控制器
5. 将 PlayerNormalAttackState 的 Timer 与段落推进迁入总控制器
6. 确认纯时间轴逻辑后，再接 CombatHitbox
7. 最后配置 Prefab、Layer、Dummy 和第一段测试数据
```

不要在下一轮同时引入：

```text
BaseController
ControllerRegistry
新的 QF System
Service Locator
完整 UI Toolkit 技能编辑器
SpecialSkill / Ultimate 的占位实现
```

## 十、工作区注意事项

归档时工作区仍存在与本次架构讨论无关的状态：

```text
D Assets/Lua.meta
D Assets/Scripts/Common.meta
D Assets/Scripts/Framework/QLua.meta
D Assets/Scripts/Net.meta
D Assets/Scripts/Tools/GM.meta
D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639214339907833159.backup
```

不要擅自恢复或清理这些文件。

`PlayerController.cs` 与 `PlayerNormalAttackState.cs` 当前没有保留错误接线的实际文本差异；`PlayerSkillController.cs` 只保留文件描述文字和文件末尾换行的轻微差异，类本身仍为空。
