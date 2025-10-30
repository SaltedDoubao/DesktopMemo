using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.App.ViewModels;

/// <summary>
/// 日志页面 ViewModel
/// </summary>
public partial class LogViewModel : ObservableObject
{
    private readonly ILogService _logService;
    private readonly object _logsLock = new();

    [ObservableProperty]
    private ObservableCollection<LogEntry> _logs = new();

    [ObservableProperty]
    private LogLevel _selectedLogLevel = LogLevel.Debug;

    [ObservableProperty]
    private bool _autoScroll = true;

    [ObservableProperty]
    private int _totalLogCount;

    [ObservableProperty]
    private int _displayedLogCount;

    public LogViewModel(ILogService logService)
    {
        _logService = logService;

        // 启用集合的线程安全访问
        BindingOperations.EnableCollectionSynchronization(Logs, _logsLock);

        // 订阅日志添加事件
        _logService.LogAdded += OnLogAdded;

        // 加载现有日志
        RefreshLogs();
    }

    [RelayCommand]
    private void RefreshLogs()
    {
        var allLogs = _logService.GetAllLogs();
        TotalLogCount = allLogs.Count;

        var filteredLogs = allLogs.Where(log => log.Level >= SelectedLogLevel).ToList();
        DisplayedLogCount = filteredLogs.Count;

        lock (_logsLock)
        {
            Logs.Clear();
            foreach (var log in filteredLogs)
            {
                Logs.Add(log);
            }
        }
    }

    [RelayCommand]
    private async Task LoadHistoryLogsAsync()
    {
        try
        {
            var historyLogs = await _logService.LoadHistoryLogsAsync(1000);

            TotalLogCount = historyLogs.Count;

            var filteredLogs = historyLogs.Where(log => log.Level >= SelectedLogLevel).ToList();
            DisplayedLogCount = filteredLogs.Count;

            lock (_logsLock)
            {
                Logs.Clear();
                foreach (var log in filteredLogs)
                {
                    Logs.Add(log);
                }
            }
        }
        catch
        {
            // 加载失败，保持当前日志
        }
    }

    [RelayCommand]
    private void ClearLogs()
    {
        _logService.ClearLogs();
        lock (_logsLock)
        {
            Logs.Clear();
        }
        TotalLogCount = 0;
        DisplayedLogCount = 0;
    }

    [RelayCommand]
    private void FilterByLevel(string? level)
    {
        if (string.IsNullOrEmpty(level) || !Enum.TryParse<LogLevel>(level, out var logLevel))
        {
            return;
        }

        SelectedLogLevel = logLevel;
        RefreshLogs();
    }

    partial void OnSelectedLogLevelChanged(LogLevel value)
    {
        RefreshLogs();
    }

    private void OnLogAdded(object? sender, LogEntry entry)
    {
        // 检查是否满足过滤条件
        if (entry.Level < SelectedLogLevel)
        {
            return;
        }

        // 在UI线程上添加日志
        System.Windows.Application.Current?.Dispatcher.InvokeAsync(() =>
        {
            lock (_logsLock)
            {
                Logs.Add(entry);
                DisplayedLogCount = Logs.Count;
            }

            // 更新总数
            TotalLogCount = _logService.GetAllLogs().Count;
        });
    }

    /// <summary>
    /// 获取日志级别的显示颜色
    /// </summary>
    public static string GetLogLevelColor(LogLevel level) => level switch
    {
        LogLevel.Debug => "#808080",    // 灰色
        LogLevel.Info => "#2196F3",     // 蓝色
        LogLevel.Warning => "#FF9800",  // 橙色
        LogLevel.Error => "#F44336",    // 红色
        _ => "#000000"
    };
}

