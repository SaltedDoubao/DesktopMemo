using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Models;

namespace DesktopMemo.UI.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IMemoService _memoService;
        private readonly IWindowManagementService _windowService;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;

        private MemoModel? _currentMemo;
        private bool _isEditMode;
        private string _searchText = string.Empty;
        private bool _isPenetrationMode;
        private string _statusText = "就绪";

        public ObservableCollection<MemoModel> Memos { get; }
        public ObservableCollection<MemoModel> FilteredMemos { get; }

        // Window properties
        public string WindowTitle => _localizationService?.GetString("window.title") ?? "备忘录";
        public string PlaceholderText => _localizationService?.GetString("memo.welcome.content") ?? "点击此处开始编辑...";
        public string MemoCountText => $"{(FilteredMemos?.Count ?? 0)} / {(Memos?.Count ?? 0)}";

        public MemoModel? CurrentMemo
        {
            get => _currentMemo;
            set
            {
                if (SetProperty(ref _currentMemo, value))
                {
                    OnPropertyChanged(nameof(HasCurrentMemo));
                    OnPropertyChanged(nameof(CurrentMemoTitle));
                    OnPropertyChanged(nameof(CurrentMemoContent));
                }
            }
        }

        public bool HasCurrentMemo => CurrentMemo != null;

        public string CurrentMemoTitle
        {
            get => CurrentMemo?.Title ?? string.Empty;
            set
            {
                if (CurrentMemo != null && CurrentMemo.Title != value)
                {
                    CurrentMemo.Title = value;
                    OnPropertyChanged();
                    _ = SaveCurrentMemoAsync();
                }
            }
        }

        public string CurrentMemoContent
        {
            get => CurrentMemo?.Content ?? string.Empty;
            set
            {
                if (CurrentMemo != null && CurrentMemo.Content != value)
                {
                    CurrentMemo.Content = value;
                    OnPropertyChanged();
                    _ = SaveCurrentMemoAsync();
                }
            }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    FilterMemos();
                }
            }
        }

        public bool IsPenetrationMode
        {
            get => _isPenetrationMode;
            set => SetProperty(ref _isPenetrationMode, value);
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        // Commands
        public ICommand AddMemoCommand { get; }
        public ICommand DeleteMemoCommand { get; }
        public ICommand SaveMemoCommand { get; }
        public ICommand SwitchToNextMemoCommand { get; }
        public ICommand SwitchToPreviousMemoCommand { get; }
        public ICommand ToggleEditModeCommand { get; }
        public ICommand TogglePinCommand { get; }
        public ICommand SetTopmostModeCommand { get; }
        public ICommand SetPresetPositionCommand { get; }
        public ICommand TogglePenetrationModeCommand { get; }
        public ICommand SelectMemoCommand { get; }

        public MainViewModel(
            IMemoService memoService,
            IWindowManagementService windowService,
            ISettingsService settingsService,
            ILocalizationService localizationService)
        {
            _memoService = memoService ?? throw new ArgumentNullException(nameof(memoService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));

            Memos = new ObservableCollection<MemoModel>();
            FilteredMemos = new ObservableCollection<MemoModel>();

            // Initialize Commands
            AddMemoCommand = new RelayCommand(async () => await AddMemoAsync());
            DeleteMemoCommand = new RelayCommand(
                async () => await DeleteCurrentMemoAsync(),
                () => HasCurrentMemo);
            SaveMemoCommand = new RelayCommand(
                async () => await SaveCurrentMemoAsync(),
                () => HasCurrentMemo);
            SwitchToNextMemoCommand = new RelayCommand(SwitchToNextMemo, () => Memos.Count > 1);
            SwitchToPreviousMemoCommand = new RelayCommand(SwitchToPreviousMemo, () => Memos.Count > 1);
            ToggleEditModeCommand = new RelayCommand(() => IsEditMode = !IsEditMode);
            TogglePinCommand = new RelayCommand(TogglePin, () => HasCurrentMemo);
            SetTopmostModeCommand = new RelayCommand<string>(SetTopmostMode);
            SetPresetPositionCommand = new RelayCommand<string>(SetPresetPosition);
            TogglePenetrationModeCommand = new RelayCommand(() => IsPenetrationMode = !IsPenetrationMode);
            SelectMemoCommand = new RelayCommand<MemoModel>(SelectMemo);

            // Load initial data
            _ = LoadMemosAsync();
        }

        private async Task LoadMemosAsync()
        {
            try
            {
                var memos = await _memoService.LoadMemosAsync();
                Memos.Clear();
                foreach (var memo in memos)
                {
                    Memos.Add(memo);
                }

                if (Memos.Count == 0)
                {
                    // Create default memo
                    await AddDefaultMemoAsync();
                }
                else
                {
                    CurrentMemo = Memos.FirstOrDefault();
                }

                FilterMemos();
                StatusText = "备忘录已加载";
            }
            catch (Exception ex)
            {
                StatusText = $"加载失败: {ex.Message}";
            }
        }

        private async Task AddDefaultMemoAsync()
        {
            var defaultMemo = await _memoService.CreateMemoAsync(
                "欢迎使用DesktopMemo",
                "这是您的第一条备忘录！\n\n点击此处开始编辑...");
            Memos.Add(defaultMemo);
            CurrentMemo = defaultMemo;
        }

        private async Task AddMemoAsync()
        {
            var newMemo = await _memoService.CreateMemoAsync();
            Memos.Insert(0, newMemo);
            CurrentMemo = newMemo;
            IsEditMode = true;
            FilterMemos();
            StatusText = "已创建新备忘录";
        }

        private async Task DeleteCurrentMemoAsync()
        {
            if (CurrentMemo == null) return;

            var memoToDelete = CurrentMemo;
            var index = Memos.IndexOf(memoToDelete);

            if (_settingsService.GetSetting("ShowDeletePrompt", true))
            {
                // TODO: Show confirmation dialog
            }

            await _memoService.DeleteMemoAsync(memoToDelete.Id);
            Memos.Remove(memoToDelete);

            if (Memos.Count > 0)
            {
                CurrentMemo = Memos[Math.Min(index, Memos.Count - 1)];
            }
            else
            {
                await AddDefaultMemoAsync();
            }

            FilterMemos();
            StatusText = "备忘录已删除";
        }

        private async Task SaveCurrentMemoAsync()
        {
            if (CurrentMemo == null) return;

            await _memoService.SaveMemoAsync(CurrentMemo);
            StatusText = "已保存";
        }

        private void SwitchToNextMemo()
        {
            if (CurrentMemo == null || FilteredMemos.Count <= 1) return;

            var currentIndex = FilteredMemos.IndexOf(CurrentMemo);
            var nextIndex = (currentIndex + 1) % FilteredMemos.Count;
            CurrentMemo = FilteredMemos[nextIndex];
        }

        private void SwitchToPreviousMemo()
        {
            if (CurrentMemo == null || FilteredMemos.Count <= 1) return;

            var currentIndex = FilteredMemos.IndexOf(CurrentMemo);
            var previousIndex = currentIndex - 1;
            if (previousIndex < 0) previousIndex = FilteredMemos.Count - 1;
            CurrentMemo = FilteredMemos[previousIndex];
        }

        private void TogglePin()
        {
            if (CurrentMemo == null) return;

            CurrentMemo.IsPinned = !CurrentMemo.IsPinned;
            _ = SaveCurrentMemoAsync();

            // Re-sort memos
            var sortedMemos = Memos.OrderByDescending(m => m.IsPinned)
                                  .ThenByDescending(m => m.UpdatedAt)
                                  .ToList();
            Memos.Clear();
            foreach (var memo in sortedMemos)
            {
                Memos.Add(memo);
            }
            FilterMemos();
        }

        private void SetTopmostMode(string? mode)
        {
            if (Enum.TryParse<TopmostMode>(mode, out var topmostMode))
            {
                _windowService.SetTopmostMode(topmostMode);
                StatusText = topmostMode switch
                {
                    TopmostMode.Normal => "已切换到普通模式",
                    TopmostMode.Desktop => "已切换到桌面置顶模式",
                    TopmostMode.Always => "已切换到总是置顶模式",
                    _ => "模式已更改"
                };
            }
        }

        private void SetPresetPosition(string? presetName)
        {
            if (!string.IsNullOrEmpty(presetName))
            {
                _windowService.SetPresetPosition(presetName);
                StatusText = $"窗口已移动到: {GetPresetPositionDisplayName(presetName)}";
            }
        }

        private string GetPresetPositionDisplayName(string presetName)
        {
            return presetName switch
            {
                "TopLeft" => "左上角",
                "TopCenter" => "顶部中央",
                "TopRight" => "右上角",
                "MiddleLeft" => "左侧中央",
                "Center" => "屏幕中央",
                "MiddleRight" => "右侧中央",
                "BottomLeft" => "左下角",
                "BottomCenter" => "底部中央",
                "BottomRight" => "右下角",
                _ => presetName
            };
        }

        private void SelectMemo(MemoModel? memo)
        {
            if (memo != null)
            {
                CurrentMemo = memo;
            }
        }

        private void FilterMemos()
        {
            FilteredMemos.Clear();

            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? Memos
                : Memos.Where(m =>
                    m.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    m.Content.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            foreach (var memo in filtered)
            {
                FilteredMemos.Add(memo);
            }
        }
    }
}