@echo off
echo 正在打包 DesktopMemo...

echo 清理旧的构建文件...
if exist "publish" rmdir /s /q "publish"

echo 发布应用程序...
dotnet publish src/DesktopMemo.App/DesktopMemo.App.csproj ^
    --configuration Release ^
    --runtime win-x64 ^
    --self-contained true ^
    --output publish ^
    --verbosity minimal ^
    /p:PublishSingleFile=true ^
    /p:IncludeNativeLibrariesForSelfExtract=true ^
    /p:DebugType=None ^
    /p:DebugSymbols=false

if %ERRORLEVEL% NEQ 0 (
    echo 打包失败！
    pause
    exit /b 1
)

echo.
echo 打包完成！输出目录: publish\
echo 可执行文件: publish\DesktopMemo.App.exe
echo.

echo 清理不需要的文件...
cd publish
if exist "*.pdb" del /q "*.pdb"
if exist "*.xml" del /q "*.xml"
cd ..

echo 最终文件:
dir publish\*.exe /b

pause