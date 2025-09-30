using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DesktopMemo.Core.Models;
using Markdig;

namespace DesktopMemo.Infrastructure.Services;

public sealed class MemoMigrationService
{
    private readonly string _dataDirectory;
    private readonly string _appDirectory;

    public MemoMigrationService(string dataDirectory, string appDirectory)
    {
        _dataDirectory = dataDirectory;
        _appDirectory = appDirectory;
        ExportDirectory = Path.Combine(_dataDirectory, "export");
    }

    public string ExportDirectory { get; }

    public async Task<IReadOnlyList<Memo>> LoadFromLegacyAsync()
    {
        var results = new List<Memo>();
        
        // 在应用程序目录下查找旧应用数据目录 Data/content
        var legacyContentDir = Path.Combine(_appDirectory, "Data", "content");

        if (Directory.Exists(legacyContentDir))
        {
            foreach (var file in Directory.GetFiles(legacyContentDir, "*.md"))
            {
                var text = await File.ReadAllTextAsync(file, Encoding.UTF8);
                var memo = ParseMarkdown(Path.GetFileNameWithoutExtension(file), text, File.GetCreationTimeUtc(file), File.GetLastWriteTimeUtc(file));
                if (memo is not null)
                {
                    results.Add(memo);
                }
            }
        }

        // TODO: 支持旧 JSON 格式迁移，可参考 .old 逻辑

        return results;
    }

    private static Memo? ParseMarkdown(string fileName, string markdown, DateTime created, DateTime updated)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return null;
        }

        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(markdown, pipeline);

        string title = fileName;
        var lines = markdown.Split('\n');
        foreach (var line in lines)
        {
            if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                title = line[2..].Trim();
                break;
            }
        }

        return new Memo(
            Guid.NewGuid(),
            title,
            markdown,
            BuildPreview(markdown),
            new DateTimeOffset(created),
            new DateTimeOffset(updated),
            Array.Empty<string>(),
            false);
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
}
