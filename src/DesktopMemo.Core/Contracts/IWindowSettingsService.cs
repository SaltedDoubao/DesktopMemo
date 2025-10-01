namespace DesktopMemo.Core.Contracts;

using DesktopMemo.Core.Models;

public interface IWindowSettingsService
{
    WindowSettings Current { get; }

    void ApplySettings(WindowSettings settings);

    void UpdatePosition(double left, double top);

    void UpdateSize(double width, double height);
}
