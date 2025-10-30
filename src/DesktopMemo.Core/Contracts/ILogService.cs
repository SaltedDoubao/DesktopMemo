using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILogService
{
    /// <summary>
    /// 记录调试信息
    /// </summary>
    void Debug(string source, string message);
    
    /// <summary>
    /// 记录常规信息
    /// </summary>
    void Info(string source, string message);
    
    /// <summary>
    /// 记录警告信息
    /// </summary>
    void Warning(string source, string message);
    
    /// <summary>
    /// 记录错误信息
    /// </summary>
    void Error(string source, string message, Exception? exception = null);
    
    /// <summary>
    /// 记录日志条目
    /// </summary>
    void Log(LogEntry entry);
    
    /// <summary>
    /// 获取所有日志条目（内存缓存）
    /// </summary>
    IReadOnlyList<LogEntry> GetAllLogs();
    
    /// <summary>
    /// 获取指定级别及以上的日志条目
    /// </summary>
    IReadOnlyList<LogEntry> GetLogsByLevel(LogLevel minLevel);
    
    /// <summary>
    /// 清空内存中的日志
    /// </summary>
    void ClearLogs();
    
    /// <summary>
    /// 从日志文件加载历史日志
    /// </summary>
    Task<IReadOnlyList<LogEntry>> LoadHistoryLogsAsync(int maxCount = 1000);
    
    /// <summary>
    /// 日志添加事件（用于实时更新UI）
    /// </summary>
    event EventHandler<LogEntry>? LogAdded;
}

