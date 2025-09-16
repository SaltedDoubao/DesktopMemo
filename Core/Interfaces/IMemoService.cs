using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopMemo.Core.Models;

namespace DesktopMemo.Core.Interfaces
{
    public interface IMemoService
    {
        Task<List<MemoModel>> LoadMemosAsync();
        Task SaveMemoAsync(MemoModel memo);
        Task DeleteMemoAsync(string memoId);
        Task<MemoModel> CreateMemoAsync(string title = "", string content = "");
        Task SaveAllMemosAsync(List<MemoModel> memos);
        event EventHandler<MemoChangedEventArgs>? MemoChanged;
    }

    public class MemoChangedEventArgs : EventArgs
    {
        public MemoModel Memo { get; }
        public MemoChangeType ChangeType { get; }

        public MemoChangedEventArgs(MemoModel memo, MemoChangeType changeType)
        {
            Memo = memo;
            ChangeType = changeType;
        }
    }

    public enum MemoChangeType
    {
        Added,
        Updated,
        Deleted
    }
}