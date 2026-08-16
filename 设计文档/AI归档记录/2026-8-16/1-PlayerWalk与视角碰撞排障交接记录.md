# Protocol_Evac Player Walk 与视角碰撞排障交接记录

## 一、记录范围

本记录接续：

[1-Enemy行为树三段攻击与巡逻导航交接记录.md](../2026-8-15/1-Enemy行为树三段攻击与巡逻导航交接记录.md)

主设计文档：

[玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本记录保存 Player Walk 循环空白帧、第一/第三人称相机近裁面、自身模型渲染和建筑碰撞排障的当前状态。Enemy 行为树、攻击和巡逻不属于本轮改动范围。

## 二、本次确认的事实

### 1. Walk 的一帧僵直来自资源裁剪，不是移动速度丢失

`Chisaki_walk` 的有效动作只有 38 帧，但原裁剪终点在第 48 帧。第 38 到第 48 帧为静止姿势，循环时表现为左脚迈出后停一帧再进入下一轮。

当前资源已确认：

```text
Assets/Animation/千咲/Clips/Grounded/Chisaki_walk.anim
├─ Frame Rate：30
├─ Stop Time：1.2666668 秒
├─ 有效帧数：38
├─ Loop Time：开启
├─ OnRightFootPlant：1.1 秒
└─ OnLeftFootPlant：1.2333333 秒
```

落脚事件由 `PlayerRootMotionReceiver` 写入 `PlayerMovementContext.RecordPlantedFoot`，用于急停表现判断；两事件当前都处于有效帧范围内。

### 2. 第一人称看到自身模型的原因已确认

Player 根节点和两个 Renderer 都在 `Player` Layer（Layer 7），而相机初始 Culling Mask 为 `-1`，即渲染所有 Layer。相机收回角色胶囊体后，向下看会直接看到自身模型。

第一人称需要排除 `Player` Layer，第三人称必须恢复初始 Culling Mask。

### 3. 侧看贴墙看到墙后内容的原因已确认

相机原始 Near Clip Plane 为 `0.3`。在第一人称侧看贴近墙面时，近处墙体会先被裁掉，露出墙后的场景，不是 CharacterController 忽略了墙碰撞。

### 4. 第三人称垂直下视导致角色离开画面的原因已确认

第三人称相机围绕玩家根部旋转，原统一的最高下视角为 `75` 度。接近垂直下视时，玩家被推至画面下方。第一人称和第三人称不能继续共享同一个最高下视角。

## 三、当前实现状态

### 1. 视角控制器

```text
Assets/Scripts/Module/Player/Core/View/PlayerViewController.cs
├─ 缓存 Camera 初始 Culling Mask 与 Near Clip Plane
├─ 第一人称
│  ├─ Culling Mask 排除 Player Layer
│  ├─ 使用 FirstPersonCameraNearClipPlane
│  └─ 使用第一人称安全镜头位置
├─ 第三人称
│  ├─ 恢复初始 Culling Mask 与 Near Clip Plane
│  ├─ 使用相机高度作为 SphereCast 枢轴
│  └─ 命中后只缩短后拉距离，不再回退到玩家根节点
└─ 俯仰角
   ├─ 第一人称：PitchMax
   └─ 第三人称：ThirdPersonPitchMax
```

第三人称碰撞检测仍使用 `Building + Ground` LayerMask；不要创建或改用 `CameraObstacle` Layer。

### 2. 视角配置

```text
Assets/Config/Player/View/PlayerViewConfig.asset
├─ FirstPersonCameraLocalPosition：(0, 1.55, 0)
├─ FirstPersonCameraNearClipPlane：0.03
├─ ThirdPersonCameraLocalPosition：(0, 1.45, -4)
├─ ThirdPersonCameraCollisionRadius：0.32
├─ CameraCollisionPadding：0.08
├─ ThirdPersonPitchMax：55
└─ EnvironmentLayerMask：Building + Ground（768）
```

`PlayerViewConfigSO` 已增加 `FirstPersonCameraNearClipPlane` 与 `ThirdPersonPitchMax` 对应的序列化字段和只读属性。

## 四、场景与 Layer 约束

```text
Player Layer：7
Building Layer：8
Ground Layer：9
```

用户明确要求：

```text
相机遮挡与建筑类物体使用 Building
地面相关使用 Ground，供后续导航使用
不要命名或新建 CameraObstacle Layer
不要删除动画源文件或未确认资源
```

`ViewRoot` 没有挂 MonoBehaviour 是当前架构设计：`PlayerViewController` 继承 `BaseController`，由 `PlayerController` 初始化并在 `Update` 中调度，不应误说明为必须挂载到 `ViewRoot` 的组件。

## 五、验证与未完成项

已验证：

```text
1. Chisaki_walk 已为 38 帧，Loop 与两条落脚事件均在有效时段
2. Player 两个 Renderer 均在 Player Layer
3. PlayerViewConfig 的 Building + Ground 掩码为 768
4. Unity AssetDatabase 已在最新视角配置改动后刷新
```

尚未完成最终用户验收：

```text
1. 第一人称侧看贴墙：确认 0.03 Near Clip Plane 后不再显示墙后内容
2. 第一人称向下看：确认 Player Layer 排除后不再显示自身模型
3. 第三人称连续贴墙、进建筑、下楼梯：确认不闪烁、不穿墙、不看到天花板外侧
4. 第三人称最大下视角：确认 55 度时角色持续位于画面内
```

如果仍有相机问题，先用 MCP 在对应复现场景读取以下运行时事实，再改代码：

```text
Camera.cullingMask
Camera.nearClipPlane
PlayerViewMode
Camera 与 Building/Ground Collider 的 OverlapSphere / SphereCast 命中结果
Player CharacterController 的位置、Height、Radius、Center
```

不要再次把检测失败统一回退到玩家根节点；该行为会导致相机闪烁和看到不应显示的场景。

## 六、提交与工作区注意事项

归档时 HEAD：

```text
2b8d1b1 第三人称初步修复
```

归档前 `git status --short` 无输出，运行时代码和配置不在未提交工作区。新增本归档文档后需按文档归属确认是否单独提交。

