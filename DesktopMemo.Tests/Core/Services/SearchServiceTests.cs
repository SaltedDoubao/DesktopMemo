using Xunit;
using Moq;
using DesktopMemo.Core.Services;
using DesktopMemo.Core.Interfaces;

namespace DesktopMemo.Tests.Core.Services
{
    public class SearchServiceTests
    {
        private readonly Mock<IMemoService> _mockMemoService;
        private readonly SearchService _searchService;

        public SearchServiceTests()
        {
            _mockMemoService = new Mock<IMemoService>();
            _searchService = new SearchService(_mockMemoService.Object);
        }

        [Fact]
        public async Task SearchAsync_WithEmptyQuery_ShouldReturnEmptyResult()
        {
            // Act
            var result = await _searchService.SearchAsync("");

            // Assert
            Assert.Empty(result.Matches);
            Assert.Equal(0, result.TotalMatches);
        }

        [Fact]
        public async Task SearchAsync_WithValidQuery_ShouldFindMatches()
        {
            // Arrange
            var memos = new List<Core.Models.MemoModel>
            {
                new Core.Models.MemoModel
                {
                    Id = "1",
                    Title = "Test Title",
                    Content = "This is test content"
                },
                new Core.Models.MemoModel
                {
                    Id = "2",
                    Title = "Another Title",
                    Content = "Different content here"
                }
            };

            _mockMemoService.Setup(x => x.LoadMemosAsync()).ReturnsAsync(memos);

            // Act
            var result = await _searchService.SearchAsync("test");

            // Assert
            Assert.NotEmpty(result.Matches);
            Assert.True(result.TotalMatches > 0);
            Assert.Equal("test", result.Query);
        }

        [Fact]
        public async Task SearchInContentAsync_ShouldFindMultipleMatches()
        {
            // Arrange
            var content = "This test content has test word multiple times for test purposes";
            var query = "test";

            // Act
            var matches = await _searchService.SearchInContentAsync(content, query);

            // Assert
            Assert.Equal(3, matches.Count);
            Assert.All(matches, match => Assert.Equal("test", match.MatchText));
        }

        [Fact]
        public async Task SearchInContentAsync_CaseSensitive_ShouldRespectCase()
        {
            // Arrange
            var content = "Test content with test and TEST words";
            var query = "test";
            var options = new SearchOptions { CaseSensitive = true };

            // Act
            var matches = await _searchService.SearchInContentAsync(content, query, options);

            // Assert
            Assert.Single(matches);
            Assert.Equal("test", matches[0].MatchText);
        }

        [Fact]
        public async Task SearchInContentAsync_WholeWord_ShouldMatchWholeWordsOnly()
        {
            // Arrange
            var content = "testing test contest test";
            var query = "test";
            var options = new SearchOptions { WholeWord = true };

            // Act
            var matches = await _searchService.SearchInContentAsync(content, query, options);

            // Assert
            Assert.Equal(2, matches.Count);
            Assert.All(matches, match => Assert.Equal("test", match.MatchText));
        }

        [Fact]
        public void ClearSearchState_ShouldResetCurrentState()
        {
            // Arrange
            _searchService.CurrentSearchState.CurrentQuery = "test";
            _searchService.CurrentSearchState.TotalMatches = 5;

            // Act
            _searchService.ClearSearchState();

            // Assert
            Assert.Equal("", _searchService.CurrentSearchState.CurrentQuery);
            Assert.Equal(0, _searchService.CurrentSearchState.TotalMatches);
            Assert.Equal(-1, _searchService.CurrentSearchState.CurrentMatchIndex);
        }

        [Fact]
        public async Task SearchAsync_ShouldRaiseSearchStateChangedEvent()
        {
            // Arrange
            var memos = new List<Core.Models.MemoModel>();
            _mockMemoService.Setup(x => x.LoadMemosAsync()).ReturnsAsync(memos);

            SearchStateChangedEventArgs? eventArgs = null;
            _searchService.SearchStateChanged += (sender, args) => eventArgs = args;

            // Act
            await _searchService.SearchAsync("test");

            // Assert
            Assert.NotNull(eventArgs);
            Assert.False(eventArgs.State.IsSearching);
        }
    }
}