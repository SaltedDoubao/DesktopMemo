# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## 环境设置

- **语言**: 始终使用简体中文回复
- **环境**: Windows PowerShell环境，注意命令格式
- **构建前清理**: 运行 `dotnet build` 前必须删除对应版本号的 artifacts/ 目录（例如当前版本2.0.0，需删除 artifacts/v2.0.0/）
- **上下文工具**: 需要代码生成、设置配置或库/API文档时，自动使用 context7 MCP 工具

## 常用开发命令

```powershell
# 还原依赖（首次或更换输出目录后务必执行）
dotnet restore DesktopMemo.sln

# 调试构建
dotnet build DesktopMemo.sln --configuration Debug

# 运行应用
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug

# 发布单文件可执行程序
.\build_exe.bat

# 清理构建产物
dotnet clean DesktopMemo.sln
```

## 核心架构

这是一个基于 .NET 9.0 + WPF + MVVM 的桌面便签应用，采用三层架构：

### 项目结构
- **DesktopMemo.App**: WPF前端层，包含UI、视图模型和命令
- **DesktopMemo.Core**: 领域模型和契约接口（纯.NET库）
- **DesktopMemo.Infrastructure**: 基础设施实现层（文件存储、系统服务）

### 关键文件
- `src/DesktopMemo.App/ViewModels/MainViewModel.cs`: 应用核心逻辑，处理备忘录列表、编辑状态与设置
- `src/DesktopMemo.Infrastructure/Repositories/FileMemoRepository.cs`: Markdown文件存储实现
- `src/DesktopMemo.App/App.xaml.cs`: 依赖注入配置和应用启动逻辑

### 数据存储
- 数据存储在可执行文件目录的 `/.memodata` 文件夹
- 备忘录使用 Markdown + YAML Front Matter 格式
- 设置使用 JSON 格式存储

### 构建配置
- 使用 `Directory.Build.props` 统一输出路径到 `artifacts/v<版本>/`
- 版本号管理涉及多个文件同步更新（详见开发规范文档）

## 异步编程规范

**关键原则**: 避免 `async void`，使用 fire-and-forget 模式防止死锁：

```csharp
// ❌ 错误 - 可能导致死锁
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await SomeAsyncOperation();
}

// ✅ 正确 - fire-and-forget 模式
private void Button_Click(object sender, RoutedEventArgs e)
{
    _ = Task.Run(async () => await HandleButtonClickAsync());
}
```

UI操作必须在UI线程，IO操作应在后台线程。设置保存使用非阻塞模式。

## 代码约定

- 使用统一的常量类（`DesktopMemo.Core.Constants`）避免硬编码
- Win32 API 调用必须包含错误检查
- 异常处理分级：特定异常提供用户友好提示，通用异常记录详细日志
- 设置保存采用原子性模式：先保存文件，成功后再更新内存状态

## 调试技巧

- 在 Debug 模式下应用输出详细调试信息到 Visual Studio 输出窗口
- 检查 `/.memodata/settings.json` 文件完整性
- 使用 Visual Studio 诊断工具监控线程切换和性能