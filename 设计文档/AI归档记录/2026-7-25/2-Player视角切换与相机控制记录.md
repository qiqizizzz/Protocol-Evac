# Protocol_Evac Player 视角切换与相机控制记录

## 一、记录范围

本记录接续：

[1-Player HFSM命名统一与动画注册式架构记录.md](1-Player%20HFSM命名统一与动画注册式架构记录.md)

主设计文档：

[../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md](../../玩家状态与敌人AI/玩家状态与敌人AI设计方案.md)

本次主要处理 Player 地面移动闭环后的视角与相机控制：

```text
1. 新增 PlayerViewConfigSO / PlayerViewMode / PlayerViewController
2. 接入第一人称与第三人称视角模式
3. 接入 Look / F1 / F3 输入
4. 将地面 MoveDir 改为视角相对方向
5. 将第三人称身体朝移动方向旋转收口到 PlayerMotor.RotateByDirection(...)
6. 新增 HierarchyFindTool，用于通过层级路径查找必要节点与组件
7. 新增 AI 协作边界文档，禁止过度安全化总判空
```

本次没有进入 Airborne、Jump、Fall、Input Buffer、Ability、Hurt、Dead 或 Enemy 侧开发。

## 二、本次确认的设计

### 1. 第一人称 / 第三人称边界

当前视角模式由 `PlayerViewMode` 表示：

```csharp
public enum PlayerViewMode
{
    FirstPerson,
    ThirdPerson
}
```

当前约定：

```text
第一人称
├─ Look 输入控制 CameraYaw / CameraPitch
├─ 玩家身体只跟随 yaw
├─ PlayerCamera.localRotation 只负责 pitch
└─ MoveDir 基于玩家 Transform.forward / right

第三人称
├─ Look 输入控制 CameraYaw / CameraPitch
├─ ViewRoot 使用 yaw + pitch 形成相机旋转
├─ PlayerCamera.localPosition 使用第三人称本地位置配置
├─ 玩家身体由 PlayerMotor.RotateByDirection(...) 朝移动方向转
└─ MoveDir 基于 CameraYaw 对应的水平 forward / right
```

### 2. 相机层级约定

当前代码约定 Player 下存在以下层级：

```text
Player
└─ ViewRoot
   └─ PlayerCamera
```

`PlayerController` 不通过 Inspector 拖拽 `ViewRoot` 或 `PlayerCamera`，而是在运行初始化中查找：

```text
VIEW_ROOT_PATH = "ViewRoot"
PLAYER_CAMERA_PATH = "ViewRoot/PlayerCamera"
```

`ViewRoot` 当前固定跟随 Player 根节点位置：

```text
ViewRoot.position = Player.position
```

视角差异主要通过 `PlayerCamera.localPosition` 和旋转分配完成。

### 3. 视角配置

当前配置文件：

```text
Assets/Scripts/Module/Player/Config/View/PlayerViewConfigSO.cs
```

当前配置字段：

```text
DefaultViewMode
FirstPersonYawSpeed
FirstPersonPitchSpeed
FirstPersonCameraLocalPosition
ThirdPersonYawSpeed
ThirdPersonPitchSpeed
ThirdPersonBodyTurnSpeed
ThirdPersonCameraLocalPosition
PitchMin
PitchMax
```

推荐调试初值：

```text
FirstPersonCameraLocalPosition = (0, 1.55, 0.15)
ThirdPersonCameraLocalPosition = (0, 1.45, -4)
PitchMin = -60
PitchMax = 75
```

鼠标转速不要使用过高值。当前 `Look` 读取的是 `<Mouse>/delta`，如果 `YawSpeed / PitchSpeed` 为 180，Play Mode 中会非常容易眩晕。建议先从 `3 ~ 8` 之间调试。

### 4. 输入接入

当前 `PlayerInputActions` 已包含：

```text
Look
SwitchToFirstPerson
SwitchToThirdPerson
```

绑定确认：

```text
Look                 -> <Mouse>/delta
Look                 -> <Gamepad>/rightStick
SwitchToFirstPerson  -> <Keyboard>/f1
SwitchToThirdPerson  -> <Keyboard>/f3
```

