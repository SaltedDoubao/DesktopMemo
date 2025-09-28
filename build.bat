@echo off
echo ���ڴ�� DesktopMemo...

echo ����ɵĹ����ļ�...
if exist "publish" rmdir /s /q "publish"

echo ����Ӧ�ó���...
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
    echo ���ʧ�ܣ�
    pause
    exit /b 1
)

echo.
echo �����ɣ����Ŀ¼: publish\
echo ��ִ���ļ�: publish\DesktopMemo.App.exe
echo.

echo ������Ҫ���ļ�...
cd publish
if exist "*.pdb" del /q "*.pdb"
if exist "*.xml" del /q "*.xml"
cd ..

echo �����ļ�:
dir publish\*.exe /b

pause