<h1 align="center">SaltedDoubao 的桌面便签</h1>

> <p align="center">面向 MVVM 架构与 Markdown 存储的全新桌面备忘录程序</p>

<div align="center">

<img src="https://img.shields.io/badge/.NET-9.0-purple" />
<img src="https://img.shields.io/badge/Platform-Windows-blue" />
<img src="https://img.shields.io/badge/License-MIT-green" />

中文 README | [English README](README_en.md)

</div>

## 📋 项目简介

DesktopMemo v2 基于 .NET 9.0 + WPF + CommunityToolkit.Mvvm，目标是在经典桌面便签的体验基础上，提供清晰的 MVVM 分层、Markdown 文件存储与可配置的窗口行为，方便长期维护与扩展。

## ✨ 主要功能

- 快速创建备忘录，以便您记录重要事项
- 三种置顶方式，选择最适合您的方式让窗口留在桌面

## 🖥️ 系统要求

- **操作系统**：Windows 10 版本 1903 及以上（WPF 依赖）
- **架构**：x86_64
- **.NET SDK**：9.0（开发/调试）
- **.NET runtime**：发布包内置，无需额外安装

## 🚀 快速开始

### 获取源码

```bash
# 克隆仓库
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo
```

### 构建与运行

```powershell
# 还原依赖
dotnet restore DesktopMemo.sln

# 调试构建
dotnet build DesktopMemo.sln --configuration Debug

# 运行应用
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> 若需要构建exe可执行文件，可使用仓库根目录下的 `build_exe.bat`。构建产物默认输出到 `artifacts/v<版本号>/bin`，可通过设置环境变量 `ArtifactsVersion` 自定义版本号。

首次运行会在可执行文件目录生成 `/.memodata`：

```
.memodata
├── content/
│   ├── index.json           # 备忘录索引
│   └── {memoId}.md          # YAML Front Matter + 正文
└── settings.json            # 窗口与全局设置
```

### 快捷键

> 全局
- Ctrl+N - 快速新建备忘录
> 仅在编辑页面可用
- Tab - 插入缩进（4个空格）
- Shift+Tab - 减少缩进
- Ctrl + S - 保存当前备忘录（似乎没有必要）
- Ctrl + Tab / Ctrl + Shift + Tab - 切换备忘录
- Ctrl + F / Ctrl+H - 查找/替换
- F3 / Shift + F3 - 查找下一个/上一个
- Ctrl + ] / Ctrl + [ - 增加/减少缩进
- Ctrl + D - 向下复制当前行

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

## 🤝 贡献指南

欢迎通过 Issue 或 PR 协作：

1. Fork 本仓库
2. 创建功能分支：`git checkout -b your-feature`
3. 提交更改：`git commit -m "commit message"`
4. 推送分支：`git push origin your-feature`
5. 创建 Pull Request，并附上需求背景、变更摘要与验证结果

如果未更改版本号，使用`dotnet build`构建项目前**必须**清空 `artifacts/vX.X.X(对应版本号)` 目录

提交前建议执行 `dotnet build` / `dotnet run`，确保基本验证通过。

其他开发注意事项详见 [应用开发规范.md](docs/应用开发规范.md)

## 📝 更新日志

详见 [Releases](../../releases)

## 🚧 施工规划

### 近期可能实现的更新
- [ ] 适配多语言
- [ ] 添加预设窗口大小方案
- [ ] 添加主题更换功能

### 未来更新趋势
- [ ] Todo-list功能

## 📄 许可证

本项目使用 [MIT License](LICENSE)。

---

<div align="center">

**如果这个项目对您有帮助，请考虑给它一个 ⭐！**\
**您的⭐就是我更新的最大动力！**

[报告 Bug](../../issues) • [请求功能](../../issues) • [贡献代码](../../pulls)

</div>
