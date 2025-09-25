using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopMemo.Core.Models;

namespace DesktopMemo.App.ViewModels;

public partial class MemoListViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Memo> _items = new();

    [ObservableProperty]
    private Memo? _selectedMemo;
}

