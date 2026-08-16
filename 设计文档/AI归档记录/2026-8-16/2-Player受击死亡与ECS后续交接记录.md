# Protocol_Evac Player 受击、死亡与 ECS 后续交接记录

## 一、记录范围

本记录接续：

[1-PlayerWalk与视角碰撞排障交接记录.md](1-PlayerWalk与视角碰撞排障交接记录.md)

主设计文档：

[玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存 Player 受击窗口、死亡动画与结算流程的当前实现边界，并确定下一阶段的两个主题：受击反应完善，以及多敌人局部避障的 ECS 预研。Enemy 现有 BT、Skill、Navigation 主流程不在本轮重新设计范围内。

## 二、当前已确认的实现

### 1. Player 伤害与状态进入链路已存在

```text
CombatHitbox
└─ DamageData
   └─ PlayerDamageReceiver
      └─ PlayerDamageController.TryTakeDamage
         └─ PlayerDamageContext.ApplyDamage
            ├─ HasPendingHurt = true
            └─ IsDead = true（生命归零时）

PlayerTransitionSelector
├─ HasPendingHurt -> DisabledHurt
└─ IsDead -> DisabledDead（Status 优先级）
```

`DamageReactionType` 已定义三种反应：`Light`、`Heavy`、`KnockUp`。`PlayerHurtState` 已按该类型预留轻受击、重受击和击飞三条执行分支；这表示运行时框架已具备分流能力，不代表敌人当前攻击已经正确产出三类反应。

### 2. 受击动画与窗口的当前行为

```text
PlayerDamageConfigSO
├─ 轻受击：左 / 右
├─ 重受击：左 / 右
└─ 击飞：起始 / 循环 / 落地

PlayerHurtState
├─ 锁定窗口内：InputLocked + MovementLocked
├─ 锁定窗口结束：允许新的 WASD / 跳跃 / 闪避 / 普攻取消
└─ 没有新输入：按 AnimationClip 完整时长自然退出
```

当前无输入运行时采样中，`LightRight` 在 `1.2s`、`2.4s`、`3.0s` 时仍位于 `DisabledHurt`，约 `3.5s` 后才进入 `GroundedIdle`。锁定窗口内记录的离散输入会在解锁时清空；锁定前持续按住的 WASD 也不会被当作解锁后的新移动取消。

涉及文件：

```text
Assets/Scripts/Module/Player/Context/Runtime/PlayerDamageContext.cs
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs
Assets/Scripts/Module/Player/HFSM/States/Disabled/PlayerHurtState.cs
Assets/Scripts/Module/Player/HFSM/Transition/Rules/PlayerDamageTransitionRules.cs
Assets/Scripts/Module/Player/HFSM/Config/Disabled/PlayerDamageConfigSO.cs
```

### 3. 死亡状态与结算已接通

死亡时 `PlayerDeadState.Enter()` 会锁定输入、移动与根运动，并请求重播 `DisabledDead`；Animator 的真实状态路径为 `Base Layer.Disabled.dead`，绑定 `Death` 动画片段，时长 `2.5s`。

```text
死亡
├─ DisabledDead
│  ├─ 锁定输入、移动、根运动与视角操作
│  └─ 显式重播 Death 动画
└─ EventDefines.PlayerDied
   └─ UISummary 显示“死亡 / 重新挑战”
      └─ PlayerRetryRequested -> RestoreFullHealth -> 退出 DisabledDead
```

相关资源与代码：

```text
Assets/Animation/千咲/Source/Disabled/Death.fbx
Assets/Prefabs/UI/Summary/UISummary.prefab
Assets/Scripts/Module/Player/HFSM/States/Disabled/PlayerDeadState.cs
Assets/Scripts/UI/Summary/UISummary.cs
```

## 三、当前未完成且需要先解决的问题

### 1. 实机未观察到击退效果

`PlayerHurtState` 的重受击与击飞分支已经调用 `SetKnockbackVelocity`，写入 `PlayerMovementContext.ForcedMoveVelocity`，但当前实机没有看到预期击退。

下一次不要先改数值或增加新状态，先按以下顺序定位：

```text
1. 读取 PlayerDamageConfig.asset 的击退速度、持续时间与击飞初速度
2. 确认 Enemy 命中是否真正发送 Heavy / KnockUp，而非全部为 Light
3. 跟踪 PlayerMotor.FixedTick 是否消费 HasForcedMoveVelocity
4. 用运行时日志或 MCP 读取命中后的 PendingReactionType、ForcedMoveVelocity 与角色位置
5. 仅在数据与 Motor 链路都确认后调整击退参数或修正消费逻辑
```

### 2. 敌人攻击尚未完成轻击、重击与击飞的语义配置

当前 `DamageData` 已携带 `DamageReactionType`，但需要检查 Enemy Ability / Hit Window 的实际配置与写入位置。目标是由敌人的攻击段落数据决定反应类型，而不是在 `PlayerHurtState` 内根据伤害数值猜测。

预期边界：

```text
Enemy Ability Hit Window
└─ 配置 Damage + DamageReactionType
   └─ CombatHitbox 创建 DamageData
      └─ Player 只消费 reactionType 并执行表现
```

第一版建议：

```text
普通斩击 -> Light
强力斩击 / 收尾段 -> Heavy
明确上挑、爆炸、击飞技能 -> KnockUp
```

不要额外在 Combat 增加 Player 专属枚举，也不要把 Player 动画选择逻辑迁入 Enemy。

### 3. 击飞分段尚未完成验收

击飞当前代码设计为：起始动画写入水平与竖直速度，离地后进入循环，落地后播放落地段，再完成退出。需要在解决 Motor 位移消费后逐项验证：

```text
起始上升 -> 空中循环 -> 落地动画 -> 恢复地面状态
```

验收时重点检查连续受击、中途死亡、落地同帧状态切换，以及强制水平速度是否正确清理，不要只看单次播放动画。

## 四、ECS 后续边界

ECS 尚未接入，不应重写当前单体 Enemy 的 OOP 模块。当前稳定链路保持：

```text
EnemyController
├─ Fluid Behavior Tree
├─ EnemySkillController
├─ INavigationController / GridPathController
├─ EnemyMotor
└─ EnemyAnimWriter（Playables）
```

ECS 的第一阶段只做多敌人的局部避障或批量邻域计算候选，输出仍回写为每个 Enemy 的移动意图；BT、Skill、A* 路径、动画与 CharacterController 不迁入 ECS。

建议顺序：

```text
1. 先完成并验收 Player 轻击 / 重击 / 击飞与击退
2. 记录多敌人数量、帧率与导航重算频率，建立性能基线
3. 单独设计 ECS 邻域数据与避障输出接口
4. 仅替换 EnemyMotor 前的局部避障计算，不改变 BT 与 Navigation 公共接口
5. 用同一巡逻场景对比接入前后的路径稳定性与 GC / 帧耗时
```

## 五、协作与资源约束

- 美术动画导入设置由用户配置，后续不要擅自修改 FBX Import Settings 或重新烘焙动画
- 受击动画时间继续从已有配置的 AnimationClip 时长读取，窗口只控制锁定与取消，不缩短无输入时的完整播放
- 用户不希望为此新增无关枚举、SO、抽象类或额外包装层；优先复用 `DamageReactionType`、`PlayerDamageConfigSO` 与现有 Ability Window 数据
- 动画资源位于 `Assets/Animation/`，该目录按现有规则不进入 Git；归档和提交时不要误以为动画资源已经被版本管理

## 六、工作区与提交注意事项

归档时 HEAD：

```text
9b43188 修复受击动画不播放完的bug
```

归档前工作区无代码改动。本文件为本轮新增文档；除非用户另行要求，不要因为归档自动创建提交。
