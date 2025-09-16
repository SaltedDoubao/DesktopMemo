using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Models;
using DesktopMemo.Infrastructure.Performance;

namespace DesktopMemo.UI.ViewModels
{
    public class OptimizedMainViewModel : ViewModelBase
    {
        private readonly IMemoService _memoService;
        private readonly IWindowManagementService _windowService;
        private readonly ISettingsService _settingsService;
        private readonly ILocalizationService _localizationService;
        private readonly IPerformanceService _performanceService;
        private readonly IBackgroundTaskService _backgroundTaskService;

        private MemoModel? _currentMemo;
        private bool _isEditMode;
        private string _searchText = string.Empty;
        private bool _isPenetrationMode;
        private string _statusText = "就绪";
        private bool _isLoading;

        public ObservableCollection<MemoModel> Memos { get; }
        public ObservableCollection<MemoModel> FilteredMemos { get; }

        // Window properties with lazy loading
        public string WindowTitle => _localizationService?.GetString("window.title") ?? "备忘录";
        public string PlaceholderText => _localizationService?.GetString("memo.welcome.content") ?? "点击此处开始编辑...";
        public string MemoCountText => $"{(FilteredMemos?.Count ?? 0)} / {(Memos?.Count ?? 0)}";

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

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
                    OnPropertyChanged(nameof(MemoCountText));
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

                    // Debounced auto-save
                    _performanceService.DebounceAction("save_title",
                        () => SaveCurrentMemoAsync(), 1000);
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

                    // Debounced auto-save
                    _performanceService.DebounceAction("save_content",
                        () => SaveCurrentMemoAsync(), 500);
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
                    // Debounced search
                    _performanceService.DebounceAction("search",
                        () => FilterMemos(), 300);
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
        public ICommand AddMemoCommand { get; private set; } = null!;
        public ICommand DeleteMemoCommand { get; private set; } = null!;
        public ICommand SaveMemoCommand { get; private set; } = null!;
        public ICommand SwitchToNextMemoCommand { get; private set; } = null!;
        public ICommand SwitchToPreviousMemoCommand { get; private set; } = null!;
        public ICommand ToggleEditModeCommand { get; private set; } = null!;
        public ICommand TogglePinCommand { get; private set; } = null!;
        public ICommand SetTopmostModeCommand { get; private set; } = null!;
        public ICommand SetPresetPositionCommand { get; private set; } = null!;
        public ICommand TogglePenetrationModeCommand { get; private set; } = null!;
        public ICommand SelectMemoCommand { get; private set; } = null!;

        public OptimizedMainViewModel(
            IMemoService memoService,
            IWindowManagementService windowService,
            ISettingsService settingsService,
            ILocalizationService localizationService,
            IPerformanceService performanceService,
            IBackgroundTaskService backgroundTaskService)
        {
            _memoService = memoService ?? throw new ArgumentNullException(nameof(memoService));
            _windowService = windowService ?? throw new ArgumentNullException(nameof(windowService));
            _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
            _localizationService = localizationService ?? throw new ArgumentNullException(nameof(localizationService));
            _performanceService = performanceService ?? throw new ArgumentNullException(nameof(performanceService));
            _backgroundTaskService = backgroundTaskService ?? throw new ArgumentNullException(nameof(backgroundTaskService));

            Memos = new ObservableCollection<MemoModel>();
            FilteredMemos = new ObservableCollection<MemoModel>();

            // Initialize Commands
            InitializeCommands();

            // Load initial data with performance optimization
            _ = LoadMemosAsync();
        }

        private void InitializeCommands()
        {
            AddMemoCommand = new RelayCommand(() => AddMemoAsync());
            DeleteMemoCommand = new RelayCommand(
                () => DeleteCurrentMemoAsync(),
                () => HasCurrentMemo);
            SaveMemoCommand = new RelayCommand(
                () => SaveCurrentMemoAsync(),
                () => HasCurrentMemo);
            SwitchToNextMemoCommand = new RelayCommand(SwitchToNextMemo, () => Memos.Count > 1);
            SwitchToPreviousMemoCommand = new RelayCommand(SwitchToPreviousMemo, () => Memos.Count > 1);
            ToggleEditModeCommand = new RelayCommand(() => IsEditMode = !IsEditMode);
            TogglePinCommand = new RelayCommand(TogglePin, () => HasCurrentMemo);
            SetTopmostModeCommand = new RelayCommand<string>(SetTopmostMode);
            SetPresetPositionCommand = new RelayCommand<string>(SetPresetPosition);
            TogglePenetrationModeCommand = new RelayCommand(() => IsPenetrationMode = !IsPenetrationMode);
            SelectMemoCommand = new RelayCommand<MemoModel>(SelectMemo);
        }

