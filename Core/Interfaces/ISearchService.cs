using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopMemo.Core.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(string query, SearchOptions? options = null);
        Task<List<SearchMatch>> SearchInContentAsync(string content, string query, SearchOptions? options = null);
        void ClearSearchState();
        SearchState CurrentSearchState { get; }
        event EventHandler<SearchStateChangedEventArgs>? SearchStateChanged;
    }

    public class SearchResult
    {
        public string Query { get; set; } = string.Empty;
        public List<SearchMatch> Matches { get; set; } = new List<SearchMatch>();
        public int TotalMatches { get; set; }
        public int CurrentIndex { get; set; }
    }

    public class SearchMatch
    {
        public string MemoId { get; set; } = string.Empty;
        public string MemoTitle { get; set; } = string.Empty;
        public int LineNumber { get; set; }
        public int StartIndex { get; set; }
        public int Length { get; set; }
        public string MatchText { get; set; } = string.Empty;
        public string Context { get; set; } = string.Empty;
    }

    public class SearchOptions
    {
        public bool CaseSensitive { get; set; } = false;
        public bool WholeWord { get; set; } = false;
        public bool UseRegex { get; set; } = false;
        public bool SearchInTitle { get; set; } = true;
        public bool SearchInContent { get; set; } = true;
    }

    public class SearchState
    {
        public string CurrentQuery { get; set; } = string.Empty;
        public int CurrentMatchIndex { get; set; } = -1;
        public int TotalMatches { get; set; } = 0;
        public bool IsSearching { get; set; } = false;
    }

    public class SearchStateChangedEventArgs : EventArgs
    {
        public SearchState State { get; }
        public SearchStateChangedEventArgs(SearchState state)
        {
            State = state;
        }
    }
}