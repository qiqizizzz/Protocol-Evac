# Protocol_Evac Agent Instructions

本文件是当前项目的 Codex 项目级指令入口。处理本项目任务时，优先遵守本文件；涉及 Unity C# 代码时，必须额外遵守项目内的 Unity C# 开发规范。

工具链入口索引见：[.codex/功能汇总.md](.codex/功能汇总.md)

## 一、完整文档地图

```text
<Protocol_Evac>/
|
|-- AGENTS.md ................................ 本文件，Codex 项目级指令入口
|-- README.md ................................ 项目简介
|
|-- .codex/
|   |-- config.toml .......................... Codex 与 Unity MCP 配置
|   |-- 功能汇总.md .......................... Codex 工具链入口索引
|   |
|   |-- work_init_CodexAI/ ................... 项目权威规范
|   |   |-- 01-项目宪章与核心原则.md .......... 项目最高原则与红线
|   |   |-- 02-AI协作与实现边界.md ............. 禁止过度安全化判空规则
|   |   `-- 04-编码规范-CSharp.md ............. Unity C# 开发权威规范
|   |
|   |-- agents/ .............................. Unity MCP 工具说明源
|   |   |-- unity-tool-list/SKILL.md .......... 当前 Unity MCP 工具列表入口
|   |   `-- <Unity MCP 工具名>/SKILL.md ....... 每个 Editor 操作的输入、行为与输出说明
|   |
|   `-- skills/
|       `-- project-archive-record/SKILL.md ... AI 归档记录的创建与续写规范
|
|-- .agents/skills/ .......................... 当前会话可发现的 Unity MCP Skill 镜像
|   `-- <Unity MCP 工具名>/SKILL.md ........... 调用具体 Unity 工具前读取的操作说明
|
|-- 设计文档/
|   |-- 玩家状态与敌人AI/
|   |   |-- 玩家状态与敌人AI设计方案.md ........ Player HFSM、Skill 与 Enemy AI 主设计
|   |   `-- 技能系统与编辑器设计方案.md ........ Skill 数据、事件与编辑器工具路线
|   |
|   `-- AI归档记录/ .......................... 按日期保存的开发交接记录
|       |-- 2026-7-16/ ....................... 早期 Player 战斗与 HFSM 交接记录
|       |-- ...
|       `-- <YYYY-M-D>/<序号-主题>.md ........ 后续按日期与顺序续写的归档记录
|
|-- Assets/ .................................. Unity 资源与主要代码目录
|-- Packages/manifest.json ................... Unity Package 依赖入口
`-- ProjectSettings/ ......................... Unity 项目设置
```

阅读顺序：

1. 先读本文件，确认项目级约束与任务入口
2. 修改代码前，阅读 `02-AI协作与实现边界.md`，确认不写过度安全化总判空
3. 涉及 `.cs` 文件时，完整阅读 `04-编码规范-CSharp.md`
4. 涉及 Player、HFSM、Ability 或 Enemy AI 时，阅读主设计文档与最新 AI 归档
5. 操作 Unity Editor 前，先读 `unity-tool-list/SKILL.md`，再读对应工具的 `SKILL.md`
6. 需要续写交接记录时，读取 `project-archive-record/SKILL.md` 与最近两份 AI 归档

新增、移动或删除文档入口时，必须同步更新本地图与 [.codex/功能汇总.md](.codex/功能汇总.md)。Unity MCP 自动生成的工具说明只在地图中保留目录模式，不逐项复制工具清单。

## 二、项目范围

- 项目类型：Unity 项目
- 主要代码目录：`Assets/`
- 本项目当前只维护根目录级 Agent 指令，不在子目录重复创建额外 Agent 指令文件

## 三、Unity C# 规范

修改、创建、审查任何 `.cs` 文件前，必须先完整阅读并严格遵守：

[.codex/work_init_CodexAI/04-编码规范-CSharp.md](.codex/work_init_CodexAI/04-编码规范-CSharp.md)

该文件是本项目 Unity C# 代码风格、注释、命名、日志、判空、Unity API 使用与代码变更摘要格式的权威规范。

## 四、执行要求

- 不确定规范细节时，先回到 [.codex/work_init_CodexAI/04-编码规范-CSharp.md](.codex/work_init_CodexAI/04-编码规范-CSharp.md) 查证，不凭记忆补全
- 修改 `.cs` 文件时，必须遵守该规范中的文件头注释、类与方法注释、字段命名、QLog 日志、错误处理、必要引用校验和判空规则
- 修改代码后，回复末尾必须按该规范附上代码变更摘要
- 非 C# 文件的修改保持最小范围，不引入与任务无关的项目结构或工具链配置

## 五、Unity MCP

Unity MCP 配置入口：

[.codex/config.toml](.codex/config.toml)

Unity MCP 能力说明目录：

[.codex/agents/](.codex/agents/)

需要操作 Unity Editor 前，先读取 [.codex/agents/unity-tool-list/SKILL.md](.codex/agents/unity-tool-list/SKILL.md) 与对应工具说明文件，再按当前会话实际可用的 MCP 工具执行。若 MCP 工具未暴露或 Unity Editor 未连接，必须明确说明连接状态，不假装已经完成 Unity 操作。
