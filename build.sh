#!/bin/bash

echo "==========================================="
echo " DesktopMemo v2.0 Build Script (Linux/Mac)"
echo "==========================================="
echo

# 设置构建配置
BUILD_CONFIG="Release"
TARGET_FRAMEWORK="net8.0-windows"
RUNTIME="win-x64"

echo "[1/5] 清理构建目录..."
rm -rf bin/$BUILD_CONFIG
rm -rf obj
rm -rf publish

echo "[2/5] 还原NuGet包..."
dotnet restore
if [ $? -ne 0 ]; then
    echo "错误: NuGet包还原失败"
    exit 1
fi

echo "[3/5] 编译项目..."
dotnet build --configuration $BUILD_CONFIG --no-restore
if [ $? -ne 0 ]; then
    echo "错误: 项目编译失败"
    exit 1
fi

echo "[4/5] 运行测试..."
dotnet test DesktopMemo.Tests --configuration $BUILD_CONFIG --no-build --verbosity normal
if [ $? -ne 0 ]; then
    echo "警告: 测试失败，但继续构建..."
fi

echo "[5/5] 发布应用程序..."

# 发布自包含版本
echo "发布自包含版本 (win-x64)..."
dotnet publish --configuration $BUILD_CONFIG --runtime $RUNTIME --self-contained true --output "publish/SelfContained"
if [ $? -ne 0 ]; then
    echo "错误: 自包含版本发布失败"
    exit 1
fi

# 发布依赖框架版本
echo "发布依赖框架版本 (win-x64)..."
dotnet publish --configuration $BUILD_CONFIG --runtime $RUNTIME --self-contained false --output "publish/FrameworkDependent"
if [ $? -ne 0 ]; then
    echo "错误: 依赖框架版本发布失败"
    exit 1
fi

# 复制发布文件
echo "复制发布文件..."
mkdir -p publish/Release
cp -r publish/SelfContained/* publish/Release/
cp README.md publish/Release/
cp LICENSE publish/Release/
cp RELEASE_NOTES.md publish/Release/

echo
echo "==========================================="
echo " 构建完成！"
echo "==========================================="
echo
echo "输出目录:"
echo "- 自包含版本: publish/SelfContained"
echo "- 依赖框架版本: publish/FrameworkDependent"
echo "- 发布版本: publish/Release"
echo
echo "版本信息: v2.0.0"
echo "目标框架: $TARGET_FRAMEWORK"
echo "运行时: $RUNTIME"
echo

# 显示文件大小
if [ -f "publish/Release/DesktopMemo.exe" ]; then
    size=$(stat -c%s "publish/Release/DesktopMemo.exe" 2>/dev/null || stat -f%z "publish/Release/DesktopMemo.exe" 2>/dev/null)
    echo "主程序大小: $size bytes"
fi

echo