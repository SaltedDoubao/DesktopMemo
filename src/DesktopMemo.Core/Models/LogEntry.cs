using System;

namespace DesktopMemo.Core.Models;

/// <summary>
/// 日志条目
/// </summary>
public sealed record LogEntry
{
    /// <summary>
    /// 日志时间
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
    
    /// <summary>
    /// 日志级别
    /// </summary>
    public LogLevel Level { get; init; }
    
    /// <summary>
    /// 日志来源（模块名称）
    /// </summary>
    public string Source { get; init; }
    
    /// <summary>
    /// 日志消息
    /// </summary>
    public string Message { get; init; }
    
    /// <summary>
    /// 异常信息（可选）
    /// </summary>
    public Exception? Exception { get; init; }

    public LogEntry(DateTimeOffset timestamp, LogLevel level, string source, string message, Exception? exception = null)
    {
        Timestamp = timestamp;
        Level = level;
        Source = source ?? string.Empty;
        Message = message ?? string.Empty;
        Exception = exception;
    }

    /// <summary>
    /// 获取日志级别的显示名称
    /// </summary>
    public string LevelName => Level switch
    {
        LogLevel.Debug => "调试",
        LogLevel.Info => "信息",
        LogLevel.Warning => "警告",
        LogLevel.Error => "错误",
        _ => "未知"
    };

    /// <summary>
    /// 获取完整的日志文本（用于文件输出）
    /// </summary>
    public string ToLogString()
    {
        var exceptionInfo = Exception != null ? $"\n{Exception}" : string.Empty;
        return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{Source}] {Message}{exceptionInfo}";
    }
}

