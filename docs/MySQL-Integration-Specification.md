# MySQL Cloud Storage Integration Specification

> **Status**: Planned Feature (Not Yet Implemented)  
> **Version**: 1.0  
> **Last Updated**: 2025-01-27

## 1. Overview

This document defines the technical specifications and best practices for integrating MySQL cloud storage capabilities into DesktopMemo. The MySQL integration will serve as an optional cloud sync feature alongside the existing local SQLite storage.

### 1.1 Design Principles

- **Local-First**: Local SQLite remains the primary data source
- **Optional Sync**: MySQL cloud sync is an opt-in feature
- **Conflict Resolution**: Clear conflict resolution strategy
- **Privacy**: User data encryption and privacy protection
- **Performance**: Minimal impact on local operations

### 1.2 Architecture Overview

```
┌─────────────────────────────────────────────┐
│           Application Layer                 │
│  (ViewModels, Commands, UI Logic)           │
└─────────────────┬───────────────────────────┘
                  │
┌─────────────────▼───────────────────────────┐
│         Repository Layer                    │
│  ┌──────────────┐      ┌─────────────────┐ │
│  │   Local      │      │   Cloud Sync    │ │
│  │   SQLite     │◄────►│   Manager       │ │
│  └──────────────┘      └────────┬────────┘ │
└─────────────────────────────────┼──────────┘
                                  │
                    ┌─────────────▼──────────────┐
                    │   MySQL Cloud Storage      │
                    │   (Remote Database)        │
                    └────────────────────────────┘
```

## 2. Data Model Design

### 2.1 Database Schema

