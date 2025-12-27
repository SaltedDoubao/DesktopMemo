# DesktopMemo Contributing Guide

Welcome to contribute to the DesktopMemo project! This guide will help you understand how to contribute to the project.

## 📋 Table of Contents

- [Ways to Contribute](#-ways-to-contribute)
- [Development Setup](#-development-setup)
- [Project Structure](#-project-structure)
- [Development Workflow](#-development-workflow)
- [Code Standards](#-code-standards)
- [Commit Message Guidelines](#-commit-message-guidelines)
- [Pull Request Process](#-pull-request-process)
- [Testing Requirements](#-testing-requirements)
- [Documentation Maintenance](#-documentation-maintenance)
- [FAQ](#-faq)

---

## 🤝 Ways to Contribute

You can contribute to the project in the following ways:

- 🐛 **Report Bugs**: Submit detailed issue descriptions in [Issues](https://github.com/SaltedDoubao/DesktopMemo/issues)
- 💡 **Feature Requests**: Propose new features or improvement suggestions
- 📝 **Improve Documentation**: Improve docs, add examples, fix typos
- 🔧 **Submit Code**: Fix bugs, implement new features, optimize performance
- 🌐 **Localization**: Add or improve multi-language support
- ⭐ **Promote**: Star the project, share with other developers

---

## 🛠️ Development Setup

### System Requirements

- **Operating System**: Windows 10 version 1903 or higher
- **Architecture**: x86_64
- **.NET SDK**: [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
- **IDE**: Visual Studio 2022 / VS Code / Rider (choose one)

### Get the Source Code

```bash
# 1. Fork this repository to your account

# 2. Clone your fork
git clone https://github.com/yourname/DesktopMemo.git
cd DesktopMemo

# 3. Add upstream repository
git remote add upstream https://github.com/SaltedDoubao/DesktopMemo.git
```

### Build the Project

```powershell
# Restore dependencies (must run on first build or after changing output directory)
dotnet restore DesktopMemo.sln

# Debug build
dotnet build DesktopMemo.sln --configuration Debug

# Run application
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> ⚠️ **Important**: If you haven't changed the version number, you **must** clear the `artifacts/vX.X.X` directory before building to avoid compilation errors from leftover files.

### Data Directory

On first run, a `/.memodata` directory will be created in the executable directory:

```
.memodata/
├── content/              # Memo Markdown files
├── .logs/                # Log files directory
├── settings.json         # Window and global settings
├── memos.db              # Memos database (v2.3.0+)
└── todos.db              # Todo items database (v2.3.0+)
```

---

## 📂 Project Structure

DesktopMemo uses a three-layer architecture design. For detailed architecture documentation, see [Project Architecture Documentation](project_structure/README.md).

---

## 🔄 Development Workflow

### 1. Sync Upstream Code

Before starting development, ensure your fork is up-to-date:

```bash
git fetch upstream
git checkout dev
git merge upstream/dev
```

### 2. Create Feature Branch

```bash
# Create a new branch from dev
git checkout -b feature/your-feature-name

# Or fix a bug
git checkout -b fix/bug-description
```

Branch naming suggestions:
- `feature/description`: New features
- `fix/description`: Bug fixes
- `docs/description`: Documentation updates
- `refactor/description`: Code refactoring
- `perf/description`: Performance optimization

### 3. Development and Testing

- Follow [Code Standards](#-code-standards)
- Add necessary comments and documentation
- Ensure code compiles and runs correctly
- Check debug information in Visual Studio output window

### 4. Commit Changes

Follow [Commit Message Guidelines](#-commit-message-guidelines):

```bash
git add .
git commit -m "feat: brief description of new feature"
```

### 5. Push Branch

```bash
git push origin feature/your-feature-name
```

### 6. Create Pull Request

See [Pull Request Process](#-pull-request-process).

---

## 📝 Code Standards

### Basic Principles

- **Follow C# coding conventions**: Use PascalCase for classes, methods, properties; camelCase for local variables
- **Use dependency injection**: Inject dependencies through constructors, avoid hard-coding
- **Single Responsibility Principle**: Each class and method should have a clear single responsibility
- **Prefer interfaces**: Program to interfaces for easier testing and extension

### Async Programming Standards ⚠️

> **Critical Standard**: Incorrect async programming can cause application crashes!

#### ❌ Wrong Approach - May cause deadlocks

```csharp
private async void Button_Click(object sender, RoutedEventArgs e)
{
    await SomeAsyncOperation(); // Dangerous! May cause deadlock
}
```

#### ✅ Correct Approach - Fire-and-Forget Pattern

```csharp
private void Button_Click(object sender, RoutedEventArgs e)
{
    // Use fire-and-forget pattern
    _ = Task.Run(async () => await HandleButtonClickAsync());
}

private async Task HandleButtonClickAsync()
{
    // Async logic goes here
}
```

#### UI Thread and Background Thread Switching

```csharp
await Task.Run(async () =>
{
    // Background thread for IO operations
    await _settingsService.SaveAsync(settings);

    // Switch back to UI thread to update interface
    await Application.Current.Dispatcher.InvokeAsync(() =>
    {
        ViewModel.Settings = settings;
    });
});
```

### Constants and Helper Classes Usage

#### ✅ Use Unified Constants Classes

```csharp
using DesktopMemo.Core.Constants;
using DesktopMemo.Core.Helpers;

var opacity = WindowConstants.DEFAULT_TRANSPARENCY;
var percent = TransparencyHelper.ToPercent(opacity);
```

#### ❌ Avoid Hard-coding

```csharp
// Wrong - Hard-coded values
var opacity = 0.05;
var maxOpacity = 0.4;
```

### Exception Handling Standards

```csharp
try
{
    await SomeAsyncOperation();
}
catch (InvalidOperationException ex)
{
    // Specific exception - Provide user-friendly message
    Debug.WriteLine($"Invalid operation: {ex.Message}");
    SetStatus("Invalid operation, please try again later");
}
catch (UnauthorizedAccessException ex)
{
    // Permission issues
    Debug.WriteLine($"Insufficient permissions: {ex.Message}");
    SetStatus("Insufficient permissions, please check file access");
}
catch (Exception ex)
{
    // General exception - Log detailed information
    Debug.WriteLine($"Unknown error: {ex}");
    SetStatus("Operation failed, please try again later");
}
```

### Settings Save Atomicity

```csharp
// ✅ Atomic save: Save to file first, then update memory on success
try
{
    var newSettings = currentSettings with { Property = newValue };
    await _settingsService.SaveAsync(newSettings); // Save to file first
    CurrentSettings = newSettings; // Update memory only after successful save
}
catch (Exception ex)
{
    // Save failed, memory state remains unchanged
    Debug.WriteLine($"Settings save failed: {ex}");
}
```

---

## 📋 Commit Message Guidelines

### Commit Message Format

```
<type>(<scope>): <brief description>

<detailed description> (optional)

<related issue> (optional)
```

### Type Descriptions

- `feat`: New feature
- `fix`: Bug fix
- `docs`: Documentation update
- `style`: Code formatting (no functional changes)
- `refactor`: Refactoring (neither new feature nor bug fix)
- `perf`: Performance optimization
- `test`: Testing related
- `chore`: Build process or auxiliary tool changes

### Examples

```bash
feat(memo): Add full-text search for memos

Implement full-text search based on SQLite FTS5 with keyword highlighting.

Closes #123
```

```bash
fix(settings): Fix transparency save failure issue

Use atomic save pattern to ensure settings save to file successfully before updating memory state.

Fixes #456
```

---

## 🔀 Pull Request Process

### 1. Create Pull Request

Create a Pull Request on GitHub, usually targeting the `dev` branch.

### 2. PR Description Template

```markdown
## Change Type
- [ ] New feature
- [ ] Bug fix
- [ ] Documentation update
- [ ] Code refactoring
- [ ] Performance optimization

## Change Description
<!-- Briefly describe your changes -->

## Background
<!-- Why is this change needed? What problem does it solve? -->

## Change Summary
<!-- List main code changes -->
-
-

## Testing & Verification
<!-- How to verify this change? Include test steps and results -->
- [ ] Local build passed
- [ ] Function testing passed
- [ ] No obvious performance issues

## Related Issues
<!-- Related issue number -->
Closes #

## Screenshots/Screen Recording
<!-- If UI changes, please attach screenshots or recordings -->

## Additional Notes
<!-- Other notes -->
```

### 3. Code Review

- Wait patiently for maintainer's code review
- Modify code promptly based on feedback
- Maintain friendly and professional communication

### 4. Merge

- After PR is approved, maintainer will merge it to main branch
- Delete your feature branch (optional)

---

## ✅ Testing Requirements

### Basic Verification

Before submitting a PR, please ensure:

```powershell
# 1. Clean old build artifacts (if version number hasn't changed)
# Manually delete artifacts/vX.X.X directory

# 2. Build verification
dotnet clean DesktopMemo.sln
dotnet restore DesktopMemo.sln
dotnet build DesktopMemo.sln --configuration Debug

# 3. Run application
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

### Function Testing

- Test that your new feature or bug fix works correctly
- Ensure existing functionality is not broken
- Check for memory leaks or performance issues
- Test in different window states (pin, transparency, mouse-through mode, etc.)

### Debugging Tips

- **View Debug Output**: Check `Debug.WriteLine` information in Visual Studio output window
- **Check Data Files**: View database and config files in `.memodata` directory
- **Performance Analysis**: Use Visual Studio diagnostics tools to monitor memory and CPU

---

## 📚 Documentation Maintenance

### When to Update Documentation

| Change Type | Documents to Update |
|------------|---------------------|
| **New module/feature** | - `docs/ProjectStructure/01_diagram.md`<br>- `docs/ProjectStructure/02_modules.md`<br>- `README.md` |
| **Upgrade dependencies or change tech** | - `docs/ProjectStructure/03_tech-stack.md` |
| **Modify business process** | - `docs/ProjectStructure/04_data-flow.md` |
| **Architecture adjustments** | - `docs/ProjectStructure/01_diagram.md`<br>- `docs/ProjectStructure/02_modules.md` |
| **Development standards changes** | - `CONTRIBUTING.md` / `Dev/` / `project_structure/` / `api/` etc. |
| **Add/modify localization** | - Resource files under `src/DesktopMemo.App/Localization/Resources/` |

### Documentation Writing Standards

- Use Markdown format
- Maintain proper spacing when mixing Chinese and English (if applicable)
- Add necessary code examples and diagrams
- Update the "Last Updated" date at bottom of documents

---

## ❓ FAQ

### Build Issues

**Q: `NETSDK1005`: Cannot find `project.assets.json`**

A: Run `dotnet restore DesktopMemo.sln` first. If necessary, delete the `artifacts/` directory and rebuild.

**Q: Duplicate assembly attributes**

A: Delete the `artifacts/` directory and run `restore + build` again.

---

### UI Issues

**Q: Data binding not refreshing**

A: Check if `OnPropertyChanged` is called, ensure ViewModel inherits from `ObservableObject`.

**Q: Dialog cannot be displayed**

A: Check if `Owner` is set correctly, ensure dialog is created on UI thread.

---

### Settings Issues

**Q: Transparency becomes 0% or 100% after restart**

A: Check `.memodata/settings.json` file integrity, verify correct use of `TransparencyHelper`.

**Q: Settings save failure causes data inconsistency**

A: Ensure atomic save pattern is used: save file first, then update memory on success.

---

### Crash Issues

**Q: Program freezes/crashes after checking "Don't show again"**

A: Check if using `async void` event handlers, switch to `Task.Run` + fire-and-forget pattern.

**Q: Win32 API call failed**

A: Check API return value, use `Marshal.GetLastWin32Error()` to get error code, add error handling.

---

## 🌟 Code of Conduct

- **Respect Others**: Maintain friendly and professional communication
- **Constructive Feedback**: Provide helpful suggestions and criticism
- **Inclusivity**: Welcome contributors of different backgrounds and skill levels
- **Patience**: Understand everyone has a learning curve

---

## 📧 Contact

- **Project Maintainer**: [SaltedDoubao](https://github.com/SaltedDoubao)
- **Report Issues**: [GitHub Issues](https://github.com/SaltedDoubao/DesktopMemo/issues)
- **Feature Requests**: [GitHub Issues](https://github.com/SaltedDoubao/DesktopMemo/issues)
- **Discussions**: [GitHub Discussions](https://github.com/SaltedDoubao/DesktopMemo/discussions)

---

## 📖 Related Documentation

- [Project Architecture Documentation](project_structure/README.md)
- [API Documentation](api/README.md)
- [MySQL Integration Specification](mysql-integration.md)
- [Main Project Repository README](https://github.com/SaltedDoubao/DesktopMemo#readme)

---

## 🙏 Acknowledgments

Thanks to all developers who have contributed to DesktopMemo!

<a href="https://github.com/SaltedDoubao/DesktopMemo/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=SaltedDoubao/DesktopMemo" />
</a>

---

**Last Updated**: 2025-11-17
