using System.Collections.Generic;
using System.Text.RegularExpressions;
using DesktopMemo.Core.Contracts;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Infrastructure.Services;

public sealed class MemoSearchService : IMemoSearchService
{
    public IEnumerable<SearchMatch> FindMatches(string text, string keyword, bool caseSensitive = false, bool useRegex = false)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
        {
            yield break;
        }

        if (useRegex)
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            foreach (Match match in Regex.Matches(text, keyword, options))
            {
                if (match.Success)
                {
                    yield return new SearchMatch(match.Index, match.Length);
                }
            }
        }
        else
        {
            var comparison = caseSensitive ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase;
            int index = 0;
            while ((index = text.IndexOf(keyword, index, comparison)) >= 0)
            {
                yield return new SearchMatch(index, keyword.Length);
                index += keyword.Length;
            }
        }
    }

    public string Replace(string text, string keyword, string replacement, bool caseSensitive = false, bool useRegex = false)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
        {
            return text;
        }

        if (useRegex)
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
            return Regex.Replace(text, keyword, replacement, options);
        }

        var comparison = caseSensitive ? System.StringComparison.Ordinal : System.StringComparison.OrdinalIgnoreCase;
        return ReplacePlain(text, keyword, replacement, comparison);
    }

    private static string ReplacePlain(string text, string oldValue, string newValue, System.StringComparison comparison)
    {
        int index = text.IndexOf(oldValue, comparison);
        if (index < 0)
        {
            return text;
        }

        var result = new System.Text.StringBuilder(text.Length);
        int previousIndex = 0;

        while (index >= 0)
        {
            result.Append(text, previousIndex, index - previousIndex);
            result.Append(newValue);
            previousIndex = index + oldValue.Length;
            index = text.IndexOf(oldValue, previousIndex, comparison);
        }

        result.Append(text, previousIndex, text.Length - previousIndex);
        return result.ToString();
    }
}
