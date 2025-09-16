using Xunit;
using Moq;
using DesktopMemo.Core.Services;
using DesktopMemo.Core.Models;
using DesktopMemo.Core.Interfaces;

namespace DesktopMemo.Tests.Core.Services
{
    public class MemoServiceTests
    {
        private readonly MemoService _memoService;

        public MemoServiceTests()
        {
            _memoService = new MemoService();
        }

        [Fact]
        public async Task CreateMemoAsync_ShouldCreateNewMemo_WithUniqueId()
        {
            // Arrange
            var title = "Test Title";
            var content = "Test Content";

            // Act
            var memo = await _memoService.CreateMemoAsync(title, content);

            // Assert
            Assert.NotNull(memo);
            Assert.False(string.IsNullOrEmpty(memo.Id));
            Assert.Equal(title, memo.Title);
            Assert.Equal(content, memo.Content);
            Assert.True(memo.CreatedAt > DateTime.MinValue);
            Assert.True(memo.UpdatedAt > DateTime.MinValue);
        }

        [Fact]
        public async Task CreateMemoAsync_WithEmptyTitle_ShouldUseDefaultTitle()
        {
            // Act
            var memo = await _memoService.CreateMemoAsync();

            // Assert
            Assert.Equal("新建备忘录", memo.Title);
        }

        [Fact]
        public async Task SaveMemoAsync_ShouldUpdateTimestamp()
        {
            // Arrange
            var memo = await _memoService.CreateMemoAsync("Test", "Content");
            var originalUpdateTime = memo.UpdatedAt;

            // Wait a bit to ensure timestamp difference
            await Task.Delay(10);

            // Modify content
            memo.Content = "Updated Content";

            // Act
            await _memoService.SaveMemoAsync(memo);

            // Assert
            Assert.True(memo.UpdatedAt > originalUpdateTime);
        }

        [Fact]
        public async Task LoadMemosAsync_ShouldReturnSortedMemos()
        {
            // Arrange - Create multiple memos
            var memo1 = await _memoService.CreateMemoAsync("First", "Content1");
            await Task.Delay(10);
            var memo2 = await _memoService.CreateMemoAsync("Second", "Content2");

            // Act
            var memos = await _memoService.LoadMemosAsync();

            // Assert
            Assert.NotEmpty(memos);
            // Should be sorted by UpdatedAt descending
            var sortedMemos = memos.OrderByDescending(m => m.UpdatedAt).ToList();
            Assert.Equal(sortedMemos.First().Id, memos.First().Id);
        }

        [Fact]
        public async Task DeleteMemoAsync_ShouldRemoveMemoFile()
        {
            // Arrange
            var memo = await _memoService.CreateMemoAsync("To Delete", "Content");
            await _memoService.SaveMemoAsync(memo);

            // Verify memo exists
            var memosBeforeDelete = await _memoService.LoadMemosAsync();
            Assert.Contains(memosBeforeDelete, m => m.Id == memo.Id);

            // Act
            await _memoService.DeleteMemoAsync(memo.Id);

            // Assert
            var memosAfterDelete = await _memoService.LoadMemosAsync();
            Assert.DoesNotContain(memosAfterDelete, m => m.Id == memo.Id);
        }

        [Fact]
        public async Task MemoChanged_EventShouldBeRaised_OnCreate()
        {
            // Arrange
            MemoChangedEventArgs? eventArgs = null;
            _memoService.MemoChanged += (sender, args) => eventArgs = args;

            // Act
            var memo = await _memoService.CreateMemoAsync("Test", "Content");

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(memo.Id, eventArgs.Memo.Id);
            Assert.Equal(MemoChangeType.Added, eventArgs.ChangeType);
        }

        [Fact]
        public async Task MemoChanged_EventShouldBeRaised_OnDelete()
        {
            // Arrange
            var memo = await _memoService.CreateMemoAsync("Test", "Content");
            MemoChangedEventArgs? eventArgs = null;
            _memoService.MemoChanged += (sender, args) => eventArgs = args;

            // Act
            await _memoService.DeleteMemoAsync(memo.Id);

            // Assert
            Assert.NotNull(eventArgs);
            Assert.Equal(memo.Id, eventArgs.Memo.Id);
            Assert.Equal(MemoChangeType.Deleted, eventArgs.ChangeType);
        }
    }
}