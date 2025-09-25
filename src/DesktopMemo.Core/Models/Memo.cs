using System;
using System.Collections.Generic;

namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示一条完整的备忘录数据。
/// </summary>
public sealed record Memo(
    Guid Id,
    string Title,
    string Content,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    bool IsPinned)
{
    public static Memo CreateNew(string title, string content)
    {
        var now = DateTimeOffset.UtcNow;

        return new Memo(
            Guid.NewGuid(),
            title,
            content,
            now,
            now,
            Array.Empty<string>(),
            false);
    }

    public Memo WithContent(string content, DateTimeOffset timestamp) => this with
    {
        Content = content,
        UpdatedAt = timestamp
    };

    public Memo WithMetadata(string title, IReadOnlyList<string> tags, bool isPinned, DateTimeOffset timestamp) => this with
    {
        Title = title,
        Tags = tags,
        IsPinned = isPinned,
        UpdatedAt = timestamp
    };
}

