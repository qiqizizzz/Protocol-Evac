# Protocol_Evac Combat 基础伤害契约与古法编程协作记录

## 一、记录范围

本记录接续：

[3-Player连段窗口配置与NaughtyAttributes接入记录.md](../2026-8-2/3-Player连段窗口配置与NaughtyAttributes接入记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录接续 Player 普攻命中判定前置工作，记录共享 `Combat` 模块、基础伤害数据契约、测试受击对象，以及用户确认的“古法编程”协作方式。

```text
1. 已在 Module 下建立独立 Combat 程序集
2. 已建立 DamageData 与 IDamageable 最小伤害契约
3. 已建立 CombatTargetDummy 作为临时受击测试对象
4. Combat 不依赖 Player，后续由 Player 单向引用 Combat
5. 用户明确不采用 DamageInfo 命名，当前统一使用 DamageData
6. 用户希望亲自手写代码，AI 负责给出步骤、代码方案和落盘后的检查
```

## 二、本次确认的设计 / 协作偏好

### 1. 共享 Combat 模块独立于 Player

当前目录：

```text
Assets/Scripts/Module/Combat/
├─ Combat.asmdef
├─ Damage/
│  ├─ DamageData.cs
│  └─ IDamageable.cs
└─ Testing/
   └─ CombatTargetDummy.cs
```

依赖方向保持为：

```text
Combat
   ↑
Player

未来：
Combat
   ↑
Enemy
```

`Combat` 不引用 `Player` 或具体 Enemy 类型，避免共享伤害协议反向依赖角色模块。

### 2. 伤害载体使用 Data 命名

用户明确不接受：

```text
DamageInfo
```

当前采用：

```text
DamageData
```

后续新增同类纯数据结构时，优先使用语义明确的 `xxData` 命名；不要擅自改回 `Info`。如果未来建立请求、结果或事件管线，再按真实职责使用 `Request`、`Result` 或 `EventData`。

### 3. 古法编程协作方式

当前用户希望亲自创建目录、程序集和 C# 文件，并手动输入代码。后续默认协作方式为：

```text
1. AI 先说明本步目标和文件位置
2. AI 给出建议代码与关键约束
3. 用户手动编写并保存
4. AI 只读检查实际落盘内容
5. 小步确认编译后再进入下一层
```

除非用户另外明确要求，AI 不直接替用户写入 C# 文件。

## 三、当前实现状态

### 1. Combat 程序集

当前文件：

```text
Assets/Scripts/Module/Combat/Combat.asmdef
```

程序集名称为 `Combat`，当前引用 `Utils`：

```text
GUID:1e6a16ebd49fdb74fad1843f95457608
```

该引用用于 `CombatTargetDummy` 调用项目统一日志 `QLog`。当前 `Combat` 未引用 `Player`。

### 2. DamageData

当前文件：

```text
Assets/Scripts/Module/Combat/Damage/DamageData.cs
```

当前为不可变值类型：

```text
readonly struct DamageData
├─ Damage
├─ Source
├─ HitPoint
└─ HitDirection
```

当前只承载一次命中的基础事实，不负责扣血、伤害结算、硬直、死亡或事件分发。本阶段没有提前加入 `DamageType`、击退参数或复杂战斗标签。

### 3. IDamageable

当前文件：

```text
Assets/Scripts/Module/Combat/Damage/IDamageable.cs
```

当前最小接口：

```text
IDamageable
└─ TakeDamage(DamageData damageData)
```

Hitbox 后续只依赖该接口，不直接依赖 Enemy 类。

### 4. CombatTargetDummy

当前文件：

```text
Assets/Scripts/Module/Combat/Testing/CombatTargetDummy.cs
```

当前行为：

```text
Awake
└─ CurrentHealth = MaxHealth

TakeDamage
├─ 非法的非正伤害使用 QLog.Error 并返回
├─ 扣除 CurrentHealth
├─ 使用 Mathf.Max 限制最低为 0
└─ 使用 QLog.Info 输出伤害值与剩余生命值
```

该组件是第一版伤害闭环的临时测试对象，不代表正式 Enemy 生命、受击、硬直或死亡系统已经实现。

## 四、关键架构边界

后续普攻命中闭环继续保持：

```text
PlayerNormalAttackState
└─ 只写当前攻击段与命中窗口意图

PlayerAttackHitbox
├─ 执行范围检测
├─ 对同一窗口内的目标去重
└─ 创建 DamageData 并调用 IDamageable.TakeDamage

IDamageable
└─ 接收通用伤害数据

CombatTargetDummy
└─ 仅用于验证受击协议

PlayerMotor
└─ 继续只执行移动与 Root Motion
```

当前阶段不创建全局 `CombatSystem`，不把 Hitbox 逻辑写入 `PlayerNormalAttackState`，也不让 `Combat` 依赖具体角色模块。

## 五、当前尚未完成

```text
尚未在本次归档中确认 Unity Console 最终编译状态
Player.asmdef 尚未引用 Combat
尚未创建 PlayerAttackHitboxIntent
尚未创建 PlayerAttackHitbox
尚未建立普攻每段的伤害与命中窗口配置
尚未在场景中配置测试目标、Collider 和目标 Layer
尚未完成单段普攻命中 CombatTargetDummy 的 Play Mode 验证
尚未接入正式 Enemy 生命值、受击、硬直或死亡逻辑
```

## 六、下一步建议

继续按古法编程小步推进：

```text
1. 先确认 Unity Console 无 Combat 编译错误
2. 在 Player.asmdef 中添加对 Combat 的单向引用
3. 在 Player/Combat 下创建 PlayerAttackHitboxIntent
4. 创建 PlayerAttackHitbox，先完成 Overlap 与窗口内目标去重
5. 为 Player 普攻增加最小的伤害与命中窗口配置
6. 在场景放置 CombatTargetDummy 与 Collider
7. 先验证一段普攻只对同一目标造成一次伤害
8. 单段稳定后再接入 attack01 / attack02 / attack03
```

不要在下一步同时实现完整 Skill Runner、Enemy AI、硬直、死亡和全局伤害系统。

## 七、工作区注意事项

Combat 基础模块已提交：

```text
ce372f1 Damage基础伤害数据载体和测试类
```

本次归档创建前，`Combat` 目录没有未提交差异。工作区仍有以下与本次 Combat 开发无关的状态：

```text
D  Assets/Lua.meta
D  Assets/Scripts/Common.meta
D  Assets/Scripts/Framework/QLua.meta
D  Assets/Scripts/Net.meta
D  Assets/Scripts/Tools/GM.meta
D  Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639210032524788533.backup
```

后续不要把这些删除和场景备份误当作 Combat 改动处理。
