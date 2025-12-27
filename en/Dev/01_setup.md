# Development Setup

## System Requirements

- Windows 10 version 1903 or higher
- Visual Studio 2022 / Rider / VS Code (choose one)
- .NET SDK 9.0

## Get the Source Code

```bash
git clone https://github.com/SaltedDoubao/DesktopMemo.git
cd DesktopMemo
```

## Build and Run

```powershell
dotnet restore DesktopMemo.sln
dotnet build DesktopMemo.sln --configuration Debug
dotnet run --project src/DesktopMemo.App/DesktopMemo.App.csproj --configuration Debug
```

> If you haven't changed the version number, it's recommended to clear the `artifacts/vX.X.X` directory before building to avoid duplicate generation/compilation issues from leftover files (see [Contributing Guide](../CONTRIBUTING.md)).

---

**Last Updated**: 2025-12-26
