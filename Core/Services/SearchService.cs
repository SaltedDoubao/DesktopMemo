using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DesktopMemo.Core.Interfaces;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Services
{
    public class SearchService : ISearchService
    {
        private readonly IMemoService _memoService;
        private SearchState _currentState;
        public event EventHandler<SearchStateChangedEventArgs>? SearchStateChanged;

        public SearchState CurrentSearchState => _currentState;

        public SearchService(IMemoService memoService)
        {
            _memoService = memoService ?? throw new ArgumentNullException(nameof(memoService));
            _currentState = new SearchState();
        }

        public async Task<SearchResult> SearchAsync(string query, SearchOptions? options = null)
        {
            options ??= new SearchOptions();

            var result = new SearchResult
            {
                Query = query,
                Matches = new List<SearchMatch>()
            };

            if (string.IsNullOrWhiteSpace(query))
            {
                ClearSearchState();
                return result;
            }

            _currentState.IsSearching = true;
            _currentState.CurrentQuery = query;
            OnSearchStateChanged();

            try
            {
                var memos = await _memoService.LoadMemosAsync();

                foreach (var memo in memos)
                {
                    // Search in title
                    if (options.SearchInTitle)
                    {
                        var titleMatches = await SearchInTextAsync(memo.Title, query, options);
                        foreach (var match in titleMatches)
                        {
                            match.MemoId = memo.Id;
                            match.MemoTitle = memo.Title;
                            result.Matches.Add(match);
                        }
                    }

                    // Search in content
                    if (options.SearchInContent)
                    {
                        var contentMatches = await SearchInTextAsync(memo.Content, query, options);
                        foreach (var match in contentMatches)
                        {
                            match.MemoId = memo.Id;
                            match.MemoTitle = memo.Title;
                            result.Matches.Add(match);
                        }
                    }
                }

                result.TotalMatches = result.Matches.Count;
                _currentState.TotalMatches = result.TotalMatches;
                _currentState.CurrentMatchIndex = result.TotalMatches > 0 ? 0 : -1;
            }
            finally
            {
                _currentState.IsSearching = false;
                OnSearchStateChanged();
            }

            return result;
        }

        public async Task<List<SearchMatch>> SearchInContentAsync(string content, string query, SearchOptions? options = null)
        {
            return await SearchInTextAsync(content, query, options);
        }

        private async Task<List<SearchMatch>> SearchInTextAsync(string text, string query, SearchOptions? options = null)
        {
            return await Task.Run(() =>
            {
                var matches = new List<SearchMatch>();
                options ??= new SearchOptions();

                if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(query))
                    return matches;

                try
                {
                    if (options.UseRegex)
                    {
                        var regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                        var regex = new Regex(query, regexOptions);
                        var regexMatches = regex.Matches(text);

                        foreach (Match match in regexMatches)
                        {
                            matches.Add(CreateSearchMatch(text, match.Index, match.Length, match.Value));
                        }
                    }
                    else
                    {
                        var comparison = options.CaseSensitive ?
                            StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

                        var searchQuery = options.WholeWord ? $"\\b{Regex.Escape(query)}\\b" : Regex.Escape(query);

                        if (options.WholeWord)
                        {
                            var regexOptions = options.CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                            var regex = new Regex(searchQuery, regexOptions);
                            var regexMatches = regex.Matches(text);

                            foreach (Match match in regexMatches)
                            {
                                matches.Add(CreateSearchMatch(text, match.Index, match.Length, match.Value));
                            }
                        }
                        else
                        {
                            int index = 0;
                            while ((index = text.IndexOf(query, index, comparison)) != -1)
                            {
                                matches.Add(CreateSearchMatch(text, index, query.Length,
                                    text.Substring(index, query.Length)));
                                index += query.Length;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Search error: {ex.Message}");
                }

                return matches;
            });
        }

        private SearchMatch CreateSearchMatch(string text, int index, int length, string matchText)
        {
            // Calculate line number
            int lineNumber = 1;
            for (int i = 0; i < index && i < text.Length; i++)
            {
                if (text[i] == '\n')
                    lineNumber++;
            }

            // Get context (surrounding text)
            int contextStart = Math.Max(0, index - 30);
            int contextEnd = Math.Min(text.Length, index + length + 30);
            string context = text.Substring(contextStart, contextEnd - contextStart);

            if (contextStart > 0)
                context = "..." + context;
            if (contextEnd < text.Length)
                context = context + "...";

            return new SearchMatch
            {
                LineNumber = lineNumber,
                StartIndex = index,
                Length = length,
                MatchText = matchText,
                Context = context
            };
        }

        public void ClearSearchState()
        {
            _currentState = new SearchState();
            OnSearchStateChanged();
        }

        private void OnSearchStateChanged()
        {
            SearchStateChanged?.Invoke(this, new SearchStateChangedEventArgs(_currentState));
        }
    }
}