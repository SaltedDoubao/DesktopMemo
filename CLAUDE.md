# Claude Code 协作手册

## 速览
- **技术栈**：.NET 9.0 · WPF · CommunityToolkit.Mvvm
- **目标**：重构 DesktopMemo，提供基于 Markdown 的备忘录、可配置窗口行为与现代 MVVM 架构
- **用途**：面向使用 Claude Code 协作的开发者，概述工作流、约定与参考资料

## 仓库导览
- `src/DesktopMemo.App`：WPF 前端（视图、资源、ViewModel）
- `src/DesktopMemo.Core`：领域模型与契约接口
- `src/DesktopMemo.Infrastructure`：文件存储、窗口与托盘等系统服务实现
- `docs/`：架构说明与迁移文档
- `.local/feedback.md`：迭代反馈与待办，需随时同步状态

## 环境准备
- Windows 平台，已安装 .NET SDK 9.0（或兼容版本）
- 推荐工具：Visual Studio / VS Code（配 C# 扩展）/ Rider，Claude Code 用作协作助手
- 首次拉取或切换输出目录后务必还原依赖

## 常用命令
```powershell
# 还原依赖（从仓库根目录执行）
dotnet restore DesktopMemo.sln

# 调试构建
dotnet build DesktopMemo.sln -c Debug

# 启动应用
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```
> 运行时数据位于可执行文件目录下的 `/.memodata`，如需重置可删除该目录后重启应用。

## 协作约定
- **响应风格**：答复保持简洁，聚焦修改目的、影响范围与验证方式
- **引用规范**：引用仓库代码时使用 Cursor 约定格式 `起始行:结束行:文件路径`
- **语言要求**：团队沟通使用简体中文；代码与注释优先保持中文，必要时可双语说明
- **命令执行**：在 Claude Code 中运行 `dotnet` 前确认当前目录位于仓库根；必要时附上命令输出
- **同步反馈**：开发前先核对 `.local/feedback.md`，完成项及时标注 ✅/⏳，避免遗漏与冲突

## 开发流程
- 明确需求：先阅读反馈清单与相关文档，确认影响范围
- 规划修改：按 App / Core / Infrastructure 分层描述变更，提前评估依赖
- 实施变更：保持 ViewModel 无 UI 依赖，遵循 MVVM 命令/事件转发约定
- 汇报结果：在更新描述中记录摘要、受影响文件与验证步骤（编译/运行/手测）

## 代码风格提示
- ViewModel 仅依赖契约接口；异步方法命名以 `Async` 结尾
- 文件存储通过 `FileMemoRepository` 写入 `content/{memoId}.md`，包含 YAML Front Matter
- 命名采用 PascalCase（类型）与 camelCase（字段/变量），遵循已有约定
- 新增服务需同步更新 `DesktopMemo.Infrastructure` 与 `App.xaml.cs` 中的依赖注入

## 手动测试检查单
- 验证备忘录列表悬浮、点击进入编辑、标题栏正确反馈当前状态
- 检查设置面板：透明度滑块、置顶模式、穿透模式、窗口位置预设及自定义输入
- 体验托盘菜单：显示/隐藏、新建、窗口定位、导入/导出动作是否生效

## 常见问题
| 场景 | 处理方式 |
| --- | --- |
| 应用运行但窗口不可见 | 检查是否最小化至托盘，通过托盘菜单选择“显示窗口” |
| 透明度或穿透状态不同步 | 确认 `WindowService` 与 `TrayService` 在 ViewModel 中同步更新 |
| 编译报错提示接口成员缺失 | 核对 Core 契约定义与 Infrastructure 实现是否一起调整 |

## 参考资料
- `README.md`：项目总览、构建与运行说明
- `.local/feedback.md`：产品反馈与当前迭代事项

---
在 Claude Code 中执行自动化操作前，请先评估影响与依赖，并遵循上述协作约定。欢迎在实践中补充更多经验。

