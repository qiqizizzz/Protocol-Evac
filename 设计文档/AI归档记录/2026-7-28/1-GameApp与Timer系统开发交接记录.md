# Protocol_Evac GameApp 与 Timer 系统开发交接记录

## 一、记录范围

本记录接续：

[../2026-7-27/1-Player闪避Action纵切与Shift输入分流记录.md](../2026-7-27/1-Player闪避Action纵切与Shift输入分流记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要记录 Player ActionAttack 开发前，先补通用计时与游戏入口装配的讨论和当前代码断点：

```text
1. 用户暂缓 ActionAttack，优先整理通用 Timer / GameApp 架构入口
2. 确认 GameApp 不直接放在 Assets/Scripts 根目录，而是放入 Assets/Scripts/Game/
3. 确认 Game 目录只放游戏启动与 QF 注册入口，不放具体 Timer 业务实现
4. 确认 Timer 作为独立模块放入 Assets/Scripts/Module/Timer/
5. 确认 DurationTimer 作为轻量无事件局部计时器放入 Assets/Scripts/Utils/Timer/
6. 确认 TimerSystem 对外统一使用 Register(...)，不拆 Delay / Loop 两个入口
```

本次没有进入 Attack 输入、ActionAttack 状态、攻击动画、Ability System、Enemy 侧或 Unity 场景挂载验证。

## 二、本次确认的设计

### 1. Game 目录只做入口和装配

用户已选择：

```text
Assets/Scripts/Game/
├─ GameApp.cs
└─ GameArchitecture.cs
```

当前边界：

```text
GameApp
└─ Unity MonoBehaviour 总入口
└─ 初始化 GameArchitecture
└─ 每帧驱动需要全局 Tick 的系统

GameArchitecture
└─ QF Architecture 注册入口
└─ 只负责 RegisterSystem / RegisterModel / RegisterUtility
```

不建议把 `TimerSystem` 放入 `Game/`，避免 `Game` 目录变成“全局系统杂物桶”。具体功能系统继续放在各自模块目录。

### 2. Timer 模块命名沿用 GameTimer 风格

用户不喜欢 `ManagedTimer` / `TimerTask` 这类命名，倾向沿用旧项目中的 `GameTimer` 命名风格。

当前确认结构：

```text
Assets/Scripts/Module/Timer/
├─ GameTimer.cs
├─ GameTimerData.cs
└─ TimerSystem.cs
```

职责划分：

```text
TimerSystem
└─ QF System，对外提供全局计时任务 API

GameTimer
└─ 管理多个 GameTimerData
└─ 负责 Register / Cancel / ClearAll / Tick

GameTimerData
└─ 单个计时任务
└─ 保存 Id / Duration / Callback / IsLooping / Cancel 状态
└─ 内部使用 DurationTimer 推进时间
```

### 3. DurationTimer 保持轻量无事件

本次讨论过其他 AI 提供的事件式计时器写法，例如 `OnComplete`、`OnProgress`。最终确认：

```text
DurationTimer
└─ 不放 callback
└─ 不放 event
└─ 不自动 Update
└─ 不依赖 Time.time
└─ 由调用方传入 deltaTime 推进
```

原因：

```text
状态内部计时拥有天然宿主，例如 DodgeState / AttackState
└─ 直接 Tick 后判断 IsFinished 更直观

全局延迟回调没有天然宿主
└─ 由 TimerSystem / GameTimerData 保存 callback 更合适
```

后续 `PlayerDodgeState`、`ActionAttack`、无敌帧窗口、取消窗口、冷却等局部行为，优先直接持有 `DurationTimer`，不要交给全局 `TimerSystem`。

### 4. TimerSystem API 统一为 Register

用户明确不想拆成：

```text
Delay(...)
Loop(...)
```

当前偏好：

```csharp
Register(float duration, Action callback, bool isLooping = false)
```

调用语义：

```text
Register(1f, callback)
└─ 注册一次性延迟回调

Register(0.5f, callback, true)
└─ 注册循环回调

Register(0.5f, callback, isLooping: true)
└─ 更推荐的可读写法
```

后续 AI 不要无明确需求重新拆回 `Delay / Loop` 两个 public API。

## 三、当前实现状态

### 1. Utils Timer

当前文件：

```text
Assets/Scripts/Utils/Timer/DurationTimer.cs
```

当前职责：

```text
Start(float duration)
Tick(float deltaTime)
Pause()
Resume()
Stop()
Complete()
Reset()

IsRunning
HasStarted
IsFinished
Duration
ElapsedTime
RemainingTime
NormalizedTime
```

当前设计选择：

```text
NormalizedTime 使用 Mathf.Clamp01
Stop() 表示停止并保留当前进度
Complete() 表示直接拉满完成
Reset() 表示回到未开始状态
```

### 2. Timer 模块

当前文件：

```text
Assets/Scripts/Module/Timer/GameTimerData.cs
Assets/Scripts/Module/Timer/GameTimer.cs
Assets/Scripts/Module/Timer/TimerSystem.cs
```

当前数据流：

```text
TimerSystem.Register(...)
└─ GameTimer.Register(...)
   └─ new GameTimerData(...)
      └─ DurationTimer.Start(duration)

GameApp.Update()
└─ TimerSystem.Tick(Time.deltaTime)
   └─ GameTimer.Tick(deltaTime)
      └─ GameTimerData.Tick(deltaTime)
         ├─ DurationTimer.Tick(deltaTime)
         ├─ 到时后执行 callback
         └─ 如果 isLooping 为 true，则重新 Start(duration)
```

当前 `GameTimer` 仍采用倒序 `for` 遍历并移除完成计时器：

```text
for i = Count - 1 -> 0
└─ Tick(timerData)
└─ IsFinished 后 RemoveAt(i)
```

这是同步 gameplay Tick，不是异步线程或协程。需要注意的是 callback 内部如果再次注册、取消或清空 Timer，后续可能需要补充重入保护。

### 3. Game 入口

当前文件：

```text
Assets/Scripts/Game/GameApp.cs
Assets/Scripts/Game/GameArchitecture.cs
```

当前链路：

```text
GameApp.Awake()
└─ GameArchitecture.InitArchitecture()

GameArchitecture.Init()
└─ RegisterSystem(new TimerSystem())

GameApp.Update()
└─ this.GetSystem<TimerSystem>().Tick(Time.deltaTime)
```

当前 `GameApp` 实现 `IController`，通过 `GetArchitecture()` 返回 `GameArchitecture.Interface`，从而使用 QF 的 `this.GetSystem<TimerSystem>()` 扩展方法。

## 四、关键架构边界

当前 Timer / GameApp 边界如下：

```text
GameApp
└─ 只负责 Unity 生命周期入口
└─ 不写具体计时任务逻辑
└─ 不直接保存一堆 gameplay 状态

GameArchitecture
└─ 只负责注册 QF System / Model / Utility
└─ 不写系统业务逻辑

TimerSystem
└─ QF System 层，对外暴露 Register / Cancel / ClearAll / Tick
└─ 不继承 MonoBehaviour
└─ 不直接读取 UnityEngine.Time

GameTimer
└─ 管理一组 GameTimerData
└─ 负责计时任务集合的增删和 Tick

GameTimerData
└─ 保存单个回调任务的运行时事实
└─ 内部使用 DurationTimer

DurationTimer
└─ 纯局部计时工具
└─ 不保存 callback / event
└─ 不注册到 QF
```

后续不要让 `DurationTimer` 反向依赖 `Module.Timer`，也不要让 `Module.Timer` 依赖具体 Player 状态。

## 五、当前需要注意的问题

### 1. 当前实现还未完成 Unity 编译验证

本次尝试执行：

```text
dotnet build .\Assembly-CSharp.csproj --no-restore
```

当前本机命令行环境返回：

```text
No .NET SDKs were found.
```

因此未能通过命令行完成 C# 编译验证。

本次也尝试读取 Unity Console，但 Unity MCP 连接失败：

```text
HTTP request failed: http://localhost:28630/
```

所以当前不能假定 Unity Editor 中无编译错误。下一次应优先打开 Unity Console 或恢复 MCP 连接后检查 Timer / GameApp 编译状态。

### 2. 当前 Timer 回调重入未做复杂保护

当前 `GameTimer.Tick()` 是同步倒序遍历。这个写法本身用于普通计时任务是合理的，但如果 callback 内部调用：

```text
Register(...)
Cancel(...)
ClearAll()
```

可能需要额外约束或后续补 pending 队列。

当前阶段为了保持用户偏好的简洁写法，暂不引入：

```text
pending add 队列
单任务暂停恢复
进度回调
unscaled time
线程 / 异步任务
协程封装
```

如果后续出现 callback 内注册 / 清空 Timer 的真实需求，再补充重入保护。

### 3. TimerSystem Count 的初始化时机

当前 `TimerSystem` 的 `m_gameTimer` 在 `OnInit()` 中创建。按 QF 流程，`RegisterSystem(new TimerSystem())` 后 Architecture 初始化会调用 `OnInit()`。

注意：

```text
不要在 TimerSystem.OnInit() 之前访问 Count / Register / Tick
```

正常通过 `GameArchitecture.InitArchitecture()` 后再使用 `this.GetSystem<TimerSystem>()` 即可。

## 六、当前尚未完成

```text
Unity Editor 编译验证
GameApp 是否已挂到场景中的 GameObject
TimerSystem.Register(...) 的 Play Mode 测试
循环 Timer 的 Play Mode 测试
Cancel / ClearAll 的 Play Mode 测试
是否需要 callback 重入保护
是否需要将 PlayerDodgeState 的 m_elapsedTime 替换为 DurationTimer
是否需要为 Timer 模块添加 asmdef
```

本次仍未进入：

```text
ActionAttack
Attack 输入绑定
Attack 动画
Ability System
Enemy AI
```

## 七、下一步建议

下一次建议先不要立刻进入 ActionAttack，而是把 Timer / GameApp 链路验证完：

```text
1. 打开 Unity，确认当前 C# 编译错误
2. 若有 Timer / GameApp 编译错误，先按现有架构修正
3. 在场景中创建或确认 GameApp GameObject，并挂载 GameApp.cs
4. 用一次临时 Register(1f, callback) 验证 TimerSystem 能在 Update 中触发
5. 用 Register(0.5f, callback, isLooping: true) 验证循环计时
6. 验证 Cancel / ClearAll 是否满足当前预期
7. 验证稳定后，再考虑把 PlayerDodgeState 改成 DurationTimer
```

如果 Timer 链路稳定，再回到 Player 下一步：

```text
ActionAttack 第一版
├─ 确认 Attack 输入键位
├─ 接入 Attack Buffer
├─ 新增 ActionAttack 状态
├─ 用 DurationTimer 或配置时长驱动 ActionAttack 结束
└─ 暂不做伤害、连招、取消窗口和完整 Ability System
```

## 八、工作区注意事项

归档前执行：

```text
git status --short --untracked-files=all
```

当前可见工作区状态主要包含旧的无关改动：

```text
 M .codex/config.toml
 D Assets/Lua.meta
 D Assets/Scripts/Common.meta
 D Assets/Scripts/Framework/QLua.meta
 D Assets/Scripts/Net.meta
 D Assets/Scripts/Tools/GM.meta
 D Assets/Scripts/UI.meta
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639196445354525317.backup
?? SceneBackups/99c9720ab356a0642a771bea13969a05/639201340524186779.backup
```

当前 `Assets/Scripts/Game/`、`Assets/Scripts/Module/Timer/`、`Assets/Scripts/Utils/Timer/` 相关文件已能被 `git ls-files` 查到，但 `git status` 未显示它们有未提交改动。后续提交前仍应重新执行：

```text
git status --short --untracked-files=all
git diff -- Assets/Scripts/Game Assets/Scripts/Module/Timer Assets/Scripts/Utils/Timer
```

以当时状态为准。
