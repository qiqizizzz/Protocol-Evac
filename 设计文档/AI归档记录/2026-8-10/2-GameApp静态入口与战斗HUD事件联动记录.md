# Protocol_Evac GameApp 静态入口与战斗 HUD 事件联动记录

## 一、记录范围

本记录接续：

[1-AbilityComposer P0窗口与MiSans字体修复交接记录.md](1-AbilityComposer%20P0窗口与MiSans字体修复交接记录.md)

主设计文档：

[战斗系统总开发文档.md](../../战斗系统/战斗系统总开发文档.md)

本记录保存运行时 UI 启动、锁定 HUD 事件联动、`GameApp` 模块入口的当前事实，以及已确认的战斗系统长期技术建设方向。

## 二、本次确认的架构边界

```text
GameApp
├─ 生命周期仍由 GameApp.Instance 驱动
└─ 对外模块入口使用静态属性
   ├─ GameApp.TimeManager
   ├─ GameApp.ControllerManager
   └─ GameApp.UIManager

GameScene
├─ Awake：查找并持久化 UIRoot，初始化 GameApp，设置 UI Root
└─ Start：注册 GameController 等场景模块

GameController
└─ 只负责默认游戏 UI 的注册、异步打开与销毁
```

`GameController` 不承接 Player 与 UI 之间的状态中转。角色模块发布真实状态，运行时 UI 直接订阅全局事件并刷新表现。

## 三、运行时 UI 与锁定事件链

`UICombatHUD` 已改为动态注册、异步加载。场景中的 `UIRoot` 只作为独立的挂载根节点，不挂管理脚本，也不保留场景内 `UICombatHUD` 实例。

```text
UICombatHUD Prefab（Addressable 地址：UICombatHUD）
-> UIManager 通过 ResManager.InstantiateAsync 动态创建
-> UIRoot
```

锁定状态的唯一事件链如下：

```text
PlayerViewController
-> EventManager.PublishEvent(PlayerLockOnStateChanged, bool)
-> UICombatHUD.SubscribeViewEvents()
-> UICombatHUD.SetLockOn(bool)
```

`PlayerViewController` 在成功锁定、手动解除、目标失效自动解除，以及切换第一人称导致解除时发布状态。`UICombatHUD` 只更新 Toggle，使用 `SetIsOnWithoutNotify`，初始化时强制显示为未锁定。

## 四、事件订阅所有权

运行时 `UIBase` 已具备与 `BaseController` 相同的事件托管责任：

```text
UIBase.RegisterEvent<TEvent>()
-> 保存事件注销委托
-> UIBase.OnDestroy()
-> RemoveViewEvent()
```

因此具体运行时 UI 不再负责单独的 `UnsubscribeViewEvents()`。`UICombatHUD` 仅在 `SubscribeViewEvents()` 中注册 `PlayerLockOnStateChanged`。

`UIBaseEditor` 不属于本次运行时 UI 改动范围，继续保持其现有 Editor 生命周期设计。

## 五、涉及文件与当前状态

```text
Assets/Scripts/Game/GameApp.cs
Assets/Scripts/Game/GameScene.cs
Assets/Scripts/Game/GameController.cs
Assets/Scripts/Framework/QTower/MVC/Type/ControllerType.cs
Assets/Scripts/Framework/QTower/Common/Defines/EventDefines.cs
Assets/Scripts/Framework/QTower/MVC/View/UIBase.cs
Assets/Scripts/Module/Player/Core/View/PlayerViewController.cs
Assets/Scripts/UI/Combat/UICombatHUD.cs
```

`ResManager` 保留用户已有实现，不应为了 UI 加载重新简化、删除或替换其接口。UI 预制体、Addressable 地址和 `ViewType` 延续同名约定：`UICombatHUD`。

## 六、验证与当前注意问题

已完成：

```text
Unity AssetDatabase Refresh：成功
dotnet build Assembly-CSharp.csproj：0 errors
```

尚未完成可信的完整 Play Mode 验证。当前 Unity Editor 仍存在既有 AbilityComposer 程序集配置问题：`Assets/Scripts/Tools/Editor/AbilityComposer/` 下有重复 asmdef / GUID `408ec2388a195ca4080faf4f9d45545b`，Console 会报告“Folder contains multiple assembly definition files”及同键重复异常。修复前不要把 HUD 运行时链路视为已完成场景级验收。

## 七、后续技术建设方向

用户已明确希望将以下学习与实现主要结合战斗系统体现，具体长期约束已同步进入[战斗系统总开发文档.md](../../战斗系统/战斗系统总开发文档.md)：

```text
ECS：跟随 B 站 C 酱课程，从高数量战斗实体和实际性能问题切入
Shader：手写脚本，以受击、技能、武器和范围提示等战斗反馈为落点
A*：服务 Enemy 的 Chase / Search / Patrol，并保持在 IPathAgent 后端
```

这三项不抢占当前 P0。当前仍先完成第一段普攻 `HitWindow` 到武器 `CombatHitbox` 的单目标、单窗口、一次伤害 Play Mode 验证。

## 八、下一步建议

1. 排查并修复 AbilityComposer 重复 asmdef / GUID，恢复可靠的 Unity Console 与 Play Mode 验证环境
2. 完成 Player P0 命中竖切并更新战斗系统状态矩阵
3. P0 稳定后，再依据真实战斗缺口选择 ECS、手写 Shader 或 A* 的第一项专题竖切

## 九、工作区注意事项

工作区可能包含用户正在调整的文件。后续继续时不得回滚、覆盖或精简用户已有的 `ResManager`、QTower 基础框架和 UI 结构；先读取实际文件与 Git 状态，再做局部修改。

## 十、Ability Composer 后续参考资料

后续继续开发技能编辑器与 `Ability Composer` 时，可参考以下视频的技能系统架构设计展示：

[UNITY 开发记录 动作游戏技能系统架构设计展示](https://www.bilibili.com/video/BV1tZum63Enj/?share_source=copy_web&vd_source=131b4639f6fe5e279b7fc45afaaea252)

该链接仅作为后续调研和设计参考，不代表其中的架构已经引入本项目。继续实现前，仍以本项目的 `Data - Controller - View` 边界、现有 `Ability Composer` 设计文档和已验证代码事实为准。
