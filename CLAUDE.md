# DesktopMemo 重构版 · Claude Code 协作指南

> 面向使用 Claude Code 协助开发本项目的说明文档。

## 项目简介

- **技术栈**：.NET 9.0 · WPF · CommunityToolkit.Mvvm
- **目标**：重构经典 DesktopMemo，提供 MVVM 架构、Markdown 文件存储、可配置窗口行为（置顶模式、透明度、穿透等）。
- **仓库布局**：
  - `src/DesktopMemo.App`：WPF 前端（视图、资源、ViewModel）。
  - `src/DesktopMemo.Core`：领域模型与契约接口。
  - `src/DesktopMemo.Infrastructure`：文件存储、窗口/托盘等系统服务实现。
  - `docs/`：架构与迁移文档。
  - `.local/feedback.md`：当前迭代的产品反馈清单（实时更新）。

## 开发环境

1. 需要 .NET SDK 9.0（或兼容）。
2. Windows 平台（WPF + Win32 托盘依赖）。
3. 推荐工具：Visual Studio / VS Code + C# 扩展 / Rider；Claude Code 作为协作助手。

## 常用命令

```powershell
# 还原依赖（首次或切换输出目录后务必执行）
dotnet restore DesktopMemo.sln

# 调试构建
dotnet build DesktopMemo.sln -c Debug

# 运行
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> 运行时数据位于可执行文件所在目录下的 `/.memodata`；如需重置，可删除该目录并重启。

## Claude Code 协作约定

1. **保持响应简洁**：面向开发者，重点说明修改目的、影响范围、验证计划。
2. **引用规范**：引用仓库代码时使用 Cursor 约定的定位格式：

```12:18:src/DesktopMemo.App/MainWindow.xaml
<示例代码片段>
```

3. **语言风格**：
   - 与团队沟通使用简体中文。
   - 提交到仓库的代码/注释保持统一（当前以中文为主，必要时可双语说明）。
4. **反馈同步**：开发前先核对 `.local/feedback.md`，避免遗漏或重复；完成项需及时标注 ✅/⏳。
5. **变更说明**：每次修改需包含：
   - 修改摘要（针对界面、ViewModel、服务等分层描述）。
   - 受影响文件、验证方式（编译/运行/手动测试）。
6. **命令执行**：在 Claude Code 中运行 `dotnet` 命令前确认当前目录为仓库根目录；必要时附上命令输出。

## 代码风格速览

- **MVVM**：保持 ViewModel 无 UI 依赖；所有 UI 事件通过命令或事件转发至 ViewModel。
- **文件存储**：备忘录通过 `FileMemoRepository` 写入 `content/{memoId}.md`，带 YAML Front Matter。
- **命名约定**：使用 PascalCase（类型）、camelCase（字段/变量）；异步方法使用 `Async` 后缀。
- **依赖注入**：通过 `App.xaml.cs` 注册服务。新增服务需同时更新 `DesktopMemo.Infrastructure` 与 `App`。

## 手动测试建议

1. 启动应用后验证：备忘录列表悬浮、点击进入编辑、标题栏动态变化。
2. 检查设置面板：透明度滑块、置顶模式切换、穿透模式勾选、窗口位置预设/自定义输入。
3. 托盘菜单：显示/隐藏、新建、窗口定位、导入/导出。

## 常见问题

| 场景 | 处理方式 |
| --- | --- |
| 应用运行但窗口不可见 | 检查是否被最小化到托盘，托盘菜单选择“显示窗口”。 |
| 透明度/穿透状态不同步 | 确认 `WindowService` 与 `TrayService` 在 ViewModel 中同步更新。 |
| 编译报错（接口成员缺失） | 检查 Core 契约与 Infrastructure 实现是否同步修改。 |

## 贡献流程

1. 在分支中实现需求/修复；保持提交信息清晰（`feat: ...` / `fix: ...`）。
2. 自测通过 (`dotnet build` / `dotnet run`) 后提交 PR。
3. PR 描述需包含：
   - 背景/需求
   - 变更概览（列表）
   - 测试结果

## 参考文档

- `README.md`：项目总览与运行说明。
- `docs/architecture/overview.md`：架构设计要点。
- `docs/rewrite-analysis.md`：旧版系统分析及重构计划。
- `.local/feedback.md`：产品反馈与待办。

---

如需在 Claude Code 中自动化变更，请严格遵循工程规范，先评估影响再执行命令。欢迎补充更多协作经验与最佳实践。