`PlayerInputReader.Tick()` 保持直接赋值风格：

```csharp
m_context.MoveInput = m_inputActions.Player.Move.ReadValue<Vector2>();
m_context.IsSprintPressed = m_inputActions.Player.Sprint.IsPressed();
m_context.LookInput = m_inputActions.Player.Look.ReadValue<Vector2>();
m_context.TargetViewMode = m_inputActions.Player.SwitchToFirstPerson.WasPressedThisFrame()
    ? PlayerViewMode.FirstPerson
    : m_inputActions.Player.SwitchToThirdPerson.WasPressedThisFrame()
        ? PlayerViewMode.ThirdPerson
        : null;
```

该写法是用户确认的当前偏好，不要再替换成规则表、结构体、数组映射或其它过度抽象。

## 三、当前实现状态

当前新增 / 相关路径：

```text
Assets/Scripts/Module/Player/Config/View/
└─ PlayerViewConfigSO.cs

Assets/Scripts/Module/Player/Core/View/
├─ PlayerViewController.cs
└─ PlayerViewMode.cs

Assets/Scripts/Utils/Find/
└─ HierarchyFindTool.cs

.codex/work_init_CodexAI/
└─ 02-AI协作与实现边界.md
```

当前修改 / 相关路径：

```text
Assets/Scripts/Module/Player/Context/PlayerContext.cs
Assets/Scripts/Module/Player/Core/PlayerController.cs
Assets/Scripts/Module/Player/Core/PlayerMotor.cs
Assets/Scripts/Module/Player/HFSM/States/Ground/PlayerMoveState.cs
Assets/Scripts/Module/Player/Input/PlayerInputReader.cs
Assets/Scripts/Module/Player/Input/PlayerInputActions.cs
Assets/Config/Player/View/PlayerViewConfig.asset
Assets/Prefabs/Character/Player.prefab
Assets/Scenes/GameScene.unity
```

当前 `PlayerContext` 视角相关运行时事实：

```text
LookInput
ViewMode
TargetViewMode
CameraYaw
CameraPitch
```

当前 `PlayerController.Awake()` 只做：

```text
findReferences()
initCore()
initHFSM()
initAnim()
m_isInited = true
```

`findReferences()` 通过 `HierarchyFindTool` 查找运行依赖：

```text
Transform
CharacterController
Animator
ViewRoot
PlayerCamera
```

## 四、关键架构边界

当前职责边界：

```text
PlayerController
└─ 查找运行依赖，初始化模块，安排 Update / FixedUpdate 调度

PlayerInputReader
└─ 读取输入并写入 PlayerContext，不直接控制 ViewController

PlayerViewController
└─ 消费 TargetViewMode，处理 yaw / pitch、第一/第三人称相机刷新与视角切换

PlayerMoveState
└─ 根据 ViewMode 计算视角相对 MoveDir，并写入 TargetMoveSpeed

PlayerMotor
└─ 执行 CharacterController.Move，并在第三人称下执行 RotateByDirection(...)

HierarchyFindTool
└─ 提供 GameObject / Component / Transform 的层级查找与组件获取扩展
```

本次确认不要把视角切换逻辑放回 `PlayerController`。`PlayerController` 不应继续增加 `handleViewModeRequest()` 这类具体业务方法。

## 五、场景 / 资源 / 配置状态

当前场景与 Prefab 已围绕玩家相机做过调整，关键约定如下：

```text
Assets/Prefabs/Character/Player.prefab
└─ Player
   └─ ViewRoot
      └─ PlayerCamera
```

当前视角配置资产路径：

```text
Assets/Config/Player/View/PlayerViewConfig.asset
```

注意：

```text
1. 场景中旧 Main Camera 应保持禁用或避免与 PlayerCamera 同时参与渲染
2. PlayerCamera 建议作为玩家实际相机使用，必要时 Tag 设置为 MainCamera
3. 如果第一人称看到头发或模型，可优先调 FirstPersonCameraLocalPosition.z
4. 如果第三人称距离或高度不合适，优先调 ThirdPersonCameraLocalPosition
```