#### Memos Table
```sql
CREATE TABLE memos (
    id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    title VARCHAR(255) NOT NULL,
    content LONGTEXT,
    created_at DATETIME(3) NOT NULL,
    updated_at DATETIME(3) NOT NULL,
    is_pinned TINYINT(1) DEFAULT 0,
    is_deleted TINYINT(1) DEFAULT 0,
    deleted_at DATETIME(3) NULL,
    sync_status ENUM('synced', 'modified', 'conflict') DEFAULT 'synced',
    local_version INT DEFAULT 1,
    server_version INT DEFAULT 1,
    INDEX idx_user_updated (user_id, updated_at),
    INDEX idx_user_deleted (user_id, is_deleted)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### Todos Table
```sql
CREATE TABLE todos (
    id VARCHAR(36) PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    content VARCHAR(1000) NOT NULL,
    is_completed TINYINT(1) DEFAULT 0,
    created_at DATETIME(3) NOT NULL,
    updated_at DATETIME(3) NOT NULL,
    completed_at DATETIME(3) NULL,
    is_deleted TINYINT(1) DEFAULT 0,
    deleted_at DATETIME(3) NULL,
    sync_status ENUM('synced', 'modified', 'conflict') DEFAULT 'synced',
    local_version INT DEFAULT 1,
    server_version INT DEFAULT 1,
    INDEX idx_user_updated (user_id, updated_at),
    INDEX idx_user_completed (user_id, is_completed)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
```

#### Sync Log Table
```sql
CREATE TABLE sync_logs (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    user_id VARCHAR(36) NOT NULL,
    entity_type ENUM('memo', 'todo') NOT NULL,
    entity_id VARCHAR(36) NOT NULL,
    action ENUM('create', 'update', 'delete') NOT NULL,
    sync_direction ENUM('upload', 'download') NOT NULL,
    status ENUM('success', 'failed', 'conflict') NOT NULL,
    error_message TEXT NULL,
    synced_at DATETIME(3) NOT NULL,
    INDEX idx_user_synced (user_id, synced_at),
    INDEX idx_entity (entity_type, entity_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
```

### 2.2 Local SQLite Schema Extension

Add sync-related fields to existing tables:

```sql
-- Add to memos table
ALTER TABLE memos ADD COLUMN sync_status TEXT DEFAULT 'local_only';
ALTER TABLE memos ADD COLUMN local_version INTEGER DEFAULT 1;
ALTER TABLE memos ADD COLUMN server_version INTEGER DEFAULT 0;
ALTER TABLE memos ADD COLUMN last_synced_at TEXT NULL;

-- Add to todos table
ALTER TABLE todos ADD COLUMN sync_status TEXT DEFAULT 'local_only';
ALTER TABLE todos ADD COLUMN local_version INTEGER DEFAULT 1;
ALTER TABLE todos ADD COLUMN server_version INTEGER DEFAULT 0;
ALTER TABLE todos ADD COLUMN last_synced_at TEXT NULL;
```

## 3. Sync Strategy

### 3.1 Sync Modes

1. **Manual Sync**: User triggers sync via UI button
2. **Auto Sync**: Periodic background sync (configurable interval)
3. **On Startup Sync**: Sync when application starts

### 3.2 Conflict Resolution

**Last-Write-Wins (LWW) Strategy**:
- Compare `updated_at` timestamps
- Keep the version with the latest timestamp
- Store conflict version in local history

**User-Prompted Resolution**:
- For significant conflicts, prompt user to choose:
  - Keep local version
  - Use server version
  - Merge manually

### 3.3 Sync Process Flow

```
1. Local Changes Detection
   ├─ Query items where sync_status != 'synced'
   └─ Collect changed items

2. Pull Remote Changes
   ├─ Request updates since last_synced_at
   ├─ Detect conflicts (local_version != server_version)
   └─ Apply changes to local database

3. Resolve Conflicts
   ├─ Apply resolution strategy
   └─ Mark conflict items

4. Push Local Changes
   ├─ Upload new/modified items
   ├─ Update server_version
   └─ Mark as 'synced'

5. Update Sync Status
   ├─ Update last_synced_at
   └─ Log sync results
```

## 4. API Design

### 4.1 RESTful API Endpoints

#### Authentication
```
POST   /api/auth/login
POST   /api/auth/register
POST   /api/auth/refresh
POST   /api/auth/logout
```

#### Memos
```
GET    /api/memos?since={timestamp}&limit={n}
GET    /api/memos/{id}
POST   /api/memos
PUT    /api/memos/{id}
DELETE /api/memos/{id}
POST   /api/memos/batch-sync
```

#### Todos
```
GET    /api/todos?since={timestamp}&limit={n}
GET    /api/todos/{id}
POST   /api/todos
PUT    /api/todos/{id}
DELETE /api/todos/{id}
POST   /api/todos/batch-sync
```

#### Sync
```
GET    /api/sync/status
POST   /api/sync/pull
POST   /api/sync/push
POST   /api/sync/full
```

### 4.2 Request/Response Format

**Batch Sync Request**:
```json
{
  "user_id": "uuid",
  "last_synced_at": "2025-01-27T12:00:00.000Z",
  "changes": [
    {
      "id": "memo-uuid",
      "action": "update",
      "data": { /* memo object */ },
      "local_version": 5
    }
  ]
}
```

**Batch Sync Response**:
```json
{
  "success": true,
  "server_changes": [
    {
      "id": "memo-uuid",
      "action": "update",
      "data": { /* memo object */ },
      "server_version": 6,
      "is_conflict": false
    }
  ],
  "conflicts": [
    {
      "id": "memo-uuid",
      "local_data": { /* local version */ },
      "server_data": { /* server version */ },
      "resolution_required": true
    }
  ],
  "sync_timestamp": "2025-01-27T12:05:00.000Z"
}
```

## 5. Implementation Guidelines

### 5.1 Repository Layer

Create new interfaces and implementations:

```csharp
// Interface
public interface ICloudSyncService
{
    Task<SyncResult> PullChangesAsync(DateTime? since = null);
    Task<SyncResult> PushChangesAsync();
    Task<SyncResult> FullSyncAsync();
    Task<bool> IsConnectedAsync();
    Task<SyncStatus> GetSyncStatusAsync();
}

// Configuration
public class CloudSyncSettings
{
    public bool IsEnabled { get; set; }
    public string ServerUrl { get; set; }
    public string UserId { get; set; }
    public TimeSpan AutoSyncInterval { get; set; }
    public bool SyncOnStartup { get; set; }
    public ConflictResolutionStrategy ConflictStrategy { get; set; }
}
```

### 5.2 Data Transfer Objects

```csharp
public class SyncResult
{
    public bool Success { get; set; }
    public int ItemsSynced { get; set; }
    public int Conflicts { get; set; }
    public List<ConflictItem> ConflictItems { get; set; }
    public string ErrorMessage { get; set; }
    public DateTime SyncTimestamp { get; set; }
}

public class ConflictItem
{
    public string Id { get; set; }
    public string Type { get; set; } // "memo" or "todo"
    public object LocalVersion { get; set; }
    public object ServerVersion { get; set; }
    public DateTime LocalUpdatedAt { get; set; }
    public DateTime ServerUpdatedAt { get; set; }
}
```

### 5.3 Security Considerations

1. **Authentication**:
   - Use JWT tokens for API authentication
   - Implement token refresh mechanism
   - Secure token storage in local settings

2. **Data Encryption**:
   - HTTPS/TLS for all network communication
   - Optional end-to-end encryption for sensitive content
   - Encrypt stored credentials

3. **Privacy**:
   - User consent for cloud sync
   - Clear data retention policy
   - Option to delete all cloud data

### 5.4 Error Handling

```csharp
public enum SyncErrorType
{
    NetworkError,
    AuthenticationError,
    ServerError,
    ConflictError,
    LocalDatabaseError,
    UnknownError
}

public class SyncException : Exception
{
    public SyncErrorType ErrorType { get; }
    public string UserMessage { get; }
    public bool IsRetryable { get; }
}
```

## 6. UI/UX Guidelines

### 6.1 Settings Panel

Add a new "Cloud Sync" section:
- Enable/Disable cloud sync toggle
- Server connection status indicator
- Last sync timestamp display
- Manual sync button
- Conflict resolution preferences

### 6.2 Sync Status Indicators

- Icon in system tray showing sync status:
  - ✅ Synced
  - 🔄 Syncing
  - ⚠️ Conflicts
  - ❌ Error

### 6.3 Conflict Resolution UI

When conflicts occur:
1. Show notification
2. Open conflict resolution dialog
3. Display both versions side-by-side
4. Allow user to choose or merge

## 7. Testing Requirements

### 7.1 Unit Tests

- Repository layer methods
- Conflict resolution logic
- Data serialization/deserialization
- Sync status management

### 7.2 Integration Tests

- End-to-end sync flow
- Network failure scenarios
- Concurrent modification handling
- Large dataset sync performance

### 7.3 Test Scenarios

1. First-time sync with existing local data
2. Multiple devices syncing same account
3. Offline changes sync when back online
4. Network interruption during sync
5. Server downtime recovery

## 8. Performance Optimization

### 8.1 Strategies

- **Incremental Sync**: Only sync changed items
- **Batch Operations**: Use batch APIs for multiple items
- **Connection Pooling**: Reuse HTTP connections
- **Compression**: Compress large content before transfer
- **Background Sync**: Don't block UI during sync

### 8.2 Metrics to Monitor

- Sync duration
- Network bandwidth usage
- Database query performance
- Conflict rate
- Sync success rate

## 9. Migration Path

### Phase 1: Preparation (Current)
- ✅ Implement local SQLite storage
- ✅ Add sync status fields to data models
- 📝 Define cloud sync specifications

### Phase 2: Backend Development
- Develop MySQL schema
- Implement REST API
- Set up authentication system
- Deploy backend infrastructure

### Phase 3: Client Integration
- Implement `ICloudSyncService`
- Add sync UI components
- Implement conflict resolution
- Add comprehensive error handling

### Phase 4: Testing & Rollout
- Internal testing
- Beta testing with volunteers
- Gradual rollout to users
- Monitor and iterate

## 10. Future Enhancements

- **Selective Sync**: Choose which items to sync
- **Shared Memos**: Collaborate with other users
- **Version History**: View and restore previous versions
- **Attachment Sync**: Sync images and files
- **Real-time Sync**: WebSocket-based instant sync

## 11. References

- [MySQL 8.0 Documentation](https://dev.mysql.com/doc/)
- [ASP.NET Core Web API](https://docs.microsoft.com/aspnet/core/web-api/)
- [JWT Authentication](https://jwt.io/)
- [RESTful API Design Best Practices](https://restfulapi.net/)

---

**Document Version**: 1.0  
**Last Updated**: 2025-01-27  
**Next Review**: Before implementation begins

