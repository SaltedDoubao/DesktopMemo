# DesktopMemo 重写准备分析

## 项目概览
- `DesktopMemo.csproj`：单一 WPF 桌面项目，目标框架 `net8.0-windows`，启用 `UseWPF`/`UseWindowsForms` 并发布为单文件、自带运行时。
- 核心代码集中在 `MainWindow.xaml`（约 1k 行 XAML）与 `MainWindow.xaml.cs`（约 4.6k 行 C#），其余文件仅为入口框架。
- 运行时数据目录：`Data/settings.json` + `Data/content/*.md`（发布包中已附带示例数据）。
- NuGet 依赖：`System.Drawing.Common`、`Microsoft.VisualBasic`（仅用于辅助 API，实际使用场景待确认）。

## 运行时架构与职责
- 启动流程：`App.xaml` 指向 `MainWindow`，无显式 Bootstrap 逻辑。
- `MainWindow` 既承载 UI，又承担全部业务逻辑、状态管理、存储读写、Win32 互操作、托盘集成等功能，缺乏模块化边界。
- 数据模型以内部 `record` 定义于 `MainWindow.xaml.cs` 中：
  - `MemoModel`：完整备忘录（含内容）。
  - `MemoMetadata`、`MemosMetadata`、`MemosData`：历史兼容结构，仍保留读取逻辑。
  - `WindowSettings`：保存窗口/交互状态到 `settings.json`。
- 定时器：`DispatcherTimer` 用于桌面模式层级重置、位置显示更新、防抖保存。
- P/Invoke：`SetWindowPos`、`FindWindow` 等控制窗口层级与点击穿透。
- WinForms 托盘：`NotifyIcon` + 自定义暗色渲染器，处理窗口显示、置顶、导入导出等菜单。

## UI 层概述
- `MainWindow.xaml` 定义玻璃拟态整体布局：无标题栏透明窗口、内置设置抽屉面板、列表 / 编辑双视图。
- 资源包含自定义按钮、滚动条、动画、单选框等样式，未拆分到独立资源字典。
- 列表控件使用 `ListBox` + `VirtualizingStackPanel`，每个条目由代码动态构建卡片（无数据模板）。
- 文本编辑采用 `TextBox`，提供 markdown 快捷键（标题、代码块、列表）、缩进控制、查找替换等增强。
- 设置面板：窗口置顶模式、位置预设、自定义坐标、背景透明度滑块、穿透模式、开机自启等。

## 数据存储与迁移逻辑
- 当前主存储：单个备忘录对应 `Data/content/{CreatedTime:yyyyMMdd_HHmmss}.md`。内容写入时仅包含一级标题 + 正文，无元数据头。
- 设置文件 `settings.json` 保存窗口状态、选项、当前编辑的临时文本等。
- 兼容旧格式：
  - `notes.json`（最早版本单条笔记）。
  - `memos.json`、`memos_metadata.json`（多条结构 + 内容文件）。
- 迁移流程（`LoadMemosFromDiskAsync`）：优先读取 Markdown → 若为空尝试元数据 JSON → 再尝试旧 JSON → 最后 `notes.json`。迁移成功后写回 Markdown。
- 数据一致性风险：
  - Markdown 文件名与 `MemoModel.Id` 无关联，更新内容时重新生成文件，存在重复 / 悬挂文件。
  - `SaveSingleMemoToMarkdownAsync` 以 `CreatedTime` 生成文件名，若 `CreatedTime` 未锁定，重复更新可能覆盖历史或产生冲突。
  - 列表刷新基于内存 `_memos` 集合，与磁盘状态不同步，缺少索引文件确保引用稳定。

## 功能清单
- 备忘录管理：新建、编辑、自动保存、防抖、列表预览、上下切换。
- 查找/替换、缩进、代码块、列表快捷键、行复制等编辑增强。
- 窗口特性：三种置顶模式（普通 / 桌面 / 永远置顶）、玻璃拟态 UI、透明背景、可穿透、固定窗口防拖动。
- 位置管理：预设九宫格、自定义输入、记住位置、自动恢复。
- 设置面板：背景透明度滑块 + 细节展示。
- 托盘菜单：窗口控制、位置、穿透、导入导出、关于、退出提示复位。
- 其他：开机自启动注册表写入、导入导出 TXT、点击穿透 Win32 扩展。

## 已识别的主要问题
**结构性**
- 单一 `MainWindow` 文件过大（~4.6k 行），集成 UI/业务/存储/系统交互，缺乏 MVVM 或分层架构，难以维护与测试。
- 逻辑重复：存在 `async Task` 与 `async void` 双版本（如 `LoadMemosFromDisk`），事件内多次调用同一保存逻辑，易出现竞态。
- 资源耦合：列表项由代码动态拼装 UI，XAML 与后台混杂。

