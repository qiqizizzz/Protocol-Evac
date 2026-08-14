# Protocol_Evac Tokyo Street 场景导入与 USD 地图调研交接记录

## 一、记录范围

本记录接续：

[1-Player急停脚步事件与Grounded混合树交接记录.md](../2026-8-9/1-Player急停脚步事件与Grounded混合树交接记录.md)

本记录保存本轮环境场景资源导入结果，以及鸣潮穗波市 USD 导出结构的已确认事实。它不包含 Enemy、Player 或战斗代码实现。

## 二、本次确认的结果

用户已确认 Tokyo Street 场景成功导入当前项目。

资源根目录：

```text
Assets/Tokyo_Street
```

已存在的主要资源目录：

```text
Assets/Tokyo_Street/
├─ Animation/
├─ Materials/
├─ Models/
├─ Other/
├─ Prefabs/
├─ Scenes/
└─ Textures/
```

场景资源：

```text
Assets/Tokyo_Street/Scenes/
├─ Day_Demo.unity
├─ Night_Demo.unity
└─ Prefabs.unity
```

资源目录还保留原始导入包，勿在未确认用途前删除：

```text
Assets/Tokyo_Street/HDRP_Tokyo_Street.unitypackage
Assets/Tokyo_Street/URP_Tokyo_Street.unitypackage
```

当前项目 `Assets/Scenes/GameScene.unity` 已有未提交变更；本记录不据此断言 Tokyo Street 已经合并进 GameScene。后续操作应先确认当前打开的是独立 Demo 场景，还是已加进 GameScene 的实例。

## 三、鸣潮穗波市 USD 导出调研结论

FModel 的 USD 导出根目录：

```text
E:\Download\FModel\Output\Exports\Client\Content\Aki\Map\Level\WP\2_8_Suibo
```

已确认导出的是带相对引用与变换信息的 USD World Partition 数据，不是需要人工逐物件摆放的 FBX 集合：

```text
WP_2_8_Suibo.usda
└─ WP_2_8_Suibo/_Generated_/0/
   └─ WP_Grid_StaticMesh_*/WP_2_8_Suibo.usda
      └─ 引用实际网格 USD，并记录 translate / orient / scale
```

抽查分块已确认包含类似引用：

```text
../../../../../../../../Scene/Assets/Levels/LiNaXiTa/HonamiStory/Common/Foliage/Mesh/SM_Old_Vin_02HL.usda
```

因此，若后续继续使用 USD 路线，应以 USD Stage 的相对目录和引用为单位处理；不应将所有分块转成 FBX 后再手动拼地图。

当前缺口仍是：`Map/Level/WP` 的布局 USD 已导出，但被其引用的 `Client/Content/Aki/Scene/Assets/...` 实际网格 USD 和贴图尚未确认完整导出。因此不能据此宣称穗波市已经可在 Unity 中完整还原。

## 四、外部旧街道工程的包修复记录

下列操作发生在独立工程，不属于当前 `Protocol_Evac` 工作区资产：

```text
D:\CCodes\unity\URP_JieDao_2020.3.35WebGL\URP_JieDao_2020.3.35WebGL
```

该工程使用 Unity `2022.3.62f3` 打开时，原本 `URP 10.9.0` 与 Unity 2022 的 Core 14 混用，导致 `ShaderVariantLogLevel`、`UNITY_PREV_MATRIX_M` 等错误。已做以下处理：

```text
Packages/manifest.json
├─ com.unity.render-pipelines.universal: 10.9.0 -> 14.0.12
└─ 移除 com.unity.modules.autostreaming

Library/
├─ 清理 PackageCache
└─ 清理 PackageManager
```

该独立工程根目录已留有：

```text
Codex交接.md
```

后续若再进入该工程，应先让 Unity 2022 完整解析包，再检查材质升级问题。不要把该工程的 `Library` 或 `Packages` 文件复制到 Protocol_Evac。

## 五、下一步建议

环境用于当前战斗开发时，优先按以下顺序推进：

```text
1. 打开 Assets/Tokyo_Street/Scenes/Day_Demo.unity，确认灯光、材质和碰撞表现
2. 确认是否以 Day_Demo 作为新测试场景，或仅挑选部分 Prefab 合并到 GameScene
3. 确定战斗测试区域后，再布置 Player 与 Enemy_流浪者
4. 需要穗波市 USD 时，先小范围补齐一个静态网格分块的依赖，而非一次导出整张城市场景
```

用户此前已明确：玩家攻击伤害流程已验证；后续开发重点是完整 Enemy 模块，不要重复要求先验证攻击。

## 六、工作区注意事项

归档时工作区存在多项未提交改动和未跟踪资产，其中包括：

```text
Assets/Tokyo_Street/
Assets/Animation/Enemy-流浪者/
Assets/Art/Models/Enemy-流浪者/
Assets/Prefabs/Character/Enemy_流浪者.prefab
Assets/Scenes/temp.unity
Assets/Scenes/GameScene.unity
Assets/Prefabs/Character/Player.prefab（显示为删除）
```

这些改动并非均由本次环境导入产生。后续提交、移动或回退前必须逐项确认归属；尤其不要因处理 Tokyo Street 而误删 Enemy 资源、覆盖 GameScene，或恢复/删除 Player.prefab。

本次仅新增归档文档，没有创建或修改 C# 源文件。

