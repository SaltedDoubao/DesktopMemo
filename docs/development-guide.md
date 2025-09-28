# DesktopMemo 开发指南

本指南面向参与 DesktopMemo 重构版开发的成员，帮助你快速了解项目结构、开发环境、构建/运行流程及常见调试路径。

## 1. 项目概览

- **解决方案**：`DesktopMemo.sln`
- **核心组件**
  - `DesktopMemo.App`：WPF 前端（UI、命令、视图模型）。
  - `DesktopMemo.Core`：领域模型与契约接口（纯 .NET 库）。
  - `DesktopMemo.Infrastructure`：文件存储、设置服务等实现。
- **数据存储**：默认写入可执行文件目录下的 `/.memodata`（Markdown + YAML Front Matter + JSON 设置）。

## 2. 开发环境要求

- Windows 10 及以上（需启用 .NET 桌面运行时支持）。
- [.NET SDK 9.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/9.0)。
- 推荐 IDE：Visual Studio 2022 (17.10+) 或 JetBrains Rider / VS Code（配合 C# 插件）。

## 3. 代码结构速览

```
DesktopMemo_rebuild/
├── DesktopMemo.sln
├── docs/                      # 文档（本指南所在目录）
└── src/
    ├── DesktopMemo.App/       # 应用入口与 MVVM 层
    ├── DesktopMemo.Core/      # 模型、接口
    └── DesktopMemo.Infrastructure/  # 基础设施实现
```

### 关键目录

- `DesktopMemo.App/ViewModels/MainViewModel.cs`：应用核心逻辑，处理备忘录列表、编辑状态与设置面板。
- `DesktopMemo.Infrastructure/Repositories/FileMemoRepository.cs`：文件存储实现，维护 `content` 目录和 `index.json`。
- `DesktopMemo.App/Resources/Styles.xaml`：全局样式与主题。

## 4. 构建与输出

项目使用 `Directory.Build.props` 统一输出路径：

- 二进制产物：`artifacts/v<版本>/bin/<项目名>/<配置>/<目标框架>/`
- 中间文件：`artifacts/v<版本>/obj/<项目名>/<配置>/<目标框架>/`

`<版本>` 默认取自 `DesktopMemo.App.csproj` 的 `Version` 属性（当前为 `2.0.0`）。如需自定义，构建前可设置环境变量 `ArtifactsVersion`（示例：PowerShell 中执行 `$env:ArtifactsVersion = '2.1.0'`）。

常用命令：

```powershell
# 还原依赖（首次或更换输出目录后务必执行）
dotnet restore DesktopMemo.sln

# 调试构建
dotnet build DesktopMemo.sln -c Debug

# 运行应用（复用已有构建产物）
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug --no-build
```

> **建议**：每次调整 `BaseOutputPath` / `BaseIntermediateOutputPath` 或版本号时，先清理旧目录（`artifacts/` 与 `src/*/bin|obj`）再执行 `restore` + `build`，避免残留文件导致重复编译错误。

## 5. 版本号管理

手动提升版本时，请确认以下位置同步更新：

- `DesktopMemo.App/DesktopMemo.App.csproj` 中的 `<Version>`、`<AssemblyVersion>`、`<FileVersion>`。
- `Directory.Build.props` 里的 `ArtifactsVersion` 默认值（决定 `artifacts/vX.Y.Z/...` 路径）。
- `MainViewModel.InitializeAppInfo()` 中的回退字符串（用于 UI 展示）。
- 文档和命令示例：搜索仓库中旧版本号（如 `2.0.0`），更新 `README.md`、`docs/development-guide.md` 等说明。
- 发布脚本或命令中的目标目录（如 `dotnet publish ... -o artifacts\publish\vX.Y.Z`）。

完成修改后，建议执行：

```powershell
dotnet clean DesktopMemo.sln
dotnet restore DesktopMemo.sln
dotnet build DesktopMemo.sln -c Debug
```

确保构建产物写入新的版本目录，应用 UI 亦能正确显示版本信息。

## 6. 调试技巧

- **Visual Studio**
  - 以 `DesktopMemo.App` 作为启动项目，使用 WPF 诊断工具观察绑定、布局。
  - 在 `MainViewModel` 中设置断点，调试命令执行与状态切换。
- **VS Code / Rider**
  - 使用 `dotnet run` 或 IDE 集成的调试配置，确保 `--no-build` 仅在已有构建的情况下使用。
- **数据问题排查**
  - 检查 `/.memodata/content` 中的 Markdown 文件及 `index.json`。
  - 若需要重置数据，可删除 `.memodata` 目录，应用会在下次启动时重新创建。

## 7. 发布与部署（草案）

1. 更新 `DesktopMemo.App.csproj` 中的 `Version` / `FileVersion`。
2. 执行 `dotnet publish`，指定目标目录，例如：
   ```powershell
   dotnet publish src/DesktopMemo.App/DesktopMemo.App.csproj -c Release -o artifacts\publish\v2.0.0
   ```
3. 将发布目录打包发布；若需要安装包，可结合 MSIX/WiX 等工具另行配置。

## 8. 质量保障

- 当前仓库尚未引入自动化测试，计划可参考 `docs/architecture/migration-plan.md`。
- 建议 PR 前至少执行 `dotnet build` 验证通过。
- 若新增依赖或调整输出目录，请在 PR 描述中说明。

## 9. 常见问题

| 问题 | 处理方式 |
| --- | --- |
| `NETSDK1005`：找不到 `project.assets.json` | 先运行 `dotnet restore`，确认新输出目录已生成；必要时删除 `artifacts/` 重新构建。 |
| 编译出现 Assembly 特性重复 | 删除旧的 `bin/obj` 与 `artifacts/` 后重新 `restore + build`。 |
| UI 未刷新 | 检查数据绑定是否调用 `OnPropertyChanged`，或确认 `MainViewModel` 的命令执行路径。 |

如有更多问题，可在仓库 issues 区登记或在团队沟通渠道反馈。
