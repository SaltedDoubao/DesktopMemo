using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;
using DesktopMemo.Core.Constants;
using DesktopMemo.Core.Helpers;
using DesktopMemo.Infrastructure.Services;
using System.IO;

namespace DesktopMemo.App.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly IMemoRepository _memoRepository;
    private readonly ISettingsService _settingsService;
    private readonly IWindowService _windowService;
    private readonly ITrayService _trayService;
    private readonly IMemoSearchService _searchService;
    private readonly MemoMigrationService _migrationService;
    private readonly TodoListViewModel _todoListViewModel;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    private ObservableCollection<Memo> _memos = new();

    [ObservableProperty]
    private Memo? _selectedMemo;

    [ObservableProperty]
    private string _editorTitle = string.Empty;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private WindowSettings _windowSettings = WindowSettings.Default;

    [ObservableProperty]
    private bool _isSettingsPanelVisible;

    [ObservableProperty]
    private bool _isWindowPinned;

    [ObservableProperty]
    private bool _isEditMode;

    [ObservableProperty]
    private bool _isInTodoListMode;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string _appInfo = string.Empty;

    [ObservableProperty]
    private TopmostMode _selectedTopmostMode = TopmostMode.Desktop;

    [ObservableProperty]
    private double _backgroundOpacity = 0.0;

    [ObservableProperty]
    private double _backgroundOpacityPercent = 0.0;

    [ObservableProperty]
    private bool _isClickThroughEnabled;

    [ObservableProperty]
    private bool _isAutoStartEnabled;

    [ObservableProperty]
    private double _currentLeft;

    [ObservableProperty]
    private double _currentTop;

    [ObservableProperty]
    private string _customPositionX = "0";

    [ObservableProperty]
    private string _customPositionY = "0";

    [ObservableProperty]
    private string _searchKeyword = string.Empty;

    [ObservableProperty]
    private string _replaceKeyword = string.Empty;

    [ObservableProperty]
    private bool _isCaseSensitive;

    [ObservableProperty]
    private bool _useRegex;

    [ObservableProperty]
    private bool _isTrayEnabled = true;

    private bool _isDisposing;

    public int MemoCount => Memos.Count;

    public bool HasSelectedMemo => SelectedMemo is not null;

    public TodoListViewModel TodoListViewModel => _todoListViewModel;

    public ILocalizationService LocalizationService => _localizationService;

    public IEnumerable<CultureInfo> AvailableLanguages => _localizationService.GetSupportedLanguages();

    [ObservableProperty]
    private CultureInfo? _selectedLanguage;

    private bool _disposed;

    public MainViewModel(
        IMemoRepository memoRepository,
        ISettingsService settingsService,
        IWindowService windowService,
        ITrayService trayService,
        IMemoSearchService searchService,
        MemoMigrationService migrationService,
        TodoListViewModel todoListViewModel,
        ILocalizationService localizationService)
    {
        _memoRepository = memoRepository;
        _settingsService = settingsService;
        _windowService = windowService;
        _trayService = trayService;
        _searchService = searchService;
        _migrationService = migrationService;
        _todoListViewModel = todoListViewModel;
        _localizationService = localizationService;

        Memos.CollectionChanged += OnMemosCollectionChanged;

        // 订阅语言切换事件
        _localizationService.LanguageChanged += OnLanguageChanged;

        // 初始化应用信息
        InitializeAppInfo();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken);
        ApplyWindowSettings(settings);

        var memos = await _memoRepository.GetAllAsync(cancellationToken);

        if (memos.Count == 0)
        {
            var migrated = await _migrationService.LoadFromLegacyAsync();
            foreach (var migratedMemo in migrated)
            {
                await _memoRepository.AddAsync(migratedMemo, cancellationToken);
            }

            memos = await _memoRepository.GetAllAsync(cancellationToken);
        }

        Memos = new ObservableCollection<Memo>(memos.OrderByDescending(m => m.UpdatedAt));

        SelectedMemo = Memos.FirstOrDefault();
        if (SelectedMemo is not null)
        {
            EditorTitle = SelectedMemo.Title;
            EditorContent = SelectedMemo.Content;
            IsWindowPinned = SelectedMemo.IsPinned;
        }
        else
        {
            EditorTitle = string.Empty;
            EditorContent = string.Empty;
            IsWindowPinned = false;
        }

        if (IsTrayEnabled)
        {
            _trayService.Show();
        }
        else
        {
            _trayService.Hide();
        }

        // 初始化 TodoListViewModel
        await _todoListViewModel.InitializeAsync(cancellationToken);

        // 初始化选择的语言
        SelectedLanguage = _localizationService.CurrentCulture;
        
        // 初始化托盘菜单文本
        _trayService.UpdateMenuTexts(key => _localizationService[key]);

        SetStatus("就绪");
        _trayService.UpdateTopmostState(SelectedTopmostMode);
        _trayService.UpdateClickThroughState(IsClickThroughEnabled);

        // 检查开机自启动状态
        CheckAutoStartStatus();
    }

    private void ApplyWindowSettings(WindowSettings settings)
    {
        WindowSettings = settings;

        if (!double.IsNaN(settings.Left) && !double.IsNaN(settings.Top))
        {
            _windowService.SetWindowPosition(settings.Left, settings.Top);
        }

        // 确保透明度值在合理范围内
        var transparencyValue = TransparencyHelper.NormalizeTransparency(settings.Transparency);
        System.Diagnostics.Debug.WriteLine($"设置加载: 原始透明度={settings.Transparency}, 规范化后={transparencyValue}");

        _windowService.SetWindowOpacity(transparencyValue);
        _windowService.SetClickThrough(settings.IsClickThrough);
        var mode = settings.IsTopMost
            ? TopmostMode.Always
            : (settings.IsDesktopMode ? TopmostMode.Desktop : TopmostMode.Normal);
        _windowService.SetTopmostMode(mode);

        SelectedTopmostMode = _windowService.GetCurrentTopmostMode();

        // 从窗口服务获取实际设置的透明度值
        var actualOpacity = _windowService.GetWindowOpacity();
        BackgroundOpacity = actualOpacity;

        // 使用统一的透明度转换逻辑，并确保百分比值有效
        var calculatedPercent = TransparencyHelper.ToPercent(actualOpacity);
        BackgroundOpacityPercent = double.IsNaN(calculatedPercent) ? 0.0 : calculatedPercent;

        System.Diagnostics.Debug.WriteLine($"透明度初始化完成: 实际值={actualOpacity}, 百分比={BackgroundOpacityPercent}");
        
        IsClickThroughEnabled = _windowService.IsClickThroughEnabled;
        UpdateCurrentPosition();
    }

    public void UpdateCurrentPosition()
    {
        var (left, top) = _windowService.GetWindowPosition();
        CurrentLeft = left;
        CurrentTop = top;
        CustomPositionX = left.ToString("F0");
        CustomPositionY = top.ToString("F0");
    }

    [RelayCommand]
    private async Task CreateMemoAsync()
    {
        var memo = Memo.CreateNew(_localizationService["Memo_NewTitle"], string.Empty);
        await _memoRepository.AddAsync(memo);

        Memos.Insert(0, memo);
        SelectedMemo = memo;
        EditorTitle = memo.Title;
        EditorContent = memo.Content;
        IsWindowPinned = memo.IsPinned;
        IsEditMode = true;
        OnPropertyChanged(nameof(MemoCount));
        SetStatus("已新建备忘录");
    }

    [RelayCommand]
    private async Task SaveMemoAsync()
    {
        if (SelectedMemo is null)
        {
            return;
        }

        var updated = SelectedMemo
            .WithContent(EditorContent, DateTimeOffset.UtcNow)
            .WithMetadata(EditorTitle, SelectedMemo.Tags, IsWindowPinned, DateTimeOffset.UtcNow);

        await _memoRepository.UpdateAsync(updated);

        var index = Memos.IndexOf(SelectedMemo);
        if (index >= 0)
        {
            Memos[index] = updated;
            SelectedMemo = updated;
        }

        OnPropertyChanged(nameof(MemoCount));
        SetStatus("已保存");
    }

    [RelayCommand]
    private async Task DeleteMemoAsync()
    {
        if (SelectedMemo is null)
        {
            return;
        }

        // 检查是否需要显示确认框
        if (WindowSettings.ShowDeleteConfirmation)
        {
            Views.ConfirmationDialog? dialog = null;
            bool? result = null;

            // 在UI线程上显示对话框
            try
            {
                await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    dialog = new Views.ConfirmationDialog(
                        _localizationService,
                        _localizationService["Dialog_DeleteConfirm_Title"],
                        string.Format(_localizationService["Dialog_DeleteConfirm_Message"], SelectedMemo.DisplayTitle));

                    // 安全地设置Owner
                    if (System.Windows.Application.Current.MainWindow != null &&
                        System.Windows.Application.Current.MainWindow.IsLoaded)
                    {
                        dialog.Owner = System.Windows.Application.Current.MainWindow;
                    }

                    result = dialog.ShowDialog();
                });
            }
            catch (InvalidOperationException ex)
            {
                // 对话框无法显示（可能是Owner问题或窗口状态异常）
                SetStatus($"无法显示删除确认对话框: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"删除确认对话框InvalidOperationException: {ex}");
                return;
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                // Win32相关错误
                SetStatus($"系统错误，无法显示对话框: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"删除确认对话框Win32Exception: {ex}");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                // 权限问题
                SetStatus($"权限不足，无法显示对话框: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"删除确认对话框UnauthorizedAccessException: {ex}");
                return;
            }
            catch (Exception ex)
            {
                // 其他未预期异常
                SetStatus($"对话框错误: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"删除确认对话框未知异常: {ex}");
                return;
            }

            if (result != true || dialog == null)
            {
                SetStatus("已取消删除");
                return;
            }

            // 如果用户选择了"不再显示"，立即更新内存中的设置，然后异步保存
            if (dialog.DontShowAgain)
            {
                // 立即更新内存中的设置，避免被后续的设置保存覆盖
                var newSettings = WindowSettings with { ShowDeleteConfirmation = false };
                WindowSettings = newSettings;

                // 异步保存到磁盘（不等待，避免阻塞删除操作）
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _settingsService.SaveAsync(newSettings);
                        System.Diagnostics.Debug.WriteLine("删除确认设置已保存");
                    }
                    catch (Exception ex)
                    {
                        // 记录错误但不影响删除操作
                        System.Diagnostics.Debug.WriteLine($"保存删除设置失败: {ex}");
                    }
                });
            }
        }

        // 执行删除操作
        var deleting = SelectedMemo;
        await _memoRepository.DeleteAsync(deleting.Id);

        Memos.Remove(deleting);
        SelectedMemo = Memos.FirstOrDefault();

        if (SelectedMemo is not null)
        {
            EditorTitle = SelectedMemo.Title;
            EditorContent = SelectedMemo.Content;
            IsWindowPinned = SelectedMemo.IsPinned;
        }
        else
        {
            EditorTitle = string.Empty;
            EditorContent = string.Empty;
            IsWindowPinned = false;
        }

        IsEditMode = false;
        OnPropertyChanged(nameof(MemoCount));
        SetStatus("已删除");
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsPanelVisible = !IsSettingsPanelVisible;
        SetStatus(IsSettingsPanelVisible ? "打开设置" : "关闭设置");
    }

    [RelayCommand]
    private async Task TogglePinAsync()
    {
        IsWindowPinned = !IsWindowPinned;

        // 实际控制窗口置顶状态
        if (IsWindowPinned)
        {
            _windowService.SetTopmostMode(TopmostMode.Always);
        }
        else
        {
            _windowService.SetTopmostMode(SelectedTopmostMode);
        }

        // 如果有选中的备忘录，保存其固定状态
        if (SelectedMemo is not null)
        {
            await SaveMemoAsync();
        }

        SetStatus(IsWindowPinned ? "窗口已固定，无法拖动" : "窗口已解除固定");
    }

    [RelayCommand]
    private async Task PersistWindowSettingsAsync()
    {
        _windowService.SetTopmostMode(SelectedTopmostMode);

        bool isTopMost = SelectedTopmostMode == TopmostMode.Always;
        bool isDesktopMode = SelectedTopmostMode == TopmostMode.Desktop;

        _windowService.SetClickThrough(IsClickThroughEnabled);
        _windowService.SetWindowOpacity(BackgroundOpacity);

        UpdateCurrentPosition();

        WindowSettings = WindowSettings.WithLocation(CurrentLeft, CurrentTop)
            .WithAppearance(BackgroundOpacity, isTopMost, isDesktopMode, IsClickThroughEnabled);

        await _settingsService.SaveAsync(WindowSettings);
        SetStatus("设置已保存");
        _trayService.UpdateTopmostState(SelectedTopmostMode);
        _trayService.UpdateClickThroughState(IsClickThroughEnabled);
    }

    [RelayCommand]
    private void EnterEditMode(Memo? memo)
    {
        if (memo is null)
        {
            return;
        }

        SelectedMemo = memo;
        IsEditMode = true;
        EditorTitle = memo.Title;
        EditorContent = memo.Content;
        SetStatus("编辑中...");
    }

    [RelayCommand]
    private async Task SaveAndBackAsync()
    {
        await SaveMemoAsync();
        IsEditMode = false;
        SetStatus("已保存并返回");
    }

    [RelayCommand]
    private void BackToList()
    {
        IsEditMode = false;
        SetStatus("返回列表");
    }

    [RelayCommand]
    private void ToggleTodoList()
    {
        if (IsEditMode)
        {
            // 如果在编辑模式，先返回列表
            IsEditMode = false;
        }

        IsInTodoListMode = !IsInTodoListMode;
        SetStatus(IsInTodoListMode ? "切换到待办事项" : "切换到备忘录");
    }

    [RelayCommand]
    private async Task ApplyCustomPositionAsync()
    {
        if (double.TryParse(CustomPositionX, out double x) && double.TryParse(CustomPositionY, out double y))
        {
            _windowService.SetWindowPosition(x, y);
            UpdateCurrentPosition();
            WindowSettings = WindowSettings.WithLocation(CurrentLeft, CurrentTop);
            await _settingsService.SaveAsync(WindowSettings);
            SetStatus("已应用自定义位置");
        }
        else
        {
            SetStatus("位置格式错误");
        }
    }

    [RelayCommand]
    private async Task MoveToPresetAsync(string? preset)
    {
        if (string.IsNullOrWhiteSpace(preset))
        {
            return;
        }

        _windowService.MoveToPresetPosition(preset);
        UpdateCurrentPosition();
        WindowSettings = WindowSettings.WithLocation(CurrentLeft, CurrentTop);
        await _settingsService.SaveAsync(WindowSettings);
        SetStatus("窗口已移动");
    }

    [RelayCommand]
    private async Task RememberPositionAsync()
    {
        UpdateCurrentPosition();
        WindowSettings = WindowSettings.WithLocation(CurrentLeft, CurrentTop);
        await _settingsService.SaveAsync(WindowSettings);
        SetStatus("位置已记录");
    }

    [RelayCommand]
    private void RestorePosition()
    {
        if (double.IsNaN(WindowSettings.Left) || double.IsNaN(WindowSettings.Top))
        {
            SetStatus("尚未记录位置");
            return;
        }

        _windowService.SetWindowPosition(WindowSettings.Left, WindowSettings.Top);
        UpdateCurrentPosition();
        SetStatus("已恢复位置");
    }

    [RelayCommand]
    private void TrayShowWindow()
    {
        IsTrayEnabled = true;
        _trayService.Show();
        _windowService.RestoreFromTray();
        SetStatus("窗口已显示");
    }

    [RelayCommand]
    private void TrayHideWindow()
    {
        _windowService.MinimizeToTray();
        SetStatus("窗口已隐藏");
    }

    [RelayCommand]
    private void TrayRestart()
    {
        _trayService.Hide();
        _trayService.Initialize();
        if (IsTrayEnabled)
        {
            _trayService.Show();
        }
        _trayService.UpdateTopmostState(SelectedTopmostMode);
        _trayService.UpdateClickThroughState(IsClickThroughEnabled);
        SetStatus("托盘已重载");
    }

    [RelayCommand]
    private void ClearEditor()
    {
        EditorContent = string.Empty;
        SetStatus("已清空内容");
    }

    [RelayCommand]
    private void ShowAbout()
    {
        SetStatus("关于 DesktopMemo");
    }

    partial void OnSelectedTopmostModeChanged(TopmostMode value)
    {
        _windowService.SetTopmostMode(value);
        _trayService.UpdateTopmostState(value);

        // 自动保存置顶模式设置
        bool isTopMost = value == TopmostMode.Always;
        bool isDesktopMode = value == TopmostMode.Desktop;

        WindowSettings = WindowSettings.WithAppearance(BackgroundOpacity, isTopMost, isDesktopMode, IsClickThroughEnabled);
        _ = _settingsService.SaveAsync(WindowSettings);
    }

    partial void OnBackgroundOpacityChanged(double value)
    {
        // 使用辅助类规范化透明度值
        var normalizedValue = TransparencyHelper.NormalizeTransparency(value);
        if (Math.Abs(value - normalizedValue) > 0.001) // 如果值被调整了
        {
            BackgroundOpacity = normalizedValue; // 更新为调整后的值
            return; // 避免递归调用
        }

        _windowService.SetWindowOpacity(normalizedValue);

        // 自动保存透明度设置
        bool isTopMost = SelectedTopmostMode == TopmostMode.Always;
        bool isDesktopMode = SelectedTopmostMode == TopmostMode.Desktop;

        WindowSettings = WindowSettings.WithAppearance(normalizedValue, isTopMost, isDesktopMode, IsClickThroughEnabled);
        _ = _settingsService.SaveAsync(WindowSettings);
    }

    partial void OnIsClickThroughEnabledChanged(bool value)
    {
        _windowService.SetClickThrough(value);
        _trayService.UpdateClickThroughState(value);

        if (value && IsSettingsPanelVisible)
        {
            IsSettingsPanelVisible = false;
        }
    }

    partial void OnIsAutoStartEnabledChanged(bool value)
    {
        try
        {
            ManageAutoStart(value);
            SetStatus(value ? "已启用开机自启动" : "已禁用开机自启动");
        }
        catch (Exception ex)
        {
            SetStatus($"设置开机自启动失败: {ex.Message}");
        }
    }

    private void ManageAutoStart(bool enable)
    {
        const string appName = "DesktopMemo";
        var keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

        using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, true);
        if (key == null) return;

        if (enable)
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
            {
                key.SetValue(appName, $"\"{exePath}\"");
            }
        }
        else
        {
            key.DeleteValue(appName, false);
        }
    }

    private void CheckAutoStartStatus()
    {
        try
        {
            const string appName = "DesktopMemo";
            var keyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";

            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath, false);
            var value = key?.GetValue(appName);
            IsAutoStartEnabled = value != null;
        }
        catch
        {
            IsAutoStartEnabled = false;
        }
    }

    partial void OnIsTrayEnabledChanged(bool value)
    {
        if (_isDisposing)
        {
            return;
        }

        if (value)
        {
            _trayService.Show();
        }
        else
        {
            _trayService.Hide();
        }
    }

    partial void OnSelectedMemoChanged(Memo? oldValue, Memo? newValue)
    {
        OnPropertyChanged(nameof(HasSelectedMemo));

        if (newValue is null)
        {
            if (!IsEditMode)
            {
                EditorTitle = string.Empty;
                EditorContent = string.Empty;
                IsWindowPinned = false;
            }
            return;
        }

        EditorTitle = newValue.Title;
        EditorContent = newValue.Content;
        IsWindowPinned = newValue.IsPinned;
    }

    partial void OnMemosChanging(ObservableCollection<Memo> value)
    {
        if (value is not null)
        {
            value.CollectionChanged -= OnMemosCollectionChanged;
        }
    }

    partial void OnMemosChanged(ObservableCollection<Memo> value)
    {
        value.CollectionChanged += OnMemosCollectionChanged;
        OnPropertyChanged(nameof(MemoCount));
    }

    private void InitializeAppInfo()
    {
        try
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "2.1.0";
            var appDir = AppContext.BaseDirectory;
            var dataPath = Path.Combine(appDir, ".memodata");
            AppInfo = $"版本：{version} | 数据目录：{dataPath}";
        }
        catch
        {
            AppInfo = "版本：2.1.0 | 数据目录：<应用目录>\\.memodata";
        }
    }

    private void OnMemosCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(MemoCount));
    }

    public void SetStatus(string status)
    {
        StatusText = status;
        _trayService.UpdateText($"DesktopMemo - {status}");
    }

    public void MarkEditing()
    {
        if (SelectedMemo is not null)
        {
            SetStatus("编辑中...");
        }
    }

    public ISettingsService GetSettingsService() => _settingsService;

    private void OnLanguageChanged(object? sender, LanguageChangedEventArgs e)
    {
        // 通知 UI 刷新所有本地化文本
        OnPropertyChanged(nameof(LocalizationService));
        
        // 更新托盘菜单文本
        _trayService.UpdateMenuTexts(key => _localizationService[key]);
    }

    partial void OnSelectedLanguageChanged(CultureInfo? value)
    {
        if (value != null && value.Name != _localizationService.CurrentCulture.Name)
        {
            // 切换语言
            _localizationService.ChangeLanguage(value.Name);
            
            // 更新内存中的设置
            WindowSettings = WindowSettings with { PreferredLanguage = value.Name };
            
            // 保存到设置（异步）
            _ = Task.Run(async () =>
            {
                try
                {
                    await _settingsService.SaveAsync(WindowSettings);
                    System.Diagnostics.Debug.WriteLine($"语言设置已保存: {value.Name}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"保存语言设置失败: {ex}");
                }
            });
            
            SetStatus($"语言已切换到 {value.NativeName}");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _isDisposing = true;

        if (IsTrayEnabled)
        {
            _trayService.Hide();
        }

        _trayService.Dispose();
    }

    [RelayCommand]
    private void FindNext()
    {
        if (SelectedMemo is null || string.IsNullOrWhiteSpace(SearchKeyword))
        {
            SetStatus("没有可以搜索的内容");
            return;
        }

        var matches = _searchService.FindMatches(EditorContent, SearchKeyword, IsCaseSensitive, UseRegex).ToList();
        if (!matches.Any())
        {
            SetStatus("未找到匹配");
            return;
        }

        SetStatus($"找到 {matches.Count} 个匹配");
    }

    [RelayCommand]
    private void ReplaceAll()
    {
        if (SelectedMemo is null || string.IsNullOrWhiteSpace(SearchKeyword))
        {
            SetStatus("替换条件不完整");
            return;
        }

        var newContent = _searchService.Replace(EditorContent, SearchKeyword, ReplaceKeyword ?? string.Empty, IsCaseSensitive, UseRegex);
        if (!ReferenceEquals(newContent, EditorContent))
        {
            EditorContent = newContent;
            SetStatus("已替换所有匹配项");
        }
        else
        {
            SetStatus("无匹配项可替换");
        }
    }

    [RelayCommand]
    private async Task ImportLegacyAsync()
    {
        var legacyMemos = await _migrationService.LoadFromLegacyAsync();
        int importCount = 0;

        foreach (var memo in legacyMemos)
        {
            await _memoRepository.AddAsync(memo);
            importCount++;
        }

        if (importCount > 0)
        {
            var memos = await _memoRepository.GetAllAsync();
            Memos = new ObservableCollection<Memo>(memos.OrderByDescending(m => m.UpdatedAt));
            SelectedMemo = Memos.FirstOrDefault();
        }

        SetStatus(importCount > 0 ? $"导入 {importCount} 条备忘录" : "未发现旧数据");
    }

    [RelayCommand]
    private async Task ExportMarkdownAsync()
    {
        var exportDir = Path.Combine(_migrationService.ExportDirectory, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
        Directory.CreateDirectory(exportDir);

        var memos = await _memoRepository.GetAllAsync();
        int count = 0;

        foreach (var memo in memos)
        {
            var path = Path.Combine(exportDir, $"{memo.Id:N}.md");
            await File.WriteAllTextAsync(path, memo.Content);
            count++;
        }

        SetStatus(count > 0 ? $"导出 {count} 条备忘录" : "没有可导出的备忘录");
    }
}

