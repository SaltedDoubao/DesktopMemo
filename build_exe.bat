@echo off
echo Packaging DesktopMemo...

echo Cleaning old build files...
if exist "publish" rmdir /s /q "publish"

echo Publishing application...
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
    echo Packaging failed!
    pause
    exit /b 1
)

echo.
echo Packaging complete! Output directory: publish\
echo Executable: publish\DesktopMemo.App.exe
echo.

echo Cleaning unnecessary files...
cd publish
if exist "*.pdb" del /q "*.pdb"
if exist "*.xml" del /q "*.xml"
cd ..

echo Final files:
dir publish\*.exe /b
