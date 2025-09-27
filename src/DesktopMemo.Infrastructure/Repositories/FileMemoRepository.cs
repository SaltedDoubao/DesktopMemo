using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Infrastructure.Repositories;

/// <summary>
/// 基于文件系统的 Markdown 备忘录存储实现。
/// </summary>
public sealed class FileMemoRepository : IMemoRepository
{
    private const string IndexFileName = "index.json";
    private readonly string _contentDirectory;
    private readonly JsonSerializerOptions _jsonOptions;

    public FileMemoRepository(string dataDirectory)
    {
        _contentDirectory = Path.Combine(dataDirectory, "content");

        Directory.CreateDirectory(_contentDirectory);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };
    }

    public async Task<IReadOnlyList<Memo>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        var memos = new List<Memo>(index.Order.Count);

        foreach (var memoId in index.Order)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var path = GetMemoPath(memoId);
            if (!File.Exists(path))
            {
                continue;
            }

            var memo = await LoadMemoAsync(path, cancellationToken).ConfigureAwait(false);
            memos.Add(memo);
        }

        return memos;
    }

    public async Task<Memo?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = GetMemoPath(id);
        if (!File.Exists(path))
        {
            return null;
        }

        return await LoadMemoAsync(path, cancellationToken).ConfigureAwait(false);
    }

    public async Task AddAsync(Memo memo, CancellationToken cancellationToken = default)
    {
        var path = GetMemoPath(memo.Id);
        await SaveMemoAsync(path, memo, cancellationToken).ConfigureAwait(false);

        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        if (!index.Order.Contains(memo.Id))
        {
            index.Order.Insert(0, memo.Id);
            await SaveIndexAsync(index, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task UpdateAsync(Memo memo, CancellationToken cancellationToken = default)
    {
        var path = GetMemoPath(memo.Id);
        await SaveMemoAsync(path, memo, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var path = GetMemoPath(id);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        var index = await LoadIndexAsync(cancellationToken).ConfigureAwait(false);
        if (index.Order.Remove(id))
        {
            await SaveIndexAsync(index, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task<Memo> LoadMemoAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(path);
        using var reader = new StreamReader(stream, Encoding.UTF8);

        string? line;
        var firstLine = await reader.ReadLineAsync().ConfigureAwait(false);
        if (firstLine is null || !firstLine.Equals("---", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"备忘录文件 {path} 缺少 front matter。");
        }

        var metadataBuilder = new StringBuilder();
        while ((line = await reader.ReadLineAsync().ConfigureAwait(false)) is not null && !line.Equals("---", StringComparison.Ordinal))
        {
            metadataBuilder.AppendLine(line);
        }

        if (line is null)
        {
            throw new InvalidDataException($"备忘录文件 {path} front matter 未正确结束。");
        }

        var content = await reader.ReadToEndAsync().ConfigureAwait(false);
        var metadata = ParseMetadata(metadataBuilder.ToString());

        return new Memo(
            metadata.Id,
            metadata.Title,
            content,
            BuildPreview(content),
            metadata.CreatedAt,
            metadata.UpdatedAt,
            metadata.Tags,
            metadata.IsPinned);
    }

    private async Task SaveMemoAsync(string path, Memo memo, CancellationToken cancellationToken)
    {
        var builder = new StringBuilder();
        builder.AppendLine("---");
        builder.AppendLine($"id: {memo.Id}");
        builder.AppendLine($"title: {EscapeYamlString(memo.Title)}");
        builder.AppendLine($"createdAt: {memo.CreatedAt.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"updatedAt: {memo.UpdatedAt.ToString("O", CultureInfo.InvariantCulture)}");
        builder.AppendLine($"isPinned: {memo.IsPinned.ToString(CultureInfo.InvariantCulture)}");
        builder.AppendLine("tags:");
        foreach (var tag in memo.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            builder.AppendLine($"  - {EscapeYamlString(tag)}");
        }
        builder.AppendLine("---");
        builder.AppendLine(memo.Content);

        await File.WriteAllTextAsync(path, builder.ToString(), Encoding.UTF8, cancellationToken).ConfigureAwait(false);
    }

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

    private static string EscapeYamlString(string value)
    {
        return value.Contains(':', StringComparison.Ordinal) || value.Contains('"')
            ? $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\""
            : value;
    }

    private async Task<MemoIndex> LoadIndexAsync(CancellationToken cancellationToken)
    {
        var path = Path.Combine(_contentDirectory, IndexFileName);
        if (!File.Exists(path))
        {
            return new MemoIndex();
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<MemoIndex>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false)
            ?? new MemoIndex();
    }

    private async Task SaveIndexAsync(MemoIndex index, CancellationToken cancellationToken)
    {
        var path = Path.Combine(_contentDirectory, IndexFileName);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, index, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    private string GetMemoPath(Guid id) => Path.Combine(_contentDirectory, $"{id:N}.md");

    private static MemoMetadata ParseMetadata(string yaml)
    {
        var id = Guid.Empty;
        var title = string.Empty;
        var createdAt = DateTimeOffset.Now;
        var updatedAt = DateTimeOffset.Now;
        var isPinned = false;
        var tags = new List<string>();

        using var reader = new StringReader(yaml);
        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            if (line.StartsWith("id:", StringComparison.Ordinal))
            {
                id = Guid.Parse(line[3..].Trim());
            }
            else if (line.StartsWith("title:", StringComparison.Ordinal))
            {
                title = line[6..].Trim().Trim('"');
            }
            else if (line.StartsWith("createdAt:", StringComparison.Ordinal))
            {
                createdAt = DateTimeOffset.Parse(line[10..].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
            else if (line.StartsWith("updatedAt:", StringComparison.Ordinal))
            {
                updatedAt = DateTimeOffset.Parse(line[10..].Trim(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            }
            else if (line.StartsWith("isPinned:", StringComparison.Ordinal))
            {
                isPinned = bool.Parse(line[9..].Trim());
            }
            else if (line.TrimStart().StartsWith("-", StringComparison.Ordinal))
            {
                var dashIndex = line.IndexOf('-');
                if (dashIndex >= 0)
                {
                    tags.Add(line.Substring(dashIndex + 1).Trim().Trim('"'));
                }
            }
        }

        return new MemoMetadata(id, title, createdAt, updatedAt, tags, isPinned);
    }

    private sealed class MemoIndex
    {
        public List<Guid> Order { get; set; } = new();
    }

    private sealed record MemoMetadata(
        Guid Id,
        string Title,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyList<string> Tags,
        bool IsPinned);
}

