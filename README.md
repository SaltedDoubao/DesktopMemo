<h1 align="center">DesktopMemo 重构版</h1>

> <p align="center">基于 MVVM 与文件存储的新一代 Windows 桌面便签应用</p>

<div align="center">

<img src="https://img.shields.io/badge/.NET-9.0-purple" />
<img src="https://img.shields.io/badge/Platform-Windows-blue" />
<img src="https://img.shields.io/badge/License-MIT-green" />

</div>

## 📋 项目简介

DesktopMemo 重构版采用 .NET 9.0 + WPF + CommunityToolkit.Mvvm，围绕 MVVM 架构重新实现备忘录管理、窗口设置与数据持久化，提供更清晰的分层与可维护性。

## ✨ 主要功能

- 新建、查看、编辑、删除备忘录
- 文本编辑区支持 Markdown 编写（未内置渲染）
- 列表与编辑区分栏布局，快速切换备忘录
- 窗口设置加载/保存，默认记录尺寸、位置、透明度与置顶状态

## 🗂️ 项目结构

```
DesktopMemo_rebuild/
├── DesktopMemo.sln
├── docs/
│   └── development-guide.md           # 开发指南
├── src/
│   ├── DesktopMemo.App/               # WPF 前端
│   │   ├── App.xaml(.cs)              # 启动与依赖注入入口
│   │   ├── MainWindow.xaml(.cs)       # 主界面
│   │   ├── ViewModels/
│   │   │   └── MainViewModel.cs       # 处理备忘录与设置
│   │   └── Resources/Styles.xaml      # 全局样式
│   ├── DesktopMemo.Core/              # 领域与契约
│   │   ├── Contracts/
│   │   │   ├── IMemoRepository.cs
│   │   │   └── ISettingsService.cs
│   │   └── Models/
│   │       ├── Memo.cs
│   │       └── WindowSettings.cs
│   └── DesktopMemo.Infrastructure/    # 基础设施实现
│       ├── Repositories/
│       │   └── FileMemoRepository.cs
│       └── Services/
│           └── JsonSettingsService.cs
├── artifacts/
│   └── vX.X.X/                        # 构建产物
└── publish/
    └── vX.X.X/                        # 发布产物
```

## 🛠️ 技术选型

- **框架**：.NET 9.0、WPF
- **模式**：MVVM (CommunityToolkit.Mvvm)
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **数据格式**：Markdown + YAML Front Matter、JSON
- **Markdown 工具**：Markdig（后续可用于渲染或迁移）

## 🚀 快速开始

```bash
# 克隆仓库
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo

# 还原依赖
dotnet restore DesktopMemo.sln

# 构建（产物输出到 artifacts/vX.X.X/bin/...）
dotnet build DesktopMemo.sln -c Debug

# 运行（使用同一输出目录，避免重复构建）
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug

# 发布（产物输出到 publish/vX.X.X/...）
dotnet publish src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Release --runtime win-x64 --output publish\vX.X.X\win-x64-single -p:PublishSingleFile=true -p:SelfContained=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true
```

> 如需发布不同版本，可在构建前设置 `ArtifactsVersion` 环境变量（例如 PowerShell 中执行 `setx ArtifactsVersion 2.1.0` 或临时设置 `$env:ArtifactsVersion = '2.1.0'`），产物将自动写入 `artifacts/v<版本>/bin` 与 `artifacts/v<版本>/obj`。

应用首次运行时将在可执行文件所在目录下的 `/.memodata` 创建数据目录：

```
.memodata
├── content/
│   ├── index.json                # 备忘录顺序索引
│   └── {memoId}.md               # YAML Front Matter + 正文
└── settings.json                 # 窗口设置
```

## 🧭 架构要点

- `MainViewModel` 负责加载/保存备忘录集合，处理 UI 绑定及命令
- `FileMemoRepository` 以 YAML Front Matter 形式读写 Markdown 文件，维护索引与内容一致性
- `JsonSettingsService` 负责序列化 `WindowSettings`
- 通过 `App` 构建 DI 容器并注入到 `MainWindow`

开发者快速上手、版本号切换说明参见 [`docs/development-guide.md`](docs/development-guide.md)。

## 🤝 贡献

欢迎提交 Issue / PR：

1. Fork 仓库
2. 创建分支：`git checkout -b feature/your-feature`
3. 提交修改：`git commit -m "feat: your feature"`
4. 推送分支：`git push origin feature/your-feature`
5. 发起 Pull Request

建议在提交前运行 `dotnet build` 确保通过编译。

## 📝 许可证

使用 [MIT License](LICENSE)。

---

<div align="center">

**如果这个项目对您有帮助，请考虑给它一个 ⭐！**\
**您的⭐就是我更新的最大动力！**

[报告Bug](../../issues) • [请求功能](../../issues) • [贡献代码](../../pulls)

</div>
