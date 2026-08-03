# Protocol_Evac 通用 CombatHitbox 开发前交接记录

## 一、记录范围

本记录接续：

[2-Player命名规范与HFSM装配收口记录.md](2-Player命名规范与HFSM装配收口记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

相关设计文档：

[../../玩家状态与敌人AI/技能系统与编辑器设计方案.md](../../玩家状态与敌人AI/技能系统与编辑器设计方案.md)

本记录保存 Combat Hitbox 的最新架构结论、当前实际进度、第一版完整建议代码，以及回家后继续古法编程的检查与接入顺序。

```text
1. 用户决定不创建 PlayerAttackHitbox，第一版直接实现可复用的 CombatHitbox
2. 已确认 Player.asmdef 成功添加对 Combat 程序集的单向引用
3. CombatHitbox.cs 当前尚未创建，本文中的代码只是待手写方案，不代表已经落盘
4. CombatHitbox 只负责检测、过滤、窗口内去重和提交 DamageData
5. Player 只负责普攻段数、每段伤害、命中窗口以及何时调用 Open / Close
6. 下一次开发先只完成 CombatHitbox 并确认编译，不同时接入 Player
```

## 二、本次确认的设计结论

### 1. 第一版采用通用 CombatHitbox

此前考虑过：

```text
PlayerAttackHitbox
```

用户认为 Hitbox 应优先做成通用能力。结合当前实际需求，`PlayerAttackHitbox` 暂时没有不可替代的 Player 专属行为，因此当前正式方向改为：

```text
Assets/Scripts/Module/Combat/Hitbox/CombatHitbox.cs
```

未来 Player、Enemy、陷阱或其他伤害来源都可以调用同一个 `CombatHitbox`，调用方只需提供本次攻击的伤害值与来源对象。

### 2. 通用 Hitbox 与 Player 的职责拆分

`CombatHitbox` 负责：

```text
OverlapBoxNonAlloc 范围检测
TargetLayers 目标层过滤
排除伤害来源自身及其子物体
同一个开启窗口内对 IDamageable 去重
计算 HitPoint 与 HitDirection
创建通用 DamageData
调用 IDamageable.TakeDamage
```

Player 负责：

```text
当前普攻段 NormalAttackIndex
每一段普攻的伤害值
每一段普攻的命中窗口
命中窗口何时打开与关闭
当前攻击应该使用哪个 Hitbox
连段输入、动画、状态切换与 Root Motion
```

明确禁止让 `CombatHitbox` 读取或依赖：

```text
PlayerContext
PlayerNormalAttackState
PlayerNormalAttackConfigSO
NormalAttackIndex
动画归一化时间
连段输入窗口
```

### 3. 连段窗口与命中窗口必须分开

当前 `PlayerStateClipData` 已有 `ComboOpenNormalizedTime` 与 `ComboCloseNormalizedTime`，其语义是：

```text
连段窗口
└─ 决定何时接受下一次普攻输入
```

后续需要新增的命中窗口语义是：

```text
命中窗口
└─ 决定何时打开 CombatHitbox 并执行伤害检测
```

两者不能共用同一组字段。玩家可能已经打中目标，但仍未进入连段输入窗口；也可能连段窗口已经打开，但这一段攻击的有效伤害帧已经结束。

### 4. DamageData 继续保持通用

当前 `DamageData` 保持：

```text
Damage
Source
HitPoint
HitDirection
```

不要加入 `NormalAttackIndex`、普攻段数或 Player 状态等专属字段。如果每段普攻伤害不同，应由 Player 侧先根据当前段落选出伤害值，再传给 `CombatHitbox.Open(...)`。

## 三、当前实际实现状态

### 1. Player 已引用 Combat

当前文件：

```text
Assets/Scripts/Module/Player/Player.asmdef
```

已新增 Combat 程序集引用：

```text
GUID:2e2028143becd3b47bc14ed467d11a06
```

该 GUID 与以下文件一致：

```text
Assets/Scripts/Module/Combat/Combat.asmdef.meta
```

依赖方向当前正确：

```text
Combat
   ↑
Player
```

`Player.asmdef` 当前处于未提交修改状态。下一次进入 Unity 后，仍需观察 Console，确认新增程序集引用没有造成编译问题。

### 2. Combat 基础契约已经存在

当前已有：

```text
Assets/Scripts/Module/Combat/
├─ Combat.asmdef
├─ Damage/
│  ├─ DamageData.cs
│  └─ IDamageable.cs
├─ Hitbox/
└─ Testing/
   └─ CombatTargetDummy.cs
```

其中：

```text
DamageData          单次通用伤害载荷
IDamageable         通用受击接口
CombatTargetDummy   临时受击测试对象
```

### 3. CombatHitbox 尚未创建

归档时已确认：

```text
Assets/Scripts/Module/Combat/Hitbox/CombatHitbox.cs
└─ 尚未创建
```

因此下一节代码是“回家后手写的建议版本”，不是已完成实现。手写保存后，应先让 Unity 完成编译，再由 AI 只读检查实际文件。

## 四、第一版 CombatHitbox 完整建议代码

目标文件：

```text
Assets/Scripts/Module/Combat/Hitbox/CombatHitbox.cs
```

建议手写内容：

```csharp
/*
 * ┌──────────────────────────────────┐
 * │  描    述: 通用战斗命中盒，负责范围检测、目标去重与伤害提交
 * │  类    名: CombatHitbox.cs
 * │  创    建: By qiqizizzz
 * └──────────────────────────────────┘
 */

using System.Collections.Generic;
using Module.Combat.Damage;
using UnityEngine;
using Utils.log;

namespace Module.Combat.Hitbox
{
    public sealed class CombatHitbox : MonoBehaviour
    {
        private const int OVERLAP_RESULT_CAPACITY = 32;

        [Tooltip("命中盒相对于当前 Transform 的半尺寸")]
        [SerializeField] private Vector3 HalfExtents = new Vector3(0.5f, 0.5f, 0.5f);

        [Tooltip("允许命中的目标 Layer")]
        [SerializeField] private LayerMask TargetLayers;

        private readonly Collider[] m_overlapResults = new Collider[OVERLAP_RESULT_CAPACITY];
        private readonly HashSet<IDamageable> m_hitTargets = new HashSet<IDamageable>();

        private GameObject m_source;
        private float m_damage;
        private bool m_isOpen;
        private bool m_hasWarnedCapacity;

        private void Awake()
        {
            if (HalfExtents.x <= 0f || HalfExtents.y <= 0f || HalfExtents.z <= 0f)
            {
                QLog.Error("CombatHitbox 的 HalfExtents 必须全部大于 0");
                enabled = false;
                return;
            }

            if (TargetLayers.value == 0)
            {
                QLog.Error("CombatHitbox 未配置 TargetLayers");
                enabled = false;
            }
        }

        private void FixedUpdate()
        {
            if (!m_isOpen)
                return;

            detectTargets();
        }

        private void OnDisable()
        {
            Close();
        }

        /// <summary>
        /// 开启一次新的命中窗口
        /// </summary>
        /// <param name="damage">本次命中的伤害值</param>
        /// <param name="source">伤害来源对象</param>
        public void Open(float damage, GameObject source)
        {
            if (damage <= 0f)
            {
                QLog.Error($"开启 CombatHitbox 失败，伤害值必须大于 0：{damage}");
                return;
            }

            if (source == null)
            {
                QLog.Error("开启 CombatHitbox 失败，伤害来源为空");
                return;
            }

            m_damage = damage;
            m_source = source;
            m_hitTargets.Clear();
            m_hasWarnedCapacity = false;
            m_isOpen = true;
        }

        // 关闭当前命中窗口
        public void Close()
        {
            m_isOpen = false;
            m_damage = 0f;
            m_source = null;
            m_hitTargets.Clear();
            m_hasWarnedCapacity = false;
        }

        // 检测当前命中盒范围内的有效受击对象
        private void detectTargets()
        {
            int hitCount = Physics.OverlapBoxNonAlloc(
                transform.position,
                HalfExtents,
                m_overlapResults,
                transform.rotation,
                TargetLayers,
                QueryTriggerInteraction.Ignore);

            if (hitCount == OVERLAP_RESULT_CAPACITY && !m_hasWarnedCapacity)
            {
                QLog.Warning($"CombatHitbox 检测结果达到容量上限：{OVERLAP_RESULT_CAPACITY}");
                m_hasWarnedCapacity = true;
            }

            for (int i = 0; i < hitCount; i++)
                tryApplyDamage(m_overlapResults[i]);
        }

        // 尝试对碰撞体所属的受击对象造成一次伤害
        private void tryApplyDamage(Collider hitCollider)
        {
            if (hitCollider == null)
                return;

            if (hitCollider.transform.IsChildOf(m_source.transform))
                return;

            IDamageable damageable = hitCollider.GetComponentInParent<IDamageable>();
            if (damageable == null || !m_hitTargets.Add(damageable))
                return;

            Vector3 hitPoint = hitCollider.ClosestPoint(transform.position);
            Vector3 hitDirection =
                (hitCollider.bounds.center - m_source.transform.position).normalized;

            DamageData damageData = new DamageData(
                m_damage,
                m_source,
                hitPoint,
                hitDirection);

            damageable.TakeDamage(damageData);
        }

        private void OnDrawGizmosSelected()
        {
            Matrix4x4 previousMatrix = Gizmos.matrix;
            Gizmos.color = Color.red;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, HalfExtents * 2f);
            Gizmos.matrix = previousMatrix;
        }
    }
}
```

## 五、第一版实现说明

### 1. Open 表示一个新的攻击命中窗口

每次调用：

```csharp
combatHitbox.Open(damage, source);
```

都会：

```text
保存本次伤害值
保存伤害来源
清空上一窗口的命中目标
开启 FixedUpdate 检测
```

因此同一个窗口持续多个物理帧时，同一 `IDamageable` 只会被命中一次；关闭后再次 `Open` 会开始一个全新的去重窗口。

### 2. Close 必须结束当前窗口

调用：

```csharp
combatHitbox.Close();
```

会停止检测并清理当前窗口数据。以后 Player 状态退出、攻击中断、组件禁用时，都必须确保 Hitbox 被关闭，避免命中窗口残留。

### 3. 第一版使用 Box 检测

当前只实现：

```text
Physics.OverlapBoxNonAlloc
```

不在第一版同时支持 Sphere、Capsule 或任意 Collider 形状。等 Box 方案完成单段普攻验证后，再根据真实武器与敌人碰撞表现决定是否扩展。

### 4. 第一版去重对象是 IDamageable

目标可能有多个 Collider，但它们向上找到的是同一个 `IDamageable`。使用：

```text
HashSet<IDamageable>
```

可以避免同一角色因多个碰撞体在一个窗口内重复受伤。

### 5. QueryTriggerInteraction 当前设为 Ignore

建议代码当前只检测非 Trigger Collider：

```csharp
QueryTriggerInteraction.Ignore
```

因此后续场景中的 `CombatTargetDummy` 必须有可被查询的非 Trigger Collider。如果项目最终决定 Hurtbox 使用 Trigger Collider，需要在实际场景方案确认后调整该参数，不要在未确认碰撞体结构前同时兼容两套行为。

## 六、后续 Player 接入边界

`CombatHitbox` 编译通过以后，才进入 Player 侧桥接。推荐数据流：

```text
PlayerNormalAttackState
└─ 根据当前段与归一化时间产生命中窗口意图

Player 战斗子模块
├─ 根据 NormalAttackIndex 读取当前段伤害
├─ 命中窗口开始时调用 CombatHitbox.Open(damage, playerGameObject)
└─ 命中窗口结束或状态退出时调用 CombatHitbox.Close()

CombatHitbox
├─ OverlapBoxNonAlloc
├─ Layer 过滤与自身排除
├─ 同窗口 IDamageable 去重
├─ 创建 DamageData
└─ IDamageable.TakeDamage()
```

Player 侧适配类型暂时可考虑：

```text
PlayerCombatController
```

但当前不要立即创建。应先确认 `CombatHitbox` 本身编译通过，再结合现有 `PlayerController`、`PlayerContext` 与状态调度决定最小接法，避免提前多建一层却没有明确职责。

## 七、普攻配置后续需要新增的数据

当前：

```text
PlayerNormalAttackConfigSO
├─ 动画段落与持续时间
├─ 普攻输入缓存时间
├─ 是否锁定移动
├─ 退出混合时长
├─ Root Motion 段索引
└─ 连段输入窗口
```

尚缺：

```text
每段普攻伤害值
每段命中窗口开始时间
每段命中窗口结束时间
```

后续可能放入每个 `PlayerStateClipData` 的攻击扩展数据，或建立独立的普攻段数据。这个数据结构尚未最终确认，不要在写 `CombatHitbox` 时顺便修改 `PlayerStateClipData`，也不要把命中窗口塞进现有 Combo 字段。

## 八、回家后的古法编程顺序

严格按以下小步推进：

```text
1. 打开 Unity，等待程序集刷新完成
2. 确认 Player 引用 Combat 后 Console 没有编译错误
3. 在 Assets/Scripts/Module/Combat/Hitbox/ 创建 CombatHitbox.cs
4. 手写本文第四节的建议代码并保存
5. 等待 Unity 编译完成
6. 暂时不要把组件挂到 Player，也不要修改 PlayerNormalAttackState
7. 将“已写好”告知 AI，由 AI 只读检查实际落盘文件与 Unity Console
8. CombatHitbox 单体代码确认后，再讨论每段伤害与命中窗口数据结构
9. 之后才接 Player 开窗与 CombatTargetDummy 场景验证
```

第一轮验证目标保持极小：

```text
一段普攻
一个 CombatTargetDummy
一个命中窗口
同一目标只受到一次伤害
```

不要同时实现：

```text
Enemy AI
正式 Enemy 血量与死亡
硬直和击退
伤害类型与抗性
全局 CombatSystem
完整 Skill Runner
三段连击全部命中配置
Sphere / Capsule 多形状 Hitbox
```

## 九、当前尚未完成

```text
尚未确认 Player 新增 Combat 引用后的 Unity Console 状态
CombatHitbox.cs 尚未创建
CombatHitbox 建议代码尚未经过 Unity 实际编译
CombatHitbox 尚未挂载到任何 GameObject
尚未确定 Player 侧开窗桥接的最终类型与位置
PlayerNormalAttackConfigSO 尚无每段伤害数据
Player 配置尚无独立命中窗口数据
尚未配置目标 Layer
尚未在场景放置并配置 CombatTargetDummy 的 Collider
尚未完成 Play Mode 单段命中验证
```

## 十、工作区注意事项

本次归档前 `git status --short` 显示：

```text
D Assets/Lua.meta
D Assets/Scripts/Common.meta
D Assets/Scripts/Framework/QLua.meta
M Assets/Scripts/Module/Player/Player.asmdef
D Assets/Scripts/Net.meta
D Assets/Scripts/Tools/GM.meta
D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639210032524788533.backup
```

其中：

```text
Player.asmdef 的修改是本次已确认的 Combat 引用
其余 .meta 删除与 SceneBackups 文件不是本次 Combat Hitbox 工作
```

后续不要把无关删除和场景备份混入 Combat Hitbox 的提交，也不要擅自恢复或清理这些用户工作区内容。
