using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 提供应用设置的加载与保存接口。
/// </summary>
public interface ISettingsService
{
    Task<WindowSettings> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(WindowSettings settings, CancellationToken cancellationToken = default);
}

