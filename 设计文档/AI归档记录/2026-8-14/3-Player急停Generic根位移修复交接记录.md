# Protocol_Evac Player 急停 Generic 根位移修复交接记录

## 一、记录范围

本记录接续：

[2-TokyoStreet迁移与流浪者材质绑定交接记录.md](./2-TokyoStreet迁移与流浪者材质绑定交接记录.md)

主设计文档：

[玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存 Player 急停动画有骨骼位移但角色原地播放的问题、最终代码适配方案与 Unity MCP 验证结果。它不包含 Enemy AI 实现，也没有修改动画资源。

## 二、问题根因

急停动画位于：

```text
Assets/Animation/千咲/Clips/Grounded/Stop/
├─ Stop_Walk_L.anim
├─ Stop_Walk_R.anim
├─ Stop_Run_L.anim
├─ Stop_Run_R.anim
├─ Stop_Sprint_L.anim
└─ Stop_Sprint_R.anim
```

Unity MCP 通过 `AnimationUtility` 检查六个旧 Clip 后确认：

```text
AnimationClip.hasRootCurves = false
AnimationClip.humanMotion = false
Animator.deltaPosition = (0, 0, 0)
```

这些动画是 Generic 骨架动画，实际位移保存在：

```text
Root/m_LocalPosition.z
```

例如：

```text
Stop_Run_L    Root.z: 0 -> -148.7768 模型单位
Stop_Run_R    Root.z: 0 -> -169.8316 模型单位
Stop_Sprint_R Root.z: 0 -> -234.6092 模型单位
```

模型节点缩放为 `0.01`。因此动画确实包含位移，但 Unity 没有将它提取为标准 `Animator.deltaPosition`。

原 `PlayerRootMotionReceiver` 在 `OnAnimatorMove` 后还会将 `Root` 节点 X/Z 恢复为锚点，避免模型骨骼自行漂移。由于没有先消费 Generic `Root` 曲线，这一步等于将急停动画位移完全抹掉。

松开 WASD 不是根因。输入释放后 `PlayerStopState` 会清空移动意图，但此前保存的 `Movement.Velocity` 仍可减速；真正缺失的是 Generic 动画位移到 `CharacterController` 的传递路径。

## 三、最终实现

修改文件：

```text
Assets/Scripts/Module/Player/HFSM/States/Ground/PlayerStopState.cs
Assets/Scripts/Module/Player/HFSM/Animation/PlayerRootMotionReceiver.cs
```

当前流程：

```text
PlayerStopState.Enter
└─ 开启 IsRootMotionMoveEnabled

PlayerRootMotionReceiver.OnAnimatorMove
├─ 优先读取标准 Animator.deltaPosition
└─ 标准 Root Motion 有效时写入 PlayerActionContext

PlayerRootMotionReceiver.LateUpdate
├─ 在 Animator 完成姿势求值后读取 Generic Root 节点
├─ 计算当前帧与上一帧的 Root localPosition 差值
├─ 使用 Animator Transform 将局部差值转换为世界空间位移
├─ 清除 Y，只保留水平根位移
├─ 写入 PlayerActionContext.RootMotionDeltaPosition
└─ 采样完成后恢复 Animator 与 Root 节点锚点，防止视觉节点重复位移

PlayerMotor.FixedTick
├─ 消费 RootMotionDeltaPosition
└─ 通过 CharacterController.Move 执行最终位移和重力

PlayerStopState.Exit
└─ 关闭 Root Motion 并清空未消费位移
```

`PlayerRootMotionReceiver` 会优先使用 Unity 标准 Root Motion；只有 `Animator.deltaPosition` 无有效位移时，才使用 Generic `Root` 节点差值，避免双重应用。

## 四、验证结果

Unity MCP 在 `Assets/Scenes/GameScene.unity` 的 PlayMode 中注入了仅运行时存在的 Input System 诊断组件，按真实路径执行：

```text
按住 W
-> GroundedMove
-> 持续移动
-> 松开 W
-> GroundedStop
-> 播放 Stop_Run_R
-> CharacterController 继续产生急停位移
```

关键采样结果：

```text
Animator.deltaPosition 始终为 (0, 0, 0)
松键后 Player 根节点累计位移约 0.143 米
用户在实际 Game View 中确认急停位移已经修复
```

代码刷新与编译通过。MCP 诊断组件只存在于 PlayMode，没有写入场景或项目文件。

## 五、资源与架构边界

本次没有执行以下操作：

```text
1. 没有修改任何 .anim
2. 没有重新导入或替换 FBX
3. 没有修改外部原始资源目录
4. 没有新增或改写 PlayerController.InitAnim()
5. 没有改变 PlayerMotor 作为最终位移执行入口的职责
```

外部原始资源目录仍应保持只读参考：

```text
E:\Download\Art\全套千咲模型动画\千咲\动画
```

后续处理 Generic 动画时，不应仅凭动画窗口中“存在位移”就断言 Unity 会生成 `Animator.deltaPosition`。应同时检查 `AnimationClip.hasRootCurves`、实际曲线路径与模型层级缩放。

## 六、当前状态与下一步

急停修复已进入当前提交：

```text
commit: 4ede9f8ab953542827492c775c0b759c3e85d3d7
subject: 修复急停唯一
```

Player 急停问题已由用户确认完成。下一步回到 Enemy 模块，不再继续改急停动画资源：

```text
1. 基于 Enemy_流浪者.prefab 建立 Enemy 运行时入口和 Context
2. 先完成感知、追击、攻击、受击、死亡闭环
3. 复用通用 Ability 数据与编辑器能力
4. 再接入通用行为树编辑器、A* 寻路与后续 ECS 群体避障
```

## 七、工作区注意事项

归档时急停代码已在 `HEAD`，不是未提交临时修改。Unity MCP 在归档阶段已无法连接，当前不能据此断言 Unity Editor 仍在运行；最后一次成功的 PlayMode 验证结果已记录在上文。

项目仍可能存在此前环境、Enemy 资源、场景和本地受许可限制资源的未提交或被忽略内容。后续提交 Enemy 工作时应按归属逐项检查，禁止用整体回退覆盖用户资源。
