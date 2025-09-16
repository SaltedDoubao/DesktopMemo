@echo off
echo ===========================================
echo  DesktopMemo v2.0 Build Script
echo ===========================================
echo.

:: 设置构建配置
set BUILD_CONFIG=Release
set TARGET_FRAMEWORK=net8.0-windows
set RUNTIME=win-x64

echo [1/5] 清理构建目录...
if exist "bin\%BUILD_CONFIG%" rmdir /s /q "bin\%BUILD_CONFIG%"
if exist "obj" rmdir /s /q "obj"
if exist "publish" rmdir /s /q "publish"

echo [2/5] 还原NuGet包...
dotnet restore
if errorlevel 1 (
    echo 错误: NuGet包还原失败
    pause
    exit /b 1
)

echo [3/5] 编译项目...
dotnet build --configuration %BUILD_CONFIG% --no-restore
if errorlevel 1 (
    echo 错误: 项目编译失败
    pause
    exit /b 1
)

echo [4/5] 运行测试...
dotnet test DesktopMemo.Tests --configuration %BUILD_CONFIG% --no-build --verbosity normal
if errorlevel 1 (
    echo 警告: 测试失败，但继续构建...
)

echo [5/5] 发布应用程序...

:: 发布自包含版本
echo 发布自包含版本 (win-x64)...
dotnet publish --configuration %BUILD_CONFIG% --runtime %RUNTIME% --self-contained true --output "publish\SelfContained"
if errorlevel 1 (
    echo 错误: 自包含版本发布失败
    pause
    exit /b 1
)

:: 发布依赖框架版本
echo 发布依赖框架版本 (win-x64)...
dotnet publish --configuration %BUILD_CONFIG% --runtime %RUNTIME% --self-contained false --output "publish\FrameworkDependent"
if errorlevel 1 (
    echo 错误: 依赖框架版本发布失败
    pause
    exit /b 1
)

:: 复制发布文件
echo 复制发布文件...
if not exist "publish\Release" mkdir "publish\Release"
xcopy "publish\SelfContained\*" "publish\Release\" /E /Y /Q
copy "README.md" "publish\Release\"
copy "LICENSE" "publish\Release\"
copy "RELEASE_NOTES.md" "publish\Release\"

echo.
echo ===========================================
echo  构建完成！
echo ===========================================
echo.
echo 输出目录:
echo - 自包含版本: publish\SelfContained
echo - 依赖框架版本: publish\FrameworkDependent
echo - 发布版本: publish\Release
echo.
echo 版本信息: v2.0.0-refactored
echo 目标框架: %TARGET_FRAMEWORK%
echo 运行时: %RUNTIME%
echo.

:: 显示文件大小
for %%f in ("publish\Release\DesktopMemo.exe") do echo 主程序大小: %%~zf bytes

echo.
pause