using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Infrastructure.Repositories;

/// <summary>
/// 基于 JSON 文件的待办事项存储实现。
/// </summary>
public sealed class JsonTodoRepository : ITodoRepository
{
    private const string TodoFileName = "todos.json";
    private readonly string _todoFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _fileLock = new(1, 1);

    public JsonTodoRepository(string dataDirectory)
    {
        Directory.CreateDirectory(dataDirectory);
        _todoFilePath = Path.Combine(dataDirectory, TodoFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() },
            WriteIndented = true
        };
    }

    public async Task<IReadOnlyList<TodoItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!File.Exists(_todoFilePath))
            {
                return Array.Empty<TodoItem>();
            }

            await using var stream = File.OpenRead(_todoFilePath);
            var items = await JsonSerializer.DeserializeAsync<List<TodoItemDto>>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false);
            
            return items?.Select(dto => dto.ToModel()).OrderBy(t => t.SortOrder).ThenBy(t => t.CreatedAt).ToList() 
                ?? new List<TodoItem>();
        }
        catch (JsonException)
        {
            // JSON 格式错误，返回空列表
            return Array.Empty<TodoItem>();
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var items = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return items.FirstOrDefault(t => t.Id == id);
    }

    public async Task AddAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = (await GetAllInternalAsync(cancellationToken).ConfigureAwait(false)).ToList();
            items.Add(TodoItemDto.FromModel(todoItem));
            await SaveAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task UpdateAsync(TodoItem todoItem, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = (await GetAllInternalAsync(cancellationToken).ConfigureAwait(false)).ToList();
            var index = items.FindIndex(t => t.Id == todoItem.Id);
            
            if (index >= 0)
            {
                items[index] = TodoItemDto.FromModel(todoItem);
                await SaveAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _fileLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var items = (await GetAllInternalAsync(cancellationToken).ConfigureAwait(false)).ToList();
            items.RemoveAll(t => t.Id == id);
            await SaveAllInternalAsync(items, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    private async Task<List<TodoItemDto>> GetAllInternalAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_todoFilePath))
        {
            return new List<TodoItemDto>();
        }

        try
        {
            await using var stream = File.OpenRead(_todoFilePath);
            return await JsonSerializer.DeserializeAsync<List<TodoItemDto>>(stream, _jsonOptions, cancellationToken).ConfigureAwait(false)
                ?? new List<TodoItemDto>();
        }
        catch (JsonException)
        {
            return new List<TodoItemDto>();
        }
    }

    private async Task SaveAllInternalAsync(List<TodoItemDto> items, CancellationToken cancellationToken)
    {
        await using var stream = File.Create(_todoFilePath);
        await JsonSerializer.SerializeAsync(stream, items, _jsonOptions, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 用于 JSON 序列化的 DTO 类。
    /// </summary>
    private sealed class TodoItemDto
    {
        public Guid Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset UpdatedAt { get; set; }
        public int SortOrder { get; set; }

        public TodoItem ToModel()
        {
            return new TodoItem(Id, Content, IsCompleted, CreatedAt, UpdatedAt, SortOrder);
        }

        public static TodoItemDto FromModel(TodoItem item)
        {
            return new TodoItemDto
            {
                Id = item.Id,
                Content = item.Content,
                IsCompleted = item.IsCompleted,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                SortOrder = item.SortOrder
            };
        }
    }
}

