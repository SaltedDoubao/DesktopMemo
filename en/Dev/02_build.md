# Build & Release

## Debug Build

```powershell
dotnet build DesktopMemo.sln --configuration Debug
```

## Release Build

> The repository provides `build_exe.bat` for one-click executable building. Build artifacts are output to `artifacts/v<version>/bin` by default.

### Environment Variables

- `ArtifactsVersion`: Customize build version number (used for output path)

## Publish Directory

The repository may contain `publish/DesktopMemo.exe`, typically used for local testing or distribution verification; official releases are based on GitHub Releases.

---

**Last Updated**: 2025-12-26
