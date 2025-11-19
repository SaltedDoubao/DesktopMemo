using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.App.ViewModels;

/// <summary>
/// 待办事项列表的视图模型。
/// </summary>
public partial class TodoListViewModel : ObservableObject
{
    private readonly ITodoRepository _todoRepository;
    private bool _isInitializing;

    [ObservableProperty]
    private ObservableCollection<TodoItem> _incompleteTodos = new();

    [ObservableProperty]
    private ObservableCollection<TodoItem> _completedTodos = new();

    [ObservableProperty]
    private string _newTodoContent = string.Empty;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private bool _isInputVisible = true;

    // 编辑状态
    [ObservableProperty]
    private Guid? _editingTodoId;

    [ObservableProperty]
    private string _editingContent = string.Empty;

    public int IncompleteTodoCount => IncompleteTodos.Count;
    public int CompletedTodoCount => CompletedTodos.Count;

    /// <summary>
    /// 输入区域可见性改变事件
    /// </summary>
    public event EventHandler<bool>? InputVisibilityChanged;

    public TodoListViewModel(ITodoRepository todoRepository)
    {
        _todoRepository = todoRepository;
    }

    /// <summary>
    /// 初始化并加载待办事项列表。
    /// </summary>
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _isInitializing = true;
        await LoadTodosAsync(cancellationToken);
        _isInitializing = false;
    }

    /// <summary>
    /// 加载所有待办事项并分组。
    /// </summary>
    private async Task LoadTodosAsync(CancellationToken cancellationToken = default)
    {
        var todos = await _todoRepository.GetAllAsync(cancellationToken);
        
        IncompleteTodos.Clear();
        CompletedTodos.Clear();

        foreach (var todo in todos)
        {
            if (todo.IsCompleted)
            {
                CompletedTodos.Add(todo);
            }
            else
            {
                IncompleteTodos.Add(todo);
            }
        }

        OnPropertyChanged(nameof(IncompleteTodoCount));
        OnPropertyChanged(nameof(CompletedTodoCount));
        SetStatus("就绪");
    }

    /// <summary>
    /// 添加新的待办事项。
    /// </summary>
    [RelayCommand]
    private async Task AddTodoAsync()
    {
        if (string.IsNullOrWhiteSpace(NewTodoContent))
        {
            SetStatus("请输入待办事项内容");
            return;
        }

        var newTodo = TodoItem.CreateNew(NewTodoContent.Trim(), IncompleteTodos.Count);
        await _todoRepository.AddAsync(newTodo);

        IncompleteTodos.Add(newTodo);
        NewTodoContent = string.Empty;

        OnPropertyChanged(nameof(IncompleteTodoCount));
        SetStatus("已添加待办事项");
    }

    /// <summary>
    /// 切换待办事项的完成状态。
    /// </summary>
    [RelayCommand]
    private async Task ToggleTodoAsync(TodoItem todoItem)
    {
        if (todoItem is null)
        {
            return;
        }

        var updatedTodo = todoItem.ToggleCompleted();
        await _todoRepository.UpdateAsync(updatedTodo);

        if (updatedTodo.IsCompleted)
        {
            // 从未完成列表移到已完成列表
            IncompleteTodos.Remove(todoItem);
            CompletedTodos.Insert(0, updatedTodo);
            SetStatus("已完成待办事项");
        }
        else
        {
            // 从已完成列表移回未完成列表
            CompletedTodos.Remove(todoItem);
            IncompleteTodos.Insert(0, updatedTodo);
            SetStatus("已标记为未完成");
        }

        OnPropertyChanged(nameof(IncompleteTodoCount));
        OnPropertyChanged(nameof(CompletedTodoCount));
    }

    /// <summary>
    /// 删除待办事项。
    /// </summary>
    [RelayCommand]
    private async Task DeleteTodoAsync(TodoItem todoItem)
    {
        if (todoItem is null)
        {
            return;
        }

        await _todoRepository.DeleteAsync(todoItem.Id);

        if (todoItem.IsCompleted)
        {
            CompletedTodos.Remove(todoItem);
        }
        else
        {
            IncompleteTodos.Remove(todoItem);
        }

        OnPropertyChanged(nameof(IncompleteTodoCount));
        OnPropertyChanged(nameof(CompletedTodoCount));
        SetStatus("已删除待办事项");
    }

    /// <summary>
    /// 清除所有已完成的待办事项。
    /// </summary>
    [RelayCommand]
    private async Task ClearCompletedAsync()
    {
        if (CompletedTodos.Count == 0)
        {
            SetStatus("没有已完成的待办事项");
            return;
        }

        var completedIds = CompletedTodos.Select(t => t.Id).ToList();
        
        foreach (var id in completedIds)
        {
            await _todoRepository.DeleteAsync(id);
        }

        CompletedTodos.Clear();
        OnPropertyChanged(nameof(CompletedTodoCount));
        SetStatus($"已清除 {completedIds.Count} 个已完成事项");
    }

    /// <summary>
    /// 编辑待办事项内容。
    /// </summary>
    [RelayCommand]
    private async Task UpdateTodoContentAsync((TodoItem todoItem, string newContent) parameters)
    {
        if (parameters.todoItem is null || string.IsNullOrWhiteSpace(parameters.newContent))
        {
            return;
        }

        var updatedTodo = parameters.todoItem.WithContent(parameters.newContent.Trim());
        await _todoRepository.UpdateAsync(updatedTodo);

        // 更新列表中的项
        if (updatedTodo.IsCompleted)
        {
            var index = CompletedTodos.IndexOf(parameters.todoItem);
            if (index >= 0)
            {
                CompletedTodos[index] = updatedTodo;
            }
        }
        else
        {
            var index = IncompleteTodos.IndexOf(parameters.todoItem);
            if (index >= 0)
            {
                IncompleteTodos[index] = updatedTodo;
            }
        }

        SetStatus("已更新待办事项");
    }

    /// <summary>
    /// 切换待办事项输入区域的显示/隐藏。
    /// </summary>
    [RelayCommand]
    private void ToggleInput()
    {
        IsInputVisible = !IsInputVisible;
        SetStatus(IsInputVisible ? "显示输入区域" : "隐藏输入区域");
    }

    /// <summary>
    /// 开始编辑待办事项。
    /// </summary>
    [RelayCommand]
    private void BeginEditTodo(TodoItem todoItem)
    {
        if (todoItem is null)
        {
            return;
        }

        EditingTodoId = todoItem.Id;
        EditingContent = todoItem.Content;
    }

    /// <summary>
    /// 保存编辑的待办事项。
    /// </summary>
    [RelayCommand]
    private async Task SaveEditTodoAsync()
    {
        if (EditingTodoId == null || string.IsNullOrWhiteSpace(EditingContent))
        {
            CancelEditTodo();
            return;
        }

        // 查找正在编辑的项
        var todoItem = IncompleteTodos.FirstOrDefault(t => t.Id == EditingTodoId)
                      ?? CompletedTodos.FirstOrDefault(t => t.Id == EditingTodoId);

        if (todoItem != null)
        {
            await UpdateTodoContentAsync((todoItem, EditingContent));
        }

        // 清除编辑状态
        EditingTodoId = null;
        EditingContent = string.Empty;
    }

    /// <summary>
    /// 取消编辑待办事项。
    /// </summary>
    [RelayCommand]
    private void CancelEditTodo()
    {
        EditingTodoId = null;
        EditingContent = string.Empty;
    }

    partial void OnIsInputVisibleChanged(bool value)
    {
        // 初始化期间不触发保存事件
        if (_isInitializing)
        {
            return;
        }

        // 触发事件，通知 MainViewModel 保存设置
        InputVisibilityChanged?.Invoke(this, value);
    }

    private void SetStatus(string status)
    {
        StatusText = status;
    }
}

