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
    int SortOrder)
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
            sortOrder);
    }

    /// <summary>
    /// 切换完成状态。
    /// </summary>
    public TodoItem ToggleCompleted()
    {
        return this with
        {
            IsCompleted = !IsCompleted,
            UpdatedAt = DateTimeOffset.Now
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
            UpdatedAt = DateTimeOffset.Now
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
            UpdatedAt = DateTimeOffset.Now
        };
    }
}