**数据与存储**
- Markdown 文件命名缺少稳定主键，`CreatedTime` 改动或同一时间创建多条会导致覆盖 / 重复文件。
- 删除 / 重命名策略缺失，容易出现孤儿文件、重复数据。
- Markdown 内容未保留标签、描述等元数据（`MemoMetadata.Tags/Description` 未写回），导致信息丢失。
- `settings.json` 存储当前编辑内容，与 Markdown 主存储重复（不一致时以哪份为准不明）。

**异步与线程**
- 多处 `async void`（事件处理器外）缺乏错误处理；定时器回调内调用异步保存，异常吞掉。
- 防抖定时器 (`_autoSaveMemoTimer`) 与编辑事件耦合紧密，重写时需重新设计保存策略。

**平台互操作**
- 使用 WinForms `NotifyIcon`，需要 STA 上下文，重写时可考虑 WPF 原生 / Windows App SDK 方案。
- P/Invoke 操作窗口层级实现桌面模式，但对多显示器、高 DPI、任务栏交互影响未验证。

**可测试性**
- 所有逻辑依赖 `MainWindow` 状态，缺少抽象接口，无法编写单元测试或进行依赖注入。
- 无日志体系，错误通过 `Debug.WriteLine` 输出。

## 重写设计建议
1. **采用 MVVM 分层**
   - `View`：`MainWindow` 及独立 UserControl（列表、编辑、设置），使用 DataTemplate 绑定。
   - `ViewModel`：负责 UI 状态、命令、导航（主页 / 编辑 / 设置）。
   - `Model` 与 `Service`：
     - `IMemoRepository`（文件 / SQLite / LiteDB 等实现）。
     - `ISettingsService`（窗口与用户偏好）。
     - `ITopmostService`、`IWindowPositionService`、`INotifyIconService` 分离系统交互。

2. **重新定义数据存储**
   - 为备忘录生成稳定 UUID，Markdown 文件命名 `{memoId}.md`；头部增加 YAML Front Matter（标题、标签、时间等）。
   - 引入索引文件（或轻量数据库）维护顺序、当前选择、元数据。
   - 设计迁移工具：读取旧目录 → 映射新结构 → 记录迁移日志 → 清理旧文件。

3. **命令与事件处理**
   - 使用 `ICommand` + `Reactive` 或社区 MVVM 框架（CommunityToolkit.Mvvm、Prism 等）管理命令与状态。
   - 将键盘快捷键抽象为 `InputBindings` 或行为。
   - 统一保存机制：编辑时更新 ViewModel，定时器或失焦时持久化。

4. **UI/UX 优化**
   - 样式 / 控件拆分资源字典；列表项使用 `DataTemplate`/`ItemsControl`。
   - 设置面板改为独立视图或飞出面板组件。
   - 支持主题切换、国际化（README 已列需求）。

5. **跨平台考量**
   - 若未来迁移到 WinUI/MAUI，需将逻辑与平台 API 解耦，抽象托盘、窗口层级、透明度等接口。

## 重写实施路线建议
1. **准备阶段**
   - 定义目标架构、目录结构（如 `DesktopMemo.App`, `DesktopMemo.Core`, `DesktopMemo.Infrastructure`）。
   - 编写数据模型、存储接口、服务契约。
   - 设计新的 Markdown + 索引格式，并提供迁移脚本。

2. **基础功能迁移**
   - 搭建 MVVM 架构与依赖注入容器。
   - 实现核心功能：列表展示、编辑、保存、切换。
   - 完成设置服务（窗口、置顶、位置）。

3. **系统集成**
   - 托盘菜单、Win32 互操作封装为服务，可注入替换。
   - 键盘快捷键、查找替换作为独立模块。
   - 写入日志与异常处理策略。

4. **迁移与验收**
   - 提供 CLI / 内置迁移向导，备份旧数据后导入新结构。
   - 增加单元测试（服务 / 存储）、UI 测试（Smoke Tests）。
   - 更新文档、README、发布脚本。

## 迁移数据步骤参考
1. 扫描旧数据目录：识别 `notes.json`、`memos.json`、`content/*.md` 等。
2. 建立 ID → 文件映射；若文件名冲突使用哈希或 GUID 重命名。
3. 将旧元数据（标题、标签、描述、时间）写入新结构。
4. 生成迁移报告：成功条目、失败条目（如解析失败、重复）。
5. 可选：保留旧文件备份目录以便回滚。

## 测试与质量保障建议
- 单元测试：存储服务、设置服务、命令逻辑、Markdown 解析。
- 集成测试：数据迁移流程、自动保存、托盘交互伪造。
- UI 回归：Smoke 测试基本操作（新建、切换、置顶模式）
- 性能：大量备忘录情况下列表虚拟化 / 搜索性能。
- 可观测性：引入日志（Serilog 等），在托盘或设置面板提供调试信息查看。

## 附：关键文件行数（便于估算重写工作量）
- `MainWindow.xaml` ~ 968 行。
- `MainWindow.xaml.cs` ~ 4,579 行。
- 其他源码文件行数均 < 20 行。

---
此文档旨在支持 DesktopMemo 的全面重写规划，可在后续迭代中持续补充：例如新增模块设计图、接口定义、迁移脚本伪代码等。

