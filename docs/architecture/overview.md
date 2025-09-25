# DesktopMemo 重构版架构概述

## 解决方案结构

- `DesktopMemo.App`：WPF 前端应用，负责视图、ViewModel 以及依赖注入 Composition Root。
- `DesktopMemo.Core`：领域层与契约定义，包括备忘录模型、窗口设置模型以及存储/设置服务接口。
- `DesktopMemo.Infrastructure`：基础设施实现，提供文件系统 Memo 仓储与 JSON 设置服务。
- `docs`：设计文档与迁移指南。
- `backups`：预留旧版本数据备份目录。

## 数据目录

默认数据存储路径：`%APPDATA%/DesktopMemo`

- `content/{memoId}.md`：采用 YAML Front Matter 的 Markdown 文件，包含 `id`、`title`、`createdAt`、`updatedAt`、`isPinned`、`tags` 等字段。
- `content/index.json`：记录备忘录排列顺序的索引文件。
- `settings.json`：窗口与外观设置，使用 `DesktopMemo.Core.Models.WindowSettings` 序列化。

## MVVM 模块

- `MainViewModel`：加载、编辑与删除备忘录，负责命令调度与窗口设置保存。
- `MemoListViewModel`：预留列表视图扩展点，当前由 `MainViewModel` 直接维护集合。
- UI 通过 `MainWindow.xaml` 的绑定实现列表、编辑器与指令按钮的交互。

## 依赖注入

在 `App.xaml.cs` 中配置 DI 容器：

```csharp
services.AddSingleton<IMemoRepository>(_ => new FileMemoRepository(dataDirectory));
services.AddSingleton<ISettingsService>(_ => new JsonSettingsService(dataDirectory));
services.AddSingleton<MainViewModel>();
```

应用启动时解析 `MainViewModel`，并在同步上下文中执行初始化以确保 UI 线程安全。

## 未来扩展方向

- 引入 `IWindowPlacementService`、`INotifyIconService` 等接口抽象，进一步解耦平台 API。
- 增加 `MemoEditView`、`SettingsView` 用户控件，与 `MainViewModel` 拆分为更细颗粒 ViewModel。
- 提供 `DesktopMemo.Infrastructure.Migrations` 模块，实现旧版本数据到新结构的转换。


