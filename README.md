# DesktopMemo 桌面便签

> 一个功能强大、界面简洁的Windows桌面便签应用程序

![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/Platform-Windows-blue)
![License](https://img.shields.io/badge/License-MIT-green)

## 📋 项目简介

DesktopMemo 是一个基于 WPF 技术开发的桌面便签应用，提供了丰富的窗口管理功能和便签管理能力。无论是日常记事、工作备忘还是灵感记录，都能为您提供便捷的解决方案。

## ✨ 主要功能

### 🏠 窗口管理
- **三种置顶模式**：普通模式、桌面层置顶、总是置顶
- **位置记忆**：自动保存和恢复窗口位置
- **点击穿透**：启用后可点击穿透到桌面
- **窗口固定**：防止意外拖动窗口
- **透明度调节**：可调节窗口透明度

### 📝 便签功能
- **多便签管理**：支持创建、编辑、删除多个便签
- **实时保存**：内容自动保存到本地
- **快速切换**：便签间快速切换浏览

### 🔧 系统功能
- **系统托盘**：最小化到托盘，不占用任务栏
- **智能退出**：退出时可选择托盘化或完全退出
- **开机自启**：支持设置开机自动启动
- **设置持久化**：所有设置自动保存

### 🎨 用户体验
- **现代界面**：简洁美观的用户界面
- **快捷操作**：丰富的键盘快捷键支持
- **状态提示**：实时状态信息显示
- **气泡提示**：重要操作的友好提示

## 🖥️ 系统要求

- **操作系统**: Windows 10 版本 1809 或更高版本
- **架构**: x64
- **.NET运行时**: 无需单独安装（已打包在可执行文件中）
- **内存**: 建议 512MB 可用内存
- **存储**: 约 200MB 磁盘空间

## 🚀 快速开始

### 下载安装

1. 在 [Releases](../../releases) 页面下载最新版本的 `DesktopMemo.exe`
2. 将文件保存到您希望的位置（如 `C:\Program Files\DesktopMemo\`）
3. 双击运行即可开始使用

> **注意**: 首次运行时，Windows Defender 可能会进行安全扫描，这是正常现象。

### 基本使用

1. **创建便签**: 点击 `+` 按钮或使用托盘菜单"新建便签"
2. **编辑内容**: 直接在文本区域输入内容，自动保存
3. **切换便签**: 使用便签列表或快捷键切换
4. **调整窗口**: 拖拽窗口边缘调整大小，拖拽标题栏移动位置

## 📖 功能详解

### 窗口模式

| 模式 | 描述 | 适用场景 |
|------|------|----------|
| 普通模式 | 普通窗口，可被其他窗口覆盖 | 临时记录，不干扰其他操作 |
| 桌面层置顶 | 置于桌面之上，但在应用窗口之下 | 长期显示，不遮挡工作窗口 |
| 总是置顶 | 始终保持在最前端 | 重要提醒，需要时刻可见 |

### 系统托盘功能

右键点击托盘图标可以访问以下功能：
- 显示/隐藏主窗口
- 新建便签
- 窗口控制（置顶模式、穿透模式）
- 位置管理（保存/恢复位置）
- 系统设置（自启动、透明度）
- 退出程序

### 退出行为

程序提供智能退出选择：
- **最小化到托盘**: 隐藏窗口但保持程序运行
- **完全退出**: 彻底关闭程序
- **不再提示**: 记住选择，下次直接执行

## 🛠️ 开发信息

### 技术栈

- **框架**: .NET 8.0 + WPF
- **语言**: C# 12.0
- **UI**: XAML + Code-behind
- **数据存储**: JSON 文件
- **包管理**: NuGet

### 项目结构

```
DesktopMemo/
├── App.xaml              # 应用程序入口
├── App.xaml.cs           # 应用程序逻辑
├── MainWindow.xaml       # 主窗口界面
├── MainWindow.xaml.cs    # 主窗口逻辑
├── AssemblyInfo.cs       # 程序集信息
├── DesktopMemo.csproj    # 项目文件
├── DesktopMemo.sln       # 解决方案文件
└── README.md             # 说明文档
```

### 构建项目

```bash
# 克隆项目
git clone <repository-url>
cd DesktopMemo

# 还原依赖
dotnet restore

# 构建项目
dotnet build --configuration Release

# 发布单文件版本
dotnet publish --configuration Release --output ./publish
```

### 开发环境要求

- **IDE**: Visual Studio 2022 或 Visual Studio Code
- **.NET SDK**: 8.0 或更高版本
- **Windows SDK**: 最新版本

## 📄 数据存储

应用程序数据保存在以下位置：

```
%APPDATA%\DesktopMemo\
├── settings.json         # 应用设置
└── Data\
    └── memos.json        # 便签数据
```

## 🔧 配置文件

### settings.json 结构

```json
{
  "SavedX": 100.0,
  "SavedY": 100.0,
  "PositionRemembered": false,
  "AutoRestorePositionEnabled": true,
  "WindowOpacity": 1.0,
  "TopmostMode": "Desktop",
  "ClickThroughEnabled": false,
  "AutoStartEnabled": false,
  "NoteContent": "",
  "ShowExitPrompt": true
}
```

## 🤝 贡献指南

欢迎贡献代码！请遵循以下步骤：

1. Fork 本项目
2. 创建功能分支 (`git checkout -b feature/AmazingFeature`)
3. 提交更改 (`git commit -m 'Add some AmazingFeature'`)
4. 推送到分支 (`git push origin feature/AmazingFeature`)
5. 创建 Pull Request

## 📝 更新日志

### v1.1.0 (Latest)
- ✅ 项目重命名为 DesktopMemo
- ✅ 新增智能退出提示功能
- ✅ 优化用户体验和界面
- ✅ 改进托盘菜单功能

### v1.0.0
- 🎉 首次发布
- ✅ 基础便签功能
- ✅ 窗口管理功能
- ✅ 系统托盘支持

## 🐛 问题反馈

遇到问题？请通过以下方式反馈：

1. [GitHub Issues](../../issues) - 提交Bug报告或功能请求
2. 详细描述问题复现步骤
3. 附上系统环境信息和错误截图

## 📄 许可证

本项目采用 [MIT 许可证](LICENSE) - 详见 LICENSE 文件

## 🙏 致谢

感谢所有为这个项目做出贡献的开发者和用户！

---

<div align="center">

**如果这个项目对您有帮助，请考虑给它一个 ⭐！**

[报告Bug](../../issues) • [请求功能](../../issues) • [贡献代码](../../pulls)

</div>
