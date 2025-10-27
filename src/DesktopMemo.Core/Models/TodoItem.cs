using System;

namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示一个待办事项。
/// </summary>
public sealed record TodoItem(
    Guid Id,
    string Content,
    bool IsCompleted,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    int SortOrder,
    int Version = 1,
    SyncStatus SyncStatus = SyncStatus.Synced,
    DateTimeOffset? CompletedAt = null,
    DateTimeOffset? DeletedAt = null)
{
    /// <summary>
    /// 创建一个新的待办事项。
    /// </summary>
    public static TodoItem CreateNew(string content, int sortOrder = 0)
    {
        var now = DateTimeOffset.Now;
        return new TodoItem(
            Guid.NewGuid(),
            content,
            false,
            now,
            now,
            sortOrder,
            Version: 1,
            SyncStatus: SyncStatus.PendingSync  // 新建项标记为待同步
        );
    }

    /// <summary>
    /// 切换完成状态。
    /// </summary>
    public TodoItem ToggleCompleted()
    {
        var now = DateTimeOffset.Now;
        return this with
        {
            IsCompleted = !IsCompleted,
            UpdatedAt = now,
            CompletedAt = !IsCompleted ? now : null,
            Version = Version + 1,
            SyncStatus = SyncStatus.PendingSync
        };
    }

    /// <summary>
    /// 更新内容。
    /// </summary>
    public TodoItem WithContent(string content)
    {
        return this with
        {
            Content = content,
            UpdatedAt = DateTimeOffset.Now,
            Version = Version + 1,
            SyncStatus = SyncStatus.PendingSync
        };
    }

    /// <summary>
    /// 更新排序顺序。
    /// </summary>
    public TodoItem WithSortOrder(int sortOrder)
    {
        return this with
        {
            SortOrder = sortOrder,
            UpdatedAt = DateTimeOffset.Now,
            Version = Version + 1,
            SyncStatus = SyncStatus.PendingSync
        };
    }

    /// <summary>
    /// 标记为已同步（云同步使用）。
    /// </summary>
    public TodoItem MarkAsSynced(int remoteVersion)
    {
        return this with
        {
            Version = remoteVersion,
            SyncStatus = SyncStatus.Synced
        };
    }

    /// <summary>
    /// 标记为冲突状态（云同步使用）。
    /// </summary>
    public TodoItem MarkAsConflict()
    {
        return this with
        {
            SyncStatus = SyncStatus.Conflict
        };
    }
}

