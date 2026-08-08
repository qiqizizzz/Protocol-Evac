# Protocol_Evac Player 急停脚步事件与 Grounded 混合树交接记录

## 一、记录范围

本记录接续：

[1-千咲Attack03收招武器曲线校准记录.md](../2026-8-8/1-千咲Attack03收招武器曲线校准记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存本轮 Player 地面移动 Animator 修复、疾跑配置收口，以及急停动作与移动脚步相位的当前实现和未完成校准项。

## 二、本次确认的设计与协作边界

用户确认：

```text
急停动作不能随机或固定左右轮换
├─ 必须依据当前移动动画最近一次实际落地的脚
└─ 急停应选择另一侧起脚的 Stop Clip，避免同脚连续导致抽搐

动画资源边界
├─ 不得修改 FBX 骨骼、关键帧、Clip 名称、时长、循环或根运动
├─ 本次允许添加 Animation Event
└─ 脚步事件帧由用户在 Unity Inspector 中最终手动校准
```

AI 基于骨骼高度采样写入的事件帧仅为初始定位，不是视觉定稿。后续不得根据当前帧号声称脚步衔接已经完成，也不要擅自继续调整事件时间。

## 三、Grounded Animator 当前状态

目标 Controller：

```text
Assets/Animation/千咲/千咲_Animator.controller
└─ Base Layer
   └─ Grounded_Common
      └─ Grounded_Locomotion
         └─ GroundedLocomotion (Simple 1D, parameter: lockOnWeight)
            ├─ threshold 0: FreeLocomotion
            │  └─ idle / walk / run / Sprint
            └─ threshold 1: LockOnLocomotion
               └─ idle / Run_F / Run_B / Run_LF / Run_RF / Run_LB / Run_RB
```

此前 `Grounded_Locomotion.m_Motion` 曾为空，导致 `Run` 和锁定方向动作在 Animator 中消失。本轮已恢复为上述外层混合树结构。不要将 `Grounded_Common` 改成替代 `Grounded_Locomotion` 的普通状态机入口，也不要重建或改名已有动画树。

## 四、移动配置当前状态

```text
Assets/Config/Player/Move/PlayerMoveConfig.asset
└─ 状态动画段落 StateClipValues
   ├─ [0] idle
   ├─ [1] walk
   ├─ [2] run
   └─ [3] Sprint

Assets/Scripts/Module/Player/HFSM/Config/Move/PlayerMoveConfigSO.cs
└─ REQUIRED_STATE_CLIP_COUNT = 4
```

`Sprint` 已从独立 Inspector 字段迁入状态动画段落，Clip 引用和原时长 `0.53333336s` 原样保留。步行、奔跑、疾跑的 Stop 左右 Clip 继续保留在“急停动画段落”，不要混入循环移动动画列表。

## 五、急停脚步相位实现

当前数据流：

```text
循环移动 Animation Event
  -> PlayerRootMotionReceiver.OnLeftFootPlant / OnRightFootPlant
  -> PlayerMovementContext.RecordPlantedFoot(bool)
  -> HasLastPlantedFoot + IsLastPlantedFootLeft
  -> PlayerStopState.SelectStopUseLeftFoot()
  -> 选择最近落脚的反脚 Stop Clip
```

关键文件：

```text
Assets/Scripts/Module/Player/Context/Runtime/PlayerMovementContext.cs
├─ HasLastPlantedFoot
├─ IsLastPlantedFootLeft
└─ RecordPlantedFoot(bool)

Assets/Scripts/Module/Player/HFSM/Animation/PlayerRootMotionReceiver.cs
├─ OnLeftFootPlant()
└─ OnRightFootPlant()

Assets/Scripts/Module/Player/HFSM/States/Ground/PlayerStopState.cs
└─ 移除 m_useLeftFoot 的左右轮换，改为选择反脚急停
```

若尚未收到任何脚步事件，例如刚起步就立即急停，当前固定回退为右脚急停；它不是随机选择。正常跑动至少经过一次落脚事件后，急停选择由真实记录的脚决定。

## 六、Animation Event 当前初始值

以下仅是 AI 基于 `Bip001LFoot / Bip001RFoot` 相对高度最低点生成的初始值。用户已经明确表示当前视觉时机不够好，需要在 Unity 中手动微调：

```text
Assets/Animation/千咲/Raw/Grounded/Walk.fbx
└─ walk: Left F2, Right F18

Assets/Animation/千咲/Raw/Grounded/Run.fbx
└─ run: Left F0, Right F11

Assets/Animation/千咲/Raw/Grounded/Sprint.fbx
└─ Sprint: Left F14, Right F6

Assets/Animation/千咲/Raw/Grounded/LockOn/
├─ Run_B:  Left F0, Right F11
├─ Run_F:  Left F0, Right F11
├─ Run_LB: Left F0, Right F10
├─ Run_LF: Left F0, Right F10
├─ Run_RB: Left F0, Right F10
└─ Run_RF: Left F0, Right F10
```

事件方法名固定为：

```text
OnLeftFootPlant
OnRightFootPlant
```

手动校准路径：选中对应 FBX -> `Inspector > Animations` -> 选择目标 Clip -> `Events`。只前后拖动事件时刻，不要增删骨骼关键帧，不要修改 Event 方法名。每个循环 Clip 应保持一左一右两个事件。

校准原则：事件应放在该脚实际踩实并承重的画面，不是脚刚抬起迈出，也不是腿摆到最大幅度的画面。完成后应分别在左脚和右脚落地后松开移动键，检查急停是否接入反脚且没有抽搐。

## 七、验证与当前问题

已确认：

```text
9 个循环移动 FBX 的 .meta 均已写入脚步事件
PlayerRootMotionReceiver 已具备两个事件接收方法
Assembly-CSharp.dll 在本轮刷新后重新生成
```

未完成：

```text
脚步事件的实际画面时机尚未定稿
├─ 需要用户手动微调所有受影响 Clip 的 Event 帧
└─ 微调后需要在 Game View 实测左右相位下的 Run / Sprint 急停
```

本轮批量重导入后 Unity 发生域重载，Unity MCP 当前请求返回 `HTTP 500: Response data is null`。这不应作为脚步实现失败的结论；后续若继续依赖 MCP，需要先确认插件连接已恢复，再做自动化 Play Mode 验证。

## 八、工作区注意事项

归档时工作区包含本轮脚步事件和相关 C# 修改：

```text
Assets/Animation/千咲/Raw/Grounded/*.fbx.meta
Assets/Scripts/Module/Player/Context/Runtime/PlayerMovementContext.cs
Assets/Scripts/Module/Player/Core/PlayerController.cs
Assets/Scripts/Module/Player/HFSM/Animation/PlayerRootMotionReceiver.cs
Assets/Scripts/Module/Player/HFSM/States/Ground/PlayerStopState.cs
```

不要通过回退整个 `.fbx.meta` 来修改单个脚步事件，否则会连带移除本轮事件数据。应使用 Unity Inspector 逐个调整 Event 时刻。