## 六、本次新增协作规范

新增文档：

```text
.codex/work_init_CodexAI/02-AI协作与实现边界.md
```

当前该文档只记录一条用户明确要求的规则：

```text
禁止过度安全化总判空
```

不要再写类似：

```text
if (a == null || b == null || c == null)
{
    QLog.Error("一大串依赖缺失");
    return;
}
```

必要引用应在具体获取方法里暴露具体错误，例如：

```text
GetOwnerComponent<T>()
GetChildComponent<T>()
FindChild(...)
FindChildComponent<T>(...)
```

## 七、当前需要注意的问题

### 1. Unity 编译与 Play Mode 尚需最终确认

本次执行过多次：

```text
git diff --check
```

结果没有空白错误，仅有 CRLF 提示。

但本次归档前未通过 Unity MCP 或 Unity Editor 完成脚本编译、Console 检查或 Play Mode 完整验证。下一次应优先确认：

```text
1. Unity Console 无编译 Error / Exception
2. F1 切换第一人称
3. F3 切换第三人称
4. 鼠标上下左右视角正常
5. 第一人称不会穿入角色模型
6. 第三人称移动时身体朝移动方向转
7. WASD / Shift 移动与 Walk / Run 动画仍正常
```

### 2. 视角速度需要调小

当前 `PlayerViewConfigSO` 默认脚本值仍为：

```text
FirstPersonYawSpeed = 180
FirstPersonPitchSpeed = 180
ThirdPersonYawSpeed = 180
ThirdPersonPitchSpeed = 180
```

如果配置资产中也仍是 180，Play Mode 中鼠标视角会过快。建议在资产里先调为：

```text
3 ~ 8
```

### 3. 当前仍保留部分运行期判空

`PlayerViewController.Tick()`、`SetViewMode()`、`refreshCameraTransform()` 中仍有针对运行期对象的判空：

```text
m_context == null
m_viewConfig == null
m_viewRoot == null
m_playerCamera == null
```

这些与“多个必要引用混在一起的总判空”不同，但后续是否继续保留可按用户偏好再调整。

## 八、当前尚未完成

```text
Unity 编译验证
Console Error / Exception / Warning 检查
Play Mode 验证 F1 / F3 视角切换
调低 PlayerViewConfig.asset 中 Look 速度
确认第一人称相机不会穿模
确认第三人称相机位置与身体转向手感
确认旧 Main Camera 不再干扰 PlayerCamera
Airborne / Jump / Fall 状态与 AirRules
Input Buffer 与 Jump 输入缓存
Ability System 与 Action TransitionRules / AnimRules
Hurt / Dead 数据与 StatusRules
Enemy 侧内容
```

## 九、下一步建议

下一次建议先不要进入 Airborne，优先做视角闭环验证：

```text
1. 打开 Unity，等待脚本编译完成
2. 检查 Console 是否有 Error / Exception / Warning
3. 在 PlayerViewConfig.asset 中将四个 Look 速度调到 3 ~ 8
4. Play Mode 验证 F1 第一人称、F3 第三人称
5. 调整 FirstPersonCameraLocalPosition，避免第一人称穿模
6. 调整 ThirdPersonCameraLocalPosition，确定第三人称高度与距离
7. 验证 WASD / Shift 移动与动画仍正常
8. 若稳定，再进入 Airborne / Jump / Fall
```

## 十、工作区注意事项

归档创建后执行：

```text
git status --short --untracked-files=all
```

```text
M Assets/Scripts/Utils/Find/HierarchyFindTool.cs
?? 设计文档/AI归档记录/2026-7-25/2-Player视角切换与相机控制记录.md
```

其中：

```text
Assets/Scripts/Utils/Find/HierarchyFindTool.cs
└─ 属于本次层级查找工具整理与 #region 分区相关改动

2-Player视角切换与相机控制记录.md
└─ 本次新增归档文件
```

如果后续准备提交，应重新执行 `git status --short --untracked-files=all`，以当时状态为准。
