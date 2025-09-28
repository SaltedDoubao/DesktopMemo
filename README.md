<h1 align="center">SaltedDoubao 的桌面便签 · 重构版进展</h1>

> <p align="center">面向 MVVM 架构与 Markdown 存储的全新 DesktopMemo</p>

<div align="center">

<img src="https://img.shields.io/badge/.NET-9.0-purple" />
<img src="https://img.shields.io/badge/Platform-Windows-blue" />
<img src="https://img.shields.io/badge/License-MIT-green" />

中文 README

</div>

## 📋 项目简介

DesktopMemo 重构版基于 .NET 9.0 + WPF + CommunityToolkit.Mvvm，目标是在经典桌面便签的体验基础上，提供清晰的 MVVM 分层、Markdown 文件存储与可配置的窗口行为，方便长期维护与扩展。

## ✨ 主要功能（迭代中）

- 备忘录列表与编辑区并列布局，支持 Markdown 文本（暂未渲染）
- YAML Front Matter + Markdown 文件存储，便于导入导出
- 可配置窗口：尺寸、位置、置顶模式、透明度、穿透等
- 托盘入口：显示/隐藏、新建备忘录等操作
- 计划加入：多选管理、主题切换、窗口预设等高级功能

## 🖥️ 系统要求

- **操作系统**：Windows 10 版本 1903 及以上（WPF 依赖）
- **架构**：x64
- **.NET SDK**：9.0（开发/调试）
- **运行时**：发布包内置，无需额外安装

## 🚀 快速开始

### 获取源码

```bash
# 克隆仓库
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo

# 切换到重构分支
git checkout rebuild
```

### 构建与运行

```powershell
# 还原依赖
dotnet restore DesktopMemo.sln

# 调试构建
dotnet build DesktopMemo.sln -c Debug

# 运行重构版应用
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> 若需要一次性构建/发布 Debug & Release 版本，可使用仓库根目录下的 `build.bat`。构建产物默认输出到 `artifacts/v<版本号>/bin`，可通过设置环境变量 `ArtifactsVersion` 自定义版本号。

首次运行会在可执行文件目录生成 `/.memodata`：

```
.memodata
├── content/
│   ├── index.json           # 备忘录索引
│   └── {memoId}.md          # YAML Front Matter + 正文
└── settings.json            # 窗口与全局设置
```

## 🧭 项目结构

```
DesktopMemo_rebuild/
├── DesktopMemo.sln
├── build.bat                       # 多配置构建脚本
├── docs/
│   ├── development-guide.md        # 开发/调试说明
│   └── architecture/...            # 架构设计文档
├── src/
│   ├── DesktopMemo.App/            # WPF 前端
│   │   ├── App.xaml(.cs)           # 启动与 DI 注册
│   │   ├── MainWindow.xaml(.cs)    # 主窗口
│   │   ├── ViewModels/             # MVVM 视图模型
│   │   └── Resources/              # 样式与资源
│   ├── DesktopMemo.Core/           # 领域模型与契约
│   │   ├── Contracts/
│   │   │   └── IMemoRepository.cs 等接口
│   │   └── Models/
│   │       ├── Memo.cs
│   │       └── WindowSettings.cs
│   └── DesktopMemo.Infrastructure/# 实现层（文件存储、系统服务）
│       ├── Repositories/
│       │   └── FileMemoRepository.cs
│       └── Services/
│           ├── JsonSettingsService.cs
│           └── WindowService.cs 等
├── artifacts/                      # 构建输出目录
└── publish/                        # 发布产物
```

## 🛠️ 技术栈

- **框架**：.NET 9.0、WPF
- **模式**：MVVM（基于 CommunityToolkit.Mvvm）
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **持久化**：Markdown + YAML Front Matter；窗口设置使用 JSON
- **工具链**：PowerShell 构建脚本、GitHub Actions（规划中）

## 🔍 架构要点

- `MainViewModel`：聚合备忘录集合、窗口设置与命令绑定
- `FileMemoRepository`：维护 `index.json` 与 Markdown 正文的一致性
- `JsonSettingsService`：负责读写窗口设置 `settings.json`
- `App`：集中注册依赖并挂载到 `MainWindow`
- 计划引入事件聚合、后台服务抽象以支持托盘、热键等高级能力

## ✅ 手动测试建议

- 启动应用后验证备忘录列表与编辑区的状态同步
- 调整窗口尺寸、透明度、置顶模式，确认设置可以保存/恢复
- 检查托盘菜单操作（显示/隐藏、新建便签）是否生效
- 删除 `/.memodata` 后重新运行，确认初始数据正确生成

## 🤝 贡献指南

欢迎通过 Issue 或 PR 协作：

1. Fork 本仓库
2. 创建功能分支：`git checkout -b your-feature`
3. 提交更改：`git commit -m "feat: 描述"`
4. 推送分支：`git push origin your-feature`
5. 创建 Pull Request，并附上需求背景、变更摘要与验证结果

提交前建议执行 `dotnet build` / `dotnet run`，确保基本验证通过。

## 📄 许可证

本项目使用 [MIT License](LICENSE)。

---

<div align="center">

**如果这个项目对您有帮助，请考虑给它一个 ⭐！**\
**你的 ⭐ 是我们持续迭代的动力！**

[报告 Bug](../../issues) • [请求功能](../../issues) • [贡献代码](../../pulls)

</div>
