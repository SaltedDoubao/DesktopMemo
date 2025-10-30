using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Infrastructure.Services;

/// <summary>
/// 基于文件的日志服务实现
/// </summary>
public sealed class FileLogService : ILogService, IDisposable
{
    private readonly string _logDirectory;
    private readonly int _maxMemoryLogs;
    private readonly List<LogEntry> _memoryLogs;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly string _currentLogFile;
    private bool _disposed;

    public event EventHandler<LogEntry>? LogAdded;

    public FileLogService(string dataDirectory, int maxMemoryLogs = 500)
    {
        _logDirectory = Path.Combine(dataDirectory, ".logs");
        _maxMemoryLogs = maxMemoryLogs;
        _memoryLogs = new List<LogEntry>(maxMemoryLogs);

        Directory.CreateDirectory(_logDirectory);

        // 创建当日日志文件
        var today = DateTime.Now.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        _currentLogFile = Path.Combine(_logDirectory, $"app_{today}.log");

        // 启动时记录一条信息
        Info("FileLogService", "日志服务已初始化");
    }

    public void Debug(string source, string message)
    {
        Log(new LogEntry(DateTimeOffset.Now, LogLevel.Debug, source, message));
    }

    public void Info(string source, string message)
    {
        Log(new LogEntry(DateTimeOffset.Now, LogLevel.Info, source, message));
    }

    public void Warning(string source, string message)
    {
        Log(new LogEntry(DateTimeOffset.Now, LogLevel.Warning, source, message));
    }

    public void Error(string source, string message, Exception? exception = null)
    {
        Log(new LogEntry(DateTimeOffset.Now, LogLevel.Error, source, message, exception));
    }

    public void Log(LogEntry entry)
    {
        if (_disposed) return;

        try
        {
            _lock.Wait();

            // 添加到内存缓存
            _memoryLogs.Add(entry);

            // 保持内存日志数量在限制内
            if (_memoryLogs.Count > _maxMemoryLogs)
            {
                _memoryLogs.RemoveRange(0, _memoryLogs.Count - _maxMemoryLogs);
            }

            // 写入文件（异步，不等待）
            _ = Task.Run(() => WriteToFileAsync(entry));

            // 触发事件
            LogAdded?.Invoke(this, entry);
        }
        finally
        {
            _lock.Release();
        }
    }

    public IReadOnlyList<LogEntry> GetAllLogs()
    {
        try
        {
            _lock.Wait();
            return new List<LogEntry>(_memoryLogs);
        }
        finally
        {
            _lock.Release();
        }
    }

    public IReadOnlyList<LogEntry> GetLogsByLevel(LogLevel minLevel)
    {
        try
        {
            _lock.Wait();
            return _memoryLogs.Where(log => log.Level >= minLevel).ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public void ClearLogs()
    {
        try
        {
            _lock.Wait();
            _memoryLogs.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<LogEntry>> LoadHistoryLogsAsync(int maxCount = 1000)
    {
        var logs = new List<LogEntry>();

        try
        {
            // 获取最近的日志文件（按日期倒序）
            var logFiles = Directory.GetFiles(_logDirectory, "app_*.log")
                .OrderByDescending(f => f)
                .Take(7) // 最多读取7天的日志
                .ToList();

            foreach (var logFile in logFiles)
            {
                if (logs.Count >= maxCount) break;

                var entries = await ParseLogFileAsync(logFile, maxCount - logs.Count);
                logs.AddRange(entries);
            }

            return logs.OrderBy(l => l.Timestamp).ToList();
        }
        catch
        {
            return logs;
        }
    }

    private async Task WriteToFileAsync(LogEntry entry)
    {
        try
        {
            var logLine = entry.ToLogString() + Environment.NewLine;
            await File.AppendAllTextAsync(_currentLogFile, logLine, Encoding.UTF8);
        }
        catch
        {
            // 写入文件失败，静默忽略（避免日志系统本身引发异常）
        }
    }

    private async Task<List<LogEntry>> ParseLogFileAsync(string filePath, int maxCount)
    {
        var logs = new List<LogEntry>();

        try
        {
            var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);
            
            // 正则表达式匹配日志格式: [时间戳] [级别] [来源] 消息
            var logPattern = new Regex(@"^\[([^\]]+)\]\s+\[([^\]]+)\]\s+\[([^\]]+)\]\s+(.+)$", RegexOptions.Compiled);

            foreach (var line in lines.Reverse().Take(maxCount))
            {
                var match = logPattern.Match(line);
                if (match.Success)
                {
                    var timestampStr = match.Groups[1].Value;
                    var levelStr = match.Groups[2].Value;
                    var source = match.Groups[3].Value;
                    var message = match.Groups[4].Value;

                    if (DateTimeOffset.TryParse(timestampStr, out var timestamp) &&
                        Enum.TryParse<LogLevel>(levelStr, out var level))
                    {
                        logs.Add(new LogEntry(timestamp, level, source, message));
                    }
                }
            }
        }
        catch
        {
            // 解析失败，返回已解析的部分
        }

        return logs;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            Info("FileLogService", "日志服务正在关闭");
        }
        catch
        {
            // 忽略
        }

        _lock?.Dispose();
    }
}

