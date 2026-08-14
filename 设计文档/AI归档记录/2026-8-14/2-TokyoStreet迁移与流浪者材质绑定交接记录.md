# Protocol_Evac Tokyo Street 迁移与流浪者材质绑定交接记录

## 一、记录范围

本记录接续：

[1-TokyoStreet场景导入与USD地图调研交接记录.md](./1-TokyoStreet场景导入与USD地图调研交接记录.md)

本记录保存 Tokyo Street 环境迁移到战斗场景，以及 Enemy_流浪者 外观资源绑定的当前结果。它不包含 Enemy AI、行为树、数值或攻击流程的实现。

## 二、场景环境当前状态

已将 `Assets/Tokyo_Street/Scenes/Day_Demo.unity` 的环境整理为一个总 Prefab：

```text
Assets/Prefabs/Environment/TokyoStreet_DayEnvironment.prefab
```

该 Prefab 已实例化到：

```text
Assets/Scenes/GameScene.unity
└─ GameScene
   └─ TokyoStreet_DayEnvironment
      ├─ Props
      ├─ Terrain
      ├─ Environment
      ├─ Decals
      ├─ Street
      ├─ Background_(Not_Interior)
      ├─ House
      └─ Lighting
```

`Day_Demo` 也保留相同的 Prefab 实例，原场景根节点只保留其示例所需的对象：

```text
Main Camera
Directional Light
Global Volume
Hero_Chaisaki
TokyoStreet_DayEnvironment
```

迁移时未复制 `Main Camera`、`Directional Light` 和 `Global Volume` 到 `GameScene`，因为 `GameScene` 已有玩家相机、Directional Light 和 Global Volume；复制会导致多相机、重复光源或重复后处理。反射探针保留在环境 Prefab 的 `Lighting` 根节点中。

已通过 `GameScene` 玩家相机完成非空画面捕获。天空盒、雾等 `RenderSettings` 属于 Scene 而非 Prefab；如需进一步接近 `Day_Demo` 的氛围，应在 `GameScene` 单独调整，而不是往环境 Prefab 内重复塞全局对象。

## 三、Enemy_流浪者 贴图与材质状态

用户确认贴图素材无问题。当前敌人 Prefab：

```text
Assets/Prefabs/Character/Enemy_流浪者.prefab
```

其唯一 Renderer 已从 FBX 内嵌的白色材质替换为独立的 URP 材质：

```text
Assets/Art/Models/Enemy-流浪者/Materials/Enemy_流浪者.mat
├─ Shader: Universal Render Pipeline/Lit
├─ Base Map: T_MO1LiufangzhemanMd00001_D.png
└─ Normal Map: T_MO1LiufangzhemanMd00001_N.tga
```

贴图本机路径：

```text
Assets/Art/Textures/Enemy-流浪者/
├─ T_MO1LiufangzhemanMd00001_D.png
└─ T_MO1LiufangzhemanMd00001_N.tga
```

法线贴图已经按 `TextureImporterType.NormalMap` 导入。已确认 Prefab Renderer 的材质引用、`_BaseMap`、`_BumpMap` 与 URP/Lit Shader 均正确，因此“流浪者显示为纯白”的贴图缺失问题已处理。

没有用外部目录中的 `fsx/Enemy-流浪者.fbx` 覆盖项目现有重绑 FBX；两者不是同一文件，保留当前模型可以避免破坏已经适配的动画。

## 四、资源许可与仓库边界

纹理来源目录中的 MMD Readme 明确限制二次配布、商用与暴力用途，且标注原始版权归库洛科技。该套本机资源不能上传 GitHub 或作为可分发项目资源。

`.gitignore` 已添加以下本地资源规则：

```text
/Assets/Tokyo_Street/
/Assets/Tokyo_Street.meta
/Assets/Art/Models/Enemy-流浪者/
/Assets/Art/Textures/Enemy-流浪者/
/Assets/Prefabs/Character/Enemy_流浪者.prefab
```

后续若要公开仓库或发布构建，必须先替换为权利清晰、允许该用途与再分发的模型和贴图，并重新配置 Enemy Prefab 材质。

## 五、当前尚未完成

```text
1. Enemy AI 模块尚未实现
2. 尚未将 Enemy_流浪者 的移动、追击、攻击、受击、死亡状态接入运行时
3. 尚未在 TokyoStreet_DayEnvironment 中确定最终战斗测试区域、碰撞和敌人出生点
4. GameScene 的天空盒、雾、光照氛围尚未按 Day_Demo 单独校准
```

用户已反复确认：Player 攻击的伤害流程已经验证正确，后续重点应直接进入完整 Enemy 模块，不要再次要求先验证玩家攻击。

## 六、下一步建议

```text
1. 在 GameScene 的 TokyoStreet_DayEnvironment 内确定一块平整的战斗区域
2. 用现有 Enemy_流浪者.prefab 建立 Enemy 运行时根组件和基础数据
3. 先实现可移动、感知 Player、追击、攻击、受击、死亡的完整闭环
4. 再接入动画状态和血条 UI，最后补出生点、巡逻与调参
```

## 七、工作区注意事项

当前工作区存在用户已有的多项未提交改动，包括 Player Prefab 删除/重命名痕迹、Enemy 动画资源、`GameScene.unity`、环境 Prefab、`SceneBackups` 备份文件变化及项目规范文档变化。后续提交、移动或清理时必须逐项确认归属，禁止用回退操作覆盖这些改动。

`SceneBackups/` 是 Unity 场景恢复备份，不参与运行或打包；当前已明显缩减。删除只会失去历史恢复点，执行清理前应先确认相关场景已保存且 Unity 已关闭。

本次仅新增归档文档与资源配置，没有创建或修改 C# 源文件。

