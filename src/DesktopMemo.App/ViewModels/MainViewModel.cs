using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IMemoRepository _memoRepository;
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private ObservableCollection<Memo> _memos = new();

    [ObservableProperty]
    private Memo? _selectedMemo;

    [ObservableProperty]
    private string _editorContent = string.Empty;

    [ObservableProperty]
    private WindowSettings _windowSettings = WindowSettings.Default;

    public MainViewModel(IMemoRepository memoRepository, ISettingsService settingsService)
    {
        _memoRepository = memoRepository;
        _settingsService = settingsService;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _settingsService.LoadAsync(cancellationToken).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() => WindowSettings = settings);

        var memos = await _memoRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Memos = new ObservableCollection<Memo>(memos.OrderByDescending(m => m.UpdatedAt));
            SelectedMemo = Memos.FirstOrDefault();
            EditorContent = SelectedMemo?.Content ?? string.Empty;
        });
    }

    [RelayCommand]
    private async Task CreateMemoAsync()
    {
        var memo = Memo.CreateNew("新的备忘录", "");
        await _memoRepository.AddAsync(memo).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Memos.Insert(0, memo);
            SelectedMemo = memo;
            EditorContent = string.Empty;
        });
    }

    [RelayCommand]
    private async Task SaveMemoAsync()
    {
        if (SelectedMemo is null)
        {
            return;
        }

        var updated = SelectedMemo.WithContent(EditorContent, DateTimeOffset.UtcNow);
        await _memoRepository.UpdateAsync(updated).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() =>
        {
            var index = Memos.IndexOf(SelectedMemo);
            if (index >= 0)
            {
                Memos[index] = updated;
                SelectedMemo = updated;
            }
        });
    }

    [RelayCommand]
    private async Task DeleteMemoAsync()
    {
        if (SelectedMemo is null)
        {
            return;
        }

        var deleting = SelectedMemo;
        await _memoRepository.DeleteAsync(deleting.Id).ConfigureAwait(false);

        Application.Current?.Dispatcher.Invoke(() =>
        {
            Memos.Remove(deleting);
            SelectedMemo = Memos.FirstOrDefault();
            EditorContent = SelectedMemo?.Content ?? string.Empty;
        });
    }

    partial void OnSelectedMemoChanged(Memo? oldValue, Memo? newValue)
    {
        EditorContent = newValue?.Content ?? string.Empty;
    }

    [RelayCommand]
    private async Task PersistWindowSettingsAsync()
    {
        await _settingsService.SaveAsync(WindowSettings).ConfigureAwait(false);
    }
}

