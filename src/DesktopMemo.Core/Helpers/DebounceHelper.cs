using System;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopMemo.Core.Helpers;

/// <summary>
/// 防抖辅助类，用于延迟执行操作以减少频繁调用
/// </summary>
public class DebounceHelper : IDisposable
{
    private readonly int _delayMilliseconds;
    private CancellationTokenSource? _cts;
    private readonly object _lock = new();

    /// <summary>
    /// 创建防抖辅助类实例
    /// </summary>
    /// <param name="delayMilliseconds">延迟时间（毫秒），默认500毫秒</param>
    public DebounceHelper(int delayMilliseconds = 500)
    {
        _delayMilliseconds = delayMilliseconds;
    }

    /// <summary>
    /// 执行防抖操作
    /// </summary>
    /// <param name="action">要执行的异步操作</param>
    public void Debounce(Func<Task> action)
    {
        lock (_lock)
        {
            // 取消之前的操作
            _cts?.Cancel();
            _cts?.Dispose();
            
            // 创建新的取消令牌
            _cts = new CancellationTokenSource();
            var token = _cts.Token;

            // 延迟执行
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(_delayMilliseconds, token);
                    
                    // 如果没有被取消，执行操作
                    if (!token.IsCancellationRequested)
                    {
                        await action();
                    }
                }
                catch (TaskCanceledException)
                {
                    // 操作被取消，忽略
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"防抖操作执行失败: {ex}");
                }
            }, token);
        }
    }

    /// <summary>
    /// 立即执行待处理的操作（如果有）
    /// </summary>
    public async Task FlushAsync()
    {
        CancellationTokenSource? currentCts;
        
        lock (_lock)
        {
            currentCts = _cts;
            _cts = null;
        }

        if (currentCts != null)
        {
            currentCts.Cancel();
            currentCts.Dispose();
        }

        // 等待一小段时间确保操作完成
        await Task.Delay(50);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }
    }
}

