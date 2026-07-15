# Protocol_Evac Agent Instructions

本文件是当前项目的 Codex 项目级指令入口。处理本项目任务时，优先遵守本文件；涉及 Unity C# 代码时，必须额外遵守项目内的 Unity C# 开发规范。

工具链入口索引见：[.codex/功能汇总.md](.codex/功能汇总.md)

## 一、项目范围

- 项目类型：Unity 项目
- 主要代码目录：`Assets/`
- 本项目当前只维护根目录级 Agent 指令，不在子目录重复创建额外 Agent 指令文件

## 二、Unity C# 规范

修改、创建、审查任何 `.cs` 文件前，必须先完整阅读并严格遵守：

[.codex/work_init_CodexAI/04-编码规范-CSharp.md](.codex/work_init_CodexAI/04-编码规范-CSharp.md)

该文件是本项目 Unity C# 代码风格、注释、命名、日志、判空、Unity API 使用与代码变更摘要格式的权威规范。

## 三、执行要求

- 不确定规范细节时，先回到 [.codex/work_init_CodexAI/04-编码规范-CSharp.md](.codex/work_init_CodexAI/04-编码规范-CSharp.md) 查证，不凭记忆补全
- 修改 `.cs` 文件时，必须遵守该规范中的文件头注释、类与方法注释、字段命名、日志包裹、必要引用校验和判空规则
- 修改代码后，回复末尾必须按该规范附上代码变更摘要
- 非 C# 文件的修改保持最小范围，不引入与任务无关的项目结构或工具链配置

## 四、Unity MCP

Unity MCP 配置入口：

[.codex/config.toml](.codex/config.toml)

Unity MCP 能力说明目录：

[.codex/agents/](.codex/agents/)

需要操作 Unity Editor 前，先读取 [.codex/agents/unity-tool-list/SKILL.md](.codex/agents/unity-tool-list/SKILL.md) 与对应工具说明文件，再按当前会话实际可用的 MCP 工具执行。若 MCP 工具未暴露或 Unity Editor 未连接，必须明确说明连接状态，不假装已经完成 Unity 操作。
