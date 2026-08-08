# Protocol_Evac 千咲 Attack03 收招武器曲线校准记录

## 一、记录范围

本记录接续：

[1-千咲普攻Begin-Recovery连段闭环记录.md](../2026-8-7/1-千咲普攻Begin-Recovery连段闭环记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

[../../战斗系统/战斗系统总开发文档.md](../../战斗系统/战斗系统总开发文档.md)

本记录保存 `attack03_end` 中 `WeaponProp01` 的收招段曲线校准结果、真实场景预览方法，以及后续继续调武器手持动画时必须遵守的边界。

## 二、本次确认的用户目标

`WeaponProp01` 在 Attack03 收招阶段必须：

```text
从 60 帧开始
├─ 武器与左手保持局部绑定，不锁死世界坐标
├─ 60-90 帧只允许一段连续的姿态过渡
├─ 91 帧后保持最终手持姿态
└─ 不允许再出现分段硬切、抖动或明显的武器震动
```

用户明确要求以 Unity 中的实际画面为准，不接受只根据曲线数值判断的修复。后续继续修改必须先逐帧预览，再写回动画。

## 三、最终动画资源状态

目标资源：

```text
Assets/Animation/千咲/Clips/Attack/attack03_end.anim
```

本次只修改 `WeaponProp01` 的以下曲线：

```text
Root/Bip001/Bip001Pelvis/Bip001Spine/Bip001Spine1/Bip001Spine2/
Bip001LClavicle/Bip001LUpperArm/Bip001LForearm/Bip001LHand/WeaponProp01
├─ m_LocalPosition.x / y / z
└─ m_LocalRotation.x / y / z / w
```

最终策略：

```text
60 帧
└─ 保留当前正确的起始手持姿态

61-90 帧
└─ 在 Bip001LHand 本地空间中，从 60 帧平滑过渡到最终手持姿态

91-154 帧
└─ 固定为最终手持姿态，随 Bip001LHand 自然移动

最后关键帧
└─ 原样保留，不写入
```

该片段帧率为 `30fps`。用户口中的末尾 `156` 帧，对应该动画曲线零起始计数的最终 `F155` 关键帧，时间为 `5.16666651s`。不要再重建或改写该最终关键帧。

`m_LocalScale.x / y / z` 未修改；为消除震动，最终版本没有在 `61-154` 帧继续叠加呼吸偏移。

## 四、真实场景与资源层级

当前预览场景：

```text
Assets/Scenes/GameScene.unity
└─ Hero_Chaisaki
   └─ Chisaki
      └─ Root/.../Bip001LHand/WeaponProp01
         └─ Chisaki_Weapon
            └─ R5Knife506Md20001
```

重要事实：`Chisaki_Weapon` 是场景层级中附加在 `WeaponProp01` 下的武器网格。单独实例化以下模型不会得到该网格，因此不能用它判断实际手持效果：

```text
Assets/Art/Models/千咲/Chisaki.fbx
```

## 五、MCP 视觉验证方法

已确认 `AnimationClip.SampleAnimation` 对当前场景克隆的显示结果不足以反映真实 `attack03_end` 曲线。后续预览应采用：

```text
克隆 Hero_Chaisaki/Chisaki（含场景附加的 Chisaki_Weapon）
  -> AnimationMode.StartAnimationMode
  -> AnimationMode.SampleAnimationClip(克隆对象, attack03_end, 帧时间)
  -> screenshot-isolated 查看实际武器网格
  -> 销毁临时克隆并 AnimationMode.StopAnimationMode
```

本次已实际检查 `60 / 70 / 80 / 89 / 90 / 91 / 120 / 155` 帧。修复后 `89 -> 90 -> 91` 的武器姿态连续，后段保持随左手移动，没有新增 Unity Console Error。

临时预览对象命名为 `__CodexAttack03Preview`，本次结束前已销毁，Animation Mode 已退出，不应遗留在场景或资产中。

## 六、后续调整边界

若仍需调手持画面，应遵守：

```text
优先修改 60-90 的单段连续插值
不要把 71-89 等中段强行锁回某个帧的局部姿态
不要把武器锁定在世界坐标
不要修改 m_LocalScale
不要修改最终 F155 关键帧
每次改动后至少复查 89、90、91 三帧
```

如果需要重新加入呼吸，应只在视觉稳定后以极小幅度叠加，并同时检查 Position 与 Rotation 的连续性；当前阶段不建议优先添加。

## 七、工作区注意事项

归档完成时，`git status --short` 仅显示本归档目录为未跟踪文件；`attack03_end.anim` 与 `GameScene.unity` 均没有未提交差异。该状态以归档完成时的工作区为准，后续继续调曲线前仍应先读取资产当前内容，不要假定本记录对应一个待提交的动画 diff。

临时预览使用 `HideAndDontSave` 对象且未保存场景。后续若 `GameScene.unity` 再次出现变更，不要将它与动画曲线改动混合提交、回退或覆盖，需先确认其归属。

本次没有创建或修改项目内的持久化 C# 源文件。
