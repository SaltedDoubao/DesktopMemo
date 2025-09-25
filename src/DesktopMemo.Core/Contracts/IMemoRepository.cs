using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Contracts;

/// <summary>
/// 定义备忘录持久化访问的契约。
/// </summary>
public interface IMemoRepository
{
    Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Memo memo, CancellationToken cancellationToken = default);

    Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

