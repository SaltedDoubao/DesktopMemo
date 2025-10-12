using System;
using System.Collections.Generic;
using System.Linq;

namespace DesktopMemo.Core.Models;

/// <summary>
/// 表示一条完整的备忘录数据。
/// </summary>
public sealed record Memo(
    Guid Id,
    string Title,
    string Content,
    string Preview,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    IReadOnlyList<string> Tags,
    bool IsPinned)
{
    /// <summary>
    /// 获取用于显示的标题。如果内容不为空，使用第一行作为标题；否则使用原标题。
    /// </summary>
    public string DisplayTitle => GetDisplayTitle();

    private string GetDisplayTitle()
    {
        if (string.IsNullOrWhiteSpace(Content))
        {
            return Title;
        }

        var lines = Content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var firstLine = lines.FirstOrDefault()?.Trim();

        if (string.IsNullOrWhiteSpace(firstLine))
        {
            return Title;
        }

        return firstLine.Length > 50 ? firstLine.Substring(0, 50) + "..." : firstLine;
    }
    public static Memo CreateNew(string title, string content)
    {
        var now = DateTimeOffset.Now;
        var preview = BuildPreview(content);

        return new Memo(
            Guid.NewGuid(),
            title,
            content,
            preview,
            now,
            now,
            Array.Empty<string>(),
            false);
    }

    public Memo WithContent(string content, DateTimeOffset timestamp) => this with
    {
        Content = content,
        Preview = BuildPreview(content),
        UpdatedAt = timestamp
    };

    public Memo WithMetadata(string title, IReadOnlyList<string> tags, bool isPinned, DateTimeOffset timestamp) => this with
    {
        Title = title,
        Tags = tags,
        IsPinned = isPinned,
        UpdatedAt = timestamp
    };

    private static string BuildPreview(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return string.Empty;
        }

        var trimmed = content.Replace("\r\n", "\n").Replace('\r', '\n').Trim();
        if (trimmed.Length <= 120)
        {
            return trimmed;
        }

        var firstLineBreak = trimmed.IndexOf('\n');
        if (firstLineBreak >= 0 && firstLineBreak < 120)
        {
            return trimmed[..firstLineBreak];
        }

        return trimmed.Substring(0, 120) + "...";
    }
}

