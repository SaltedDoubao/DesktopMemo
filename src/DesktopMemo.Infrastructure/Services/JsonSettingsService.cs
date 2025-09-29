using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

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
            return WindowSettings.Default;
        }

        try
        {
            await using var stream = File.OpenRead(_settingsFile);
            var settings = await JsonSerializer.DeserializeAsync<WindowSettings>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            return settings ?? WindowSettings.Default;
        }
        catch (JsonException)
        {
            // 设置文件格式错误，返回默认设置
            return WindowSettings.Default;
        }
        catch (Exception)
        {
            // 其他文件读取错误，返回默认设置
            return WindowSettings.Default;
        }
    }

    public async Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(_settingsFile);
        await JsonSerializer.SerializeAsync(stream, settings, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }
}

