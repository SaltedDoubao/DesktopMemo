using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 定义待办事项持久化访问的契约。
/// </summary>
public interface ITodoRepository
{
    Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default);

    Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

