# Protocol_Evac ResManager UniTask 与资源生命周期重构记录

## 一、记录范围

本记录接续：

[2-GameApp静态入口与战斗HUD事件联动记录.md](../2026-8-10/2-GameApp静态入口与战斗HUD事件联动记录.md)

本记录保存 `ResManager` 的目录与命名空间整理、UniTask 接入、Addressables Asset 缓存/引用计数、实例化与对象池异步接口，以及应用退出时的资源收口规则。

## 二、本次确认的设计与协作偏好

```text
ResManager 对外形态
├─ 保持静态入口：ResManager.xxx
├─ 不使用 ResManager.Instance
├─ 不继承 ManagerBase 或 MonoBehaviour
└─ GameApp 只负责在应用销毁时调用 ResManager.Destroy()

资源地址约定
├─ Addressables Address 在本项目中全局唯一
├─ Asset 缓存直接以 string path 为键
└─ 不额外引入 Address + Type 的泛化缓存键

命名偏好
├─ 沿用 CacheEntry / caches / handle / refCount
├─ 抽象类规范使用 XxxBase 后缀
└─ 内部获取方法使用 AcquireAssetSync / AcquireAssetAsync，明确资源领域与同步/异步差异
```

## 三、当前实现状态

相关代码已整理到：

```text
Assets/Scripts/Framework/QTower/Common/Res/
└─ ResManager.cs
```

对应命名空间为 `Common.Res`；`UIManager` 与 `GameApp` 已更新引用。`QTower.asmdef` 已显式引用：

```text
UniTask
UniTask.Addressables
```

项目已通过 Git URL 安装 UniTask：

```text
Packages/manifest.json
└─ com.cysharp.unitask
   └─ https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask
```

### 1. Asset 缓存与引用计数

`ResManager` 内部新增：

```text
CacheEntry
├─ AsyncOperationHandle handle
└─ int refCount

caches
└─ Dictionary<string, CacheEntry>
```

Asset 资源的获取与释放规则：

```text
LoadAsset / LoadAssetAsync
-> AcquireAssetSync / AcquireAssetAsync
-> 命中 caches：refCount + 1，复用同一 Handle
-> 未命中：Addressables.LoadAssetAsync<T>(path)
-> 成功后返回 T，Handle 保持在 ResManager 内部

ReleaseAsset(path)
-> refCount - 1
-> 降至 0：Addressables.Release(handle) 并移除缓存

加载失败
-> ReleaseFailedCache 回滚本次获取的 refCount
-> 最后一个失败获取释放 Handle 并移除缓存
```

`AsyncOperationHandle<T>` 在创建时隐式转换并保存为非泛型 `AsyncOperationHandle`；这是 Addressables 的正常句柄转换。缓存层只负责等待、状态检查与释放，泛型类型只在加载入口和最终返回 `T` 时使用。

业务代码不应直接持有或释放 `AsyncOperationHandle`，只通过 `ResManager` 的资源 API 管理引用。

### 2. 实例化与 UniTask

回调式 Addressables 实例化接口已移除。当前异步实例化统一为：

```csharp
await ResManager.InstantiateAsync(path, parent);
await ResManager.InstantiateFromPoolAsync(path, parent);
```

`UIManager.OpenAsync` 对外仍保留回调，原因是它要合并同一个 UI 重复打开时的多个完成通知；但其内部实例化已通过 `LoadAndOpenViewAsync()` 使用 `await ResManager.InstantiateAsync(...)`。这不是资源层回调加载的遗留。

同步 `Instantiate`、`LoadAsset<T>` 与 `InstantiateFromPool` 仍保留。它们含 `WaitForCompletion()`，只适合启动期、小型必须立即取得的资源、编辑器工具或测试；普通运行时 UI、角色与特效加载优先使用对应的 Async API。

### 3. 对象池与跨场景根节点

对象池结构已确定为：

```text
ResPoolRoot（DontDestroyOnLoad）
├─ AddressablesPool
│  └─ 地址对象池 _pool 中的空闲实例
└─ PrefabPool
   └─ 普通 Prefab 对象池 _prefabPool 中的空闲实例
```

`ResPoolRoot` 由 `EnsurePoolRoots()` 在首次回收对象时创建。归还对象不再 `SetParent(null)`，而是进入对应子节点；从池取出时会按传入 `parent` 重新设置父节点，未传 parent 时会脱离池根。

## 四、销毁与所有权边界

退出顺序：

```text
GameApp.OnDestroy()
-> UIManager.Destroy()         先释放动态 UI 实例
-> ControllerManager.Destroy()
-> TimeManager.Destroy()
-> ResManager.Destroy()
   -> ClearAllPools()
   -> ClearAllPrefabPools()
   -> ReleaseAll()             释放 caches 中的 Asset Handle
   -> DestroyPoolRoot()
```

边界：

```text
Asset caches
└─ 只管理 Addressables.LoadAssetAsync<T> 产生的资源本体 Handle

Addressables 实例
└─ 由 UnLoadInstance 或地址对象池管理，不进入 caches

普通 Prefab 实例
└─ 由 _prefabPool 管理，超出池上限时 Object.Destroy
```

## 五、预加载接口

已按既有参考代码的串行方式提供：

```csharp
await ResManager.PreLoadAssets(paths);
```

内部会逐个调用 `AcquireAssetAsync<UnityEngine.Object>(path)`。每个预加载项会占用一份 `refCount`；调用方在不再需要预热资源时必须逐个调用：

```csharp
ResManager.ReleaseAsset(path);
```

当前不改为并行预加载，也未加入 `CancellationToken`。后续若要并行或支持 UI 快速关闭取消，必须先明确共享 Handle 下“取消等待”与“释放资源引用”的区别，不能在任一调用者取消时直接释放公共 Handle。

## 六、验证结果与未完成项

已完成：

```text
Unity AssetDatabase Refresh：成功
Unity Console：最近检查无 Error
dotnet build QTower.csproj：0 warning / 0 error
Git 提交：575e9e7（引入UniTask ResManager重构并加入UniTask）
```

尚未完成：

```text
完整 Play Mode 验证
├─ UICombatHUD 动态打开、关闭、重复打开
├─ UI 加载中销毁 GameApp 时实例是否正确释放
├─ 地址对象池跨场景保留与复用
├─ 普通 Prefab 池跨场景保留与复用
└─ PreLoadAssets 后成对 ReleaseAsset 的实际调用点
```

## 七、下一步建议

1. 在实际模块选择一个确定会使用的配置或图标，通过 `PreLoadAssets` 与 `ReleaseAsset` 验证引用计数使用习惯
2. 在 Play Mode 验证 `UICombatHUD` 的打开/关闭/重复打开，以及停止运行时 `ResPoolRoot` 是否随 `GameApp.Destroy()` 清除
3. 资源层稳定后再继续 Player 普攻 `HitWindow -> CombatHitbox` 的单目标一次伤害验收；不要在没有实际加载需求时继续堆叠取消令牌、并行预加载或新的资源抽象

## 八、工作区注意事项

归档时工作区干净；本轮改动已提交为 `575e9e7`。后续不要回退或重新引入回调式 `ResManager.InstantiateAsync`。保留 `UIManager` 对外回调 API 的并发打开合并职责，不要误认为它与资源层回调加载相同。
