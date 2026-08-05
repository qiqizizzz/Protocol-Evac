# Protocol_Evac 千咲普通闪避动画 Land_Roll 定案记录

## 一、记录范围

本记录接续：

[2026-8-5/2-QTower控制器生命周期与Player模块接入记录.md](../2026-8-5/2-QTower控制器生命周期与Player模块接入记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存新版千咲普通闪避动画的最终选型、当前正式资源绑定和验证结论，供后续继续维护 Player Action 动画时直接接续。

## 二、本次最终决定

普通闪避最终采用：

```text
Assets/Animation/千咲/Raw/Action/Land_Roll.fbx
└─ AnimationClip: roll
```

`Sp_Dodge_F_A.fbx` 已排除。该动作实际属于特殊技能的飞天动作，不应再作为普通闪避候选或绑定到 `Action/dodge`。

后续默认结论：

```text
普通闪避 = Land_Roll
特殊飞天动作 = Sp_Dodge_F_A
```

除非用户重新调整设计，不再继续搜索或替换普通闪避动画。

## 三、当前正式资源状态

### 1. Land_Roll 导入状态

当前 `Land_Roll.fbx` 的关键导入数据为：

```text
Clip Name: roll
Frame Range: 0 - 55
Loop Time: 关闭
Animation Type: Generic
Avatar Setup: Copy From Other Avatar
Source Avatar: 新版千咲 ChisakiAvatar
```

### 2. Animator 绑定

当前 Animator Controller：

```text
Assets/Animation/千咲/千咲_Animator.controller
```

正式状态路径与动画绑定：

```text
Base Layer.Action.dodge
└─ Motion: Land_Roll / roll
```

`PlayerAnimWriter` 仍通过 `Base Layer.Action.dodge` 发起一次性闪避动画播放请求，状态路径没有新增或改名，也没有增加 Animator 参数。

### 3. 闪避配置现状

当前配置：

```text
Assets/Config/Player/Action/PlayerDodgeConfig.asset
├─ State Clip: Land_Roll / roll
├─ Dodge Duration: 1.8333334 秒
├─ Dodge Speed: 9
├─ Dodge Input Threshold Sqr: 0.01
└─ Dodge Buffer Time: 0.18 秒
```

`StateClipValue` 与 Animator 的 `Action/dodge` Motion 当前均已指向 `Land_Roll`，闪避状态计时与实际动画资源已经统一。以后若重新裁剪动画帧范围，需要同步刷新 `DodgeDuration`。

## 四、验证结论

用户已在 Unity 中完成实际验证，并确认：

```text
Land_Roll 可以正常作为普通闪避动作使用
Animator 中的 Action/dodge 绑定正确
运行时闪避表现已验证成功
本轮不需要继续调整状态机、代码或 Animator 参数
```

本结论以用户的完整运行验证为准，不再把临时候选预览结果作为正式依据。

对应正式资源绑定已位于提交：

```text
d19ac56 绑定roll动画
```

## 五、后续维护边界

```text
PlayerDodgeState 继续负责闪避计时与强制位移意图
PlayerAnimWriter 继续负责请求播放 Base Layer.Action.dodge
Animator Controller 负责将 Action/dodge 映射到 Land_Roll
动画根运动、根旋转或首帧异常优先从 FBX Import Settings 排查
```

当前没有待实现的普通闪避动画任务。后续只有在手感或位移不同步时，才需要联合检查 `Land_Roll` 的根运动、`DodgeSpeed` 与 `DodgeDuration`。

## 六、工作区注意事项

归档前工作区存在以下与临时候选审计清理有关的状态：

```text
M Assets/Config/Player/Action/PlayerDodgeConfig.asset
D Assets/__CodexTempDodgeAudit.meta
```

`PlayerDodgeConfig.asset` 的修改是正式 `Land_Roll` 配置的一部分，当前尚未提交。临时审计目录的 `.meta` 曾被提交 `d19ac56` 带入，但对应目录当前已不存在。本次归档不恢复或提交这些状态，也不继续修改正式动画资源。
