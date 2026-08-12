# Protocol_Evac Ability Composer AnimationEvent Function 选择记录

## 一、记录范围

本记录接续：

[1-ResManager UniTask与资源生命周期重构记录.md](1-ResManager UniTask与资源生命周期重构记录.md)

主设计文档：

[AbilityComposer设计方案.md](../../工具与编辑器/AbilityComposer设计方案.md)

本次完成 Ability Composer 的 `Animation Event` Function 候选扫描与选择。该功能仍处于事件草稿阶段，未向 `.anim` 或 FBX 写入事件。

## 二、本次确认的设计与协作偏好

用户确认 Function 行为应遵循 Unity 原生 `AnimationEvent`：

```text
不保存组件或组件类型
不添加组件拖拽目标
事件草稿只保存 FunctionName
候选方法来自当前临时预览对象的动画接收层级
保留手动输入，支持尚未挂载接收组件或跨对象约定的方法名
```

因此不同组件出现同名方法时，编辑器按 `FunctionName` 去重显示。运行时实际分发范围仍由 Unity Animation Event 的原生行为决定，编辑器不会伪造单组件绑定。

## 三、当前实现状态

目录与职责：

```text
Assets/Scripts/Tools/Editor/AbilityComposer/
├─ Preview/AbilityPreviewController.cs
│  └─ 暴露 AnimationEventReceiver，即动画采样根节点
├─ Right/Event/AbilityEventFunctionResolver.cs
│  └─ 扫描临时预览层级的合法 Animation Event 方法
├─ Right/Event/AbilityEventInspectorView.cs
│  ├─ Function 下拉候选
│  └─ Custom Function 手动输入
├─ Right/AbilityRightView.cs
├─ AbilityComposerView.cs
└─ AbilityComposerController.cs
   └─ 在创建、切换或返回预览时同步候选列表
```

扫描规则：

```text
包含
├─ public 实例方法
├─ 返回值为 void
└─ 零参数，或仅一个 float / int / string / UnityEngine.Object / AnimationEvent 参数

排除
├─ static 方法
├─ 非 public 方法
├─ 非 void 返回值
├─ 两个或以上参数
└─ object、Component、Behaviour、MonoBehaviour 的继承基础方法
```

## 四、使用流程

```text
选择 Prefab 与 AnimationClip
  -> 创建预览
  -> Controller 取得 AnimationEventReceiver
  -> Resolver 扫描该根节点及所有子节点的 MonoBehaviour
  -> Event Inspector 的 Function 下拉显示候选
  -> 用户选择方法
  -> 草稿仅写入该方法名称 FunctionName
```

切换 Prefab、切换 Clip、返回旧场景时，Function 候选会清空，避免继续显示已销毁临时预览的组件方法。

## 五、验证结果

本次通过 Unity MCP 验证：

```text
AbilityEventFunctionSmokeTest
├─ ValidWithoutParameter() 被扫描到
├─ ValidFloat(float) 被扫描到
├─ InvalidReturn() 未被扫描到
├─ InvalidTwoParameters(float, float) 未被扫描到
├─ InvalidStatic() 未被扫描到
└─ Ability Composer 窗口与 Function DropdownField 可正常创建
```

`AssetDatabase.Refresh` 已成功完成。Unity Console 中存在插件自身网络更新失败日志和一次 MCP `console-get-logs` 参数验证异常，均与 Ability Composer 源码无关；本次没有发现 Ability Composer 编译错误。

## 六、当前未完成

```text
P3 仍只维护内存事件草稿
├─ 尚未编辑 float / int / string / Object 参数
├─ 尚未显示 Function 签名或参数类型
├─ 尚未向 .anim 或 FBX 应用事件
└─ 尚未加入 Undo / Redo 命令栈
```

后续若做事件参数编辑，应根据选择的方法签名决定字段，而不能因当前只保存 `FunctionName` 反向引入持久化组件引用。

## 七、工作区注意事项

本次改动只涉及 Ability Composer 的编辑器代码与本归档：

```text
Assets/Scripts/Tools/Editor/AbilityComposer/
└─ Right/Event/AbilityEventFunctionResolver.cs
```

新增脚本已由 Unity 生成 `.meta`。提交时必须将脚本与 `.meta` 一并纳入。
