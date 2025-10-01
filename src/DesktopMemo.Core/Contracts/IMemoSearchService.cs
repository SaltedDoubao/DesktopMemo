using System.Collections.Generic;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Contracts;

public interface IMemoSearchService
{
    IEnumerable<SearchMatch> FindMatches(string text, string keyword, bool caseSensitive = false, bool useRegex = false);

    string Replace(string text, string keyword, string replacement, bool caseSensitive = false, bool useRegex = false);
}

public sealed record SearchMatch(int Index, int Length);
