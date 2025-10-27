namespace DesktopMemo.Core.Models;

/// <summary>
/// 数据同步状态枚举。
/// </summary>
public enum SyncStatus
{
    /// <summary>
    /// 已同步到云端。
    /// </summary>
    Synced = 0,

    /// <summary>
    /// 等待同步到云端。
    /// </summary>
    PendingSync = 1,

    /// <summary>
    /// 存在同步冲突。
    /// </summary>
    Conflict = 2
}

