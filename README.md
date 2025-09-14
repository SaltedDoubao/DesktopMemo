<h1 align="center">SaltedDoubao 的桌面便签</h1>

> <p align="center">一个简单的 Windows 桌面便签应用程序</p>

<div align="center">

<img src="https://img.shields.io/badge/.NET-8.0-purple" />
<img src="https://img.shields.io/badge/Platform-Windows-blue" />
<img src="https://img.shields.io/badge/License-MIT-green" />

中文 README | [English README](./README_en.md)

</div>

## 📋 项目简介

DesktopMemo 是一个基于 WPF 技术开发的桌面便签应用，提供了丰富的窗口管理功能和便签管理能力。无论是日常记事、工作备忘还是灵感记录，都能为您提供便捷的解决方案。

## ✨ 主要功能

- **三种置顶模式**：普通模式、固定在桌面、固定在窗口上层
- **位置记忆**：自动保存和恢复窗口位置
- **点击穿透**：启用后可点击穿透到桌面，不会影响到您正常工作
- **窗口固定**：防止意外拖动窗口
- **快捷操作**：丰富的键盘快捷键支持

## 🖥️ 系统要求

- **操作系统**: Windows 10 版本 1809 或更高版本
- **架构**: x86_64
- **.NET运行时**: 无需单独安装（已打包在可执行文件中）
- **内存**: 建议 512MB 可用内存
- **存储**: 约 200MB 磁盘空间

## 🚀 快速开始

### 下载安装

1. 在 [Releases](../../releases) 页面下载最新版本的 `DesktopMemo.exe`
2. 将应用程序保存在您能找到的位置
3. 双击运行即可开始使用

> **注意**: 首次运行时，Windows Defender 可能会进行安全扫描，甚至报错报毒，这是正常现象。

### 基本使用

1. **创建便签**: 点击 `+` 按钮或使用托盘菜单"新建便签"
2. **编辑内容**: 直接在文本区域输入内容，第一行为标题，第二行开始为正文
3. **切换便签**: 使用便签列表或快捷键切换
4. **调整窗口**: 拖拽窗口边缘调整大小，拖拽标题栏移动位置

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

### 构建项目

```bash
# 克隆项目
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo

# 还原依赖
dotnet restore

# 构建项目
dotnet build --configuration Release

# 运行项目
dotnet run

# 发布单文件版本
dotnet publish DesktopMemo.csproj --configuration Release --output ./publish
```

## 🛠️ 开发信息

### 技术栈

- **框架**: .NET 8.0 + WPF
- **语言**: C# 12.0
- **UI**: XAML + Code-behind
- **数据存储**: JSON
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


## 📄 数据存储

应用程序数据保存在以下位置：

```
DesktopMemo.exe       # 应用程序
Data\
├── settings.json     # 应用设置
└── content\          # 便签数据
```

## 🔧 配置文件

### settings.json 结构

```json
{
  "SavedX": 100.0,
  "SavedY": 100.0,
  "PositionRemembered": false,
  "AutoRestorePositionEnabled": true,
  "TopmostMode": "Desktop",
  "ClickThroughEnabled": false,
  "AutoStartEnabled": false,
  "NoteContent": "",
  "ShowExitPrompt": true
  ...
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

详见 [Releases](../../releases)

## 🚧 施工规划

### 近期可能实现的更新
- [ ] 使用yaml存储项目配置
- [ ] 添加多选、删除功能
- [ ] 添加预设窗口大小方案
- [ ] 添加主题更换功能

### 未来更新趋势
- [ ] 重构项目
- [ ] 适配多语言

## 🐛 问题反馈

遇到问题？请通过以下方式反馈：

1. [GitHub Issues](../../issues) - 提交Bug报告或功能请求
2. 详细描述问题复现步骤
3. 附上系统环境信息和错误截图

## 🏛️ 许可证

本项目采用 [MIT 许可证](LICENSE) - 详见 LICENSE 文件

## 🙏 致谢

感谢所有为这个项目做出贡献的开发者和用户！

---

<div align="center">

**如果这个项目对您有帮助，请考虑给它一个 ⭐！**\
**您的⭐就是我更新的最大动力！**

[报告Bug](../../issues) • [请求功能](../../issues) • [贡献代码](../../pulls)

</div>
