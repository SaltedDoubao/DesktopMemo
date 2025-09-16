using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace DesktopMemo.Infrastructure.Performance
{
    public interface IPerformanceService
    {
        Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation, string loadingMessage = "加载中...");
        void DebounceAction(string key, Action action, int delayMs = 300);
        Task<T> CacheResult<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
        void ClearCache();
    }

    public class PerformanceService : IPerformanceService
    {
        private readonly Dictionary<string, System.Threading.Timer> _debounceTimers = new();
        private readonly Dictionary<string, CacheEntry> _cache = new();
        private readonly object _lockObject = new object();

        public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> operation, string loadingMessage = "加载中...")
        {
            try
            {
                // Show loading indicator (would integrate with UI service)
                ShowLoadingIndicator(loadingMessage);

                var result = await operation();
                return result;
            }
            finally
            {
                HideLoadingIndicator();
            }
        }

        public void DebounceAction(string key, Action action, int delayMs = 300)
        {
            lock (_lockObject)
            {
                // Cancel existing timer for this key
                if (_debounceTimers.TryGetValue(key, out var existingTimer))
                {
                    existingTimer.Dispose();
                }

                // Create new timer
                var timer = new System.Threading.Timer(_ =>
                {
                    lock (_lockObject)
                    {
                        _debounceTimers.Remove(key);
                    }

                    // Execute on UI thread if needed
                    if (System.Windows.Application.Current?.Dispatcher != null)
                    {
                        System.Windows.Application.Current.Dispatcher.BeginInvoke(action);
                    }
                    else
                    {
                        action();
                    }
                }, null, delayMs, Timeout.Infinite);

                _debounceTimers[key] = timer;
            }
        }

        public async Task<T> CacheResult<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            lock (_lockObject)
            {
                if (_cache.TryGetValue(key, out var entry))
                {
                    if (!entry.IsExpired)
                    {
                        return (T)entry.Value;
                    }
                    else
                    {
                        _cache.Remove(key);
                    }
                }
            }

            // Execute factory and cache result
            var result = await factory();

            lock (_lockObject)
            {
                _cache[key] = new CacheEntry
                {
                    Value = result!,
                    CreatedAt = DateTime.Now,
                    ExpiresAt = expiry.HasValue ? DateTime.Now.Add(expiry.Value) : DateTime.MaxValue
                };
            }

            return result;
        }

        public void ClearCache()
        {
            lock (_lockObject)
            {
                _cache.Clear();
            }
        }

        private void ShowLoadingIndicator(string message)
        {
            // TODO: Integrate with actual UI loading indicator
            System.Diagnostics.Debug.WriteLine($"Loading: {message}");
        }

        private void HideLoadingIndicator()
        {
            // TODO: Hide loading indicator
            System.Diagnostics.Debug.WriteLine("Loading complete");
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                foreach (var timer in _debounceTimers.Values)
                {
                    timer.Dispose();
                }
                _debounceTimers.Clear();
                _cache.Clear();
            }
        }

        private class CacheEntry
        {
            public object Value { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
            public DateTime ExpiresAt { get; set; }
            public bool IsExpired => DateTime.Now > ExpiresAt;
        }
    }

    // Lazy loading service for ViewModels
    public interface ILazyLoadingService
    {
        Task<IEnumerable<T>> LoadItemsAsync<T>(Func<Task<IEnumerable<T>>> loader, int batchSize = 50);
        void RegisterLazyProperty<T>(string key, Func<Task<T>> loader);
        Task<T> GetLazyPropertyAsync<T>(string key);
    }

    public class LazyLoadingService : ILazyLoadingService
    {
        private readonly Dictionary<string, Lazy<Task<object>>> _lazyProperties = new();
        private readonly IPerformanceService _performanceService;

        public LazyLoadingService(IPerformanceService performanceService)
        {
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
        }

        public async Task<IEnumerable<T>> LoadItemsAsync<T>(Func<Task<IEnumerable<T>>> loader, int batchSize = 50)
        {
            // For now, return all items. In a real implementation,
            // this would implement virtual scrolling or pagination
            return await loader();
        }

        public void RegisterLazyProperty<T>(string key, Func<Task<T>> loader)
        {
            _lazyProperties[key] = new Lazy<Task<object>>(async () => (object)(await loader())!);
        }

        public async Task<T> GetLazyPropertyAsync<T>(string key)
        {
            if (_lazyProperties.TryGetValue(key, out var lazyTask))
            {
                var result = await lazyTask.Value;
                return (T)result;
            }

            throw new KeyNotFoundException($"Lazy property '{key}' not found");
        }
    }

    // Background task scheduler
    public interface IBackgroundTaskService
    {
        void QueueTask(Func<Task> task, TaskPriority priority = TaskPriority.Normal);
        void QueueTask<T>(Func<Task<T>> task, Action<T> onCompleted, TaskPriority priority = TaskPriority.Normal);
        Task WaitForAllTasksAsync();
        void CancelAllTasks();
    }

    public enum TaskPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }

    public class BackgroundTaskService : IBackgroundTaskService
    {
        private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);
        private readonly List<Task> _runningTasks = new();
        private readonly object _lockObject = new object();
        private readonly CancellationTokenSource _cancellationTokenSource = new();

        public void QueueTask(Func<Task> task, TaskPriority priority = TaskPriority.Normal)
        {
            QueueTask(async () =>
            {
                await task();
                return true;
            }, _ => { }, priority);
        }

        public void QueueTask<T>(Func<Task<T>> task, Action<T> onCompleted, TaskPriority priority = TaskPriority.Normal)
        {
            var taskWrapper = Task.Run(async () =>
            {
                await _semaphore.WaitAsync(_cancellationTokenSource.Token);
                try
                {
                    var result = await task();

                    // Execute callback on UI thread if available
                    if (System.Windows.Application.Current?.Dispatcher != null)
                    {
                        _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(() => onCompleted(result));
                    }
                    else
                    {
                        onCompleted(result);
                    }
                }
                finally
                {
                    _semaphore.Release();
                }
            }, _cancellationTokenSource.Token);

            lock (_lockObject)
            {
                _runningTasks.Add(taskWrapper);

                // Clean up completed tasks
                _runningTasks.RemoveAll(t => t.IsCompleted || t.IsCanceled || t.IsFaulted);
            }
        }

        public async Task WaitForAllTasksAsync()
        {
            Task[] tasksToWait;
            lock (_lockObject)
            {
                tasksToWait = _runningTasks.Where(t => !t.IsCompleted).ToArray();
            }

            if (tasksToWait.Length > 0)
            {
                await Task.WhenAll(tasksToWait);
            }
        }

        public void CancelAllTasks()
        {
            _cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            CancelAllTasks();
            _semaphore.Dispose();
            _cancellationTokenSource.Dispose();
        }
    }
}