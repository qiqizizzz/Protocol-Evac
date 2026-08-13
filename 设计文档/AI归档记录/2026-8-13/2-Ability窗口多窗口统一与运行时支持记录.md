# Protocol_Evac Ability 窗口多窗口统一与运行时支持记录

## 一、记录范围

本记录接续：

[1-Ability窗口配置与普攻同步未完成记录.md](./1-Ability窗口配置与普攻同步未完成记录.md)

主设计文档：

[../../工具与编辑器/AbilityComposer设计方案.md](../../工具与编辑器/AbilityComposer设计方案.md)

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录保存本轮 Ability 窗口基类、多窗口轨道查询、命中窗口与连招窗口运行时接入，以及旧字段清理后的最终状态。

## 二、本次确认的设计 / 协作偏好

用户确认窗口应统一放在 Ability 的 Window 模块下：

```text
Ability.Window
├─ AbilityWindowDataBase
├─ AbilityWindowTrackBaseSO
├─ Hit
├─ StepAdvance
└─ Invincible
```

窗口数据统一使用基类保存：

```text
AbilityWindowDataBase
├─ Id
├─ StartNormalizedTime
├─ EndNormalizedTime
├─ IsActiveAt(time)
└─ IsCrossedBy(previousTime, currentTime)
```

每种窗口保留自己的业务字段，例如命中窗口的伤害值；轨道基类只负责动画片段、窗口集合和通用时间查询，不承载命中、连招等具体业务。

用户偏好继续保持：

```text
Player 配置中的窗口区域
├─ 启用窗口 bool
├─ 启用后绑定对应窗口轨道资产
└─ 窗口轨道资产作为唯一时间区间数据源
```

## 三、当前实现状态

### 1. 窗口基类与轨道基类

`Assets/Scripts/Module/Ability/Window/AbilityWindowDataBase.cs` 已提供窗口的通用时间范围判断。

`Assets/Scripts/Module/Ability/Window/AbilityWindowTrackBaseSO.cs` 已提供：

```text
WindowDataValues
WindowCount
TryGetActiveWindow<TWindow>()
TryGetCrossedWindow<TWindow>()
HasWindowAtOrAfter()
```

`WindowDataValues` 由命中、阶段推进、无敌三个轨道子类分别映射到自己的强类型列表，因此业务代码可以复用查询算法，同时仍通过泛型获得具体窗口数据。

### 2. Player 技能运行时

`Assets/Scripts/Module/Player/Skill/Core/PlayerSkillController.cs`：

```text
当前技能段 Begin 阶段
    -> 读取 PlayerSkillStepData.BeginHitWindowTrack
    -> 按归一化时间查询活动命中窗口
    -> 进入不同窗口时切换 CombatHitbox 伤害
    -> 离开窗口或切换阶段时关闭 CombatHitbox
```

因此同一个动画内可以配置多个不连续命中区间，例如 `attack02` 的三个命中窗口。

`Assets/Scripts/Module/Player/Skill/Core/PlayerSkillTimeline.cs`：

```text
收到下一段请求
    -> 查询当前时间是否处于阶段推进窗口
    -> 查询本帧时间推进是否跨过窗口
    -> 若当前窗口未到但后面仍有窗口，保留请求等待
    -> 命中任一窗口后缓存推进
    -> 当前 Begin 阶段结束时进入下一技能段
```

这修复了多窗口之间请求被过早清除的问题。

### 3. 旧字段与旧数据源

已删除代码中的旧单窗口字段与查询入口：

```text
PlayerSkillStepData.TryGetStepAdvanceWindow()
PlayerNormalAttackConfigSO.TryGetComboWindow()
PlayerStateClipData.TryGetComboWindow()
ComboOpenNormalizedTimeValue
ComboCloseNormalizedTimeValue
LegacyComboOpenNormalizedTimeValue
LegacyComboCloseNormalizedTimeValue
```

同时清理了以下配置资产中的旧 YAML 序列化键：

```text
Assets/Config/Player/Action/PlayerDodgeConfig.asset
Assets/Config/Player/Air/PlayerAirConfig.asset
Assets/Config/Player/Move/PlayerMoveConfig.asset
```

当前窗口时间区间应只从对应的 `AbilityWindowTrackSO` 读取。

## 四、当前已确认的窗口数据

命中窗口轨道：

```text
attack01
└─ 0.1579 - 0.2631

attack02
├─ 0.1363 - 0.2045
├─ 0.2727 - 0.3181
└─ 0.3409 - 0.3863

attack03
├─ 0.1017 - 0.1864
└─ 0.4407 - 0.5424
```

连招窗口轨道：

```text
attack01
└─ 0.10526316 - 1

attack02
└─ 0.3 - 0.751

attack03
└─ 未配置连招窗口
```

## 五、验证结果

已完成：

```text
Unity AssetDatabase Refresh：成功
Player.csproj 编译：0 个错误
Unity Console 编译错误：无
EditMode 测试：1/1 通过
Assets 内旧窗口字段 / 旧查询入口搜索：无结果
```

尚未完成运行时 PlayMode 手工验证，尤其需要确认实际输入节奏和动画播放下的行为。

## 六、当前需要注意的问题

1. `TryGetActiveWindow<TWindow>()` 和 `TryGetCrossedWindow<TWindow>()` 当前返回第一个匹配窗口。窗口配置应保持时间顺序；如果未来允许重叠窗口，需要先明确重叠时的优先级或叠加规则。
2. `PlayerSkillController` 当前用窗口 Id 与段落索引判断是否需要重新打开命中框，窗口 Id 必须在编辑器保存后保持稳定。
3. `HasWindowAtOrAfter()` 只判断窗口结束时间是否仍在当前时间之后，不会替请求自动跨越动画循环；技能 Begin 阶段不是循环动画，因此当前语义足够。
4. 无敌窗口轨道已经接入统一基类查询，但本轮没有新增敌人运行时消费者，后续接入敌人技能时应复用同一套查询，不要重新实现时间扫描。

## 七、当前尚未完成

```text
1. PlayMode 验证 attack01 / attack02 / attack03 的真实输入流程
2. 验证请求早于窗口、窗口之间、晚于最后窗口三种时序
3. 验证命中窗口切换时 CombatHitbox 伤害是否正确更新
4. 验证敌人技能接入无敌窗口时的消费者边界
5. 如未来需要窗口重叠，补充窗口排序 / 优先级设计
```

## 八、下一步建议

建议按以下顺序继续：

```text
第一步：进入 PlayMode，测试 attack01 单窗口推进
第二步：测试 attack02 多个命中窗口与连招窗口
第三步：测试输入在命中窗口之间到达时是否保留到下一连招窗口
第四步：检查 CombatHitbox 在每个命中区间的伤害切换
第五步：将敌人技能窗口消费者接入 AbilityWindowTrackBaseSO
```

## 九、工作区注意事项

本轮工作区包含窗口基类、窗口轨道、Player 技能运行时和相关 Player 配置资产的修改。不要回退用户此前关于跳跃下落重力、UIManager 销毁检查、Ability Composer 布局和窗口 Inspector 排序的已有修改。

窗口配置资产仍由用户在 Unity Inspector 中拖拽维护；本轮没有创建新的窗口资产，也没有擅自修改动画资源。

