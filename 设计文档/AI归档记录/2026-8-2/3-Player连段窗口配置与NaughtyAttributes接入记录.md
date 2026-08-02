# Protocol_Evac Player 连段窗口配置与 NaughtyAttributes 接入记录

## 一、记录范围

本记录接续：

[2-Player普攻RootMotion与连段窗口交接记录.md](2-Player普攻RootMotion与连段窗口交接记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录接续本日普攻 Root Motion 与连段窗口实现，记录 NaughtyAttributes 安装、Player 程序集引用边界、连段窗口 Inspector 配置方案，以及当前普攻配置状态。

```text
1. NaughtyAttributes 已通过 UPM Git 安装并完成 Unity 域重载
2. PlayerStateClipData 保留现有数据结构，不新增 Skill Step 或独立连段数据类
3. 嵌套数组元素不使用 Foldout，改用 ShowIf + AllowNesting 显示连段时间字段
4. PlayerStateCommonConfigSOEditor 保留，用于同步动画时长、Undo 和脏标记
5. Player.asmdef 只引用 NaughtyAttributes.Core，不引用 NaughtyAttributes.Editor
6. 用户已将 attack01 / attack02 连段窗口启用并完成时间调整
```

## 二、本次确认的设计 / 协作偏好

### 1. 保留 PlayerStateClipData 数据结构

当前不进行 PlayerStateClipData -> PlayerSkillStepData 的整体重构。连段窗口继续放在：

```text
Assets/Scripts/Module/Player/HFSM/Config/Common/PlayerStateClipData.cs
```

原因是当前 `StateClipValues` 数组同时被 Move、Air、Dodge、NormalAttack 使用，先保持序列化结构稳定，避免扩大配置资产迁移范围。

### 2. NaughtyAttributes 只作为 Inspector 辅助

NaughtyAttributes 不参与 Player 运行时逻辑。运行时仍只读取配置属性：

```text
PlayerStateClipData.TryGetComboWindow
PlayerNormalAttackConfigSO.TryGetComboWindow
PlayerNormalAttackState.refreshComboBufferedInput
```

`NaughtyAttributes.Editor` 是编辑器程序集，不能加入 Player 运行时程序集。当前 Player 只引用 `NaughtyAttributes.Core`。

### 3. 嵌套连段字段使用条件显示

官方 Foldout 文档明确说明，Foldout 不支持嵌套在序列化类或结构体中的字段。因此当前不继续使用：

```text
[Foldout("连段窗口")]
```

改为：

```text
[ShowIf(nameof(UseComboWindowValue))]
[AllowNesting]
```

当前 Inspector 语义为：

```text
UseComboWindowValue = false
└─ 隐藏 ComboOpenNormalizedTimeValue / ComboCloseNormalizedTimeValue

UseComboWindowValue = true
├─ 显示 ComboOpenNormalizedTimeValue
└─ 显示 ComboCloseNormalizedTimeValue
```

这不是独立三角形折叠，但不会引入额外编辑器 Drawer，也不会增加编辑器结构膨胀。

### 4. 同步动画时长按钮继续保留在现有 Editor

当前 `PlayerStateCommonConfigSOEditor` 仍负责：

```text
同步全部动画时长
Undo.RecordObject
EditorUtility.SetDirty
无动画段时的提示
```

不使用 `[Button]` 替代该逻辑。NaughtyAttributes 的 Button 只能扫描被检查对象的方法，不能直接给自定义 Editor 类的方法提供按钮；当前 Editor 方案能保留完整的 Undo 与失败提示行为。

## 三、当前实现状态

### 1. NaughtyAttributes 包

当前依赖：

```text
com.dbrizov.naughtyattributes
└─ https://github.com/dbrizov/NaughtyAttributes.git#upm
└─ resolved version: 2.1.6
```

相关文件：

```text
Packages/manifest.json
Packages/packages-lock.json
```

Unity 已完成域重载，最近一次编译检查未发现 Console 错误。

### 2. PlayerStateClipData Inspector 配置

当前文件：

```text
Assets/Scripts/Module/Player/HFSM/Config/Common/PlayerStateClipData.cs
```

当前连段字段：

```text
UseComboWindowValue
ComboOpenNormalizedTimeValue
ComboCloseNormalizedTimeValue
```

开始和结束时间字段只有在 `UseComboWindowValue` 开启后显示。

### 3. Player 程序集边界

当前文件：

```text
Assets/Scripts/Module/Player/Player.asmdef
```

当前明确保留：

```text
NaughtyAttributes.Core
```

当前明确移除：

```text
NaughtyAttributes.Editor
```

`PlayerStateCommonConfigSOEditor.cs` 继续使用 UnityEditor.Editor，避免运行时 Player 程序集依赖编辑器程序集。

## 四、场景 / 资源 / 配置状态

### 1. 普攻配置资产

当前文件：

```text
Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
```

当前用户已调整为：

```text
attack01
├─ StateDurationValue: 1.2666668
├─ UseComboWindowValue: 1
├─ ComboOpenNormalizedTimeValue: 0.2
└─ ComboCloseNormalizedTimeValue: 0.75

attack02
├─ StateDurationValue: 1.8333334
├─ UseComboWindowValue: 1
├─ ComboOpenNormalizedTimeValue: 0.2
└─ ComboCloseNormalizedTimeValue: 0.75

attack03
└─ 当前未启用连段窗口
```

当前 `NormalAttackBufferTimeValue` 仍为 `0.25` 秒，`LockMovementValue` 仍为开启状态。

### 2. Root Motion

当前 `RootMotionAttackIndexValues` 仍为：

```text
{ 2 }
```

即只有 attack03 使用动画 XZ Root Motion。此前用户已确认 attack03 跟随动画位移效果正常。

## 五、当前需要注意的问题

### 1. 连段窗口的运行时语义

窗口只决定是否承认下一段输入：

```text
窗口内输入
└─ 写入 m_hasComboBufferedInput

当前段 DurationTimer 结束
└─ tryAdvanceCombo 再推进下一段
```

当前不是窗口内立即切换动画。如果后续手感要求按下后马上进入下一段，需要单独调整 `PlayerNormalAttackState` 的推进时机，不要直接修改计时默认值。

### 2. 当前连段字段对所有 PlayerStateClipData 生效

因为 `PlayerStateClipData` 是通用状态动画段落数据，Move、Air、Dodge 配置也会拥有这些序列化字段。当前字段默认关闭，运行时不启用时不会改变原有逻辑。

### 3. 尚未开始命中判定

本轮只完成连段窗口配置与 Inspector 接入，没有创建 Hitbox、伤害接口或 Enemy 受击逻辑。下一阶段需要先盘点现有敌人和伤害接口，再决定最小命中闭环，不把不存在的接口假设写入 Player 状态。

## 六、当前尚未完成

```text
确认 attack01 / attack02 新窗口值在 Play Mode 中的最终手感
是否需要把窗口内输入升级为窗口内立即推进
盘点现有 Enemy、碰撞体与伤害接口
设计并实现第一版 PlayerNormalAttack Hitbox / Hitbox Window
确认是否需要把命中帧、前摇、后摇加入现有 PlayerStateClipData
处理 D Assets/Scripts/Module/Player/Skill/Core.meta
处理未跟踪 SceneBackups 备份文件
```

## 七、下一步建议

下一阶段进入普攻命中判定，但先做接口盘点：

```text
1. 搜索现有 Enemy、IDamageable、TakeDamage、Collider 和受击状态实现
2. 如果没有现成伤害接口，先建立最小受击协议，不在 PlayerNormalAttackState 中直接写敌人类型
3. 为攻击段增加明确的命中窗口意图
4. 由独立 Hitbox 组件执行 Overlap / 目标去重
5. 通过通用受击接口传递伤害
6. Play Mode 验证单段攻击命中，再接入三段连段
```

职责边界继续保持：

```text
PlayerNormalAttackState
└─ 写当前攻击段与命中窗口意图

PlayerAttackHitbox
└─ 执行碰撞检测、目标去重和命中回调

IDamageable / 受击接口
└─ 接收伤害，不让 Player 依赖具体 Enemy 类

PlayerMotor
└─ 继续只执行移动与 Root Motion
```

## 八、工作区注意事项

当前 `git status --short`：

```text
M  Assets/Config/Player/Skill/PlayerNormalAttackConfig.asset
D  Assets/Scripts/Module/Player/Skill/Core.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639212679172188995.backup
```

NaughtyAttributes 接入代码与程序集调整已包含在当前 HEAD 提交：

```text
ab948d2 引入NaughtyAttributes 优化相关Editor脚本
```

本记录不处理上述删除和备份文件，后续提交前单独确认。