        private async Task LoadMemosAsync()
        {
            try
            {
                IsLoading = true;
                StatusText = "加载备忘录...";

                // Use cache for memo loading
                var memos = await _performanceService.CacheResult(
                    "all_memos",
                    () => _memoService.LoadMemosAsync(),
                    TimeSpan.FromMinutes(5));

                Memos.Clear();
                foreach (var memo in memos)
                {
                    Memos.Add(memo);
                }

                if (Memos.Count == 0)
                {
                    await AddDefaultMemoAsync();
                }
                else
                {
                    CurrentMemo = Memos.FirstOrDefault();
                }

                FilterMemos();
                StatusText = _localizationService.GetString("status.memo_loaded");
            }
            catch (Exception ex)
            {
                StatusText = $"加载失败: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task AddDefaultMemoAsync()
        {
            var defaultMemo = await _memoService.CreateMemoAsync(
                _localizationService.GetString("memo.welcome.title"),
                _localizationService.GetString("memo.welcome.content"));

            Memos.Add(defaultMemo);
            CurrentMemo = defaultMemo;
        }

        private void AddMemoAsync()
        {
            // Use background task for non-blocking operation
            _backgroundTaskService.QueueTask(
                async () => await _memoService.CreateMemoAsync(),
                newMemo =>
                {
                    Memos.Insert(0, newMemo);
                    CurrentMemo = newMemo;
                    IsEditMode = true;
                    FilterMemos();
                    StatusText = _localizationService.GetString("status.memo_created");

                    // Clear cache since we added new memo
                    _performanceService.ClearCache();
                },
                TaskPriority.High);
        }

        private void DeleteCurrentMemoAsync()
        {
            if (CurrentMemo == null) return;

            var memoToDelete = CurrentMemo;
            var index = Memos.IndexOf(memoToDelete);

            if (_settingsService.GetSetting("ShowDeletePrompt", true))
            {
                // TODO: Show confirmation dialog
            }

            // Use background task for deletion
            _backgroundTaskService.QueueTask(
                async () =>
                {
                    await _memoService.DeleteMemoAsync(memoToDelete.Id);
                    return memoToDelete.Id;
                },
                deletedId =>
                {
                    Memos.Remove(memoToDelete);

                    if (Memos.Count > 0)
                    {
                        CurrentMemo = Memos[Math.Min(index, Memos.Count - 1)];
                    }
                    else
                    {
                        _ = AddDefaultMemoAsync();
                    }

                    FilterMemos();
                    StatusText = _localizationService.GetString("status.deleted");

                    // Clear cache since we deleted memo
                    _performanceService.ClearCache();
                });
        }

        private void SaveCurrentMemoAsync()
        {
            if (CurrentMemo == null) return;

            _backgroundTaskService.QueueTask<bool>(
                async () => { await _memoService.SaveMemoAsync(CurrentMemo); return true; },
                _ => StatusText = _localizationService.GetString("status.saved"));
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
            SaveCurrentMemoAsync();

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
                    TopmostMode.Normal => _localizationService.GetString("status.mode_normal"),
                    TopmostMode.Desktop => _localizationService.GetString("status.mode_desktop"),
                    TopmostMode.Always => _localizationService.GetString("status.mode_always"),
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
                "TopLeft" => _localizationService.GetString("position.top_left"),
                "TopCenter" => _localizationService.GetString("position.top_center"),
                "TopRight" => _localizationService.GetString("position.top_right"),
                "MiddleLeft" => _localizationService.GetString("position.middle_left"),
                "Center" => _localizationService.GetString("position.center"),
                "MiddleRight" => _localizationService.GetString("position.middle_right"),
                "BottomLeft" => _localizationService.GetString("position.bottom_left"),
                "BottomCenter" => _localizationService.GetString("position.bottom_center"),
                "BottomRight" => _localizationService.GetString("position.bottom_right"),
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

            OnPropertyChanged(nameof(MemoCountText));
        }
    }
}