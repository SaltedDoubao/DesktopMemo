using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;
using DesktopMemo.Core.Helpers;
using System;
using System.Diagnostics;

namespace DesktopMemo.Infrastructure.Services;

/// <summary>
/// 使用 JSON 文件存储窗口设置。
/// </summary>
public sealed class JsonSettingsService : ISettingsService
{
    private readonly string _settingsFile;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public JsonSettingsService(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _settingsFile = Path.Combine(dataDirectory, "settings.json");
    }

    public async Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_settingsFile))
        {
            Debug.WriteLine("设置文件不存在，使用默认设置");
            return WindowSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFile);
            var rawJson = await new StreamReader(stream).ReadToEndAsync();
            stream.Position = 0; // 重置流位置

            Debug.WriteLine($"读取设置文件内容: {rawJson}");

            var settings = await JsonSerializer.DeserializeAsync<WindowSettings>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);

            if (settings == null)
            {
                Debug.WriteLine("设置反序列化为null，使用默认设置");
                return WindowSettings.Default;
            }

            // 验证和修复透明度值
            var normalizedTransparency = TransparencyHelper.NormalizeTransparency(settings.Transparency);
            if (Math.Abs(settings.Transparency - normalizedTransparency) > 0.001)
            {
                Debug.WriteLine($"透明度值无效 ({settings.Transparency})，已规范化为 {normalizedTransparency}");
                settings = settings with { Transparency = normalizedTransparency };
            }

            Debug.WriteLine($"成功加载设置: 透明度={settings.Transparency}, 宽度={settings.Width}, 高度={settings.Height}");
            return settings;
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"设置文件JSON格式错误: {ex.Message}，使用默认设置");
            // 备份损坏的设置文件
            try
            {
                File.Copy(_settingsFile, _settingsFile + ".backup", true);
                Debug.WriteLine("已备份损坏的设置文件");
            }
            catch { }
            return WindowSettings.Default;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取设置文件时发生错误: {ex.Message}，使用默认设置");
            return WindowSettings.Default;
        }
    }

    public async Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default)
    {
        try
        {
            // 在保存前验证设置
            var normalizedTransparency = TransparencyHelper.NormalizeTransparency(settings.Transparency);
            if (Math.Abs(settings.Transparency - normalizedTransparency) > 0.001)
            {
                Debug.WriteLine($"保存前规范化透明度: {settings.Transparency} -> {normalizedTransparency}");
                settings = settings with { Transparency = normalizedTransparency };
            }

            Debug.WriteLine($"保存设置: 透明度={settings.Transparency}, 宽度={settings.Width}, 高度={settings.Height}");

            await using var stream = File.Create(_settingsFile);
            await JsonSerializer.SerializeAsync(stream, settings, _jsonOptions, cancellationToken).ConfigureAwait(false);

            Debug.WriteLine("设置文件保存成功");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"保存设置文件失败: {ex.Message}");
            throw; // 重新抛出异常，让调用者知道保存失败
        }
    }
}

