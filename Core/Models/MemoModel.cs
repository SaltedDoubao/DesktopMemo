using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DesktopMemo.Core.Models
{
    public class MemoModel : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _title = string.Empty;
        private string _content = string.Empty;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private bool _isPinned;
        private string _tags = string.Empty;
        private string _category = string.Empty;
        private int _priority;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        public bool IsPinned
        {
            get => _isPinned;
            set => SetProperty(ref _isPinned, value);
        }

        public string Tags
        {
            get => _tags;
            set => SetProperty(ref _tags, value);
        }

        public string Category
        {
            get => _category;
            set => SetProperty(ref _category, value);
        }

        public int Priority
        {
            get => _priority;
            set => SetProperty(ref _priority, value);
        }

        // Computed properties
        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "无标题" : Title;
        public string FormattedDate => UpdatedAt.ToString("yyyy/MM/dd HH:mm");
        public bool HasTags => !string.IsNullOrWhiteSpace(Tags);
        public string[] TagList => Tags.Split(',', StringSplitOptions.RemoveEmptyEntries);

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);

            // Update timestamp when content changes
            if (propertyName != nameof(UpdatedAt) && propertyName != nameof(CreatedAt))
            {
                UpdatedAt = DateTime.Now;
            }

            return true;
        }

        // Factory method for creating new memo
        public static MemoModel CreateNew(string title = "", string content = "")
        {
            return new MemoModel
            {
                Id = Guid.NewGuid().ToString(),
                Title = title,
                Content = content,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsPinned = false,
                Priority = 0
            };
        }

        public MemoModel Clone()
        {
            return new MemoModel
            {
                Id = this.Id,
                Title = this.Title,
                Content = this.Content,
                CreatedAt = this.CreatedAt,
                UpdatedAt = this.UpdatedAt,
                IsPinned = this.IsPinned,
                Tags = this.Tags,
                Category = this.Category,
                Priority = this.Priority
            };
        }

        public override string ToString()
        {
            return $"{DisplayTitle} - {FormattedDate}";
        }
    }
}